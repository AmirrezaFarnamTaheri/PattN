#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=release-assets.lock.sh
source "$SCRIPT_DIR/release-assets.lock.sh"

Arch="${1:-}"
OutputPath="${2:-}"
Version="${3:-}"

die() {
  echo "$*" >&2
  exit 1
}

[[ -n "$Arch" && -n "$OutputPath" && -n "$Version" ]] ||
  die "Usage: $0 <macos-64|macos-arm64> <output-path> <version>"

case "$Arch" in
  macos-64)
    XrayAsset="Xray-macos-64.zip"
    ;;
  macos-arm64)
    XrayAsset="Xray-macos-arm64-v8a.zip"
    ;;
  *)
    die "Unsupported architecture: $Arch"
    ;;
esac

mkdir -p "$OutputPath"
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

FileName="v2rayN-${Arch}.zip"
BundleURL="https://raw.githubusercontent.com/2dust/v2rayN-core-bin/${PATTN_CORE_BIN_COMMIT}/${FileName}"
BundleBlob="$(pattn_core_bundle_blob_sha1 "$Arch")" || die "No locked core bundle for $Arch"
pattn_download_git_blob "$BundleURL" "$tmp/$FileName" "$BundleBlob"
7z x -y -o"$tmp/core-bin" "$tmp/$FileName" >/dev/null
[[ -d "$tmp/core-bin/v2rayN-$Arch" ]] || die "Core bundle did not contain v2rayN-$Arch"
cp -rf "$tmp/core-bin/v2rayN-$Arch/." "$OutputPath/"

# PattN: replace upstream Xray with the reviewed, digest-locked fork release.
XrayURL="https://github.com/patterniha/Xray-core/releases/download/v${PATTN_XRAY_VERSION}/${XrayAsset}"
XraySHA="$(pattn_xray_sha256 "$Arch")" || die "No locked Xray digest for $Arch"
pattn_download_sha256 "$XrayURL" "$tmp/xray-core.zip" "$XraySHA"
7z x -y -o"$tmp/xray-core" "$tmp/xray-core.zip" >/dev/null
mkdir -p "$OutputPath/bin/xray"
install -m 0755 "$tmp/xray-core/xray" "$OutputPath/bin/xray/xray"

# PattN: reviewed geo release plus commit-pinned Iran sing-box rules.
pattn_download_sha256   "https://github.com/Chocolate4U/Iran-v2ray-rules/releases/download/${PATTN_IRAN_GEO_RELEASE}/geosite.dat"   "$OutputPath/bin/geosite.dat"   "$PATTN_IRAN_GEOSITE_SHA256"
pattn_download_sha256   "https://github.com/Chocolate4U/Iran-v2ray-rules/releases/download/${PATTN_IRAN_GEO_RELEASE}/geoip.dat"   "$OutputPath/bin/geoip.dat"   "$PATTN_IRAN_GEOIP_SHA256"

mkdir -p "$OutputPath/bin/srss"
for f in geosite-category-ir.srs geoip-ir.srs; do
  expected="$(pattn_raw_rule_blob_sha1 iran "$f")" || die "No locked Iran rule digest for $f"
  pattn_download_git_blob     "https://raw.githubusercontent.com/chocolate4u/Iran-sing-box-rules/${PATTN_IRAN_SING_RULES_COMMIT}/$f"     "$OutputPath/bin/srss/$f"     "$expected"
done

helper="$OutputPath/bin/pattn-discovery/pattn-discovery"
[[ -s "$helper" && -x "$helper" ]] ||
  die "Build artifact is missing executable pattn-discovery: $helper"

PackagePath="v2rayN-Package-${Arch}"
rm -rf "$PackagePath"
mkdir -p "$PackagePath/PattN.app/Contents/Resources"
cp -rf "$OutputPath" "$PackagePath/PattN.app/Contents/MacOS"
cp -f "$PackagePath/PattN.app/Contents/MacOS/v2rayN.icns" "$PackagePath/PattN.app/Contents/Resources/AppIcon.icns"
echo "When this file exists, app will not store configs under this folder" > "$PackagePath/PattN.app/Contents/MacOS/NotStoreConfigHere.txt"
chmod 0755 "$PackagePath/PattN.app/Contents/MacOS/PattN"
chmod 0755 "$PackagePath/PattN.app/Contents/MacOS/bin/pattn-discovery/pattn-discovery"

cat >"$PackagePath/PattN.app/Contents/Info.plist" <<-EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleDevelopmentRegion</key>
  <string>en</string>
  <key>CFBundleLocalizations</key>
  <array>
    <string>zh-Hans</string>
    <string>zh-Hant</string>
    <string>en</string>
    <string>fa</string>
    <string>fr</string>
    <string>ru</string>
    <string>hu</string>
  </array>
  <key>CFBundleDisplayName</key>
  <string>PattN</string>
  <key>CFBundleExecutable</key>
  <string>PattN</string>
  <key>CFBundleIconFile</key>
  <string>AppIcon</string>
  <key>CFBundleIconName</key>
  <string>AppIcon</string>
  <key>CFBundleIdentifier</key>
  <string>patterniha.PattN</string>
  <key>CFBundleName</key>
  <string>PattN</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleShortVersionString</key>
  <string>${Version}</string>
  <key>CSResourcesFileMapped</key>
  <true/>
  <key>NSHighResolutionCapable</key>
  <true/>
  <key>LSMinimumSystemVersion</key>
  <string>13.6</string>
</dict>
</plist>
EOF

create-dmg   --volname "PattN Installer"   --window-size 700 420   --icon-size 100   --icon "PattN.app" 160 185   --hide-extension "PattN.app"   --app-drop-link 500 185   "PattN-${Arch}.dmg"   "$PackagePath/PattN.app"
