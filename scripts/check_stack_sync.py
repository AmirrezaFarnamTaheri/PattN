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

Exit code 1 when any check fails; 0 when everything passes (drift allowlist hits
are reported but do not fail the run).

Usage:
    scripts/check_stack_sync.py [--parity-prefix split/]
                                [--integration feat/discovery-reviver-integration]
                                [--base-branch my-releases]
                                [--allow-drift path[,path...]]
                                [--repo OWNER/NAME] [--json]

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


class GateError(RuntimeError):
    pass


def run(argv: list[str], *, cwd: str = REPO_ROOT, check: bool = True) -> str:
    proc = subprocess.run(argv, cwd=cwd, text=True, capture_output=True)
    if check and proc.returncode != 0:
        raise GateError(
            f"command failed ({proc.returncode}): {' '.join(argv)}\n{proc.stderr.strip()}"
        )
    return proc.stdout


def gh_json(args: list[str]) -> object:
    out = run(["gh", "api", *args])
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
    p.add_argument("--json", action="store_true")
    return p.parse_args(argv)


def main(argv: list[str]) -> int:
    args = parse_args(argv)
    prefixes = tuple(args.parity_prefix or ["split/"])
    allow = {x.strip() for x in args.allow_drift.split(",") if x.strip()}

    pulls = list_open_pulls(args.repo)
    if not pulls:
        print("no open pull requests; nothing to check", file=sys.stderr)
        return 0

    integration = fetch_ref(args.integration, args.repo).strip()
    for pull in pulls:
        try:
            head = fetch_ref(pull.head, args.repo).strip()
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

    failures = [
        p
        for p in pulls
        if p.error or p.behind_base or p.drift
    ]

    if args.json:
        print(
            json.dumps(
                {
                    "integration": integration,
                    "parityPrefixes": list(prefixes),
                    "pulls": [p.__dict__ for p in pulls],
                    "failed": [p.number for p in failures],
                },
                indent=2,
                sort_keys=True,
            )
        )
    else:
        print(f"integration ref: {integration[:10]}  parity prefixes: {', '.join(prefixes)}")
        for pull in pulls:
            status = "FAIL" if pull in failures else "ok"
            print(
                f"  #{pull.number:<4} {status:<4} {pull.head[:8]} base={pull.base} "
                f"behind-base={pull.behind_base} drift={len(pull.drift)} "
                f"allowlisted={len(pull.allowlisted)}"
                + (f" error={pull.error}" if pull.error else "")
            )
            for path in pull.drift:
                print(f"        drift: {path}")
            for path in pull.allowlisted:
                print(f"        allowlisted drift: {path}")

    if failures:
        print(
            "\nStack is not integrable as reviewed. Either rebase the PR onto its base "
            "(then re-verify blob parity) or mirror the integration-branch fix into the "
            "owning split PR. See AUDIT_RECONCILIATION_2026-10-02.md.",
            file=sys.stderr,
        )
        return 1
    print("\nall open PRs contain their base tip and match the integration ref")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
