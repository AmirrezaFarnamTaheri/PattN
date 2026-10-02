# PattN Discovery/Reviver audit reconciliation — 2026-10-02

This ledger is the durable reconciliation for the October 1–2, 2026 forensic reports covering PRs #1–#9. GitHub Issues are disabled in this repository, so unresolved/date-dependent items are tracked here instead of being silently dropped.

## Scope

Evaluated live heads at the time of reconciliation:

| PR | Head |
|---|---|
| #1 | `7d7998a93d31863f8f498eb961454108d4ee53fd` |
| #2 | `ed133216a8861e590d44c45262bf68f871153241` |
| #3 | `38edfca8e1bc3d87f16341b36a70c886e8608245` |
| #4 | `d3c632f8b86bcf33eaf27b459c5f219f1d9094d4` |
| #5 | `ea6d0bce0c4532293cd6bfcf6ed1250a239c8b2a` |
| #6 | `08624454d5ddd12c2ac9af37aeea7e123c8e07d6` |
| #7 | `9c9880072316b6a3ecee8207b4b14202d2940665` |
| #8 | `41df4409f65048a2ed6c9e2337b47257ef5175f2` |
| #9 | `cdfa6fde9c4282a687a03fec5c688078c3f371bb` |

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
- Network-intelligence sharing is explicitly policy-gated: `LocalOnly` is the default and produces no shareable payload; anonymous aggregate events exclude raw endpoint/profile identifiers, bucket confidence, sanitize strategy labels, and omit network fingerprints unless explicitly allowed.
- Strategy outcome history now includes recency-weighted learning and stronger weight for explicit human confirmation without creating a second history database.
- The optional upload-stall experiment sends random non-secret bytes only to an operator-configured HTTP(S) endpoint, reports the narrower `UploadStall` signal, and never labels a timeout as DPI/censorship by itself.

### PR #8 — desktop UI

- DNS apply/rollback and history commands share busy/stale-plan gates.
- Avalonia JSON filter mismatch fixed.
- Numeric speed/history inputs are bounded in the view-model.
- New settings controls have accessible names in both WPF and Avalonia.
- Umbrella #1 was repaired to include #8's resource-backed DNS repair strings and the two missing Avalonia `.DisposeWith(disposables)` bindings.
- The evidence-gated upload probe/finalMask controls are exposed in both WPF and Avalonia, validated for HTTP(S)/payload/timeout/template bounds, and localized in English/Persian.
- Discovery empty states and promotion/revision summaries that had duplicate hard-coded English now use resource-backed strings.
- Persian coverage is substantial; full human-reviewed translation parity across the other supported cultures is still open below.

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
- DEB/RPM builds now normalize `SOURCE_DATE_EPOCH`, payload mtimes, tar ordering/ownership, gzip headers and RPM build metadata; pull-request CI performs a second native-package build and requires byte-for-byte equality.
- macOS pull-request CI now rebuilds the unsigned `PattN.app` payload twice and compares deterministic ZIPs. Signed/notarized DMGs are intentionally treated as trust-bearing containers rather than byte-for-byte reproducibility targets because timestamped code-signing/notarization material is expected to differ.
- RISC-V/LoongArch SDK origins are documented in `EXOTIC_TOOLCHAIN_PROVENANCE.md`; release operators can redirect both SDKs to organization-controlled immutable mirrors while retaining the reviewed digest locks.

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

**Status: repository-controlled payloads substantially complete.**

ZIPs are deterministic and self-checked. DEB/RPM now normalize source/build timestamps and metadata and are rebuilt twice in pull-request CI with byte-for-byte comparison. macOS pull-request CI performs the same two-build comparison on the unsigned `PattN.app` payload using deterministic ZIPs.

The final signed/notarized DMG is not treated as a byte-for-byte reproducibility target: Apple code signing, secure timestamps, notarization and stapling intentionally inject trust material that can vary between otherwise identical payloads. The reproducibility contract is therefore the unsigned app payload plus signed-release provenance/checksums, not equality of the post-notarization container.

