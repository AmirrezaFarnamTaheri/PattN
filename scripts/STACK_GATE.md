# Stack gate runbook

Two scripts gate the failure modes this repository's Discovery/Reviver review
stack keeps re-entering. Both are stdlib-only, both run in seconds, and both are
wired into `.github/workflows/stack-audit.yml`.

## 1. `scripts/check_stack_sync.py` — the stack may not drift behind its base

Contract for the `split/*` review PRs:

* the PR head **contains the current tip** of the branch it targets
  (`git merge-base --is-ancestor`), and
* every payload blob of a `split/*` PR is **identical** to the same path on the
  integration ref (`feat/discovery-reviver-integration`, which is the reviewed
  authority for those subsystems).

Violating the first half is what makes a merge silently *revert* hardening: a PR
that is 15 commits behind its own base carries the pre-fix copy of the base's
files, and GitHub will happily offer a green "merge" button for it.

```
python3 scripts/check_stack_sync.py            # every open PR, human-readable
python3 scripts/check_stack_sync.py --json     # machine-readable
python3 scripts/check_stack_sync.py --full-payload   # compare everything the PR changed
python3 scripts/check_stack_sync.py --pinned-base    # GitHub's payload view, not the live base tip
python3 scripts/check_stack_sync.py --allow-drift path/one,path/two
```

Notes that matter when reading a failure:

* Head and base are resolved from the **live branch**, not from the SHA GitHub
  recorded on the PR object, because reviewers push while a PR is open.
* Intentional layering (e.g. `review/updater-archive-boundary` adding commits on
  top of `review/finalmask-range-hardening`, or `feat/intelligence-roadmap`
  building on the integration branch) drifts by design. Blob parity is only
  enforced for refs matching `--parity-prefix` (default `split/`); the ancestry
  check applies to everything and is the one that catches reverts.
* A PR with no drift *per GitHub's payload* can still be stale: its drifted files
  may sit on the base side of a pinned base and therefore not appear in the
  payload. That is why the default is the live base tip and why `--full-payload`
  exists.

## 2. `scripts/check_localization.py` — a view may not bind a missing resource key

`{Binding i18n.X, Source={x:Static p:ResUI.X}}` compiles against the generated
Designer member. Delete the `<data name="X">` entry from `ResUI.resx` and the
build stays green while the window throws on load — a startup crash that looks
like a localization miss.

| rule | check |
|---|---|
| R1 | every `ResUI.X` / `i18n.X` reference in `*.xaml`/`*.axaml` exists in `ResUI.resx` |
| R2 | `ResUI.resx` entries and `ResUI.Designer.cs` members agree in both directions |
| R3 | no culture `.resx` defines a key absent from `ResUI.resx`; `en` coverage reported |

```
python3 scripts/check_localization.py                       # whole tree
python3 scripts/check_localization.py --root <worktree>    # audit another checkout
python3 scripts/check_localization.py --base <sha> --head HEAD   # PR-scoped (what CI runs)
```

R3 intentionally does not require every culture to be complete — upstream
`ResUI.resx` grows faster than the translations — but it does fail on a culture
key that the master file does not define, and it always prints the per-culture
gap so the number cannot be argued from memory.

## Verified state (2026-10-02, static only — no .NET/Go toolchain in this sandbox)

| tree | keys | Designer members | views scanned | result |
|---|---|---|---|---|
| `feat/discovery-reviver-integration` 38c4a10 | 930 | 930 | 55 | pass |
| `my-releases` 0d1dfbb | 605 | 605 | 53 | pass |
| `feat/intelligence-roadmap` fc7dd06 (post-fix) | 943 | 943 | 55 | pass |

Negative test for R1/R3 (mutation, not a repo state): deleting the
`menuServers` entry from `ResUI.resx` at #18 produced **11 R1 findings** (one per
binding view, across both the WPF and Avalonia heads) and **8 R3 findings** (one
per culture file), exit 1. The gate is therefore not vacuous.

Stack-sync numbers, same date. Before the sync: 7 PRs carried 27 drifted payload
files (#2 1, #3 5, #5 11, #6 3, #7 4, #8 2, #9 1) and 4 PRs were behind their base
(#5 by 15 commits, #6/#7/#18 by 1). After the forward-only sync commits (and the
two follow-up commits #3's sync made necessary in #6 and #7), all 12 open PRs
report `behind-base=0 drift=0`, exit 0, in both default and `--full-payload` mode.
Reproduce with `python3 scripts/check_stack_sync.py --full-payload`.
