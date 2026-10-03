#!/usr/bin/env bash
set -euo pipefail

LOCK_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=release-assets.lock.sh
source "$LOCK_DIR/release-assets.lock.sh"
# shellcheck source=release-reproducibility.sh
source "$LOCK_DIR/release-reproducibility.sh"

VERSION_ARG=""
WITH_CORE="both"
FORCE_NETCORE=0
BUILD_FROM=""
XRAY_VER="${XRAY_VER:-}"
SING_VER="${SING_VER:-}"

PKGROOT="v2rayN-publish"
PROJECT_HINT="v2rayN.Desktop/v2rayN.Desktop.csproj"
OUTPUT_DIR="${HOME}/debbuild"
DOTNET_RISCV_VERSION="$PATTN_RISCV_DOTNET_VERSION"
# Microsoft does not currently publish a .NET 10 RISC-V Linux SDK. The default
# source is therefore an external bootstrap origin, but the bytes remain locked
# by PATTN_RISCV_DOTNET_SHA256. Release operators can point this at an
# organization-controlled immutable mirror without changing the reviewed digest.
DOTNET_RISCV_BASE="${PATTN_RISCV_DOTNET_BASE:-https://github.com/xujiegb/dotnet-riscv/releases/download}"
DOTNET_RISCV_FILE="dotnet-sdk-${DOTNET_RISCV_VERSION}-linux-riscv64.tar.gz"
DOTNET_SDK_URL="${DOTNET_RISCV_BASE}/${DOTNET_RISCV_VERSION}/${DOTNET_RISCV_FILE}"

OS_ID=""
OS_NAME=""
OS_VERSION_ID=""
HOST_ARCH=""
SCRIPT_DIR=""
PROJECT=""
VERSION=""
PUBLISH_ROOT=""

declare -a BUILT_DEBS=()

die() {
  echo "$*" >&2
  exit 1
}

parse_args() {
  local first_arg="${1:-}"

  if [[ -n "$first_arg" && "$first_arg" != --* ]]; then
    VERSION_ARG="$first_arg"
    shift || true
  fi

  while [[ $# -gt 0 ]]; do
    case "$1" in
      --with-core)   WITH_CORE="${2:-both}"; shift 2 ;;
      --xray-ver)    XRAY_VER="${2:-}"; shift 2 ;;
      --singbox-ver) SING_VER="${2:-}"; shift 2 ;;
      --netcore)     FORCE_NETCORE=1; shift ;;
      --buildfrom)   BUILD_FROM="${2:-}"; shift 2 ;;
      *)
        [[ -n "${VERSION_ARG:-}" ]] || VERSION_ARG="$1"
        shift
        ;;
    esac
  done

  if [[ -n "${VERSION_ARG:-}" && -n "${BUILD_FROM:-}" ]]; then
    die "You cannot specify both an explicit version and --buildfrom at the same time.
        Provide either a version (e.g. 7.14.0) OR --buildfrom 1|2|3."
  fi
}

detect_environment() {
  . /etc/os-release

  OS_ID="${ID:-}"
  OS_NAME="${NAME:-$OS_ID}"
  OS_VERSION_ID="${VERSION_ID:-}"
  HOST_ARCH="$(uname -m)"

  case "$OS_ID" in
    debian|ubuntu)
      echo "Detected supported system: ${OS_NAME:-$OS_ID} ${OS_VERSION_ID:-}"
      ;;
    *)
      die "Unsupported system: ${OS_NAME:-unknown} (${OS_ID:-unknown}).
This script supports Debian/Ubuntu RISC-V build guests."
      ;;
  esac

  case "$HOST_ARCH" in
    riscv64) ;;
    *) die "Only supports riscv64" ;;
  esac

}