### 3. Exotic toolchain provenance

**Status: repository-side controls complete; upstream availability remains external.**

RISC-V/LoongArch SDK/images remain digest-pinned before use. Microsoft still does not publish the required .NET 10 Linux SDKs for these architectures, so the reviewed external/vendor origins cannot be replaced with a Microsoft artifact today. `EXOTIC_TOOLCHAIN_PROVENANCE.md` records the provenance and risk, and both package families accept organization-controlled immutable mirror overrides without changing the reviewed digest.

When an official vendor artifact becomes available, replacing the bootstrap origin remains a normal dependency migration rather than an untracked audit defect.

### 4. Runtime/history privacy controls

**Status: privacy boundary implemented; no remote sharing transport exists.**

The intelligence model now has an explicit `IntelligencePrivacyPolicy` with `LocalOnly` as the default. In that mode no shareable learning event is produced. The only approved anonymous event shape excludes credentials, UUIDs, subscription URLs, endpoint hostnames/IPs and raw support-bundle secrets; strategy labels are sanitized/hashed when needed, confidence is bucketed, timestamps are reduced to a UTC day, and network fingerprints are opt-in even inside anonymous aggregate mode.

Operational local history still stores identifiers needed for local diagnostics and rollback; those records remain subject to lifecycle-retention/maintenance controls. No global sharing, sync or upload transport has been added.

### 5. Context-aware network intelligence / uplink-stall experiments

**Status: safe bounded experiment implemented; transport-level ACK introspection intentionally out of scope.**

PattN now supports an operator-configured HTTP(S) upload probe through the temporary candidate proxy. It sends random non-secret bytes, bounds payload and timeout, distinguishes HTTP/application failure from timeout, repeats through the existing runtime-attempt policy, and emits only the narrower `UploadStall` classification after application reachability. The optional Xray finalMask candidate is local/operator-supplied and must pass the same real-core validation quorum before promotion. Both WPF and Avalonia expose these settings with validation.

This deliberately does **not** infer carrier/DPI/censorship, does not encode a universal packet count or static IP rule, and does not claim transport-level acknowledged-byte telemetry that `HttpClient` cannot supply. Any deeper packet/ACK experiment still requires a cooperating endpoint/protocol fixture and separate evidence review.

### 6. Strategy learning / rollback intelligence

**Status: recency/human-feedback learning implemented; live validation remains authoritative.**

Per-strategy promotions, rollbacks, automatic outcome verdicts and explicit human confirmation are persisted in the existing history. Summaries now compute recency-weighted success and a learning rate that gives explicit human confirmation higher weight. The experiment planner remains bounded/conflict-aware.

Historical/context similarity is intentionally **not** allowed to promote an unvalidated repair or override live real-core evidence. Privacy-preserving context buckets may later be used only as a bounded ordering/tie-break signal if a reproducible fixture demonstrates value.

### 7. Localization parity

**Status: resource safety implemented; human translation parity remains open.**

Resource-key/placeholder parity CI exists. New Reviver/upload-probe controls and the duplicated Discovery empty-state/revision/promotion summaries are resource-backed, with Persian translations. The remaining supported cultures still rely on neutral English fallback for many of the newer Discovery/Reviver keys.

Do not auto-fill those files with unreviewed machine translations merely to make a percentage reach 100%; completion here requires human-reviewed translations for Azerbaijani, French, Hungarian, Indonesian, Russian, Simplified Chinese and Traditional Chinese.

### 8. DNS root KSK rollover live review

**Status: date-gated; follow-up scheduled for 2026-10-11.**

The repository already contains KSK-2024 and a scheduled authenticated IANA source check. On/after 2026-10-11, run the scheduled source check against the live post-rollover IANA state, retain the evidence artifact, and update the verified date only after review. The workflow is intentionally fail-closed after the rollover date until this happens.

