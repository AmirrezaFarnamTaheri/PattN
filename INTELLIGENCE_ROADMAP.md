# PattN Network Intelligence Roadmap

This document reconciles the Discovery/Reviver forensic audits and the follow-on intelligence brainstorm against the current implementation.

## Design rules

1. **Evidence before mutation.** Discovery measures; Reviver decides and mutates.
2. **Probabilistic diagnosis.** Network interference/DPI is never asserted from one symptom. Classifiers return confidence and reasons.
3. **Privacy by construction.** Learning keys are derived fingerprints/genomes. Raw credentials, UUIDs, subscription content, domains, and endpoint addresses are excluded from strategy-learning records.
4. **Fail-closed automation.** Automatic actions require explicit confidence and mutation-risk gates. Uncertain recommendations remain informational.
5. **Age matters.** Historical evidence decays and raw data is rolled up/pruned.
6. **User intent wins.** Promotion/rollback and destructive policy operations reject stale state and newer user choices.

## P0 — release and correctness blockers

| Item | Status | Implementation |
|---|---|---|
| Locked release assets | Implemented | `release-assets.lock.sh`; digest/blob verification across package paths |
| ZIP integrity lock | Implemented | `package-zip.yml` consumes locked assets |
| RHEL SDK mismatch | Implemented | RHEL packager installs exact locked .NET SDK rather than distro floating SDK |
| Source-pinned PR packaging | Implemented | PR packaging keeps checked-out source |
| macOS helper packaging/smoke | Implemented | final DMG checks executable `pattn-discovery` |
| macOS code signing/notarization | Source support implemented; credentials required | release workflow imports signing cert, signs with hardened runtime, notarizes and staples |
| Signed checksums | Implemented | release upload emits and signs SHA-256 manifests |
| SBOM | Implemented | release upload emits and signs SPDX 2.3 inventory |
| Provenance | Implemented | release upload emits and signs in-toto/SLSA-shaped provenance |
| Runtime update verification | Implemented primitive | ECDSA-signed manifest + artifact SHA-256 + freshness + minimum-version/downgrade checks |
| Action pinning / least privilege | Implemented on current release stack | workflow actions pinned; PR permissions read-only by default |
| Catalog rollback protection | Implemented | signed monotonic revision, expiry and independent high-water mark |
| Helper supervision/framing | Implemented | bounded line size, restart backoff, stream/unary concurrency caps |
| Reviver stale promotion/rollback | Implemented | source fingerprint revalidation, quorum/mutation checks, newer-user-choice protection |

## P1 — highest ROI intelligence

| Brainstorm item | Status | Implementation |
|---|---|---|
| Failure classification engine | Implemented | `NetworkIntelligenceService.Classify` with confidence/reasons |
| Network fingerprinting | Implemented | privacy-safe `NetworkFingerprint` |
| Proxy genome | Implemented | protocol/transport/core/security/address-family traits without raw endpoint identity |
| Strategy effectiveness history | Implemented | SQLite strategy outcomes + time decay + human-evidence weighting |
| Explainable repair | Implemented | `RepairExplanation` |
| Evidence decay | Implemented | configurable confidence half-life |
| Learning from rollback | Implemented | promotion/rollback feed actual outcomes into strategy learning |
| Uplink-stall evidence | Implemented | controlled bounded HTTPS upload probe through Discovery RPC |
| Human feedback | Implemented | privacy-safe feedback store |
| History growth control | Implemented | hot raw retention + daily aggregate rollups |

## P2 — advanced automation