install_dependencies() {
  local install_ok=0
  local tmp_dotnet=""
  local deps=(
    curl unzip tar jq rsync ca-certificates git dpkg-dev fakeroot file
    desktop-file-utils xdg-utils wget gcc make pkg-config binutils
    libicu-dev libssl-dev libfontconfig1 libfreetype6 zlib1g
  )

  if [[ -z "${PATTN_DISCOVERY_PREBUILT:-}" ]]; then
    deps+=(golang-go)
  fi

  mkdir -p "$OUTPUT_DIR"

  if command -v apt-get >/dev/null 2>&1; then
    sudo apt-get update
    sudo apt-get -y install "${deps[@]}"

    mkdir -p "$HOME/.dotnet"
    tmp_dotnet="$(mktemp -d)"
    pattn_download_sha256 "$DOTNET_SDK_URL" "$tmp_dotnet/$DOTNET_RISCV_FILE" "$PATTN_RISCV_DOTNET_SHA256"
    tar -C "$HOME/.dotnet" -xzf "$tmp_dotnet/$DOTNET_RISCV_FILE"
    rm -rf "$tmp_dotnet"

    export PATH="$HOME/.dotnet:$PATH"
    export DOTNET_ROOT="$HOME/.dotnet"

    dotnet --info >/dev/null 2>&1 && install_ok=1
  fi

  if [[ "$install_ok" -ne 1 ]]; then
    echo "Could not auto-install dependencies for '$OS_ID'. Make sure these are available:"
    echo "dotnet-riscv SDK, curl, unzip, tar, rsync, git, gcc, make, dpkg-deb, fakeroot, libicu-dev, libssl-dev"
    exit 1
  fi
}

prepare_workspace() {
  SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
  PUBLISH_ROOT="$SCRIPT_DIR/.pattn-package-publish"
  cd "$SCRIPT_DIR"

  if [[ -f .gitmodules ]]; then
    git submodule sync --recursive || true
    git submodule update --init --recursive || true
  fi

  PROJECT="$PROJECT_HINT"
  [[ -f "$PROJECT" ]] || PROJECT="$(find . -maxdepth 3 -name 'v2rayN.Desktop.csproj' | head -n1 || true)"
  [[ -f "$PROJECT" ]] || die "v2rayN.Desktop.csproj not found"
}

choose_channel() {
  local ch="latest"
  local sel=""

  if [[ -n "${BUILD_FROM:-}" ]]; then
    case "$BUILD_FROM" in
      1) echo "latest"; return 0 ;;
      2) echo "prerelease"; return 0 ;;
      3) echo "keep"; return 0 ;;
      *) die "[ERROR] Invalid --buildfrom value: ${BUILD_FROM}. Use 1|2|3." ;;
    esac
  fi

  if [[ -t 0 ]]; then
    echo "[?] Choose v2rayN release channel:" >&2
    echo "    1) Latest (stable)  [default]" >&2
    echo "    2) Pre-release (preview)" >&2
    echo "    3) Keep current (do nothing)" >&2
    printf "Enter 1, 2 or 3 [default 1]: " >&2

    if read -r sel </dev/tty; then
      case "${sel:-}" in
        2) ch="prerelease" ;;
        3) ch="keep" ;;
      esac
    fi
  fi

  echo "$ch"
}

get_latest_tag_latest() {
  curl -fsSL "https://api.github.com/repos/AmirrezaFarnamTaheri/PattN/releases/latest" \
    | jq -re '.tag_name' \
    | sed 's/^v//'
}

get_latest_tag_prerelease() {
  curl -fsSL "https://api.github.com/repos/AmirrezaFarnamTaheri/PattN/releases?per_page=20" \
    | jq -re 'first(.[] | select(.prerelease == true) | .tag_name)' \
    | sed 's/^v//'
}

sync_submodules() {
  if [[ -f .gitmodules ]]; then
    git submodule sync --recursive || true
    git submodule update --init --recursive || true
  fi
}

git_try_checkout() {
  local want="$1"
  local ref=""

  if git rev-parse --git-dir >/dev/null 2>&1; then
    git fetch --tags --force --prune --depth=1 || true
    git rev-parse "refs/tags/${want}" >/dev/null 2>&1 && ref="$want"

    if [[ -n "$ref" ]]; then
      echo "[OK] Found ref '${ref}', checking out..."
      git checkout -f "$ref"
      sync_submodules
      return 0
    fi
  fi

  return 1
}

