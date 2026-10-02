# PattN Discovery/Reviver audit reconciliation — 2026-10-02

This ledger is the durable reconciliation for the October 1–2, 2026 forensic reports covering PRs #1–#9. GitHub Issues are disabled in this repository, so unresolved/date-dependent items are tracked here instead of being silently dropped.

## Scope

Evaluated live heads at the time of reconciliation:

| PR | Head |
|---|---|
| #1 | `fbf8ce2a5a74f008f8f9cb1d360d6ac484e7ca05` |
| #2 | `d5d8557065a693410738a82b71e692fb352c17b5` |
| #3 | `808a72590eb1729e79206f90da38366fefe2f82f` |
| #4 | `d3c632f8b86bcf33eaf27b459c5f219f1d9094d4` |
| #5 | `e2ec05e320cfcbc5c3bf908f9a1f8a58277ce94d` |
| #6 | `d2c83c73c494a58e505c79da253d030fc9b121a3` |
| #7 | `127739ef41da6a8f697edc1828c683f9065848aa` |
| #8 | `14a09a9fd0c44d2bb32cf5dcfe903c8cf4ec4ab0` |
| #9 | `04d08f53d425373970c238c1e828cef7bf6dac60` |

The review re-checked source, current-head workflow state/logs, split-to-umbrella reconstruction, and the supplied audit reports. Stale report claims are not treated as current defects.

## Current conclusion

The core Discovery/Reviver architecture is no longer blocked by the headline defects in the October 1 reports. The remaining work is either (a) a current CI validation step for just-pushed fixes, (b) a trust/product design decision that cannot be safely invented in code, or (c) future/date-dependent validation.

## Closed / implemented

### PR #2 — Go discovery engine

- Bounded streaming concurrency with `server_busy`.
- Output failures cancel in-flight work and propagate from `run`.
- IPv4-mapped CIDR/native identity handling fixed.
- DNSSEC validation time is explicitly threaded through chain validation.
- IANA root trust anchors are consumed by the chain validator.
- DNS message parsing rejects trailing bytes.
- NSEC3 iteration work is capped.
- Extreme timeout values are range-checked before `time.Duration` multiplication.
- Oversized inbound NDJSON is bounded. The helper now emits a terminal `frame_too_large` protocol response before returning the scanner error, with a regression test.

### PR #3 — C# discovery services

- Helper stdout records use a bounded 4 MiB reader.
- Restart storms are constrained with failure backoff.
- Process-state reads tolerate disposal races.
- `DiscoveryEngineService` is async-disposable; the production Reviver UI owns it with `await using`.
- Candidate budgets are validated/clamped.
- Endpoint-pool metadata upsert preserves a pin unless pin state is explicitly supplied.
- Endpoint history / resolver telemetry indexes are installed.
- IPv4-mapped endpoint identities are canonicalized.

### PR #4 — sing-box WS metadata

- `ed` parsing uses invariant `NumberStyles.None`.
- `eh` is constrained to valid HTTP-token syntax.
- Existing unit/generator coverage remains the gate.

### PR #5 — assurance

- Fuzz corpus audit validates exact target argument types through Go AST inspection, not count only.
- The checked-in DNS root trust-anchor snapshot includes KSK-2017 (20326) and KSK-2024 (38696).
- Pre/post rollover policy tests exist.
- The scheduled IANA source-check workflow verifies IANA's detached CMS signature, compares exact XML to the embedded snapshot, retains evidence, and deliberately fails after the scheduled rollover until a new review updates the verification date.

### PR #6 — catalog trust/lifecycle

- Signed catalogs use a monotonic revision plus expiry and preserve a high-water mark across trust/source reconfiguration.
- Refresh/enable operations acquire the same operation lease used by apply/recovery.
- Remote transport has total/idle deadlines and bounded body reads.
- `DurableAtomicFile` records replacement immediately after the replace operation, before parent-directory fsync.
- Local file readers enforce byte limits while reading, not only via a pre-read size check.
- SQLite compaction rechecks under an exclusive application write gate.
- Registered provider catalogs are wired into production candidate composition.
- The residual non-cooperating-writer file CAS limitation is documented below; it is not a remotely exploitable catalog bug.

### PR #7 — Reviver/history

- Destructive history-policy removal re-checks current profile fingerprint and active/default selection under serialized mutation.
- Profile history uses a semantic versioned `fp2:` fingerprint.
- Cancellation persists a final cancelled outcome without converting it to failure evidence.
- Speed-test CTS/process cleanup cannot be skipped by UI/persistence errors.
- Diagnostic failure counts match the evidence used by consecutive-failure policy.
- `Prepare` rechecks the candidate's recorded runtime quorum and declared mutation fields.
- Promotion binds the source fingerprint, checks it before insert and again after insert, and compensates if the source changes during persistence.
- Rollback respects whether promotion made the child default and refuses to delete a later manual active selection.
- Missing/empty previous defaults fail before destructive rollback.
- Promotion/rollback history failures are surfaced as partial-success reconciliation errors instead of being silently swallowed.
- Candidate scoring neutralizes NaN/Infinity metrics.
- Temporary validator/measurement processes are stopped and disposed robustly.
- Support-bundle unknown keys/values are tokenized and tested for non-leakage.

