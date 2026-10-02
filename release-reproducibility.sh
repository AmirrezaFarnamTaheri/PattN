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
