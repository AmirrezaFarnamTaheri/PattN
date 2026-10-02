#!/usr/bin/env python3
"""Static gate for C# *test* sources: two failure modes that only CI caught, late.

Both were real, repeated failures in this workstream, each costing a full CI cycle:

1. Assertion spelling. These tests assert through TUnit, where a no-argument assertion
   takes **no** message: `await x.Should().BeTrue("why")` does not compile
   (`No overload for method 'BeTrue' takes 1 arguments`). The repository spells
   explanations fluently: `await x.Should().BeTrue().Because("why")`
   (precedent: v2rayN/ServiceLib.Tests/CoreConfig/Singbox/CoreConfigSingboxServiceTests.cs:15).

2. `await` inside a method that is not `async` (`CS4032`), which happens when an assertion is
   switched to the awaited form without marking the test method `async Task`.

Deliberately scoped to test trees (`*/…Tests/…`) and to *direct* method-body statements:
awaits inside a lambda body (for example `Task.Run(async () => { await … })`) are legal and are
not reported. That limitation is why this is a test-sources gate and not a whole-repo one.

Usage:
    scripts/check_test_assertions.py [--root PATH] [--json]

Exit 0 when clean, 1 when anything is reported, 2 on a bad usage/root.
"""

from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys

NO_ARG_ASSERTIONS = ("BeTrue", "BeFalse", "BeNull", "NotBeNull", "BeEmpty", "NotBeEmpty")
MESSAGE_RE = re.compile(r"\.Should\(\)\.(" + "|".join(NO_ARG_ASSERTIONS) + r")\(\s*[\"$]")
SIGNATURE_RE = re.compile(
    r"^\s*(?:public|private|internal|protected)\s+(?:static\s+)?(async\s+)?"
    r"([\w<>\[\],\.\?]+)\s+(\w+)\s*\("
)


def is_test_source(path: pathlib.Path) -> bool:
    """True for files inside a *.Tests / *Tests directory (the projects this gate owns)."""
    return any(part.endswith("Tests") or part.endswith(".Tests") for part in path.parts[:-1])


def scan(path: pathlib.Path) -> list[dict[str, object]]:
    text = path.read_text(encoding="utf-8", errors="replace")
    findings: list[dict[str, object]] = []

    for number, line in enumerate(text.splitlines(), 1):
        match = MESSAGE_RE.search(line)
        if match:
            findings.append(
                {
                    "path": str(path),
                    "line": number,
                    "rule": "assertion-message-argument",
                    "message": f"{match.group(1)}() takes no argument; use .Because(\"...\")",
                }
            )

    depth = 0
    pending: tuple[str, bool] | None = None
    method: tuple[str, bool, int] | None = None
    for number, line in enumerate(text.splitlines(), 1):
        signature = SIGNATURE_RE.match(line)
        if signature:
            pending = (signature.group(3), bool(signature.group(1)))
        for char in line:
            if char == "{":
                depth += 1
                if pending:
                    method = (pending[0], pending[1], depth)
                    pending = None
            elif char == "}":
                if method and depth == method[2]:
                    method = None
                depth = max(0, depth - 1)
        # Only a statement directly in the method body: a lambda body sits one level deeper
        # (see the module docstring for why this matters).
        if "await " in line and method and not method[1] and depth == method[2]:
            findings.append(
                {
                    "path": str(path),
                    "line": number,
                    "rule": "await-in-non-async-method",
                    "message": f"'await' inside non-async {method[0]}()",
                }
            )
    return findings


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--root", default=".", help="checkout/worktree to scan")
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args(argv)

    root = pathlib.Path(args.root)
    if not root.is_dir():
        print(f"not a directory: {root}", file=sys.stderr)
        return 2

    findings: list[dict[str, object]] = []
    scanned = 0
    for path in sorted(root.rglob("*.cs")):
        if not is_test_source(path):
            continue
        scanned += 1
        for finding in scan(path):
            display = finding["path"]
            try:
                display = str(pathlib.Path(display).relative_to(root))
            except ValueError:
                pass
            findings.append({**finding, "path": display})

    if args.json:
        print(json.dumps({"scanned": scanned, "findings": findings}, indent=2))
        return 1 if findings else 0

    if not findings:
        print(f"test assertion gate: clean ({scanned} test source file(s) scanned)")
        return 0
    for finding in findings:
        print(f"{finding['path']}:{finding['line']}: {finding['rule']}: {finding['message']}")
    print(f"test assertion gate: {len(findings)} finding(s) in {scanned} scanned file(s)")
    return 1


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