### PR #8 — desktop UI

- DNS apply/rollback and history commands share busy/stale-plan gates.
- Avalonia JSON filter mismatch fixed.
- Numeric speed/history inputs are bounded in the view-model.
- New settings controls have accessible names in both WPF and Avalonia.
- Umbrella #1 was repaired to include #8's resource-backed DNS repair strings and the two missing Avalonia `.DisposeWith(disposables)` bindings.
- Persian coverage is substantial; full locale parity is still open below.

### PR #9 — release engineering

- ZIP/DEB/RPM/DMG external assets are pinned to repository locks/digests; mutable `latest`/branch release inputs are forbidden by CI.
- PR package builds use the checked-out source instead of falling through to an upstream tag.
- Release callers are read-only by default; write permission is scoped to publishing jobs.
- Checkout credentials are not persisted in packaging jobs.
- Third-party GitHub Actions references are full-SHA pinned and CI-audited.
- `global.json` and Go toolchain are exact-pinned for release gates.
- macOS DMGs include and smoke the packaged discovery helper; release builds support Apple signing/notarization/stapling.
- Release upload has a valid tag regex and `GH_REPO`.
- Release output includes SHA-256 inventories, SPDX metadata, SLSA-style provenance metadata, and detached signatures.
- ZIP packaging has an explicit deterministic two-build self-check using the source commit timestamp.
- The current-head RPM failure was reproduced from logs: UBI10 lacked ICU before the locked .NET SDK started. `libicu` is now installed in the RHEL build path and declared as an RPM runtime dependency for x64/arm64, RISC-V, and LoongArch packages.

## Umbrella reconstruction check

Every changed path from split PRs #2–#9 exists in umbrella #1. Blob differences were re-reviewed individually. The remaining differences are intentional integration supersets (for example catalog tables/indexes, serialized SQLite maintenance, option clamps, exact Go toolchain pinning, and stronger support-bundle assertions), not missing split changes.

During this check, five real omissions were found and repaired in #1:

1. provider remote-source revision test semantics for removed sources;
2. localized/resource-backed DNS repair status text;
3. two missing Avalonia binding disposals;
4. Avalonia option-control accessible names;
5. WPF option-control accessible names.

## Findings that were rejected or downgraded

- Do not add HMAC secrets to anonymous parent/child stdio as an IPC "authentication" layer. The useful controls are bounded framing, strict schema/versioning, request IDs, concurrency limits, cancellation, deadlines, and process isolation. A symmetric secret exposed to the same compromised local process boundary does not solve the strongest local attacker model.
- Go DNS TCP/DoT short-write handling was a false positive: an `io.Writer` must return a non-nil error for a short write.
- Retention value zero in the proxy-test policy is an explicit "keep indefinitely" contract, not an accidental overflow bug.
- The old FaultHarness no-argument finalizer finding was false.
- A mandatory short timeout for manual fuzz campaigns was rejected; ordinary GitHub job limits plus campaign intent make that a policy choice, not a defect.
- Do not ship a static anecdotal Cloudflare/Anycast "bad IP" blocklist.
- Do not auto-apply FinalMask/fragmentation or carrier-specific mutations merely from carrier/domain labels. New Reviver strategies remain behind `REVIVER_STRATEGY_EVIDENCE.md`.

## Strategic audit items already present (do not duplicate)

The later "network intelligence" reports describe several capabilities that already exist under different names:

- rich typed failure classification in `ERepairFailureClass`;
- immutable profile snapshots and bounded candidate mutations;
- endpoint pool/history and endpoint timeline queries;
- per-strategy promotion/rollback history;
- improved/stable/regressed outcome summaries;
- explainable candidate scoring (reliability, stability, loss, latency, mutation safety/simplicity, resolver quality);
- lifecycle retention policies for DNS repair history, resolver telemetry, promotion history, endpoint observations, remote provenance, and source revisions.

Future work should extend these primitives instead of creating parallel stores/classifiers.

## Explicitly open / not falsely marked fixed

### 1. Portable runtime-update authenticity

**Status: open security design.**

The hardened release pipeline publishes digests/signatures/provenance, but the legacy portable updater still downloads application/core/geo update payloads without verifying a client-pinned trust root. A checksum fetched from the same release metadata is not sufficient authentication.

