#!/usr/bin/env python3
"""Create a deterministic ZIP archive from one directory tree."""

from __future__ import annotations

import argparse
import os
from pathlib import Path
import shutil
import stat
import time
import zipfile

ZIP_EPOCH = 315532800  # 1980-01-01 UTC, minimum representable ZIP timestamp.


def zip_datetime(epoch: int) -> tuple[int, int, int, int, int, int]:
    value = max(ZIP_EPOCH, epoch)
    dt = time.gmtime(value)
    # ZIP timestamps have two-second resolution.
    return (dt.tm_year, dt.tm_mon, dt.tm_mday, dt.tm_hour, dt.tm_min, dt.tm_sec - (dt.tm_sec % 2))


def info_for(path: Path, archive_name: str, epoch: int) -> zipfile.ZipInfo:
    st = path.lstat()
    is_dir = path.is_dir() and not path.is_symlink()
    name = archive_name.rstrip("/") + ("/" if is_dir else "")
    info = zipfile.ZipInfo(name, zip_datetime(epoch))
    info.create_system = 3
    info.flag_bits |= 0x800  # UTF-8 names.

    if path.is_symlink():
        mode = stat.S_IFLNK | 0o777
        info.external_attr = mode << 16
        info.compress_type = zipfile.ZIP_STORED
    elif is_dir:
        mode = stat.S_IFDIR | stat.S_IMODE(st.st_mode)
        info.external_attr = (mode << 16) | 0x10
        info.compress_type = zipfile.ZIP_STORED
    else:
        mode = stat.S_IFREG | stat.S_IMODE(st.st_mode)
        info.external_attr = mode << 16
        info.compress_type = zipfile.ZIP_DEFLATED
    return info


def build(root: Path, output: Path, epoch: int) -> None:
    root = root.resolve()
    if not root.is_dir():
        raise SystemExit(f"input directory does not exist: {root}")

    entries = sorted(root.rglob("*"), key=lambda p: p.relative_to(root).as_posix())
    output.parent.mkdir(parents=True, exist_ok=True)

    with zipfile.ZipFile(
        output,
        mode="w",
        compression=zipfile.ZIP_DEFLATED,
        compresslevel=1,
        allowZip64=True,
        strict_timestamps=True,
    ) as archive:
        # Include the root directory so extraction preserves the existing release layout.
        root_info = zipfile.ZipInfo(root.name + "/", zip_datetime(epoch))
        root_info.create_system = 3
        root_info.external_attr = ((stat.S_IFDIR | 0o755) << 16) | 0x10
        root_info.compress_type = zipfile.ZIP_STORED
        archive.writestr(root_info, b"")

        for path in entries:
            relative = path.relative_to(root).as_posix()
            archive_name = f"{root.name}/{relative}"
            info = info_for(path, archive_name, epoch)
            if path.is_symlink():
                archive.writestr(info, os.readlink(path).encode("utf-8"))
            elif path.is_dir():
                archive.writestr(info, b"")
            else:
                with path.open("rb") as source, archive.open(info, "w", force_zip64=True) as destination:
                    shutil.copyfileobj(source, destination, length=1024 * 1024)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("root", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--epoch", type=int, required=True)
    args = parser.parse_args()
    build(args.root, args.output, args.epoch)


if __name__ == "__main__":
    main()
