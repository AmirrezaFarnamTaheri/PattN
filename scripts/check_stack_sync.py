#!/usr/bin/env python3
"""Split-stack integrity gate.

The repository reviews the Discovery/Reviver workstream as a set of small "split"
pull requests whose payload files are asserted to be *identical* to the integration
branch, while the integration branch keeps advancing. When a split branch stops
containing its own base tip, merging it silently reverts hardening that already
landed on the base. This tool makes that condition fail loudly.

Checks, per open pull request:

1. base-ancestry -- the PR base tip must be an ancestor of the PR head
   (`git merge-base --is-ancestor`). A PR that is behind its base is stale and its
   review is no longer about the code that would merge.
2. blob-parity -- for the subset of the PR payload owned by a split stack
   (see --parity-prefix), each payload blob must equal the blob on the
   integration ref, unless the file is listed in --allow-drift.
3. shared-slice parity -- `--slice-prefix` (default `v2rayN/ServiceLib/Reviver/`)
   is the slice every split PR shares with the integration ref. Two violations:

     * a split PR rewrote a shared file ("blob X != Y") -- a sync merge that
       resolved a shared file to an older side, or an unsynced edit;
     * the aggregation base (`--slice-base-ref`, default
       `split/final-integration-base`) is missing part of the slice or carries a
       different blob, because its whole purpose is to carry all of it.

   A split PR legitimately owning only part of the slice is *reported*, not
   failed: splits are subsets by construction. Payload parity cannot see either
   violation: the shared file is not part of the PR's own payload, so its diff
   looks clean.

   This is not hypothetical. `dff02073` ("carry complete Reviver intelligence
   slice into final base") merged its second parent but kept the pre-merge blob
   for three model files, dropping `StrategyId`, `RequiredRuntimeSuccesses`,
   `OriginalProfileFingerprint`, `IntegritySuspect` and
   `MeetsQuorumWithoutIntegrityDoubt` while keeping the services that call them.
   The base -- and both split PRs stacked on it -- stopped compiling, and the
   F-05 hardening that refuses evidence from a core that never served it lost its
   model members. Payload parity reported `drift=0` throughout.

   Run the negative control with `--check-slice-of REF`, which checks one ref
   against the integration ref and exits 1 on gaps.

Exit code 1 when any check fails; 0 when everything passes (drift allowlist hits
are reported but do not fail the run).

Usage:
    scripts/check_stack_sync.py [--parity-prefix split/]
                                [--integration feat/discovery-reviver-integration]
                                [--base-branch my-releases]
                                [--allow-drift path[,path...]]
                                [--slice-prefix v2rayN/ServiceLib/Reviver/]
                                [--allow-slice-drift path[,path...]]
                                [--repo OWNER/NAME] [--json]
    scripts/check_stack_sync.py --check-slice-of dff02073   # negative control

Network: only `gh` (already authenticated in CI and in this repo's dev shells).
"""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
from dataclasses import dataclass, field

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# The slice of the tree every split PR of this stack is required to carry exactly as the
# integration ref has it. Keep this in sync with the stack's own file ownership; widen it
# only for paths that are genuinely shared by all splits.
DEFAULT_SLICE_PREFIXES = ("v2rayN/ServiceLib/Reviver/",)


class GateError(RuntimeError):
    pass


def run(argv: list[str], *, cwd: str = REPO_ROOT, check: bool = True) -> str:
    proc = subprocess.run(argv, cwd=cwd, text=True, capture_output=True)
    if check and proc.returncode != 0:
        raise GateError(
            f"command failed ({proc.returncode}): {' '.join(argv)}\n{proc.stderr.strip()}"
        )
    return proc.stdout


def ensure_full_history(*, dry_run: bool = False) -> str:
    """Deepen a shallow clone, or say why ancestry cannot be computed.

    `actions/checkout` defaults to `fetch-depth: 1`, and CI's stack-audit job ran on exactly such a
    checkout: `git merge-base` then fails with exit 1 (it cannot find a common ancestor), and the
    *gate* looked broken when the repository simply had no history. Detected, not guessed -- the same
    command is what the local runbook tells you to check first.
    """
    shallow = run(["git", "rev-parse", "--is-shallow-repository"]).strip() == "true"
    if not shallow:
        return "full"
    if dry_run:
        return "shallow"
    run(["git", "fetch", "--quiet", "--unshallow", "origin", "+refs/heads/*:refs/remotes/origin/*"])
    still = run(["git", "rev-parse", "--is-shallow-repository"]).strip() == "true"
    return "shallow-after-fetch" if still else "unshallowed"


