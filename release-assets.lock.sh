#!/usr/bin/env bash
# PattN release-asset integrity lock.
# Update this file deliberately when bumping third-party release/runtime assets.
# Release archives/toolchains use repository-locked SHA-256 or SHA-512 digests.
# Raw GitHub content is pinned to an immutable commit and verified against the
# expected Git blob object id when a publisher SHA-2 digest is unavailable.

PATTN_CORE_BIN_COMMIT="5dab9a302a08001779a397e9e61ec78a9b56f884"

PATTN_XRAY_VERSION="26.9.27"
PATTN_SINGBOX_VERSION="1.14.2"

PATTN_ACTIONLINT_VERSION="1.7.12"
PATTN_ACTIONLINT_LINUX_AMD64_SHA256="8aca8db96f1b94770f1b0d72b6dddcb1ebb8123cb3712530b08cc387b349a3d8"

PATTN_IRAN_GEO_RELEASE="202609281111"
PATTN_IRAN_GEOSITE_SHA256="9db4bebeecf7b28cc22c3bff3892ffc5e1a119b326f22adb1723ce5ca14a50b8"
PATTN_IRAN_GEOIP_SHA256="9ca65ade22ea146559a90ac178be5c346eb67ffab7b121c25ee6e3c094bff88a"

PATTN_METACUBEX_RELEASE_COMMIT="a6544a371c34182ecec0363eafccd4ab3b93a58f"
PATTN_METACUBEX_GEOIP_METADB_BLOB_SHA1="7ab9c856defc89d56a44c85012c6d80c72d7b673"

PATTN_LOYALSOLDIER_GEOIP_COMMIT="46cf66bf178fee7c8d655de65ac34650fe712345"
PATTN_SING_RULE_GEOIP_COMMIT="619da1453857ca3ba37ffc567ce56a82e4fa7790"
PATTN_SING_RULE_GEOSITE_COMMIT="553443a98bf3c2064e04c1a1125e699ddfcf8ea4"
PATTN_IRAN_SING_RULES_COMMIT="5a5dbd60f033c0d777162835e9bd0a2c89a9316a"

PATTN_DOTNET_SDK_VERSION="10.0.111"
PATTN_DOTNET_SDK_LINUX_X64_SHA512="aae221be96a3b510d5b6fffefc69d8ad2fa595a1430299419316bb71c65f260a457ca9af24d044e1709b28a9118798caafec535ccfe58f7767c5acb735c00392"
PATTN_DOTNET_SDK_LINUX_ARM64_SHA512="1e115ddb850950d4514d6a3b32b2d17b240a4f0f40b37202df4e5bdf6832a0e546722e6bf9b9ed7df7cccb34df5f5e48bcb075322fb01815bffc6e9c23999f0e"

PATTN_RISCV_DOTNET_VERSION="10.0.111"
PATTN_RISCV_DOTNET_SHA256="118c4e1abdb3dbd365faf5963ba35fc0a97ff32421b2f6e358303e2be0e99420"

PATTN_LOONG_DOTNET_VERSION="10.0.111"
PATTN_LOONG_DOTNET_TAG="v10.0.111-loongarch64"
PATTN_LOONG_DOTNET_SHA256="a037a316e30d455d04e151ad5e5a27671a71e44a368b129986600e784fe906a9"

PATTN_LOONG_QEMU_VERSION="10.2.4"
PATTN_LOONG_QEMU_ARM64_V82_SHA256="83abea1dfa89cd65b8423272838e8683cb589e3bed9967117968a88227e1e2c1"

PATTN_DEBIAN_LOONG_IMAGE_VERSION="13.5"
PATTN_DEBIAN_LOONG_QCOW2_SHA256="9d320e0f400d813c8fa0c6ddd0a584f6ff785123de1c04d02f573c7c9ef71990"
PATTN_LOONG_EFI_CODE_SHA256="edd5a67fe50f7597faecb2fe67c5733b9a31b0b345a2127c3c358a5737446ef7"
PATTN_LOONG_EFI_VARS_SHA256="adbfcb31d6470ef090220baeb53559e26323b6cfb5e7614f879e89608a3ed748"

PATTN_FEDORA_LOONG_IMAGE_VERSION="43"
PATTN_FEDORA_LOONG_QCOW2_SHA256="8c233276f22c8a73dc74a9e5a0b7ee57411d6f716c08709333100920d9d4e0f9"