apply_channel_or_keep() {
  local ch="$1"
  local tag=""

  if [[ "$ch" == "keep" ]]; then
    echo "[*] Keep current repository state (no checkout)."
    VERSION="$(git describe --tags --abbrev=0 2>/dev/null || echo '0.0.0+git')"
    VERSION="${VERSION#v}"
    return 0
  fi

  echo "[*] Resolving ${ch} tag from GitHub releases..."

  case "$ch" in
    latest)     tag="$(get_latest_tag_latest || true)" ;;
    prerelease) tag="$(get_latest_tag_prerelease || true)" ;;
    *)          die "Failed to resolve latest tag for channel '${ch}'." ;;
  esac

  [[ -n "$tag" ]] || die "Failed to resolve latest tag for channel '${ch}'."

  echo "[*] Latest tag for '${ch}': ${tag}"
  git_try_checkout "$tag" || die "Failed to checkout '${tag}'."
  VERSION="${tag#v}"
}

resolve_version() {
  if git rev-parse --git-dir >/dev/null 2>&1; then
    if [[ -n "${VERSION_ARG:-}" ]]; then
      local clean_ver="${VERSION_ARG#v}"

      if git_try_checkout "$clean_ver"; then
        VERSION="$clean_ver"
      elif [[ "${PATTN_SOURCE_PINNED:-0}" == "1" ]]; then
        echo "[*] Source-pinned build: keeping checked-out tree for version ${clean_ver}."
        VERSION="$clean_ver"
      else
        die "Requested tag '${VERSION_ARG}' is absent; refusing to switch source."
      fi
    else
      apply_channel_or_keep "$(choose_channel)"
    fi
  else
    echo "Current directory is not a git repo; proceeding on current tree."
    VERSION="${VERSION_ARG:-0.0.0}"
  fi

  VERSION="${VERSION#v}"
  echo "[*] GUI version resolved as: ${VERSION}"
}

xray_url_for_rid() {
  local rid="$1"
  local ver="$2"

  case "$rid" in
    linux-riscv64) echo "https://github.com/patterniha/Xray-core/releases/download/v${ver}/Xray-linux-riscv64.zip" ;;
    *)             return 1 ;;
  esac
}

singbox_url_for_rid() {
  local rid="$1"
  local ver="$2"

  case "$rid" in
    linux-riscv64) echo "https://github.com/SagerNet/sing-box/releases/download/v${ver}/sing-box-${ver}-linux-riscv64.tar.gz" ;;
    *)             return 1 ;;
  esac
}

bundle_url_for_rid() {
  local rid="$1"

  case "$rid" in
    linux-riscv64) echo "https://raw.githubusercontent.com/2dust/v2rayN-core-bin/${PATTN_CORE_BIN_COMMIT}/v2rayN-linux-riscv64.zip" ;;
    *)             return 1 ;;
  esac
}

download_xray() {
  local outdir="$1"
  local rid="$2"
  local ver="${XRAY_VER:-$PATTN_XRAY_VERSION}"
  local url=""
  local tmp=""
  local expected=""

  mkdir -p "$outdir"

  [[ "$ver" == "$PATTN_XRAY_VERSION" ]] || {
    echo "[xray] Version $ver is not present in release-assets.lock.sh; update the lock deliberately." >&2
    return 1
  }
  expected="$(pattn_xray_sha256 "$rid")" || { echo "[xray] No locked digest for $rid"; return 1; }
  url="$(xray_url_for_rid "$rid" "$ver")" || { echo "[xray] Unsupported RID: $rid"; return 1; }

  echo "[+] Download xray: $url"

  tmp="$(mktemp -d)"
  pattn_download_sha256 "$url" "$tmp/xray.zip" "$expected" || { rm -rf "$tmp"; return 1; }
  unzip -q "$tmp/xray.zip" -d "$tmp" || { rm -rf "$tmp"; return 1; }
  install -m 755 "$tmp/xray" "$outdir/xray" || { rm -rf "$tmp"; return 1; }
  rm -rf "$tmp"
}