def gh_json(args: list[str]) -> object:
    try:
        out = run(["gh", "api", *args])
    except GateError as exc:
        # In CI the token is present (github.token) but `gh` itself is only on runner images; a
        # plain "command failed (1): gh api ..." made the job look like a stack failure. Name the
        # real cause so a reader does not start auditing PRs that are fine.
        if "gh" in str(exc) and ("No such file" in str(exc) or "not found" in str(exc)):
            raise GateError(
                "the `gh` CLI is not available in this environment; this gate needs it to list "
                "open pull requests (CI supplies it, and `GH_TOKEN`/`actions/GITHUB_TOKEN` authorises it)"
            ) from exc
        raise
    return json.loads(out) if out.strip() else None


@dataclass
class Pull:
    number: int
    head: str
    head_ref: str
    base: str
    base_ref: str
    draft: bool
    base_sha: str = ""
    behind_base: int = 0
    drift: list[str] = field(default_factory=list)
    slice_gaps: list[str] = field(default_factory=list)
    slice_missing: list[str] = field(default_factory=list)
    allowlisted: list[str] = field(default_factory=list)
    error: str = ""


def list_open_pulls(repo: str) -> list[Pull]:
    """Open PRs in this repository, paginated (gh api returns one JSON doc per page)."""
    pulls: list[Pull] = []
    page = 1
    while True:
        out = run(
            [
                "gh",
                "api",
                f"repos/{repo}/pulls?state=open&per_page=100&page={page}",
                "-H",
                "Accept: application/vnd.github+json",
            ]
        )
        docs: list[dict] = []
        decoder = json.JSONDecoder()
        idx = 0
        while idx < len(out):
            while idx < len(out) and out[idx] in " \n\r\t":
                idx += 1
            if idx >= len(out):
                break
            obj, idx = decoder.raw_decode(out, idx)
            docs.extend(obj if isinstance(obj, list) else [obj])
        if not docs:
            break
        for item in docs:
            pulls.append(
                Pull(
                    number=item["number"],
                    head=item["head"]["sha"],
                    head_ref=item["head"]["ref"],
                    base=item["base"]["sha"] or item["base"]["ref"],
                    base_ref=item["base"]["ref"],
                    draft=bool(item["draft"]),
                )
            )
        if len(docs) < 100:
            break
        page += 1
    return pulls


def fetch_ref(ref: str, repo: str) -> str:
    """Make sure a commit object exists locally, returning the ref that resolves."""
    try:
        return run(["git", "rev-parse", "--verify", f"{ref}^{{commit}}"])
    except GateError:
        pass
    if "/" in ref:
        candidate = f"origin/{ref}"
        try:
            return run(["git", "rev-parse", "--verify", f"{candidate}^{{commit}}"])
        except GateError:
            pass
    run(["git", "fetch", "--quiet", "origin", f"+refs/heads/{ref}:refs/remotes/origin/{ref}"])
    return run(["git", "rev-parse", "--verify", f"origin/{ref}^{{commit}}"])


def payload_files(base: str, head: str, *, full_against: str | None = None) -> list[str]:
    """Files the PR changes.

    Two-dot (default): exactly the payload GitHub attributes to the pull request.
    full_against: three-dot semantics, i.e. everything changed on the PR side since
    it forked. This surfaces stale-stack files that a pinned base hides.
    """
    spec = f"{full_against or base}...{head}" if full_against else f"{base} {head}"
    out = run(["git", "diff", "--name-only"] + spec.split(), check=False)
    return sorted({line for line in out.splitlines() if line.strip()})


def blob_of(rev: str, path: str) -> str | None:
    proc = subprocess.run(
        ["git", "rev-parse", "-q", "--verify", f"{rev}:{path}"],
        cwd=REPO_ROOT,
        text=True,
        capture_output=True,
    )
    value = proc.stdout.strip()
    return value or None


def slice_gaps(ref: str, integration: str, prefixes: tuple[str, ...]) -> list[str]:
    """Files of the shared slice where `ref` disagrees with the integration ref.

    Missing files are reported too: dropping a shared file is the same class of revert as
    rewinding it, and is just as invisible to payload parity.
    """
    gaps: list[str] = []
    for prefix in prefixes:
        out = run(["git", "ls-tree", "-r", "--name-only", integration, "--", prefix], check=False)
        for path in sorted({line for line in out.splitlines() if line.strip()}):
            want = blob_of(integration, path)
            got = blob_of(ref, path)
            if got is None:
                gaps.append(f"{path} (missing)")
            elif got != want:
                gaps.append(f"{path} (blob {got[:10]} != {want[:10]})")
    return gaps