pattn_xray_sha256() {
  case "$1" in
    windows-32)       echo "8e605e38181216e4122662d6a075e073af6af6e60508912daf03a25f94186b1f" ;;
    windows-64)       echo "89bed4cc0dffe57ee221d9149c361ac70586ac85074a5e5c17b0deb50fdfd473" ;;
    windows-arm64)    echo "5bf2f93e82ad7470a214fe5b3ae8604bf5228cb2e5552d1b1b803bb84f9eeb77" ;;
    linux-x64)         echo "632d913cc2e702ad1f8e77352a087a5e501363bc950c85c1a1acf1e471894bed" ;;
    linux-arm64)       echo "bf93c8c01dc90fa87feb671c02118332531c8b63a9ca747cca8f35d4751b050d" ;;
    linux-riscv64)     echo "3a8d1f86e011d48edaca6922592365482722367b8b5dca008fd3dfae31eb2073" ;;
    linux-loongarch64) echo "b497b494376b0ba7ffce54c2e964e158dad619beaf98287799503bc88b2f3aae" ;;
    macos-64)          echo "567d1f9dc2371d6d635a7e36a07962abb325856e0fbabf41a841c106744ba1fa" ;;
    macos-arm64)       echo "e25b108dce636ff4829a31b856dc17b65e3ad60ba3f9f136483787acc1a38f15" ;;
    *) return 1 ;;
  esac
}

pattn_singbox_sha256() {
  case "$1" in
    windows-386)       echo "745b3ae034972244396414a54a1518aaa46fa55b6b9c4690dc7a4c2b71bac69b" ;;
    linux-x64)         echo "a684484d7477d1437282ee411f4d131d0340aaad60a7868841ebd5d87dd8a0c6" ;;
    linux-arm64)       echo "b43a1fb1bda131c6653576741ce527eb2bdeab7c9308ca90ee8b972abb7e4a7f" ;;
    linux-riscv64)     echo "7ee2d238081085a4047569b5a9f296763ede3f59742786a30a2ebc8a9f611e4f" ;;
    linux-loongarch64) echo "2dc49c812c95b48464469d413890642778738ea064209f853ed374d503e61e3d" ;;
    *) return 1 ;;
  esac
}

pattn_core_bundle_blob_sha1() {
  case "$1" in
    windows-64)       echo "0c562d227cbe5ae142aa550545001b143c3fc125" ;;
    windows-arm64)    echo "2fd08acc14c6b0520ef9036cadd8a7d1ca86d4d2" ;;
    linux-x64)         echo "23a310fd082ef02913d3ba18bc33d0d1a064f992" ;;
    linux-arm64)       echo "2d2dcaa6975366b7e7b83e34970fcf0888576134" ;;
    linux-riscv64)     echo "2546c78fbf9f886a92de947e1decd9018efecdee" ;;
    linux-loongarch64) echo "73a7aca1e2695df924fe1419dea53538dc231447" ;;
    macos-64)          echo "3b179595923b02f58e11a6a8db0c06bab6cd9183" ;;
    macos-arm64)       echo "deb2098430d4575b4870f0cf998f91ad1a25d825" ;;
    *) return 1 ;;
  esac
}

pattn_raw_rule_blob_sha1() {
  case "$1:$2" in
    loyal:geoip-only-cn-private.dat) echo "8164445e24396e5b84d4d9ec21e260f0f2bb9d11" ;;
    loyal:Country.mmdb) echo "69cecc0f0d0e03fa7414accf1e30735faf4eec8b" ;;
    geoip:geoip-private.srs) echo "ecb31e76f21047f4b7e572e4389f35969ab8ddb6" ;;
    geoip:geoip-cn.srs) echo "09176e09f2a476d53e3686e8cb7ed565e1ea656e" ;;
    geoip:geoip-facebook.srs) echo "a0271713b980837c5cbd20ad92da1c4f9e1ff069" ;;
    geoip:geoip-fastly.srs) echo "e0ac281cdab5e07c38f2b3d0df6e7b03092dec9e" ;;
    geoip:geoip-google.srs) echo "f7a30a31ec921251ed0bc5f73005f4820672d806" ;;
    geoip:geoip-netflix.srs) echo "d49d67dd246e0d20df77cb5dfcd15496e189913c" ;;
    geoip:geoip-telegram.srs) echo "461698b07bfd3eb17a65467a2278ecab53c6a85c" ;;
    geoip:geoip-twitter.srs) echo "c39416e7ecdc0d8c32078008962413111d9db9a3" ;;
    geosite:geosite-cn.srs) echo "1bb1fb9522b34b2779705729fecf53b77662aff7" ;;
    geosite:geosite-gfw.srs) echo "8468354dc3d5454d0e989bb1d9ef50abe7618bb4" ;;
    geosite:geosite-google.srs) echo "5a151aca569da3a69d607f1c26e5a45a9b782836" ;;
    geosite:geosite-greatfire.srs) echo "9dbe45df8bbafde7568d13ecc1113a1a355e5f91" ;;
    geosite:geosite-geolocation-cn.srs) echo "abbd44bd3a70384cfbef50fdc14c5bcbaf90484b" ;;
    geosite:geosite-category-ads-all.srs) echo "a9c61e246b3a5136863478ecf78dd6f87c26f02f" ;;
    geosite:geosite-private.srs) echo "3feaae63bbf22bdeb4f7f91ed5606c2035dab695" ;;
    iran:geosite-category-ir.srs) echo "cffdb525c5ea5011f1ce7f0b5df688bfe25e30d2" ;;
    iran:geoip-ir.srs) echo "a241ba972dcd43bbda5daa8af44e03710134a9fa" ;;
    *) return 1 ;;
  esac
}