A complete fix requires an explicit update trust design:
- pinned public verification key or equivalent independently anchored trust root;
- signed update manifest containing version, artifact digest, minimum accepted version, timestamp/expiry, and platform/architecture;
- rollback/downgrade policy;
- fail-closed verification before the archive reaches `AmazTool.UpgradeApp`;
- migration/key-rotation procedure.

Do not claim this fixed until the client trust root is deliberately selected and shipped. Packaged installs already disable PattN self-update; portable installs remain affected.

### 2. Full release reproducibility beyond ZIP

**Status: partially complete.**

ZIPs are deterministic and self-checked. Toolchains and external inputs are pinned, and release provenance captures the source SHA/workflow. DEB/RPM/DMG have integrity/provenance controls but are not yet proven byte-for-byte reproducible across independent builders. A future reproducibility gate should normalize package timestamps/build metadata and compare independent-builder hashes.

### 3. Exotic toolchain provenance

**Status: residual supply-chain risk, integrity mitigated.**

RISC-V/LoongArch SDK/images are digest-pinned before use. The RISC-V .NET SDK distribution point is still a personal/external repository. Prefer official vendor artifacts when available; otherwise mirror the reviewed bytes into organization-controlled immutable storage without weakening current digest checks.

### 4. Runtime/history privacy controls

**Status: open product/privacy design.**

Support-bundle export is tokenized, but local history legitimately stores operational endpoint/profile identifiers required for diagnostics. Before adding network fingerprints, carrier learning, sync, or sharing:
- define a local-only/private/anonymous policy;
- prohibit credentials, UUIDs, subscription URLs, and raw support-bundle secrets from intelligence features;
- prefer opaque keyed tokens for correlation;
- expose clear/disable controls;
- preserve retention bounds.

No global/anonymous sharing or multi-device sync should be implemented before this policy exists.

### 5. Context-aware network intelligence / uplink-stall experiments

**Status: evidence-gated future work.**

Some field/research evidence supports stateful/asymmetric blocking patterns, including flows that survive handshake but fail after a small packet budget. That does not justify a universal "six packets", carrier rule, exact FinalMask offset, or static IP policy.

The next safe implementation is a controlled bidirectional experiment against a cooperating endpoint, recording:
- handshake result;
- acknowledged upload byte/chunk count;
- downstream response;
- failure phase;
- IPv4/IPv6;
- repeated trials and time window.

Only after reproducible differentiation from congestion, packet loss, server overload, and route failure should a new failure subtype/evidence type feed Reviver. Any mitigation candidate must still pass real-core validation and the existing strategy evidence gate.

### 6. Strategy learning / rollback intelligence

**Status: foundation implemented, context learning open.**

Per-strategy promotions, rollbacks and outcome verdicts are already persisted. Do not create a second strategy-history database. Future ranking may add recency decay and privacy-preserving context buckets, but current live validation must remain authoritative; historical similarity alone must never promote a repair.

### 7. Localization parity

**Status: non-blocking UX follow-up.**

User-visible Discovery/Reviver/DNS-repair strings should all be resource-backed and translated across supported cultures. Persian is substantially covered; other cultures still rely on English fallback and some Discovery-management status/error summaries remain hardcoded. Add resource-key/placeholder parity CI when completing this work.

### 8. DNS root KSK rollover live review

**Status: date-gated, not executable before the event.**

The repository already contains KSK-2024 and a scheduled authenticated IANA source check. On/after 2026-10-11, run the scheduled source check against the live post-rollover IANA state, retain the evidence artifact, and update the verified date only after review. The workflow is intentionally fail-closed after the rollover date until this happens.

### 9. Non-cooperating local-writer atomic CAS

**Status: documented platform limitation.**

The application write gate/sidecar leases protect cooperating PattN writers. There is no portable filesystem primitive in the current design that turns arbitrary third-party writers into an atomic compare-and-swap with replacement. Keep this in the local attacker/host-integrity threat model; do not present it as a remote catalog vulnerability.

## CI validation status

The new commits retrigger current-head CI. A previously current #9 RPM run failed only because ICU was absent in UBI10; the failure was reproduced from the job log and patched as described above. New current-head runs are the final Tier-1 validation gate and may still be queued depending on runner availability.

No branch should be merged solely on this ledger if its current-head required check is red.

## Merge/review discipline

- Treat the split PR current heads as canonical for their subsystem.
- Keep umbrella #1 as the integrated superset; any split fix must be mirrored or intentionally superseded.
- Do not resurrect rejected audit patches from stale blobs.
- Do not add carrier/DPI mitigation strategies without a reproducible fixture and real-core validation.
- Do not weaken digest/signature/source-pinning checks to make an exotic build pass.
- Do not mark the 2026-10-11 KSK review complete before authenticated post-rollover evidence exists.