def parse_args(argv: list[str]) -> argparse.Namespace:
    p = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    p.add_argument("--repo", default=os.environ.get("GITHUB_REPOSITORY", "AmirrezaFarnamTaheri/PattN"))
    p.add_argument("--integration", default="feat/discovery-reviver-integration")
    p.add_argument("--base-branch", default="my-releases")
    p.add_argument(
        "--parity-prefix",
        action="append",
        default=None,
        help="only enforce blob parity for PRs whose head ref starts with this prefix "
        "(repeatable). default: split/",
    )
    p.add_argument(
        "--allow-drift",
        default="",
        help="comma-separated payload paths allowed to differ from the integration ref",
    )
    p.add_argument(
        "--slice-prefix",
        action="append",
        default=None,
        help="tree prefix whose files a split PR must carry exactly as the integration ref "
        "does (repeatable). default: " + ", ".join(DEFAULT_SLICE_PREFIXES),
    )
    p.add_argument(
        "--allow-slice-drift",
        default="",
        help="comma-separated shared-slice paths allowed to differ (recorded, not failing)",
    )
    p.add_argument(
        "--slice-base-ref",
        default="split/final-integration-base",
        help="ref declared to carry the complete shared slice; it must contain every "
        "slice file with the integration ref's blob. Empty string disables the check.",
    )
    p.add_argument(
        "--check-slice-of",
        default="",
        help="check one ref against the integration ref's shared slice and exit; this is the "
        "executable negative control for the shared-slice check (e.g. --check-slice-of dff02073)",
    )
    p.add_argument("--skip-base-ancestry", action="store_true")
    p.add_argument(
        "--pinned-base",
        action="store_true",
        help="compare against the base commit recorded when the PR was opened instead of "
        "the base branch tip. Default (off) is stricter: a stacked PR must contain the "
        "current tip of the branch it targets, because that is what will merge next.",
    )
    p.add_argument(
        "--full-payload",
        action="store_true",
        help="compare the whole PR-side change set (merge-base..head) with blob parity "
        "instead of only the two-dot payload GitHub attributes to the PR.",
    )
    p.add_argument(
        "--no-deepen",
        action="store_true",
        help="do not auto-unshallow a shallow clone before computing merge-base "
        "(CI checkouts default to fetch-depth: 1, where merge-base fails and looks like a stack failure)",
    )
    p.add_argument("--json", action="store_true")
    return p.parse_args(argv)


