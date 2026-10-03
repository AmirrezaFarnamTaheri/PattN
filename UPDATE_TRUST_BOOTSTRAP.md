# PattN runtime update trust bootstrap

PattN intentionally blocks the legacy application/core/geo runtime updater until an independently anchored trust root is enrolled. HTTPS plus a SHA-256 digest returned by the same release origin protects against accidental corruption, not a compromised release account/origin.

## Required re-enable contract

Runtime network updates may be re-enabled only after all of these are implemented and reviewed:

1. **Pinned trust root** — a public verification key (or equivalent root) is shipped in the already-installed client. The private signing material must not be stored in the repository.
2. **Signed canonical manifest** — the signature covers at least schema/version, release version, artifact name/platform/architecture/SHA-256, a downgrade floor, publication time, expiry, and key/rotation metadata.
3. **Fail-closed verification** — signature, freshness, platform/architecture binding, downgrade policy and artifact digest are verified before any archive reaches `AmazTool.UpgradeApp` or core/geo installers.
4. **Key rotation** — a new key is accepted only through a rotation statement authorized by an already-trusted key, or through an explicitly reviewed application release.
5. **Release CI** — publishing fails if the signed manifest is absent, malformed, expired, or does not cover every runtime-updatable artifact.
6. **Negative tests** — wrong key, modified manifest, modified asset, expiry, replay/downgrade, wrong platform/arch and unknown/revoked key all fail closed.

## Release provenance versus updater trust

Release CI now emits GitHub OIDC-backed build attestations in addition to checksums/SBOM/provenance files. These authenticate the build/repository identity for operators and release verification, but they are **not** used as an implicit in-app update trust root. The installed client still has no deliberately enrolled key/identity policy for verifying a future update manifest, so runtime self-update remains fail-closed.

## Current state

`RuntimeUpdateTrustPolicy.BlockUnauthenticatedLegacyUpdater` is deliberately `true`. Application, core and geo runtime network update entry points return a localized trust error without performing a download. Subscription refresh and other non-`UpdateService` network features are unaffected.

This closes the unauthenticated execution path by disabling it; it does not claim that authenticated self-update is already implemented.