download_singbox() {
  local outdir="$1"
  local rid="$2"
  local ver="${SING_VER:-$PATTN_SINGBOX_VERSION}"
  local url=""
  local tmp=""
  local expected=""
  local bin=""
  local cronet=""

  mkdir -p "$outdir"

  [[ "$ver" == "$PATTN_SINGBOX_VERSION" ]] || {
    echo "[sing-box] Version $ver is not present in release-assets.lock.sh; update the lock deliberately." >&2
    return 1
  }
  expected="$(pattn_singbox_sha256 "$rid")" || { echo "[sing-box] No locked digest for $rid"; return 1; }
  url="$(singbox_url_for_rid "$rid" "$ver")" || { echo "[sing-box] Unsupported RID: $rid"; return 1; }

  echo "[+] Download sing-box: $url"

  tmp="$(mktemp -d)"
  pattn_download_sha256 "$url" "$tmp/singbox.tar.gz" "$expected" || { rm -rf "$tmp"; return 1; }
  tar -C "$tmp" -xzf "$tmp/singbox.tar.gz" || { rm -rf "$tmp"; return 1; }

  bin="$(find "$tmp" -type f -name 'sing-box' | head -n1 || true)"
  [[ -n "$bin" ]] || { echo "[!] sing-box unpack failed"; rm -rf "$tmp"; return 1; }

  install -m 755 "$bin" "$outdir/sing-box" || { rm -rf "$tmp"; return 1; }

  cronet="$(find "$tmp" -type f -name 'libcronet*.so*' | head -n1 || true)"
  [[ -n "$cronet" ]] && install -m 644 "$cronet" "$outdir/libcronet.so" || true

  rm -rf "$tmp"
}

unify_geo_layout() {
  local outroot="$1"
  local n
  local names=(
    geosite.dat
    geoip.dat
    geoip-only-cn-private.dat
    Country.mmdb
    geoip.metadb
  )

  mkdir -p "$outroot/bin"

  for n in "${names[@]}"; do
    if [[ -f "$outroot/bin/xray/$n" ]]; then
      mv -f "$outroot/bin/xray/$n" "$outroot/bin/$n"
    fi
  done
}

download_geo_assets() {
  local outroot="$1"
  local bin_dir="$outroot/bin"
  local srss_dir="$bin_dir/srss"
  local f=""
  local expected=""

  mkdir -p "$bin_dir" "$srss_dir"
  pattn_download_sha256 "https://github.com/Chocolate4U/Iran-v2ray-rules/releases/download/${PATTN_IRAN_GEO_RELEASE}/geosite.dat" "$bin_dir/geosite.dat" "$PATTN_IRAN_GEOSITE_SHA256" || return 1
  pattn_download_sha256 "https://github.com/Chocolate4U/Iran-v2ray-rules/releases/download/${PATTN_IRAN_GEO_RELEASE}/geoip.dat" "$bin_dir/geoip.dat" "$PATTN_IRAN_GEOIP_SHA256" || return 1
  expected="$(pattn_raw_rule_blob_sha1 loyal geoip-only-cn-private.dat)"
  pattn_download_git_blob "https://raw.githubusercontent.com/Loyalsoldier/geoip/${PATTN_LOYALSOLDIER_GEOIP_COMMIT}/geoip-only-cn-private.dat" "$bin_dir/geoip-only-cn-private.dat" "$expected" || return 1
  expected="$(pattn_raw_rule_blob_sha1 loyal Country.mmdb)"
  pattn_download_git_blob "https://raw.githubusercontent.com/Loyalsoldier/geoip/${PATTN_LOYALSOLDIER_GEOIP_COMMIT}/Country.mmdb" "$bin_dir/Country.mmdb" "$expected" || return 1
  pattn_download_git_blob "https://raw.githubusercontent.com/MetaCubeX/meta-rules-dat/${PATTN_METACUBEX_RELEASE_COMMIT}/geoip.metadb" "$bin_dir/geoip.metadb" "$PATTN_METACUBEX_GEOIP_METADB_BLOB_SHA1" || return 1
  for f in geoip-private.srs geoip-cn.srs geoip-facebook.srs geoip-fastly.srs geoip-google.srs geoip-netflix.srs geoip-telegram.srs geoip-twitter.srs; do
    expected="$(pattn_raw_rule_blob_sha1 geoip "$f")"
    pattn_download_git_blob "https://raw.githubusercontent.com/2dust/sing-box-rules/${PATTN_SING_RULE_GEOIP_COMMIT}/$f" "$srss_dir/$f" "$expected" || return 1
  done
  for f in geosite-cn.srs geosite-gfw.srs geosite-google.srs geosite-greatfire.srs geosite-geolocation-cn.srs geosite-category-ads-all.srs geosite-private.srs; do
    expected="$(pattn_raw_rule_blob_sha1 geosite "$f")"
    pattn_download_git_blob "https://raw.githubusercontent.com/2dust/sing-box-rules/${PATTN_SING_RULE_GEOSITE_COMMIT}/$f" "$srss_dir/$f" "$expected" || return 1
  done
  for f in geosite-category-ir.srs geoip-ir.srs; do
    expected="$(pattn_raw_rule_blob_sha1 iran "$f")"
    pattn_download_git_blob "https://raw.githubusercontent.com/chocolate4u/Iran-sing-box-rules/${PATTN_IRAN_SING_RULES_COMMIT}/$f" "$srss_dir/$f" "$expected" || return 1
  done
  unify_geo_layout "$outroot"
}
populate_assets_zip_mode() {
  local outroot="$1"
  local rid="$2"
  local url=""
  local tmp=""
  local nested_dir=""
  local expected=""

  url="$(bundle_url_for_rid "$rid")" || { echo "[!] Bundle unsupported RID: $rid"; return 1; }

  expected="$(pattn_core_bundle_blob_sha1 "$rid")" || { echo "[!] No locked bundle digest for $rid"; return 1; }
  echo "[+] Try verified v2rayN bundle archive: $url"

  tmp="$(mktemp -d)"
  pattn_download_git_blob "$url" "$tmp/v2rayn.zip" "$expected" || { echo "[!] Bundle verification failed"; rm -rf "$tmp"; return 1; }
  unzip -q "$tmp/v2rayn.zip" -d "$tmp" || { echo "[!] Bundle unzip failed"; rm -rf "$tmp"; return 1; }

  if [[ -d "$tmp/bin" ]]; then
    mkdir -p "$outroot/bin"
    rsync -a "$tmp/bin/" "$outroot/bin/"
  else
    rsync -a "$tmp/" "$outroot/"
  fi

  rm -f "$outroot/v2rayn.zip" 2>/dev/null || true
  find "$outroot" -type d -name "mihomo" -prune -exec rm -rf {} + 2>/dev/null || true

  nested_dir="$(find "$outroot" -maxdepth 1 -type d -name 'v2rayN-linux-*' | head -n1 || true)"
  if [[ -n "$nested_dir" && -d "$nested_dir/bin" ]]; then
    mkdir -p "$outroot/bin"
    rsync -a "$nested_dir/bin/" "$outroot/bin/"
    rm -rf "$nested_dir"
  fi

  unify_geo_layout "$outroot"
  rm -rf "$tmp"

  echo "[+] Bundle extracted to $outroot"
}

