#!/usr/bin/env bash
# Reproducibility helpers for PattN native release packages.
# Call pattn_init_reproducible_build after the packaging script has resolved
# the exact source ref so SOURCE_DATE_EPOCH follows the source being packaged.

pattn_init_reproducible_build() {
  local repo_root="${1:-.}"
  local epoch="${SOURCE_DATE_EPOCH:-}"

  if [[ -z "$epoch" ]]; then
    epoch="$(git -C "$repo_root" show -s --format=%ct HEAD 2>/dev/null || true)"
  fi
  [[ "$epoch" =~ ^[0-9]+$ && "$epoch" -gt 0 ]] || {
    echo "Invalid SOURCE_DATE_EPOCH: '$epoch'" >&2
    return 1
  }

  export SOURCE_DATE_EPOCH="$epoch"
  export TZ=UTC
  export LC_ALL=C
  export LANG=C
}

pattn_normalize_tree_mtime() {
  local root="${1:?tree root is required}"
  [[ -d "$root" ]] || {
    echo "Reproducibility tree does not exist: $root" >&2
    return 1
  }
  [[ "${SOURCE_DATE_EPOCH:-}" =~ ^[0-9]+$ ]] || {
    echo "SOURCE_DATE_EPOCH is not initialized" >&2
    return 1
  }

  # Native Linux packagers run with GNU coreutils. -h keeps symlink targets untouched.
  find "$root" -exec touch -h -d "@${SOURCE_DATE_EPOCH}" {} +
}

pattn_reproducible_tar_gz() {
  local root="${1:?archive root is required}"
  local entry="${2:?archive entry is required}"
  local output="${3:?archive output is required}"

  [[ "${SOURCE_DATE_EPOCH:-}" =~ ^[0-9]+$ ]] || {
    echo "SOURCE_DATE_EPOCH is not initialized" >&2
    return 1
  }

  tar \
    --sort=name \
    --mtime="@${SOURCE_DATE_EPOCH}" \
    --owner=0 \
    --group=0 \
    --numeric-owner \
    -C "$root" \
    -cf - "$entry" \
    | gzip -n > "$output"
}

# Checksums authenticate bytes; this additionally proves staged native binaries
# match the package RID before a RISC-V/LoongArch artifact can be emitted.
pattn_verify_elf_machine() {
  local file="${1:?ELF file is required}"
  local rid="${2:?target RID is required}"
  local label="${3:-$file}"
  local machine=""

  command -v readelf >/dev/null 2>&1 || {
    echo "readelf is required to verify ELF architecture for $label" >&2
    return 1
  }
  [[ -s "$file" ]] || {
    echo "ELF architecture check target is missing or empty: $label ($file)" >&2
    return 1
  }

  machine="$(LC_ALL=C readelf -h "$file" 2>/dev/null |
    sed -n 's/^[[:space:]]*Machine:[[:space:]]*//p' | head -n1)"
  [[ -n "$machine" ]] || {
    echo "Unable to read ELF machine for $label ($file)" >&2
    return 1
  }

  case "$rid" in
    linux-x64)
      [[ "$machine" == *"X86-64"* || "$machine" == *"x86-64"* || "$machine" == *"Advanced Micro Devices"* ]]
      ;;
    linux-arm64)
      [[ "$machine" == *"AArch64"* ]]
      ;;
    linux-riscv64)
      [[ "$machine" == *"RISC-V"* ]]
      ;;
    linux-loongarch64)
      [[ "$machine" == *"LoongArch"* ]]
      ;;
    *)
      echo "Unsupported ELF target RID: $rid" >&2
      return 1
      ;;
  esac || {
    echo "ELF architecture mismatch for $label: target=$rid, machine=$machine" >&2
    return 1
  }

  echo "[OK] ELF architecture $label: target=$rid, machine=$machine"
}