pattn_sha256_file() {
  if command -v sha256sum >/dev/null 2>&1; then
    sha256sum "$1" | awk '{print $1}'
  else
    shasum -a 256 "$1" | awk '{print $1}'
  fi
}

pattn_sha512_file() {
  if command -v sha512sum >/dev/null 2>&1; then
    sha512sum "$1" | awk '{print $1}'
  else
    shasum -a 512 "$1" | awk '{print $1}'
  fi
}

pattn_git_blob_sha1() {
  local file="$1" size
  size="$(wc -c <"$file" | tr -d '[:space:]')"
  if command -v sha1sum >/dev/null 2>&1; then
    { printf 'blob %s\0' "$size"; cat "$file"; } | sha1sum | awk '{print $1}'
  else
    { printf 'blob %s\0' "$size"; cat "$file"; } | shasum -a 1 | awk '{print $1}'
  fi
}

pattn_verify_sha256() {
  local file="$1" expected="$2" actual
  actual="$(pattn_sha256_file "$file")"
  [[ "$actual" == "$expected" ]] || {
    echo "SHA-256 mismatch for $file: expected $expected, got $actual" >&2
    return 1
  }
}

pattn_verify_sha512() {
  local file="$1" expected="$2" actual
  actual="$(pattn_sha512_file "$file")"
  [[ "$actual" == "$expected" ]] || {
    echo "SHA-512 mismatch for $file: expected $expected, got $actual" >&2
    return 1
  }
}

pattn_verify_git_blob() {
  local file="$1" expected="$2" actual
  actual="$(pattn_git_blob_sha1 "$file")"
  [[ "$actual" == "$expected" ]] || {
    echo "Git blob mismatch for $file: expected $expected, got $actual" >&2
    return 1
  }
}

pattn_xray_asset_id() {
  # Pin the GitHub release asset object as an availability fallback in addition
  # to the content digest. If the ordinary browser download endpoint is
  # transiently unavailable, the REST asset endpoint returns the same reviewed
  # bytes. A digest mismatch never falls back and always fails closed.
  case "$1" in
    Xray-linux-64.zip)          echo "591812214" ;;
    Xray-linux-arm64-v8a.zip)   echo "591812088" ;;
    Xray-linux-riscv64.zip)     echo "591812511" ;;
    Xray-linux-loong64.zip)     echo "591812296" ;;
    Xray-macos-64.zip)          echo "591814122" ;;
    Xray-macos-arm64-v8a.zip)   echo "591811485" ;;
    Xray-windows-32.zip)        echo "591811435" ;;
    Xray-windows-64.zip)        echo "591812666" ;;
    Xray-windows-arm64-v8a.zip) echo "591814157" ;;
    *) return 1 ;;
  esac
}

pattn_download_sha256() {
  local url="$1" output="$2" expected="$3"
  local asset="" asset_id="" api_url=""

  rm -f "$output"
  if curl --fail --location --silent --show-error --retry 3 --retry-all-errors "$url" -o "$output"; then
    # A successful transport with unexpected bytes is suspicious: fail closed
    # rather than hiding it behind a fallback origin.
    if pattn_verify_sha256 "$output" "$expected"; then
      return 0
    fi
    rm -f "$output"
    return 1
  fi

  rm -f "$output"
  case "$url" in
    https://github.com/patterniha/Xray-core/releases/download/v${PATTN_XRAY_VERSION}/*)
      asset="${url##*/}"
      asset_id="$(pattn_xray_asset_id "$asset")" || {
        echo "No immutable Xray asset ID is locked for $asset" >&2
        return 1
      }
      api_url="https://api.github.com/repos/patterniha/Xray-core/releases/assets/$asset_id"
      echo "[!] Primary Xray release endpoint unavailable; retrying immutable asset $asset_id" >&2
      if ! curl --fail --location --silent --show-error --retry 3 --retry-all-errors \
        --header 'Accept: application/octet-stream' \
        --header 'X-GitHub-Api-Version: 2022-11-28' \
        "$api_url" -o "$output"; then
        rm -f "$output"
        return 1
      fi
      if ! pattn_verify_sha256 "$output" "$expected"; then
        rm -f "$output"
        return 1
      fi
      ;;
    *)
      return 1
      ;;
  esac
}

pattn_download_sha512() {
  local url="$1" output="$2" expected="$3"
  rm -f "$output"
  if ! curl --fail --location --silent --show-error --retry 3 --retry-all-errors "$url" -o "$output"; then
    rm -f "$output"
    return 1
  fi
  if ! pattn_verify_sha512 "$output" "$expected"; then
    rm -f "$output"
    return 1
  fi
}

pattn_download_git_blob() {
  local url="$1" output="$2" expected="$3"
  rm -f "$output"
  if ! curl --fail --location --silent --show-error --retry 3 --retry-all-errors "$url" -o "$output"; then
    rm -f "$output"
    return 1
  fi
  if ! pattn_verify_git_blob "$output" "$expected"; then
    rm -f "$output"
    return 1
  fi
}