populate_assets_netcore_mode() {
  local outroot="$1"
  local rid="$2"

  mkdir -p "$outroot/bin/xray" "$outroot/bin/sing_box"

  if [[ "$WITH_CORE" == "xray" || "$WITH_CORE" == "both" ]]; then
    download_xray "$outroot/bin/xray" "$rid" || return 1
  fi

  if [[ "$WITH_CORE" == "sing-box" || "$WITH_CORE" == "both" ]]; then
    download_singbox "$outroot/bin/sing_box" "$rid" || return 1
  fi

  download_geo_assets "$outroot" || return 1
}

stage_runtime_assets() {
  local outroot="$1"
  local rid="$2"

  mkdir -p "$outroot/bin/xray" "$outroot/bin/sing_box"

  if [[ "$FORCE_NETCORE" -eq 0 ]]; then
    if populate_assets_zip_mode "$outroot" "$rid"; then
      # PattN: the core-bin bundle ships upstream Xray; replace it with patterniha/Xray-core
      download_xray "$outroot/bin/xray" "$rid" || { echo "[!] PattN: failed to fetch patterniha/Xray-core, aborting"; return 1; }
      # PattN: replace bundled geo files with Chocolate4U + Iran rule-sets
      download_geo_assets "$outroot" || { echo "[!] Verified geo download failed, aborting"; return 1; }
      echo "[*] Using v2rayN bundle archive."
    else
      echo "[*] Bundle failed, fallback to separate core + rules."
      populate_assets_netcore_mode "$outroot" "$rid"
    fi
  else
    echo "[*] --netcore specified: use separate core + rules."
    populate_assets_netcore_mode "$outroot" "$rid"
  fi
}

describe_target() {
  local short="$1"

  case "$short" in
    riscv64) printf '%s\n%s\n' "linux-riscv64" "riscv64" ;;
    *)       echo "Unknown arch '$short' (use riscv64)" >&2; return 1 ;;
  esac
}

