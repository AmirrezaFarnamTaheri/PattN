# Discovery + Reviver audit remediation status

This file reconciles the 2026-10-01/02 forensic reviews with the current integration stack.
It distinguishes implemented findings, rejected/stale hypotheses, date-gated verification, and
work that requires infrastructure or evidence outside this repository.

## Implemented correctness and security findings

- Discovery protocol/output failure cancellation, bounded streaming concurrency, bounded request/response framing, timeout-overflow checks, explicit oversized DoH rejection, and deterministic DNSSEC validation time.
- DNSSEC NSEC3 iteration work cap and strict DNS-message trailing-byte rejection.
- Discovery helper restart backoff, disposal-safe process state, bounded NDJSON reads, candidate-budget limits, endpoint identity normalization, pin-preserving endpoint upserts, and history/telemetry indexes.
- Provider/ASN lifecycle operation leases, bounded local/remote reads, remote total/idle deadlines, serialized maintenance, durable replacement compensation, signed catalog monotonic revision + expiry, and persisted anti-rollback watermarks including source removal.
- Reviver source-fingerprint revalidation, active-profile deletion serialization, rollback/default integrity, validator disposal, cancelled final outcomes, semantic profile fingerprint v2, quorum revalidation, non-finite metric hardening, and explicit history-persistence uncertainty.
- Desktop destructive-command gating, bounded settings, accessibility names, disposed visibility bindings, and Persian localization of Discovery/Reviver safety flows.
- Release source pinning, immutable asset locks, immutable GitHub Action/toolchain pins, least-privilege workflow permissions, deterministic ZIPs, signed checksums/SBOM/provenance, runtime update digest binding, updater archive traversal guards, and mandatory macOS signing/notarization for release publication.
- macOS packages preserve and smoke-test the executable pattn-discovery helper.
- PR assurance includes fuzz/corpus gates, abrupt-loss durability, IANA root-anchor source verification, and cross-platform packaging CI.

## Implemented intelligence foundations

- Evidence-confidence assessment with age decay.
- Privacy-preserving proxy genome and network-observation fingerprints.
- Explainable repair recommendations.
- Explicit upload-stall failure classification and bounded in-band upload probe.
- Evidence-gated, operator-configured upload-stall mitigation; no carrier-specific fragment recipe is embedded.
- Recency-weighted strategy effectiveness and human-confirmed outcome weighting.
- Local-only intelligence by default and privacy-reviewed anonymous aggregate event models.
- Privacy-safe operational repair metrics.
- Structured human repair feedback.
- Conflict-aware repair experiment planning that never merges incompatible mutations and only reorders normal validation arms.
- Repair incident replay timeline.
- Endpoint lifecycle projection (New / Healthy / Degraded / Dead / Recovered) from observed local evidence.
- Per-subscription fleet health where each proxy remains an independent profile and history row.

## Rejected or stale findings

- **HMAC over parent/child anonymous pipes:** not adopted. Anonymous stdio pipes are already kernel-scoped to the spawned process relationship; an environment-secret HMAC does not protect against an attacker powerful enough to attach to either process. Robust framing, schema checks, limits, and supervisor behavior are the relevant controls.
- **Static carrier-specific finalMask recipe:** not embedded. Field-chat parameters are observations, not a universal invariant. PattN accepts an operator-configured template only after an in-band upload-stall signal and still requires normal real-core quorum validation.
- **Static Anycast blocklist from chat reports:** not embedded. Address quality is represented by observed endpoint history/lifecycle so stale or network-specific IP claims cannot become permanent global policy.
- **Old packaging claims (unchecked downloads, macOS helper omission, mutable latest assets, PR source switching, write tokens):** superseded by the current release stack and green split-PR packaging runs.
- **MIN_KERNEL=6.12 blocker:** superseded; current package scripts no longer use that heuristic and CI guards against reintroduction.
- **“All locale files must copy new English keys”:** neutral-resource fallback is intentional. Persian safety/repair coverage is implemented; other languages require real translation rather than duplicated English strings.

## External / evidence-dependent follow-ups

These cannot be truthfully completed by client-only changes and remain intentionally gated:

1. **Anonymous global intelligence service / heatmaps / knowledge graph**
   - Requires a separately deployed service, abuse controls, schema governance, retention/deletion policy, minimum-cohort privacy rules, and independent privacy/security review.
   - Client sharing must remain opt-in; no raw proxy credentials, domains, IPs, SNI, subscription URLs, or stable user identifiers may be uploaded.

2. **Encrypted multi-device sync**
   - Requires device identity, key management, revocation, conflict semantics, and a server or user-owned synchronization substrate.
   - Local-only operation must remain fully supported.

3. **Carrier/DPI claims**
   - Must be reproduced on affected networks with controlled experiments varying packet count, payload size, IPv4/IPv6, server load, congestion, route and packet loss.
   - A handshake-success/upload-failure observation is evidence for an upload stall, not proof of a specific censorship mechanism.

4. **Cloudflare Worker IPv6 parser**
   - PattN should not vendor unrelated Worker code. Any independently reproduced fix belongs upstream in the affected Worker projects.

5. **RISC-V / LoongArch toolchain provenance**
   - Current nonstandard toolchains are digest-bound by the release lock. Move to official or organization-controlled distribution when a suitable supported upstream artifact exists.

6. **Remaining non-Persian translations**
   - Require native translation/review, not mechanical English duplication.

7. **Long-duration and real-environment assurance**
   - A >24-hour helper soak/heap-regression campaign and high-contention SQLite stress campaign require dedicated runner time and are not substituted by unit tests.
   - Accessibility conformance still requires real release builds with UI Automation / AT-SPI / VoiceOver; static XAML checks are only regression guards.
   - DNSSEC insecure-delegation and alias-authentication confidence should continue to grow through reviewed real-capture fixtures; synthetic and fuzz coverage do not make the corpus exhaustive.

8. **Optional architectural simplification**
   - Generated cross-language RPC schemas, a consolidated SQLite unit-of-work abstraction, and further platform-specific sysproxy abstraction remain maintainability refactors rather than correctness blockers. They should be pursued only with migration tests and measurable reduction in duplicated surface.

## Date-gated DNSSEC verification

The embedded IANA root-anchor snapshot contains the current and successor KSKs and the scheduled workflow verifies IANA's detached CMS signature. The local audit is intentionally designed to require an explicit review after **2026-10-11**. This cannot be marked complete before that date; a post-rollover source verification must update the reviewed snapshot if IANA's active material changes.

## Merge verification

Every split PR #2-#9 must be green at its current head. The umbrella branch must run the complete stack/localization, Code Test, DNS assurance, durability, and release matrices after the final integration changes. A green split matrix does not substitute for a green umbrella matrix.