### 9. Non-cooperating local-writer atomic CAS

**Status: documented platform limitation.**

The application write gate/sidecar leases protect cooperating PattN writers. There is no portable filesystem primitive in the current design that turns arbitrary third-party writers into an atomic compare-and-swap with replacement. Keep this in the local attacker/host-integrity threat model; do not present it as a remote catalog vulnerability.

## Stack gates (2026-10-02, second pass)

The three static gates that run in `.github/workflows/stack-audit.yml` now encode one incident each.
All of them are stdlib-only, take seconds, and are meant to be run before pushing:

### `scripts/check_stack_sync.py`

1. **base ancestry** — a split PR must contain the tip of the branch it targets; otherwise its review is
   not about the code that would merge.
2. **payload parity** — the files a split PR changes must carry the integration ref's blob.
3. **shared-slice parity** *(added this pass)* — for `v2rayN/ServiceLib/Reviver/`, a split PR that carries a
   file must carry the integration ref's blob, and `split/final-integration-base` (declared to carry the
   whole slice) must have no missing or differing file. A split owning a subset is reported, not failed.

Why (3) exists: `dff02073` merged `74ccbe88` but resolved only the service files to the incoming side,
rewinding three Reviver model files to their pre-merge blob — 17 lines, among them `IntegritySuspect` and
`MeetsQuorumWithoutIntegrityDoubt`, i.e. the model members of the loopback-trust hardening — while keeping
every service that calls them. `split/final-integration-base`, #8 and #9 all stopped compiling, twice
annotated by CI (`RepairPromotionService.cs:40/72/74/97/100/124/127`, `RepairValidationAccumulator.cs:72`,
CS1061). Checks (1) and (2) reported `drift=0` throughout: the rewound files were in no PR's payload.
`bfe7154c` repaired the base; check (3) is what fails if it happens again.

Controls, executed (`--check-slice-of` is the one-ref entry point):

| command | result |
|---|---|
| `--check-slice-of dff02073` | 7 gaps, exit 1 |
| `--check-slice-of 85c2b4a6` (broken #8 head) | 7 gaps, exit 1 |
| `--check-slice-of origin/split/final-integration-base` | 0 gaps, exit 0 |
| `--full-payload` (13 open PRs) | all ok, exit 0 |
| `--slice-base-ref dff02073` | base FAIL, exit 1 |

The workflow also runs the negative control on every build (`Shared-slice check has not gone dead`): it
asserts that `dff02073` is still flagged, and says so instead of passing silently if that commit ever
disappears.

### `scripts/check_localization.py`

Resource-key/placeholder parity across `ResUI*.resx`. Resolution note for the Persian file: it is not
uniformly indented, and branch and upstream disagree only by *adding* keys, so conflicts are resolved as a
key-name union (integration side wins on a name collision) and then validated with `xml.etree`
(entry count + duplicate check) rather than by hand.

### `scripts/check_test_assertions.py`

1. no message argument on a no-argument TUnit assertion (`BeTrue("why")` does not compile; the repository
   spelling is `.Because("why")`);
2. no `await` in a non-async method (CS4032), counting method-body depth so awaits inside lambdas are not
   false positives.

Covers `ServiceLib.Tests` (141 files) and `AmazTool` tests (5), with controls recorded in its commit message.

### Lint lesson (cost one CI cycle)

`git cat-file -e <sha>^{commit}` in a `run:` block is flagged by shellcheck (SC1083, literal braces), and
actionlint runs shellcheck over every `run:` block — so the control step took down `Validate GitHub Actions
workflows`. `git cat-file -e <sha>` needs no peel suffix. After the fix, `Validate` is green on `c467a49b`;
note that `git rev-parse --verify --quiet 0000…0` exits 0 (it accepts the null object name) and would have
made the control silently skip, so it is not an acceptable substitute.

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