publish_binary() {
  local rid="$1"
  local pubdir="$PUBLISH_ROOT/$rid"

  rm -rf "$pubdir"
  mkdir -p "$pubdir"
  dotnet clean "$PROJECT" -c Release
  dotnet restore "$PROJECT"
  dotnet publish "$PROJECT" -c Release -r "$rid" -p:PublishSingleFile=false -p:SelfContained=true -o "$pubdir" ${VERSION_ARG:+-p:Version=${VERSION_ARG#v}}
}

host_can_execute_target() {
  local short="$1"
  local host
  host="$(uname -m)"
  case "$short:$host" in
    x64:x86_64|arm64:aarch64|riscv64:riscv64|loongarch64:loongarch64) return 0 ;;
    *) return 1 ;;
  esac
}

stage_discovery_helper() {
  local outroot="$1"
  local rid="$2"
  local goarch=""
  local helper="$outroot/bin/pattn-discovery/pattn-discovery"

  case "$rid" in
    linux-x64)         goarch=amd64 ;;
    linux-arm64)       goarch=arm64 ;;
    linux-riscv64)     goarch=riscv64 ;;
    linux-loongarch64) goarch=loong64 ;;
    *) echo "Unsupported pattn-discovery RID: $rid" >&2; return 1 ;;
  esac

  mkdir -p "$(dirname "$helper")"
  if [[ -n "${PATTN_DISCOVERY_PREBUILT:-}" ]]; then
    [[ -s "$PATTN_DISCOVERY_PREBUILT" ]] || {
      echo "PATTN_DISCOVERY_PREBUILT is missing or empty: $PATTN_DISCOVERY_PREBUILT" >&2
      return 1
    }
    install -m 0755 "$PATTN_DISCOVERY_PREBUILT" "$helper"
  else
    command -v go >/dev/null 2>&1 || {
      echo "Go 1.23+ or PATTN_DISCOVERY_PREBUILT is required to package pattn-discovery" >&2
      return 1
    }
    (
      cd "$SCRIPT_DIR/pattn-discovery"
      CGO_ENABLED=0 GOOS=linux GOARCH="$goarch"         go build -buildvcs=false -trimpath -ldflags="-s -w" -o "$helper" ./cmd/pattn-discovery
    )
    chmod 0755 "$helper"
  fi

  [[ -s "$helper" && -x "$helper" ]] || {
    echo "pattn-discovery helper was not staged as an executable: $helper" >&2
    return 1
  }
}

verify_staged_elf_architecture() {
  local outroot="${1:?staged output root is required}"
  local rid="${2:?target RID is required}"
  local helper="$outroot/bin/pattn-discovery/pattn-discovery"

  pattn_verify_elf_machine "$helper" "$rid" "pattn-discovery"

  if [[ "$WITH_CORE" == "xray" || "$WITH_CORE" == "both" ]]; then
    pattn_verify_elf_machine "$outroot/bin/xray/xray" "$rid" "Xray"
  fi
  if [[ "$WITH_CORE" == "sing-box" || "$WITH_CORE" == "both" ]]; then
    pattn_verify_elf_machine "$outroot/bin/sing_box/sing-box" "$rid" "sing-box"
  fi
}

smoke_discovery_helper() {
  local helper="$1"
  local output
  output="$(printf '%s\n' '{"v":1,"id":"package-smoke","method":"engine.version"}' | "$helper")"
  grep -F '"id":"package-smoke"' <<<"$output" >/dev/null
  grep -F '"engine":"pattn-discovery"' <<<"$output" >/dev/null
}

verify_discovery_deb() {
  local package="$1"
  local short="$2"
  local tmp helper
  tmp="$(mktemp -d)"
  dpkg-deb -x "$package" "$tmp"
  helper="$tmp/opt/v2rayN/bin/pattn-discovery/pattn-discovery"
  [[ -s "$helper" && -x "$helper" ]] || {
    rm -rf "$tmp"
    echo "final DEB is missing executable pattn-discovery: $package" >&2
    return 1
  }
  if host_can_execute_target "$short"; then
    smoke_discovery_helper "$helper"
    echo "[OK] Final DEB pattn-discovery handshake passed for $short."
  else
    echo "[OK] Final DEB contains pattn-discovery for $short; execution deferred to matching architecture."
  fi
  rm -rf "$tmp"
}

