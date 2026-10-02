#!/usr/bin/env python3
"""Generate a deterministic SPDX 2.3 inventory of PattN's declared source dependencies."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import xml.etree.ElementTree as ET


def spdx_id(kind: str, name: str, version: str) -> str:
    digest = hashlib.sha256(f"{kind}|{name}|{version}".encode()).hexdigest()[:20]
    return f"SPDXRef-{kind}-{digest}"


def nuget_packages(path: Path) -> list[dict]:
    root = ET.parse(path).getroot()
    packages = []
    for element in root.iter():
        if element.tag.split("}")[-1] != "PackageVersion":
            continue
        name = element.attrib.get("Include", "").strip()
        version = element.attrib.get("Version", "").strip()
        if not name or not version:
            continue
        packages.append({
            "SPDXID": spdx_id("NuGet", name, version),
            "name": name,
            "versionInfo": version,
            "downloadLocation": f"https://www.nuget.org/packages/{name}/{version}",
            "filesAnalyzed": False,
            "licenseConcluded": "NOASSERTION",
            "licenseDeclared": "NOASSERTION",
            "externalRefs": [{
                "referenceCategory": "PACKAGE-MANAGER",
                "referenceType": "purl",
                "referenceLocator": f"pkg:nuget/{name}@{version}",
            }],
            "comment": "Centrally pinned direct NuGet dependency from Directory.Packages.props.",
        })
    return packages


def go_packages(path: Path) -> list[dict]:
    text = path.read_text(encoding="utf-8")
    requirements: list[tuple[str, str]] = []
    in_block = False
    for raw in text.splitlines():
        line = raw.split("//", 1)[0].strip()
        if not line:
            continue
        if line == "require (":
            in_block = True
            continue
        if in_block and line == ")":
            in_block = False
            continue
        if in_block:
            parts = line.split()
            if len(parts) >= 2:
                requirements.append((parts[0], parts[1]))
            continue
        match = re.fullmatch(r"require\s+(\S+)\s+(\S+)", line)
        if match:
            requirements.append((match.group(1), match.group(2)))

    packages = []
    for name, version in requirements:
        clean_version = version.lstrip("v")
        packages.append({
            "SPDXID": spdx_id("Go", name, version),
            "name": name,
            "versionInfo": version,
            "downloadLocation": f"https://proxy.golang.org/{name}/@v/{version}.zip",
            "filesAnalyzed": False,
            "licenseConcluded": "NOASSERTION",
            "licenseDeclared": "NOASSERTION",
            "externalRefs": [{
                "referenceCategory": "PACKAGE-MANAGER",
                "referenceType": "purl",
                "referenceLocator": f"pkg:golang/{name}@{clean_version}",
            }],
            "comment": "Direct Go module dependency declared in pattn-discovery/go.mod.",
        })
    return packages


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path("."))
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--source-sha", required=True)
    parser.add_argument("--repository", required=True)
    args = parser.parse_args()

    root = args.root.resolve()
    packages = (
        nuget_packages(root / "v2rayN/Directory.Packages.props")
        + go_packages(root / "pattn-discovery/go.mod")
    )
    packages.sort(key=lambda p: (p["name"].lower(), p.get("versionInfo", "")))

    document = {
        "spdxVersion": "SPDX-2.3",
        "dataLicense": "CC0-1.0",
        "SPDXID": "SPDXRef-DOCUMENT",
        "name": "PattN-declared-source-dependencies",
        "documentNamespace": (
            f"https://github.com/{args.repository}/sbom/"
            f"{args.source_sha}/declared-source-dependencies"
        ),
        "creationInfo": {
            # Deliberately deterministic; source SHA is the freshness identity.
            "created": "1980-01-01T00:00:00Z",
            "creators": ["Tool: PattN declared dependency SBOM generator"],
        },
        "documentDescribes": [p["SPDXID"] for p in packages],
        "packages": packages,
        "annotations": [{
            "annotationDate": "1980-01-01T00:00:00Z",
            "annotationType": "OTHER",
            "annotator": "Tool: PattN declared dependency SBOM generator",
            "comment": (
                "Inventory covers centrally pinned direct NuGet package versions and direct Go "
                "module requirements. It does not claim resolved transitive NuGet closure."
            ),
        }],
    }

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(document, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
