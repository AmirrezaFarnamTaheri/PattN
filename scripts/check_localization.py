#!/usr/bin/env python3
"""Localization-surface parity gate for the hand-maintained .resx/Designer pairs.

Views bind strings as `{Binding i18n.<Key>, Source={x:Static p:ResUI.<Key>}}` (Avalonia),
`{x:Static resx:ResUI.<Key>}` (WPF) or `Resx.Resource.<Key>` (console helpers). The compiler
only checks the *generated Designer member*, so deleting a `<data>` entry from the .resx builds
cleanly and then throws while the window loads -- a startup crash that looks like a translation
miss. AmazTool has the same hazard in a sharper form: its `Resource.Designer.cs` is edited by
hand, so a key can exist in the resx and simply have no member (null/missing at runtime) or the
reverse.

Surfaces (both are checked in one run):
  ServiceLib  v2rayN/ServiceLib/Resx/ResUI*.resx   + Properties/Resx/ResUI.Designer.cs, views in
              v2rayN/v2rayN/** and v2rayN/v2rayN.Desktop/**
  AmazTool    v2rayN/AmazTool/Resx/Resource*.resx  + Resx/Resource.Designer.cs, references in
              v2rayN/AmazTool/*.cs

Rules:
  R1  every resource reference in a source/view file resolves to a master-resx key
  R2  master-resx keys and Designer members agree in both directions
  R3  no culture resx defines a key absent from the master resx; the `--culture` locale is
      additionally reported (and required) to cover every master key

Usage:
    scripts/check_localization.py [--culture en] [--json]
                                  [--base <rev> --head <rev>]   # PR-scoped subset
                                  [--root <dir>]                # audit another checkout

R3 deliberately does not require every culture to be complete -- upstream grows faster than its
translations -- but it always prints the per-culture gap so the number cannot be argued from
memory, and it fails on a culture key the master file does not define.
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

DESIGNER_MEMBER_RE = re.compile(
    r"\b(?:public|internal)\s+static\s+string\s+([A-Za-z_]\w*)\s*\{", re.MULTILINE
)
XSTATIC_RE = re.compile(r"x:Static\s+[A-Za-z0-9_]+:([A-Za-z_]\w*)\.([A-Za-z_]\w*)")
I18N_RE = re.compile(r"\bi18n\.([A-Za-z_]\w*)")
AT_RE = re.compile(r"@([A-Za-z_]\w*)\.([A-Za-z_]\w*)")
DOTTED_RE = re.compile(r"\bResx\.([A-Za-z_]\w*)\.([A-Za-z_]\w*)")

SOURCE_EXT = (".xaml", ".axaml", ".cs")


@dataclass
class Surface:
    """One resx/Designer pair plus the files allowed to reference it."""

    name: str
    resx_dir: str
    master: str
    designer: str
    class_name: str
    roots: tuple

    def culture_files(self, limit: set[str] | None) -> list[str]:
        base = os.path.join(ROOT, self.resx_dir)
        stem = self.master[: -len(".resx")]
        out = []
        for name in sorted(os.listdir(base)) if os.path.isdir(base) else []:
            if not name.endswith(".resx") or name == self.master:
                continue
            if not name.startswith(stem + "."):
                continue
            rel = f"{self.resx_dir}/{name}"
            if limit is None or rel in limit:
                out.append(rel)
        return out

    def sources(self, limit: set[str] | None) -> list[str]:
        out: list[str] = []
        for rel_root in self.roots:
            for dirpath, dirnames, filenames in os.walk(os.path.join(ROOT, rel_root)):
                dirnames[:] = [d for d in dirnames if d not in ("obj", "bin")]
                for name in filenames:
                    if not name.endswith(SOURCE_EXT):
                        continue
                    rel = os.path.relpath(os.path.join(dirpath, name), ROOT).replace(os.sep, "/")
                    if limit is None or rel in limit:
                        out.append(rel)
        return sorted(out)


SURFACES = (
    Surface(
        name="ServiceLib",
        resx_dir="v2rayN/ServiceLib/Resx",
        master="ResUI.resx",
        designer="v2rayN/ServiceLib/Resx/ResUI.Designer.cs",
        class_name="ResUI",
        roots=("v2rayN/v2rayN", "v2rayN/v2rayN.Desktop"),
    ),
    Surface(
        name="AmazTool",
        resx_dir="v2rayN/AmazTool/Resx",
        master="Resource.resx",
        designer="v2rayN/AmazTool/Resx/Resource.Designer.cs",
        class_name="Resource",
        roots=("v2rayN/AmazTool",),
    ),
)


@dataclass
class Finding:
    rule: str
    surface: str
    path: str
    key: str
    detail: str


@dataclass
class Report:
    keys: int = 0
    designer_members: int = 0
    files_scanned: int = 0
    culture_coverage: dict = field(default_factory=dict)
    findings: list[Finding] = field(default_factory=list)


def read(rel: str) -> str | None:
    try:
        with open(os.path.join(ROOT, rel), encoding="utf-8-sig") as handle:
            return handle.read()
    except OSError:
        return None


def resx_names(xml_text: str) -> set[str]:
    """Data entry names, via the XML parser (a malformed resx is itself a failure)."""
    names = set()
    for node in ET.fromstring(xml_text).iter():
        if node.tag == "data" or node.tag.endswith("}data"):
            name = node.attrib.get("name")
            if name:
                names.add(name)
    return names


def referenced_keys(text: str, surface: Surface) -> set[str]:
    keys: set[str] = set()
    for match in XSTATIC_RE.finditer(text):
        if match.group(1) == surface.class_name:
            keys.add(match.group(2))
    for match in AT_RE.finditer(text):
        if match.group(1) == surface.class_name:
            keys.add(match.group(2))
    for match in DOTTED_RE.finditer(text):
        if match.group(1) == surface.class_name:
            keys.add(match.group(2))
    if surface.class_name == "ResUI":
        # WPF/Avalonia bind the same identifier twice: `i18n.X` is the view-model property that
        # wraps ResUI.X, so a missing ResUI.X is also a missing binding target.
        keys |= set(I18N_RE.findall(text))
    return keys


def changed_files(base: str, head: str) -> set[str]:
    proc = subprocess.run(
        ["git", "-C", ROOT, "diff", "--name-only", f"{base}...{head}"],
        text=True,
        capture_output=True,
    )
    if proc.returncode != 0:
        raise SystemExit(f"git diff failed: {proc.stderr.strip()}")
    return {line for line in proc.stdout.splitlines() if line.strip()}


def build_report(limit: set[str] | None, culture: str) -> list[tuple[Surface, Report]]:
    reports: list[tuple[Surface, Report]] = []
    for surface in SURFACES:
        report = Report()
        if limit is None:
            touched = True
        else:
            changed = set(limit)
            touched = bool(
                {f"{surface.resx_dir}/{surface.master}", surface.designer} & changed
                or changed & set(surface.sources(changed))
            )
        if not touched:
            reports.append((surface, report))
            continue

        master_rel = f"{surface.resx_dir}/{surface.master}"
        master_text, designer_text = read(master_rel), read(surface.designer)
        if master_text is None:
            report.findings.append(
                Finding("R0", surface.name, master_rel, "-", "master resx not readable")
            )
            reports.append((surface, report))
            continue
        if designer_text is None:
            report.findings.append(
                Finding("R0", surface.name, surface.designer, "-", "Designer.cs not readable")
            )
            reports.append((surface, report))
            continue

        try:
            ET.fromstring(master_text)
        except ET.ParseError as exc:
            report.findings.append(
                Finding("R0", surface.name, master_rel, "-", f"malformed XML: {exc}")
            )
            reports.append((surface, report))
            continue

        keys = resx_names(master_text)
        members = set(DESIGNER_MEMBER_RE.findall(designer_text))
        report.keys, report.designer_members = len(keys), len(members)

        for key in sorted(keys - members):
            report.findings.append(
                Finding("R2", surface.name, surface.designer, key, "resx entry has no Designer member")
            )
        for member in sorted(members - keys):
            report.findings.append(
                Finding("R2", surface.name, surface.designer, member, "Designer member has no resx entry")
            )

        for rel in surface.sources(limit):
            text = read(rel)
            if text is None:
                continue
            report.files_scanned += 1
            for ref in sorted(referenced_keys(text, surface)):
                if ref not in keys:
                    report.findings.append(
                        Finding(
                            "R1",
                            surface.name,
                            rel,
                            ref,
                            "references a key absent from the master resx "
                            "(compiles, throws when the view/window loads)",
                        )
                    )

        for rel in surface.culture_files(limit):
            text = read(rel)
            if text is None:
                continue
            try:
                names = resx_names(text)
            except ET.ParseError as exc:
                report.findings.append(Finding("R0", surface.name, rel, "-", f"malformed XML: {exc}"))
                continue
            culture_name = os.path.basename(rel)[len(surface.master[: -len(".resx")]) + 1 : -len(".resx")]
            report.culture_coverage[culture_name] = {
                "keys": len(names),
                "missing": len(keys - names),
            }
            for extra in sorted(names - keys):
                report.findings.append(
                    Finding("R3", surface.name, rel, extra, "culture resx defines a key absent from master")
                )
            if culture_name == culture:
                for missing in sorted(keys - names):
                    report.findings.append(
                        Finding(
                            "R3",
                            surface.name,
                            rel,
                            missing,
                            f"key exists in {surface.master} but not in {culture_name}",
                        )
                    )
        reports.append((surface, report))
    return reports


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--culture", default="en", help="culture treated as required-complete")
    parser.add_argument(
        "--base", default="", help="only enforce for files changed in <base>...<head> (PR-scoped CI)"
    )
    parser.add_argument("--head", default="HEAD")
    parser.add_argument("--root", default="", help="checkout/worktree to audit")
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args(argv)

    if args.root:
        global ROOT
        ROOT = os.path.abspath(args.root)

    limit = changed_files(args.base, args.head) if args.base else None
    reports = build_report(limit, args.culture)
    findings = [f for _, r in reports for f in r.findings]

    if args.json:
        print(
            json.dumps(
                {
                    "root": ROOT,
                    "scoped": limit is not None,
                    "surfaces": [
                        {
                            "name": s.name,
                            "summary": {
                                "keys": r.keys,
                                "designer_members": r.designer_members,
                                "files_scanned": r.files_scanned,
                                "culture_coverage": r.culture_coverage,
                            },
                            "findings": [asdict(f) for f in r.findings],
                        }
                        for s, r in reports
                    ],
                    "failed": len(findings),
                },
                indent=2,
                sort_keys=True,
            )
        )
    else:
        scope = f" (scoped to {len(limit)} changed files)" if limit is not None else ""
        for surface, report in reports:
            print(
                f"[{surface.name}] master keys={report.keys} designer members={report.designer_members} "
                f"files scanned={report.files_scanned}{scope}"
            )
            for name, cov in sorted(report.culture_coverage.items()):
                print(
                    f"    culture {name}: {cov['keys']} keys, {cov['missing']} missing vs master"
                )
            for f in report.findings[:40]:
                print(f"    {f.rule} FAIL {f.path} :: {f.key} -- {f.detail}")
            if len(report.findings) > 40:
                print(f"    ... {len(report.findings) - 40} more (use --json for all)")
        if not findings:
            print("localization surfaces are consistent")

    by_rule: dict[str, int] = {}
    for f in findings:
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