write_launcher_file() {
  local stage="$1"

  install -m 755 /dev/stdin "$stage/usr/bin/v2rayn" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
DIR="/opt/v2rayN"
cd "$DIR"

if [[ -x "$DIR/PattN" ]]; then
  exec "$DIR/PattN" "$@"
fi

for dll in PattN.dll; do
  if [[ -f "$DIR/$dll" ]]; then
    exec /usr/bin/dotnet "$DIR/$dll" "$@"
  fi
done

echo "PattN launcher: no executable found in $DIR" >&2
ls -l "$DIR" >&2 || true
exit 1
EOF
}

write_desktop_file() {
  local stage="$1"

  install -m 644 /dev/stdin "$stage/usr/share/applications/v2rayn.desktop" <<'EOF'
[Desktop Entry]
Type=Application
Name=PattN
Comment=PattN for Debian GNU Linux
Exec=v2rayn
Icon=v2rayn
StartupWMClass=PattN
Terminal=false
Categories=Network;
EOF
}

write_maintainer_scripts() {
  local debian_dir="$1"

  install -m 755 /dev/stdin "$debian_dir/postinst" <<'EOF'
#!/bin/sh
set -e
update-desktop-database /usr/share/applications >/dev/null 2>&1 || true
if command -v gtk-update-icon-cache >/dev/null 2>&1; then
  gtk-update-icon-cache -f /usr/share/icons/hicolor >/dev/null 2>&1 || true
fi
exit 0
EOF

  install -m 755 /dev/stdin "$debian_dir/postrm" <<'EOF'
#!/bin/sh
set -e
update-desktop-database /usr/share/applications >/dev/null 2>&1 || true
if command -v gtk-update-icon-cache >/dev/null 2>&1; then
  gtk-update-icon-cache -f /usr/share/icons/hicolor >/dev/null 2>&1 || true
fi
exit 0
EOF
}

