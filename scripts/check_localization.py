#!/usr/bin/env python3
"""Localization-surface parity gate for v2rayN Discovery/Reviver resources.

The Discovery/Reviver UI binds strings as `{Binding i18n.<Key>, Source={x:Static p:ResUI.<Key>}}`
(Avalonia) and `@ResUI.<Key>` (WPF). A referenced key that no longer exists in `ResUI.resx`
compiles -- the Designer member is what the compiler checks -- and then throws at runtime while
loading the view. That is a startup crash, not a cosmetic miss, so it must be a build gate.

Checks (all fail the run):
  R1  every `ResUI.<Key>` reference in *.xaml / *.axaml resolves to a `Data` entry in
      v2rayN/ServiceLib/Resx/ResUI.resx
  R2  every `Data` name in ResUI.resx has a matching `internal static string <Name>` member in
      v2rayN/ServiceLib/Properties/ResUI.Designer.cs (and the reverse), so no view can bind to a
      key that the resource manager will not serve and no designer entry is orphaned by a
      hand-edited Designer file
  R3  every culture resource (ResUI.<culture>.resx) defines only keys that exist in ResUI.resx,
      and reports keys present in English but missing in that culture

Usage: scripts/check_localization.py [--culture en] [--json] [--base <rev> --head <rev>]

With --base/--head the checks are limited to files changed by that range, so a PR only has to
satisfy the gate for the localization surface it actually touches.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from dataclasses import asdict, dataclass, field

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ROOT = REPO_ROOT  # overridden by --root so the gate can audit any checkout/worktree
RESX_DIR = "v2rayN/ServiceLib/Resx"
MASTER_RESX = f"{RESX_DIR}/ResUI.resx"
DESIGNER = "v2rayN/ServiceLib/Resx/ResUI.Designer.cs"
UI_ROOTS = ("v2rayN",)
VIEWS_ONLY = False  # scan every *.xaml/*.axaml under v2rayN; the WPF and Avalonia heads both bind ResUI
VIEW_EXT = (".xaml", ".axaml")

DESIGNER_MEMBER_RE = re.compile(
    r"\b(?:public|internal)\s+static\s+string\s+([A-Za-z_]\w*)\s*\{", re.MULTILINE
)
XSTATIC_RE = re.compile(r"x:Static\s+[A-Za-z0-9_]+:ResUI\.([A-Za-z_]\w*)")
AT_RE = re.compile(r"@ResUI\.([A-Za-z_]\w*)")
I18N_RE = re.compile(r"\bi18n\.([A-Za-z_]\w*)")


@dataclass
class Finding:
    rule: str
    path: str
    key: str
    detail: str


@dataclass
class Report:
    keys: int = 0
    designer_members: int = 0
    views: int = 0
    culture_coverage: dict = field(default_factory=dict)
    findings: list[Finding] = field(default_factory=list)


def read(resx_root: str | None, rel: str) -> str | None:
    path = os.path.join(resx_root or ROOT, rel)
    try:
        with open(path, encoding="utf-8-sig") as handle:
            return handle.read()
    except OSError:
        return None


def resx_names(xml_text: str) -> set[str]:
    """Data entry names, via the XML parser (a malformed resx is itself a failure)."""
    try:
        root = ET.fromstring(xml_text)
    except ET.ParseError:
        return set()
    names = set()
    for node in root.iter():
        if node.tag.lower().endswith("}data") or node.tag.lower() == "data":
            name = node.attrib.get("name")
            if name:
                names.add(name)
    return names


def changed_files(base: str, head: str) -> set[str]:
    proc = subprocess.run(
        ["git", "-C", ROOT, "diff", "--name-only", f"{base}...{head}"],
        text=True,
        capture_output=True,
    )
    if proc.returncode != 0:
        raise SystemExit(f"git diff failed: {proc.stderr.strip()}")
    return {line for line in proc.stdout.splitlines() if line.strip()}


def view_files(limit: set[str] | None) -> list[str]:
    out: list[str] = []
    for rel_root in UI_ROOTS:
        for dirpath, dirnames, filenames in os.walk(os.path.join(ROOT, rel_root)):
            dirnames[:] = [d for d in dirnames if d not in ("obj", "bin")]
            for name in filenames:
                if not name.endswith(VIEW_EXT):
                    continue
                full = os.path.join(dirpath, name)
                rel = os.path.relpath(full, ROOT).replace(os.sep, "/")
                if limit is None or rel in limit:
                    out.append(rel)
    return sorted(out)


def culture_files(limit: set[str] | None) -> list[str]:
    base = os.path.join(ROOT, RESX_DIR)
    names = []
    for name in sorted(os.listdir(base)) if os.path.isdir(base) else []:
        if not name.endswith(".resx") or name == os.path.basename(MASTER_RESX):
            continue
        rel = f"{RESX_DIR}/{name}"
        if limit is None or rel in limit:
            names.append(rel)
    return names


def build_report(limit: set[str] | None, culture: str) -> Report:
    report = Report()

    master_text = read(None, MASTER_RESX)
    designer_text = read(None, DESIGNER)
    if not master_text:
        report.findings.append(
            Finding("R0", MASTER_RESX, "-", "resx not readable at the requested revision")
        )
        return report
    if not designer_text:
        report.findings.append(
            Finding("R0", DESIGNER, "-", "Designer.cs not readable at the requested revision")
        )
        return report

    if limit is not None and MASTER_RESX not in limit and DESIGNER not in limit and not any(
        f.endswith(VIEW_EXT) for f in limit
    ):
        return report  # this change does not touch the localization surface

    try:
        ET.fromstring(master_text)
    except ET.ParseError as exc:
        report.findings.append(
            Finding("R0", MASTER_RESX, "-", f"malformed XML: {exc}")
        )
        return report

    keys = resx_names(master_text)
    report.keys = len(keys)
    members = set(DESIGNER_MEMBER_RE.findall(designer_text))
    report.designer_members = len(members)

    for key in sorted(keys - members):
        report.findings.append(
            Finding("R2", DESIGNER, key, "resx entry has no generated Designer member")
        )
    for member in sorted(members - keys):
        report.findings.append(
            Finding("R2", DESIGNER, member, "Designer member has no resx entry")
        )

    for rel in view_files(limit):
        text = read(None, rel) or ""
        report.views += 1
        refs = set(XSTATIC_RE.findall(text)) | set(AT_RE.findall(text)) | set(I18N_RE.findall(text))
        for ref in sorted(refs):
            if ref not in keys:
                report.findings.append(
                    Finding(
                        "R1",
                        rel,
                        ref,
                        "view binds a resource key that does not exist in ResUI.resx "
                        "(compiles, throws when the view loads)",
                    )
                )

    for rel in culture_files(limit):
        text = read(None, rel)
        if text is None:
            continue
        names = resx_names(text)
        culture_name = os.path.basename(rel)[len("ResUI.") : -len(".resx")]
        report.culture_coverage[culture_name] = {"keys": len(names), "missing": len(keys - names)}
        for extra in sorted(names - keys):
            report.findings.append(
                Finding("R3", rel, extra, "culture resource defines a key absent from ResUI.resx")
            )
        if culture_name == culture and keys - names:
            for missing in sorted(keys - names):
                report.findings.append(
                    Finding("R3", rel, missing, f"key exists in ResUI.resx but not in {culture_name}")
                )
    return report


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--culture", default="en", help="culture treated as required-complete")
    parser.add_argument(
        "--base",
        default="",
        help="only enforce for files changed in <base>...<head> (scoped mode for PR CI)",
    )
    parser.add_argument("--head", default="HEAD")
    parser.add_argument(
        "--root",
        default="",
        help="checkout/worktree to audit (defaults to the script's own repository root)",
    )
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args(argv)

    global ROOT
    if args.root:
        ROOT = os.path.abspath(args.root)
    limit = changed_files(args.base, args.head) if args.base else None
    report = build_report(limit, args.culture)

    if args.json:
        payload = asdict(report)
        payload["findings"] = [asdict(f) for f in report.findings]
        print(json.dumps(payload, indent=2, sort_keys=True))
    else:
        print(
            f"ResUI.resx keys={report.keys} designer members={report.designer_members} "
            f"views scanned={report.views}"
            + (f" (scoped to {len(limit)} changed files)" if limit is not None else "")
        )
        for name, cov in sorted(report.culture_coverage.items()):
            print(f"  culture {name}: {cov['keys']} keys, {cov['missing']} missing vs ResUI.resx")
        shown = 0
        for f in report.findings:
            if shown >= 60:
                print(f"  ... {len(report.findings) - shown} more findings (use --json for all)")
                break
            print(f"  {f.rule} FAIL {f.path} :: {f.key} -- {f.detail}")
            shown += 1
        if not report.findings:
            print("localization surface is consistent")

    by_rule: dict[str, int] = {}
    for f in report.findings:
        by_rule[f.rule] = by_rule.get(f.rule, 0) + 1
    if by_rule:
        print(
            "findings by rule: " + ", ".join(f"{k}={v}" for k, v in sorted(by_rule.items())),
            file=sys.stderr,
        )
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