def main(argv: list[str]) -> int:
    args = parse_args(argv)
    prefixes = tuple(args.parity_prefix or ["split/"])
    slice_prefixes = tuple(args.slice_prefix or DEFAULT_SLICE_PREFIXES)
    state = ensure_full_history(dry_run=args.no_deepen)
    if state != "full":
        print(f"note: clone history state after preparation: {state}", file=sys.stderr)
    allow = {x.strip() for x in args.allow_drift.split(",") if x.strip()}
    slice_allow = {x.strip() for x in args.allow_slice_drift.split(",") if x.strip()}

    if args.check_slice_of:
        integration = fetch_ref(args.integration, args.repo).strip()
        ref = fetch_ref(args.check_slice_of, args.repo).strip()
        gaps = slice_gaps(ref, integration, slice_prefixes)
        gaps = [g for g in gaps if g.split(" ")[0] not in slice_allow]
        print(
            f"shared-slice check: {args.check_slice_of} -> {ref[:10]} vs {integration[:10]} "
            f"({', '.join(slice_prefixes)}): {len(gaps)} gap(s)"
        )
        for gap in gaps:
            print(f"  gap: {gap}")
        return 1 if gaps else 0

    pulls = list_open_pulls(args.repo)
    if not pulls:
        print("no open pull requests; nothing to check", file=sys.stderr)
        return 0

    integration = fetch_ref(args.integration, args.repo).strip()
    for pull in pulls:
        try:
            # Reviewers push while a PR is open, so prefer the live head branch over the
            # head commit GitHub recorded when the PR was opened/last synced.
            head = fetch_ref(pull.head_ref, args.repo).strip() or fetch_ref(pull.head, args.repo).strip()
            base_ref = pull.base if args.pinned_base else pull.base_ref
            base = fetch_ref(base_ref, args.repo).strip()
            pull.base_sha = base
            merge_base = run(["git", "merge-base", head, base]).strip()
            if not args.skip_base_ancestry:
                proc = subprocess.run(
                    ["git", "merge-base", "--is-ancestor", base, head],
                    cwd=REPO_ROOT,
                    capture_output=True,
                )
                if proc.returncode != 0:
                    count = run(
                        ["git", "rev-list", "--count", f"{head}..{base}"]
                    ).strip()
                    pull.behind_base = int(count or "1")
            # Checked before payload parity on purpose: this one has caught a shared slice the
            # PR payload could not see (see the module docstring). Only split PRs are held to
            # blob identity -- an ordinary branch may carry its own resolution of a shared file
            # (the umbrella PR *is* the integration ref, and this session's branches carry merge
            # resolutions the integration ref has not absorbed yet).
            if pull.head_ref.startswith(prefixes):
                for gap in slice_gaps(head, integration, slice_prefixes):
                    path, _, verdict = gap.partition(" ")
                    if path in slice_allow:
                        continue
                    if verdict.startswith("(missing)"):
                        pull.slice_missing.append(gap)
                    else:
                        pull.slice_gaps.append(gap)
            for path in payload_files(base, head, full_against=merge_base if args.full_payload else None):
                if not pull.head_ref.startswith(prefixes):
                    continue
                head_blob = blob_of(head, path)
                integration_blob = blob_of(integration, path)
                if head_blob is None or integration_blob is None:
                    continue
                if head_blob == integration_blob:
                    continue
                (pull.allowlisted if path in allow else pull.drift).append(path)
        except GateError as exc:
            pull.error = str(exc)

    slice_base_gaps: list[str] = []
    if args.slice_base_ref and args.slice_base_ref not in slice_allow:
        try:
            slice_base = fetch_ref(args.slice_base_ref, args.repo).strip()
            slice_base_gaps = [
                gap
                for gap in slice_gaps(slice_base, integration, slice_prefixes)
                if gap.split(" ")[0] not in slice_allow
            ]
        except GateError as exc:
            slice_base_gaps = [f"could not resolve --slice-base-ref {args.slice_base_ref}: {exc}"]

    failures = [
        p for p in pulls if p.error or p.behind_base or p.drift or p.slice_gaps
    ] + ([None] if slice_base_gaps else [])

    if args.json:
        print(
            json.dumps(
                {
                    "integration": integration,
                    "parityPrefixes": list(prefixes),
                    "slicePrefixes": list(slice_prefixes),
                    "sliceBaseRef": args.slice_base_ref,
                    "sliceBaseGaps": slice_base_gaps,
                    "pulls": [p.__dict__ for p in pulls],
                    "failed": [p.number for p in failures],
                },
                indent=2,
                sort_keys=True,
            )
        )
    else:
        print(
            f"integration ref: {integration[:10]}  parity prefixes: {', '.join(prefixes)}  "
            f"shared slice: {', '.join(slice_prefixes)}"
        )
        for pull in pulls:
            status = "FAIL" if pull in failures else "ok"
            print(
                f"  #{pull.number:<4} {status:<4} {pull.head[:8]} base={pull.base} "
                f"behind-base={pull.behind_base} drift={len(pull.drift)} "
                f"allowlisted={len(pull.allowlisted)} slice-gaps={len(pull.slice_gaps)}"
                + (f" error={pull.error}" if pull.error else "")
            )
            for path in pull.drift:
                print(f"        drift: {path}")
            for path in pull.allowlisted:
                print(f"        allowlisted drift: {path}")
            for gap in pull.slice_gaps:
                print(f"        shared-slice gap: {gap}")
            if pull.slice_missing:
                print(
                    f"        shared-slice subset: {len(pull.slice_missing)} slice file(s) not "
                    "carried by this split (not fatal; listed in --json)"
                )

    if slice_base_gaps:
        print(
            f"  {args.slice_base_ref}: FAIL -- declared to carry the whole shared slice "
            f"({len(slice_base_gaps)} gap(s))"
        )
        for gap in slice_base_gaps:
            print(f"        {gap}")

    if failures:
        print(
            "\nStack is not integrable as reviewed. Either rebase the PR onto its base "
            "(then re-verify blob parity) or mirror the integration-branch fix into the "
            "owning split PR. See AUDIT_RECONCILIATION_2026-10-02.md.",
            file=sys.stderr,
        )
        return 1
    if not args.json:
        print(
            "\nall open PRs contain their base tip, match the integration ref, and the split "
            "stack's shared slice is unchanged"
        )
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