package_binary() {
  local short="$1"
  local rid="$2"
  local deb_arch="$3"
  local pubdir=""
  local workdir=""
  local stage=""
  local debian_dir=""
  local project_dir=""
  local icon_candidate=""
  local shlibs_depends=""
  local extra_depends=""
  local final_depends=""
  local multiarch=""
  local sys_libdir=""
  local sys_usrlibdir=""
  local deb_out=""

  pubdir="$PUBLISH_ROOT/$rid"
  [[ -d "$pubdir" ]] || { echo "Publish directory not found: $pubdir"; return 1; }

  workdir="$(mktemp -d)"
  trap '[[ -n "${workdir:-}" ]] && rm -rf "$workdir"' RETURN

  stage="$workdir/${PKGROOT}_${VERSION}_${deb_arch}"
  debian_dir="$stage/DEBIAN"

  mkdir -p "$stage/opt/v2rayN" "$stage/usr/bin" "$stage/usr/share/applications" "$stage/usr/share/icons/hicolor/256x256/apps" "$debian_dir"
  cp -a "$pubdir/." "$stage/opt/v2rayN/"

  project_dir="$(cd "$(dirname "$PROJECT")" && pwd)"
  icon_candidate="$project_dir/v2rayN.png"
  [[ -f "$icon_candidate" ]] && cp "$icon_candidate" "$stage/usr/share/icons/hicolor/256x256/apps/v2rayn.png" || true

  stage_runtime_assets "$stage/opt/v2rayN" "$rid"
  stage_discovery_helper "$stage/opt/v2rayN" "$rid"
  verify_staged_elf_architecture "$stage/opt/v2rayN" "$rid"
  write_launcher_file "$stage"
  write_desktop_file "$stage"
  write_maintainer_scripts "$debian_dir"

  extra_depends="libc6 (>= 2.39), fontconfig (>= 2.15.0), desktop-file-utils (>= 0.26), xdg-utils (>= 1.1.3), coreutils (>= 9.4), bash (>= 5.2.21), libfreetype6 (>= 2.13)"
  
  mkdir -p "$workdir/debian"
  cat > "$workdir/debian/control" <<EOF
Source: v2rayn
Section: net
Priority: optional
Maintainer: 2dust <noreply@github.com>
Standards-Version: 4.7.0

Package: v2rayn
Architecture: ${deb_arch}
Description: PattN
EOF

  multiarch="$(dpkg-architecture -a"$deb_arch" -qDEB_HOST_MULTIARCH)"
  sys_libdir="/lib/$multiarch"
  sys_usrlibdir="/usr/lib/$multiarch"

  : > "$debian_dir/substvars"

  mapfile -t ELF_FILES < <(
    find "$stage/opt/v2rayN" -type f \( -name "*.so*" -o -perm -111 \) ! -name 'libcoreclrtraceptprovider.so'
  )

  if [[ "${#ELF_FILES[@]}" -gt 0 ]]; then
    (
      cd "$workdir"
      dpkg-shlibdeps \
        -l"$stage/opt/v2rayN" \
        -l"$sys_libdir" \
        -l"$sys_usrlibdir" \
        -T"$debian_dir/substvars" \
        "${ELF_FILES[@]}"
    ) >/dev/null 2>&1 || true
  fi

  shlibs_depends="$(sed -n 's/^shlibs:Depends=//p' "$debian_dir/substvars" | head -n1 || true)"

  if [[ -n "$shlibs_depends" ]]; then
    shlibs_depends="$(echo "$shlibs_depends" \
      | sed -E 's/ *\([^)]*\)//g' \
      | sed -E 's/ *, */, /g' \
      | sed -E 's/^, *//; s/, *$//')"
    final_depends="${shlibs_depends}, ${extra_depends}"
  else
    final_depends="${extra_depends}"
  fi

  cat > "$debian_dir/control" <<EOF
Package: v2rayn
Version: ${VERSION}
Architecture: ${deb_arch}
Maintainer: 2dust <noreply@github.com>
Homepage: https://github.com/patterniha/PattN
Section: net
Priority: optional
Depends: ${final_depends}
Description: PattN (Avalonia) GUI client for Linux
 Support vless / vmess / Trojan / http / socks / Anytls / Hysteria2 /
 Shadowsocks / tuic / WireGuard.
EOF

  find "$stage/opt/v2rayN" -type d -exec chmod 0755 {} +
  find "$stage/opt/v2rayN" -type f -exec chmod 0644 {} +
  [[ -f "$stage/opt/v2rayN/PattN" ]] && chmod 0755 "$stage/opt/v2rayN/PattN" || true
  [[ -f "$stage/opt/v2rayN/bin/pattn-discovery/pattn-discovery" ]] && chmod 0755 "$stage/opt/v2rayN/bin/pattn-discovery/pattn-discovery" || true

  # Normalize all package payload/control mtimes to the source commit before dpkg-deb.
  # dpkg-deb honors SOURCE_DATE_EPOCH for archive-member metadata as well.
  pattn_normalize_tree_mtime "$stage"

  deb_out="$OUTPUT_DIR/v2rayn_${VERSION}_${deb_arch}.deb"
  dpkg-deb --root-owner-group --build "$stage" "$deb_out"
  verify_discovery_deb "$deb_out" "$short"

  echo "Build done for $short. DEB at:"
  echo "  $deb_out"
  BUILT_DEBS+=("$deb_out")
}

select_targets() {
  printf '%s\n' riscv64
}

build_one_target() {
  local short="$1"
  local meta=()
  local rid=""
  local deb_arch=""

  mapfile -t meta < <(describe_target "$short") || return 1
  rid="${meta[0]}"
  deb_arch="${meta[1]}"

  echo "[*] Building for target: $short  (RID=$rid, DEB arch=$deb_arch)"
  publish_binary "$rid"
  package_binary "$short" "$rid" "$deb_arch"
}

print_summary() {
  local pkg=""

  echo ""
  echo "================ Build Summary ================="
  if [[ "${#BUILT_DEBS[@]}" -gt 0 ]]; then
    echo "Output directory: $OUTPUT_DIR"
    for pkg in "${BUILT_DEBS[@]}"; do
      echo "$pkg"
    done
  else
    echo "No DEBs detected in summary (check build logs above)."
  fi
  echo "==============================================="
}

main() {
  local targets=()
  local arch=""

  parse_args "$@"
  detect_environment
  install_dependencies
  prepare_workspace
  resolve_version
  pattn_init_reproducible_build "$SCRIPT_DIR"

  mapfile -t targets < <(select_targets)

  for arch in "${targets[@]}"; do
    build_one_target "$arch"
  done

  print_summary
}

main "$@"