| Brainstorm item | Status | Implementation |
|---|---|---|
| Active-learning experiment engine | Implemented | information-gain/cost/risk ordered hypotheses |
| Strategy conflict resolution | Implemented | higher-scored candidate wins conflicting target fields; conflict ledger returned |
| Shadow/canary testing | Implemented | candidate vs baseline score with minimum-improvement gate |
| A/B comparison | Implemented by shadow primitive | repeatable sample sets, success + latency score |
| Endpoint lifecycle | Implemented | Birth/Peak/Degrading/Dead/Revived classification |
| Carrier intelligence | Implemented backend | carrier/ASN aggregate profiles and confidence |
| Incident timeline/replay | Implemented backend | ordered typed timeline events |
| Automatic regression detection | Implemented | recent success-rate/latency risk signals |
| Fleet management intelligence | Implemented backend | healthy/degraded/dead/duplicate genome accounting |
| Automatic optimization | Implemented | score + confidence + mutation-risk gate; unsafe cases remain advisory |
| Internal observability | Implemented | counters and numeric averages for intelligence workflows |
| Architecture health | Implemented backend | test/package/durability/security/dependency health snapshot |

## P3 — privacy-safe platform features

| Brainstorm item | Status | Implementation / boundary |
|---|---|---|
| Anonymous intelligence records | Implemented | derived network/genome keys only |
| Sharing privacy model | Implemented | explicit opt-in + AnonymousAggregate mode + minimum cohort size |
| Global intelligence transport | Interface/data contract ready; service backend external | no hard-coded third-party collection endpoint |
| Heatmaps | Implemented backend | coarse country + hashed carrier aggregates |
| Knowledge graph | Implemented backend | environment → failure → strategy weighted edges |
| Multi-device sync | Implemented primitive | AES-GCM encrypted anonymous-record payload |
| Predictive failure detection | Implemented | bounded heuristic risk signal with explanations |
| Release security dashboard | Implemented backend model | digest/signature/provenance assessment |
| Developer health dashboard | Implemented backend model | architecture health snapshot |

## Deliberately not hard-coded as truth

### `finalMask` or carrier-specific bypass presets

The field reports are valuable hypotheses, but fixed packet thresholds, exact fragment offsets, and carrier-wide claims require reproducible captures on the affected networks. PattN now has an uplink evidence probe and confidence-bearing diagnosis path. A transport mutation such as a fragmentation preset should enter `ReviverStrategyCatalog` only after the existing strategy-evidence gate is satisfied.

### Static Anycast “bad IP” lists

The intelligence layer supports endpoint lifecycle and failure history. A permanent baked-in blocklist is intentionally avoided because Anycast health is time-, route-, and carrier-dependent. Evidence-backed local quarantine is the safer architecture.

### Global collection service

The client enforces explicit opt-in and anonymous aggregation, but no remote service URL is embedded. Operating such a service requires a separate privacy/security design, retention policy, abuse controls, and deployment ownership.

## External evidence / credential gates

These cannot be completed by source changes alone:

- **Apple release credentials:** Developer ID certificate and App Store Connect notarization key secrets must be configured for signed/notarized release execution.
- **Real carrier/DPI validation:** the uplink-stall and fragmentation hypotheses require controlled captures across affected networks; generic CI runners cannot establish carrier-specific behavior.
- **Root KSK rollover:** the scheduled 2026-10-11 event must be re-audited after the real transition; pre-rollover synthetic fixtures are not equivalent to live evidence.
- **Accessibility conformance:** real WPF/Avalonia release builds must be exercised with UI Automation/AT-SPI/VoiceOver as specified by the repository protocol.
- **Localization:** the Discovery/Reviver key set materially exceeds translations in Persian, Russian and Simplified Chinese. Neutral-resource fallback remains functional, but language-quality completion requires reviewed translations.
- **External Cloudflare Worker projects:** fixes to third-party Worker repositories are outside PattN's repository boundary and should be submitted upstream separately.

## Merge discipline

The intelligence branch is additive and should remain a separate PR until:
1. Code Test / Go test / race checks are green on the exact head.
2. Release workflow lint passes after SBOM/provenance and macOS-signing changes.
3. The RHEL package job proves the locked-SDK fix.
4. New intelligence persistence migrations pass upgrade/restart tests.
5. UI exposure is added incrementally, preserving explicit user control for destructive actions.
