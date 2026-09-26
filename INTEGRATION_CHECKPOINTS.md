# PattN Discovery + Reviver Integration Checkpoints

This document is the restart and evidence ledger for the WhiteDNS/donor decomposition into PattN.

> **Reading rule:** the sections below through **Current roadmap** are the authoritative present-state summary. The later `Continuation checkpoint` sections are chronological history. Their embedded “Next boundary” text records what was next *at that checkpoint* and must not be interpreted as the current backlog.

## Current checkpoint

- Pull request: `#1`, branch `feat/discovery-reviver-integration`.
- The deterministic in-repository implementation/remediation pass includes the final provider-catalog recovery fixes plus the WAL-checkpoint validation correction recorded in this synchronization checkpoint.
- The final audit closed three provider-catalog recovery defects: ordinary registry reads no longer create adjacent lease artifacts when no recovery state exists; permanent lease-path errors fail immediately instead of masquerading as lock contention; and partial rollback failures now distinguish a durably restored catalog file from subsequent registry/revision persistence failure while retaining the rollback journal when reconciliation cannot persist. The durability verifier also now accepts SQLite's documented `(-1, -1)` no-WAL checkpoint sentinel while continuing to reject busy or malformed mixed-negative results.
- Merge readiness is defined by the **current PR-head checks**, not by a hard-coded historical run number in this document. The full gate must be green after the final branch head is established.
- Machine-verifiable implementation is now separated from evidence that cannot honestly be manufactured by unit tests or static CI: human-reviewed real DNSSEC packet promotion, repeated abrupt-loss campaigns, retained special-architecture execution proof, post-2026-10-11 KSK rollover review, and real assistive-technology execution on representative desktops.
- A green gate is necessary but is not treated as proof of semantic completeness. Protocol, trust-boundary, durability, concurrency, release-packaging, privacy, accessibility, and lifecycle review remain separate obligations.

## Permanent boundaries

- **PattN** owns UI, `ProfileItem`, persistence, subscriptions, routing/DNS settings, core lifecycle, repair promotion/rollback, and release packaging.
- **`pattn-discovery`** is a bundled Go network-evidence engine. It discovers, measures, traces, validates, and reports evidence; it never edits PattN profiles or application settings.
- **Reviver** lives in `v2rayN/ServiceLib/Reviver`. It diagnoses one canonical PattN profile, consumes Discovery evidence, produces bounded typed mutations, validates candidates through real cores, and keeps the source profile unchanged while searching.
- Discovery -> Reviver integration happens through typed candidate/evidence contracts. Discovery owns network observation; Reviver owns semantic repair planning; explicit PattN services own persistent application/catalog mutations.
- Historical evidence, catalog membership, pinned endpoints, and resolver recommendations never bypass current validation.

## PR-visible integration milestones

The original local development checkpoints were squash-composed into the PR's first commit. Use the PR-visible commits below when reviewing or restarting from GitHub; older short SHAs mentioned in historical sections are provenance notes, not branch commits.

| Commit | Milestone |
|---|---|
| `8fdca54c` | Initial Discovery engine + Reviver integration, process/RPC seam, scanning, endpoint evidence, core-backed repair validation |
| `812bb9b3` | Iterative DNS trace foundation |
| `32102a7f` | Root-anchored DNSSEC chain validation |
| `f2f4df8a` | Resolver quorum/protocol evidence and Reviver-facing ranking evidence |
| `e1834550` | Policy-aware resolver catalog and bounded DNS repair planning |
| `ebeabe49` | Persistent DNS repair history and transactional DNS settings |
| `4ae33373` | Decayed endpoint history and explicit endpoint pools |
| `00911066` | Persistent provider-catalog registry and revision ledger |
| `607a92c9` | Cross-platform Discovery management UI |
| `b665457a` | Explicit remote catalog fetch and signed provenance |
| `7a742073` | Optional HTTPS SPKI pinning |
| `39b319ef` | Signed retired archives and durable remote trust-revision history |
| `ecf7046c` | Pre-audit regression checkpoint |
| `71b7821a` | Archive fsync and late audit durability hardening |
| `17299713` | DNS evidence identity, exact DS-matched DNSKEY selection, and fail-closed alias validation |
| `53a2f521` | Signed-catalog replay continuity and repository-wide workflow-lint cleanup |
| `d17c55bc` | Iterative DNS authority retry and conflicting-CNAME hardening; complete post-audit CI gate green |
| `2dcaa429` | Synchronized the authoritative integration ledger after the broad audit |
| `647a920d` | Hardened repair persistence compensation and rollback behavior |
| `c88b7303` | Serialized per-registry remote-catalog state changes and propagated profile-write failures |
| `fca17caf` | Hardened referral ownership, authority baselines, and contradictory invalid-name hijack evidence; full CI gate green |
| `b0fa05e9` | Rejected non-authoritative iterative terminal evidence and bounded best-effort Discovery stream cancellation |
| `1b67fc5a` | Made repair rollback fail closed and reduced the durable default/profile deletion crash window |
| `b795c37e` | Replaced collision-prone sentinel profile deletion with transactional row deletion |
| `707d5655` | Canonicalized literal endpoint identity across Discovery, history, pools, and Reviver |
| `d9a54525` | Kept merged endpoint evidence observationally coherent instead of synthesizing impossible metric combinations |
| `b87809f5` | Enforced bounded same-authority HTTPS redirect handling for remote catalog transport |
| `81083832` | Made lifecycle-retention receipts count actual deletes rather than attempted deletes |
| `69990da1` | Bound remote apply previews to canonical trust/source configuration fingerprints |
| `fc8d73a3` | Persisted configured and final catalog/signature fetch URIs in remote apply provenance; full PR gate green |
| `ffeeeda9` | Hardened rollback against thrown transactional delete failures and stale previous-default receipts; audit wording cleanup |
| `afe5a70f` | Completed remote destination policy, DNSSEC insecure-delegation and alias-chain semantics, trust-anchor audit foundations, and initial fuzz/race coverage |
| `f4eabf77` | Repaired the expanded CI workflow, added helper protocol/package checks, and introduced cross-process catalog update leasing |
| `8dbd5bb2` | Fixed root trust-anchor audit handling for the DNS root name |
| `9f7f7f61` | Closed the public arbitrary-HttpClient transport bypass while preserving an internal test seam |
| `b3c66811` | Made SPKI pin scope explicit when catalog and detached-signature authorities differ |
| `ff1266b9` | Added DNSSEC RDATA fuzz coverage |
| `10ac69a6` | Added DNSSEC-aware reviewed real-DNS capture/replay workflow |
| `50cd66b7` | Added reusable durable atomic-file replacement with parent-directory durability where supported |
| `06af1435` | Durability/test checkpoint, including durable deletion and direct atomic-file regression coverage |
| `94a2c8d7` | Cross-process remote catalog trust-operation serialization |
| `81e3df4c` | Deterministic IANA trust-anchor source comparison and validity-aware auditing |
| `128e6f69` | Manual Discovery large-range/load characterization workflow |
| `4c40db4c` | Green full-gate hardening checkpoint after lease retry/fallback fixes |
| `bf74ac65` | DNS settings persistence compensation on thrown writes |
| `76e6f0d9` | Failure-contract and compensation regression coverage for DNS repair persistence |
| `dc9513c` | Discovery presentation formatting extraction |
| `ed41b95c` | Discovery command-coordinator decomposition |
| `52a0d0c3` | Catalog/remote management handler extraction |
| `e3e870b6` | Endpoint/maintenance handler extraction |
| `3f4bab28` | Stale UI target hardening across asynchronous management operations |
| `36efe5be` | DNS repair/config persistence fail-closed closure; complete PR gate green with 437 ServiceLib tests |
| `a6175ced` | Closed the stale integration roadmap and synchronized the verified-head ledger |
| `614cd50a` | Hardened endpoint identity normalization and TLS-observation authority binding |
| `c41af52d` | Kept TLS observation validation non-bypassable across redirects/authority checks |
| `a647d9ad` | Failed closed on corrupted persisted remote trust-history evidence |
| `a1753c41` | Failed closed on corrupted persisted Reviver promotion evidence |
| `90c019bd` | Failed closed on corrupted provider-catalog registry/revision evidence |
| `9089b4cb` | Failed closed on invalid persisted remote-health signature policy |
| `bf54b304` | Bounded maintenance/retention queries to frozen candidate IDs and rechecked apply eligibility |
| `1157bfac` | Bounded maintenance payload estimation by frozen IDs |
| `79ff90de` | Added strict JSON parsing for persisted audit evidence; complete PR gate green |
| `eac18caa` | Synchronized the final pre-remediation integration ledger |
| `8f53de73` | Added pattn-discovery to all Linux DEB/RPM packages and final-package presence/native handshake checks |
| `cb736bfe` | Added UDP truncation -> TCP capture fallback and explicit Go dependencies for RISC-V/LoongArch packaging |
| `83024d4d` | Recorded independent IANA XML/signature/CA-bundle publication dates |
| `822ea8f5` | Added scheduled detached-CMS verification of IANA trust-anchor publication |
| `34ad152f` | Retained load benchmark provenance/raw evidence for 90 days and strengthened repeated characterization |
| `40aec77b` / `79f53038` | Added retention interruption seam plus idempotent-retry regression coverage |
| `ee7838ba` / `adb27fd9` | Added SQLite WAL-checkpoint/VACUUM interruption boundaries and tests |
| `a7c2bfad` | Added native x64/arm64 execution from final DEB/RPM artifacts |
| `32cb10c7` | Added the real-desktop accessibility execution matrix and completion rule |
| `f382f122` | Added the evidence gate for deeper Reviver transport/TLS/SNI strategies |
| `87683073` / `e36081ea` | Added explicit accessible names to Discovery fields in Avalonia and WPF |
| `e36448c9` | Kept ordinary provider-catalog registry reads filesystem-read-only when no lease/recovery artifacts exist |
| `dfcc73fd` | Failed fast on permanent provider-catalog lease path errors instead of retrying them as contention |
| `b16bd4c2` | Surfaced partial catalog rollback persistence failures explicitly and retained durable reconciliation evidence |
| `fec32c5d` | Accepted SQLite's documented no-WAL checkpoint sentinel while retaining fail-closed busy/malformed-result handling |

## Current `pattn-discovery` RPC surface

Unary:

- `engine.version`
- `engine.capabilities`
- `targets.inspect`
- `targets.normalize`
- `endpoint.probe`
- `dns.trace`
- `dns.authority.compare`
- `dns.trust-anchor.audit`
- `dns.dnssec.inspect`
- `dns.dnssec.chain`
- `dns.dnssec.validate`
- `dns.consensus.compare`
- `dns.repair.inspect`
- `dns.resolver.catalog`
- `dns.resolver.catalog.audit`
- `dns.resolver.profile`
- `dns.resolver.qualify`

Streaming/control:

- `scan.tcp`
- `dns.resolver.discover`
- `scan.pause`
- `scan.resume`
- `scan.cancel`

The process protocol is NDJSON v1 over redirected stdin/stdout. Stdout is protocol-only; diagnostics belong on stderr.

## Current Reviver pipeline

1. Deep-clone the original into an immutable-by-convention `ProfileSnapshot`.
2. Normalize only representation-equivalent fields.
3. Validate PattN/transport/Reality/XHTTP invariants.
4. Validate core compatibility and generated profile semantics.
5. Run baseline DNS/TCP/runtime diagnosis as applicable.
6. Classify the observed failure before proposing mutations.
7. Select only strategies relevant to that failure class.
8. Enforce global/per-strategy budgets, confidence policy, semantic dedupe, and mutation guards.
9. Validate candidates through current real-core runtime probes with repeated-success quorum.
10. Rank safety/reliability before minor latency improvements.
11. Keep the source profile unchanged throughout search.
12. Promote only through explicit detached/transactional paths with rollback evidence.

Built-in strategies currently composed by `ReviverStrategyCatalog`:

- evidence-backed physical endpoint replacement while preserving logical identity and unrelated protocol fields;
- bounded DNS address-family / target-strategy repair driven by current DNS evidence;
- conservative Xray <-> sing-box local-core fallback where PattN supports the same profile shape.

## Major capabilities now landed

- Lazy IPv4/IPv6 targets, bounded workers, adaptive scheduling, backpressure, pause/resume/cancel, streaming results.
- Physical dial endpoint separated from logical Host/SNI/Reality identity.
- Resolver discovery, qualification, repeated sampling, EDNS/TXT/DoT/DoH profiling, protocol evidence, and policy-aware resolver catalog.
- Iterative/authoritative trace, authority comparison, consensus, root-anchored DNSSEC validation, parent-authenticated insecure-delegation proof, CNAME/DNAME alias-chain authentication, RRSIG/RRset validation, and composed NSEC/NSEC3 denial evidence.
- DNS repair inspection, resolver health telemetry/trends, explicit DNS recommendation preview/apply/rollback, and persistent outcome history.
- Endpoint observation history, decayed historical eligibility, explicit endpoint pools, promotion/admin operations, and reviewable maintenance.
- Versioned provider/ASN catalogs, candidate adapters, semantic diff, metadata audit, atomic update/rollback, persistent registry/revision ledger, and lifecycle retention.
- Discovery management UI for catalogs, remote sources, endpoint pools, repair history, retired resources, maintenance, and provenance.
- HTTPS-only remote catalogs with detached signature policy, public trust portability, ordinary TLS validation, optional SPKI pinning, explicit overlap-based pin rotation, fail-closed public-destination resolution/direct connection, no ambient proxy use, authority-bounded redirects, explicit shared-pin authority semantics, remote provenance, source-health history, and trust-revision history.
- Retired-catalog archives, optional archive signing/verification, export inspection, and explicit SQLite compaction.
- DNS packet fixture/replay infrastructure and offline production-validator execution, plus a manual DNSSEC-aware capture/replay workflow for reviewed real-world evidence promotion.
- Versioned IANA root trust-anchor snapshot metadata with freshness/rollover audit RPC and CLI checks.
- Reusable durable atomic-file replacement/deletion used by provider-catalog updates and retired-archive export, with cross-process update leases for catalog file mutation.

## Donor behavior recomposed

- **WhiteDNS:** lazy targets, bounded workers, centralized pause/resume/cancel, adaptive concurrency, and DNS probing concepts.
- **cfray:** logical identity hierarchy, bounded repair concept, Reality-aware constraints, and real-core validation pattern.
- **Sub-Store:** loss-conscious transport query parsing and modern XHTTP normalization/validation behavior.
- **CloudflareSpeedTest:** physical dial target != logical Host/SNI, staged measurement, and edge metadata.
- **NovaRadar:** repeated-success quorum and source-category/health concepts.
- **proxyUtil / Xunter:** temporary real-core validation pattern and validation ladder concepts.
- **Xray examples:** modern PattN-native golden fixtures now cover Reality/Vision/gRPC/XHTTP, WS early data, and Hysteria2. Expansion to additional fallback/SplitHTTP/H3/cross-core shapes remains useful.

## Verification status

The authoritative verification source is the GitHub PR checks for the **current head**, not a developer-specific container and not a historical commit recorded here.

The final gate is expected to prove all of the following on the established head:

- `go test ./...` and `go vet ./...`;
- bundled `pattn-discovery` executable protocol smoke;
- `go test -race ./...`;
- short DNS/DNSSEC/DNAME/NDJSON fuzz smoke;
- committed fuzz-regression-corpus audit;
- root trust-anchor snapshot audit;
- DNS fixture-corpus audit with orphan detection;
- the complete current ServiceLib test suite;
- durability fault-harness build plus real `SIGKILL` smoke at apply/rollback cross-store checkpoints;
- repository-wide GitHub Actions/actionlint validation;
- WPF Release build;
- Avalonia Release build.

PR/scheduled assurance workflows additionally exercise DNSSEC candidate capture/replay, coverage-guided DNS fuzzing with retained evidence, and repeated Discovery load characterization with runner provenance.

Primary release workflows build the bundled helper for supported Windows/Linux/macOS x64/arm64 targets and the dedicated Windows x86 path. Linux packaging contains explicit final-package helper checks. RISC-V and LoongArch package execution paths are implemented for matching native/QEMU environments, but their successful execution must be demonstrated by retained workflow runs; ordinary PR Linux builds do not substitute for that evidence.

Do not edit this ledger merely to chase a new CI run number. A final PR description may cite the actual green run IDs once the branch head stops changing.

## Post-audit findings and residual risk

The 2026-09-24 audit found issues that green CI alone did not expose. Confirmed defects already remediated include:

- resolver hijack classification now requires positive synthesized invalid-name answer evidence; bare REFUSED, SERVFAIL, NXDOMAIN, and empty NOERROR responses are not hijack proof, while contradictory responses that still synthesize an address are;
- workflow/reusable inputs enter shell execution through bounded environment values and shell arguments are quoted/array-safe;
- invalid stored remote signature-policy values fail closed instead of silently projecting as `None`;
- RRSIG validity windows use RFC 1982-style 32-bit serial arithmetic, including wraparound/undefined-half-range handling;
- missing DS remains indeterminate unless an authenticated parent denial establishes insecure delegation;
- DNSKEY trust is bound to exact DS/trust-anchor-matched key material rather than non-unique key-tag+algorithm hints;
- non-zone/non-protocol-3 DNSKEYs are not accepted as RRSIG verification keys;
- iterative trace accepts only QNAME/CNAME-reachable answer data, ignores unrelated answer-section records, validates referral-owner ancestry/single-zone coherence, retries alternate authorities after unusable or non-authoritative terminal responses, and rejects contradictory CNAME evidence;
- authority comparison excludes non-authoritative responses and transient/error RCODEs from majority/reference baselines;
- application RRset validation is scoped to the terminal owner and fails closed across a CNAME hop until the alias RRset itself is authenticated;
- Discovery selection refreshes reject stale asynchronous results before mutating active details;
- Discovery helper lifecycle/backpressure is generation-scoped and bounded so obsolete helpers or a slow stream cannot poison replacement requests; best-effort stream cancellation writes are also time-bounded;
- raw SQLite values remain query parameters rather than SQL text;
- signed remote catalogs enforce monotonic accepted signing time/hash continuity and reject excessive future timestamps;
- retired catalog archive payloads are flushed to stable storage before atomic replacement;
- workflow lint now passes against the complete repository while explicitly accounting for current/custom runner labels.
- repair rollback now validates a non-empty previous-default profile before mutating durable state and compensates default selection when transactional profile removal throws; compensation restores the promoted ID only if that profile still exists.
- remote catalog UI copy now distinguishes a network-fetching preview from catalog mutation: Fetch preview contacts the configured HTTPS source, while Apply is the catalog mutation boundary.

Important residual risks are intentionally not papered over:

- DNSSEC insecure-delegation classification and CNAME/DNAME alias authentication are now implemented, but still need a substantially larger reviewed real-capture corpus before they should be treated as exhaustively validated;
- remote catalog transport rejects non-public destinations, dials reviewed resolutions directly, disables ambient proxies, bounds redirects to one authority, prevents a public arbitrary-HttpClient bypass, and serializes catalog/trust mutations across processes. Remaining trust risk is operational: key-rotation/rollback policy, reviewed trust-anchor updates, and fault injection across coordinated multi-store changes;
- provider-catalog replacement/archive persistence now share a reusable durable atomic-file primitive and catalog updates use a stable cross-process sidecar lease. Deterministic failure coverage includes DNS-settings history compensation, remote provenance/revision compensation, retention interruption/retry, and WAL-checkpoint/VACUUM interruption boundaries. **Abrupt process death, filesystem fault, and true power-loss behavior remain separate campaigns** and are not implied by exception/cancellation tests;
- final Linux DEB/RPM composition now includes pattn-discovery and native target-matching handshake verification; the release workflow also executes the helper from extracted final x64/arm64 DEB/RPM artifacts. RISC-V/LoongArch packaging has target-matching package-level handshake paths. Completed workflow evidence is still required before calling every architecture production-verified;
- the committed DNS fixture corpus remains predominantly synthetic. Capture now defaults to UDP with TCP fallback on the TC bit and rejects still-truncated packets, but real A/AAAA/CNAME/DNAME/NXDOMAIN/NODATA/NSEC/NSEC3/DS/DNSKEY/RRSIG captures must still be reviewed before repository promotion;
- the IANA trust-anchor audit now tracks the XML publication (2024-11-05), detached CMS signature publication (2025-08-04), and CA-bundle publication (2026-05-28) independently, verifies the live detached CMS chain in the scheduled workflow, and still compares semantics to the deterministic embedded snapshot. The 2026-10-11 KSK rollover remains an explicit post-event review boundary;
- Discovery WPF/Avalonia fields now have broader explicit automation names and `ACCESSIBILITY_VALIDATION.md` defines the real-desktop completion matrix. Actual keyboard/focus/high-DPI/screen-reader behavior remains environment-dependent and must be executed rather than inferred from markup.

## Current roadmap

The deterministic in-repository portion of the current remediation is implemented. Do not substitute deterministic tests for evidence that requires real networks, filesystems, architectures, future publication state, human review, or assistive technologies.

1. **Reviewed real DNSSEC corpus — human review/promotion pending.** Candidate capture/replay is automated and truncation-safe, but promotion remains a deliberate human evidence decision. Review real A/AAAA/CNAME/DNAME/NXDOMAIN/NODATA/NSEC/NSEC3/DS/DNSKEY/RRSIG captures, record stable provenance/expectations, then commit only reviewed fixtures.
2. **Abrupt-loss durability — smoke implemented; repeated campaign evidence remains useful.** The normal PR gate performs real `SIGKILL` recovery smoke across apply/rollback checkpoints. The dedicated durability workflow should still be run repeatedly against real SQLite/file stores and retained as evidence; filesystem/power-loss behavior beyond process death remains environment-dependent.
3. **Special-architecture package proof — paths implemented; retained runs required.** Keep native x64/arm64 package execution green, then explicitly run the RISC-V/LoongArch proof path and retain run IDs, package hashes, build logs, and target/native-or-QEMU execution evidence.
4. **Load baselines — automation/retention implemented.** Continue comparable repeated runs and retain raw measurements plus runner/CPU/kernel/Go provenance. Optimize only reproduced regressions or measured bottlenecks.
5. **IANA KSK rollover — future review required.** Automated CMS/source comparison and freshness policy are implemented. Because the rollover is dated **2026-10-11**, perform and record a new explicit source/format/trust-material review after that date; do not pre-declare it complete on 2026-09-25.
6. **Real desktop accessibility — static/compile gates implemented; execution evidence pending.** Execute `ACCESSIBILITY_VALIDATION.md` on representative shipped WPF/Avalonia desktops with keyboard-only navigation, focus inspection, scaling/high-DPI, and real screen readers. Treat static XAML checks as necessary but insufficient.
7. **Deeper Reviver strategies — evidence-gated by design.** Add TLS/SNI/transport/network-adaptation strategies only when a reproducible failure satisfies `REVIVER_STRATEGY_EVIDENCE.md`, invariants are preserved, and repeated real-core validation plus regression evidence exists.
8. **Coverage-guided DNS/DNSSEC corpus management — ongoing assurance.** Keep scheduled/PR fuzzing green, retain minimized failing inputs, review them, fix the root cause, and promote only useful minimized regressions into the durable corpus.
9. **Privacy-safe support bundles — mechanism complete; field evidence ongoing.** Preserve default-deny/redaction invariants for addresses, hostnames, paths, credentials, trust material, and unknown evidence keys. Review representative exported bundles when new evidence types are added and add regression cases before exposing new fields.

The top summary and this roadmap are authoritative. Historical `Continuation checkpoint` sections below are provenance only and must not be read as a competing backlog.

## Restart / verification commands

From a normal clone of this PR branch:

```bash
git status --short
git log --oneline -20

cd pattn-discovery
go test ./...
go vet ./...
go run ./cmd/dns-fixture-audit -root ./internal/dnsfixture/testdata -fail-on-orphans
cd ..

dotnet test --project ./v2rayN/ServiceLib.Tests -c Release
```

On Windows, also build both UI targets as CI does:

```powershell
dotnet build ./v2rayN/v2rayN/v2rayN.csproj -c Release -p:EnableWindowsTargeting=true
dotnet build ./v2rayN/v2rayN.Desktop/v2rayN.Desktop.csproj -c Release
```

---

## Continuation checkpoint: deep DNS trace foundation

The first deep-DNS slice extends the shared DNS layer rather than creating a parallel resolver implementation.

- DNS wire parsing now preserves answer, authority, and additional sections as typed observations.
- Queries can explicitly disable recursion for iterative resolution.
- `dns.trace` starts from root servers, follows NS referrals using in-message A/AAAA glue, preserves every hop, and restarts from roots when following a CNAME.
- UDP truncation is retried over TCP before advancing the trace.
- Referral failures are returned as structured trace outcomes (`nameserver_unreachable`, `no_referral`, `no_delegation_address`, `cname_loop`, `max_hops`) rather than being mislabeled as poisoned/broken DNS.
- Existing resolver discovery and qualification continue using the same shared `dnsmeasure` layer, so packet behavior is not duplicated.

Deliberately not included in this slice: resolving out-of-bailiwick NS hostnames when referrals lack glue, querying every authoritative endpoint for comparison, DNSSEC chain validation, or consensus/reference verdicts. Those remain the next deep-DNS checkpoints.


## Continuation checkpoint: broader deep DNS evidence

This checkpoint expands the first trace slice into a reusable authoritative-DNS evidence subsystem.

- Iterative trace now recovers nameserver addresses when a referral omits glue by resolving the NS hostname from the root hierarchy, bounded by `maxNsDepth`.
- Every trace exposes the most recent delegation as nameserver identities plus all resolved A/AAAA endpoints, including per-nameserver resolution errors.
- `dns.authority.compare` reuses that delegation and independently queries every discovered authoritative endpoint.
- Authority responses are grouped by canonical answer signatures that ignore non-semantic TTL/order/name-case differences while retaining RCODE and answer identity.
- Comparison output preserves every endpoint observation, authoritative flag, transport, latency, response signature, answer set, responding count, agreement groups, unanimity/divergence, and dominant group size.
- UDP truncation continues to fall back to TCP both during iterative traversal and direct authority comparison.
- `dnstruth.Aggregate` now provides a verdict-neutral evidence grouping primitive for later authoritative + trusted-resolver + candidate-resolver consensus. Failed observations do not count as disagreement.
- DNSSEC groundwork now parses DS and DNSKEY observations, including DNSKEY key-tag calculation. This checkpoint deliberately does not claim cryptographic validation yet.
- ServiceLib now has typed deep-DNS request, trace, delegation, record, authority-observation, agreement-group, and comparison DTOs plus `TraceDnsAsync` and `CompareDnsAuthoritiesAsync`.
- Go and C# contract tests cover missing-glue recovery, authority divergence grouping, TTL/order-insensitive RRset normalization, consensus aggregation, DS/DNSKEY parsing, capability advertisement, and JSON DTO shape.

The next deep-DNS boundary is cryptographic DNSSEC validation (DS -> DNSKEY -> RRSIG) plus active trusted-reference collection and candidate-vs-authoritative consensus. EDNS/TXT and encrypted DNS qualification should then build on the same shared observations rather than introducing a second DNS stack.


## Continuation checkpoint: DNSSEC delegation link + active consensus

This checkpoint turns the previous evidence primitives into callable diagnostic workflows.

- `dns.dnssec.inspect` performs independent iterative DS and DNSKEY observations and validates the delegation link by matching DS key-tag, algorithm, and digest against observed DNSKEY RDATA.
- Supported DS digest types are SHA-1 (1), SHA-256 (2), and SHA-384 (4); unsupported digest types are reported explicitly instead of being treated as mismatches.
- DNSSEC status vocabulary remains deliberately narrow: `ds-key-match`, `unsigned-no-ds`, `ds-without-dnskey`, `ds-key-mismatch`, and `indeterminate`. A DS/DNSKEY match is **not** labeled as full DNSSEC validation because RRSIG validation is not landed yet.
- `dnstrace.Result` now retains terminal wire records internally (`json:"-"`) so higher-level validators can operate on exact observed RDATA without exposing opaque wire blobs through the public RPC.
- `dns.consensus.compare` collects authoritative evidence plus trusted and candidate recursive-resolver observations through the same DNS exchange layer.
- Built-in trusted-reference endpoints are Cloudflare (`1.1.1.1`), Google (`8.8.8.8`), and Quad9 (`9.9.9.9`) and can be disabled or supplemented per request.
- Candidate resolvers are assessed against authority only when the authoritative endpoint set yields a strict majority signature. Otherwise the result is `no-authority-baseline`, preventing a split authority set from becoming a false ground truth.
- Candidate outcomes are `agrees-authority`, `differs-authority`, `no-authority-baseline`, or `query-failed`; raw evidence and the aggregate grouping remain available alongside that derived classification.
- Resolver timeouts/errors are excluded from disagreement counts rather than silently voting for a different answer.
- ServiceLib exposes typed DNSSEC inspection and consensus-comparison requests/results, preserving trusted and candidate resolver roles separately.

The next deep-DNS slice is full signed-RRset verification: EDNS(0) DO-bit queries, RRSIG parsing/canonicalization/signature verification, DS -> DNSKEY -> RRSIG chain checks, and NSEC/NSEC3 authenticated-denial observations. After that, resolver qualification can consume DNSSEC and consensus evidence without duplicating packet logic.


## Continuation checkpoint: EDNS/DO + DNSKEY RRSIG authentication

This checkpoint advances DNSSEC inspection from delegation matching into cryptographic DNSKEY RRset authentication.

- The shared DNS wire layer now supports EDNS(0) OPT queries with configurable UDP payload size and the DNSSEC OK (DO) bit.
- Existing discovery/qualification callers remain backward-compatible; DNSSEC inspection opts into EDNS/DO through the advanced shared query path.
- Iterative DNSSEC traces retain the complete terminal answer section internally so DNSKEY and sibling RRSIG records remain available to validators, while authority/additional terminal proof records are also retained for the upcoming NSEC/NSEC3 slice.
- RRSIG parsing now preserves type-covered, algorithm, labels, original TTL, validity interval, key tag, signer name, and signature bytes.
- DNSKEY RRset validation reconstructs canonical DNSSEC signed data using the RRSIG original TTL, canonical owner/signer names, canonical RR ordering, and wildcard-label rules.
- Cryptographic verification supports:
  - RSA/SHA-256 (algorithm 8)
  - RSA/SHA-512 (algorithm 10)
  - ECDSA P-256/SHA-256 (algorithm 13)
  - ECDSA P-384/SHA-384 (algorithm 14)
  - Ed25519 (algorithm 15)
- Signature results explicitly distinguish `valid`, `expired`, `not-yet-valid`, `no-matching-key`, `unsupported-algorithm`, and `invalid`.
- `dns.dnssec.inspect` now reports `dnskeyAuthenticated` only when a valid DNSKEY RRset RRSIG is made by a DNSKEY that is itself matched by the parent DS. A signature from an otherwise untrusted key in the same RRset cannot bootstrap trust.
- Authentication status is deliberately scoped to the DNSKEY RRset: `dnskey-authenticated`, `delegation-match-only`, `unsigned`, `dnskey-signature-invalid`, or `indeterminate`. This still does not claim full validation of arbitrary application RRsets.
- ServiceLib DTOs now expose per-signature cryptographic results and the DNSKEY authentication state.

The next slice is signed application-RRset validation plus authenticated denial: canonicalization for name-bearing RDATA where required, RRSIG verification for A/AAAA/CNAME and related records, and NSEC/NSEC3 parsing/proof evaluation. That can then feed resolver qualification with a cryptographic signal separate from authority/consensus disagreement.


## Continuation checkpoint: generalized RRset DNSSEC + denial evidence

This is the first large cross-layer DNSSEC validation checkpoint rather than another parser-only increment.

### Canonical RRset support

- DNS parsing now retains both raw wire RDATA and DNSSEC-canonical RDATA.
- Name-bearing RDATA is canonicalized at parse time so message compression pointers cannot leak into signature verification.
- Canonicalization currently covers A/AAAA/raw records naturally plus NS, CNAME, PTR, MX, SOA, SRV, NSEC, SVCB, and HTTPS name-bearing structures.
- Malformed RDATA boundary/offset conditions are rejected before canonical slices are built.
- `dnssec.ValidateRRSet` verifies arbitrary typed RRsets against a supplied trusted DNSKEY set and applies RRSIG original TTL, signer/owner canonicalization, wildcard label rules, canonical RR ordering, validity windows, and the already-landed cryptographic algorithms.

### NSEC/NSEC3 evidence

- NSEC parsing exposes owner, next-domain, and decoded type bitmaps.
- NSEC evaluation distinguishes exact-owner NODATA evidence from canonical-name interval coverage.
- NSEC3 parsing exposes owner hash, zone, algorithm, flags/opt-out, iterations, salt, next hash, and decoded type bitmaps.
- SHA-1 NSEC3 hashing (algorithm 1) is implemented with the RFC iteration/salt construction and Base32hex representation.
- NSEC3 evaluation distinguishes exact-hash NODATA evidence from cyclic hash-interval coverage.
- Unsupported NSEC3 hash algorithms remain explicit and indeterminate.
- This checkpoint does **not** yet claim complete NXDOMAIN proof validation: closest-encloser/wildcard nonexistence and full NSEC3 opt-out semantics still require the next proof-composition layer.

### Domain-level DNSSEC validation

- New `dns.dnssec.validate` performs an iterative EDNS/DO trace for the requested name/type.
- It discovers the RRSIG signer zone, runs the existing DS/DNSKEY inspection for that signer, and only uses DNSKEY records that are matched by the observed parent DS and authenticate the DNSKEY RRset.
- Positive answers are verified as signed RRsets against that trusted key subset.
- Negative/NODATA responses inspect signed NSEC/NSEC3 authority records and return their proof evidence and signature status.
- Unsupported signature algorithms do not become bogus results; only definitively invalid/expired/not-yet-valid/no-key evidence is classified as bogus.
- Results expose `answerAuthenticated`, `denialAuthenticated`, per-RRset/per-proof signature evidence, signer inspection, RCODE, and an explicit trust scope.
- The current trust scope is `delegation-local`: the parent DS observation itself is not yet recursively authenticated to a configured root trust anchor, so this is deliberately not labeled a complete root-anchored DNSSEC chain.

### ServiceLib

- `DiscoveryEngineService.ValidateDnssecAsync` exposes the new RPC.
- Typed models cover domain validation, denial evidence, proof signatures, answer/denial authentication, signer inspection, and trust scope.
- Contract tests preserve this distinction so future UI/Reviver code cannot silently collapse delegation-local evidence into root-anchored validity.

Next DNSSEC work: configurable/root trust anchors, parent-by-parent chain authentication to the root, complete NSEC closest-encloser + wildcard proofs, complete NSEC3 closest-encloser/next-closer/wildcard + opt-out rules, then integration of those cryptographic outcomes into resolver qualification and consensus classification. Resolver transport depth (EDNS behavior, TXT, DoT, DoH) remains the next parallel slice.


## Continuation checkpoint: root-anchored chain + resolver truth integration

This checkpoint replaces the previous delegation-local ceiling with an explicit root-to-signer chain of trust and feeds the result into resolver diagnostics without making every cheap probe pay the cost automatically.

### Root trust anchors and rollover posture

- The built-in root trust-anchor set contains both SHA-256 DS anchors distributed for the current rollover window:
  - KSK-2017 key tag 20326, algorithm 8, digest type 2.
  - KSK-2024 key tag 38696, algorithm 8, digest type 2.
- Keeping both is intentional during the 2025-2026 pre-publication/rollover period; the helper should not pin only the currently-signing KSK.
- Root name (`.`) queries are now first-class in the wire encoder and iterative trace path, allowing the root DNSKEY RRset to be observed and cryptographically verified rather than special-cased.
- Trust-anchor records are represented as typed DS-equivalent evidence and remain separate from live DNS observations.

### Parent-by-parent chain validation

- New `dnschain` validation authenticates the root DNSKEY RRset against the configured root trust anchors.
- For each descendant zone from TLD to target signer zone:
  1. obtain the child's DS RRset from the parent hierarchy;
  2. verify the DS RRset RRSIG using the already-authenticated parent DNSKEY set;
  3. obtain the child's DNSKEY RRset;
  4. match the child DS to the appropriate DNSKEY;
  5. verify the child DNSKEY RRset using a DS-matched signing key;
  6. once the DNSKEY RRset is authenticated, promote **all DNSKEYs in that authenticated RRset** as trusted zone keys for the next verification step.
- That last rule corrects an overly restrictive earlier implementation which allowed only DS-matched KSKs to verify application RRsets. Authenticated ZSKs must also be trusted after the DNSKEY RRset itself has been authenticated.
- Each chain step preserves DS/DNSKEY traces, DS validation, DS signatures, DNSKEY signatures, parent zone, trusted-key count, and an explicit status.
- Chain results expose `rootAuthenticated`, `chainAuthenticated`, and the deepest `authenticatedZone`.

### RPC and domain validation

- New `dns.dnssec.chain` exposes standalone chain validation for a zone.
- `dns.dnssec.validate` now uses the root-anchored chain for the RRSIG signer zone.
- Positive and negative DNSSEC outcomes move from `delegation-local` to `root-anchored` only after the complete chain succeeds.
- Application RRsets are verified against the full authenticated DNSKEY RRset, allowing legitimate ZSK signatures.
- The signer-level `dns.dnssec.inspect` result remains available as diagnostic evidence but no longer serves as the final trust boundary.

### Resolver qualification

- `dns.resolver.qualify` now accepts opt-in `checkDnssec`.
- When enabled, the engine independently derives a root-anchored A-record reference using `dns.dnssec.validate`.
- Resolver output keeps manual reference comparison and DNSSEC reference comparison separate:
  - `dnssecReferenceCompared`
  - `dnssecReferenceDivergence`
  - `dnssecReferenceStatus`
- A resolver that disagrees with an authenticated DNSSEC answer receives `dnssec-divergent`, distinct from generic reference divergence.
- DNSSEC comparison is opt-in so bulk resolver discovery/cheap qualification does not unexpectedly perform a full hierarchy walk.

### Consensus

- `dns.consensus.compare` also accepts opt-in `checkDnssec`.
- Authority-majority and DNSSEC-rooted baselines are preserved as independent evidence channels rather than one overwriting the other.
- Candidate assessments therefore carry both ordinary authority status and `dnssecStatus`:
  - `agrees-dnssec`
  - `differs-dnssec`
  - `no-dnssec-baseline`
  - `query-failed`
- A split authority set can remain non-definitive even when a root-anchored signed RRset provides a cryptographic baseline, and vice versa.

### ServiceLib

- Added typed root trust-anchor, chain-step, and chain-result contracts.
- Added `ValidateDnssecChainAsync`.
- Resolver qualification and consensus request/response DTOs expose their opt-in DNSSEC comparison fields.
- Contract tests cover chain shape, trust-anchor rollover coexistence, DNSSEC-qualified divergence, and separate authority/DNSSEC candidate assessments.

Remaining DNSSEC proof work is deliberately narrower now: complete RFC-style NXDOMAIN proof composition (closest encloser, next closer, wildcard nonexistence), NSEC3 opt-out semantics, and authenticated proof of insecure delegations. After that, the next broad DNS track is resolver transport depth (EDNS behavior, TXT, DoT, DoH) and using those observations in Reviver/Discovery ranking.


## Continuation checkpoint: composed denial proofs + resolver transport depth

This batch closes two previously independent gaps: DNSSEC negative-proof composition and deep resolver transport diagnostics.

### Complete proof composition from authenticated denial records

- Individual signed NSEC/NSEC3 observations are no longer sufficient by themselves to mark an NXDOMAIN response authenticated.
- `dnssec.DenialProof` composes the authenticated proof records into one explicit result.
- NSEC NXDOMAIN composition now requires:
  - an authenticated closest-encloser owner;
  - authenticated interval coverage for the next-closer name;
  - authenticated interval coverage proving nonexistence of `*.<closest-encloser>`.
- NSEC exact-owner NODATA requires the queried RR type and CNAME to both be absent from the authenticated type bitmap.
- NSEC3 proof composition now requires all participating records to use the same zone, hash algorithm, iteration count, and salt.
- NSEC3 closest-encloser discovery hashes ancestors from the query name upward.
- NXDOMAIN composition then separately checks:
  - exact hash of the closest encloser;
  - interval coverage of the next-closer hash;
  - interval coverage of the wildcard hash.
- NSEC3 opt-out is retained as a first-class signal. For DS lookups, a signed opt-out interval that covers the next-closer name can produce `insecure-delegation-optout` rather than being mislabeled as ordinary secure NXDOMAIN.
- Composed proof statuses are:
  - `nxdomain-proven`
  - `nodata-proven`
  - `insecure-delegation-optout`
  - `incomplete-proof`
  - `unsupported-proof`
- `dns.dnssec.validate` sets `denialAuthenticated` only from a complete composed proof built exclusively from denial RRsets whose RRSIGs validated through the root-anchored key chain.
- The public result exposes closest encloser, next closer, wildcard name, proof-component booleans, opt-out/insecure-delegation state, completeness, and status.

### Shared resolver-depth profiler

- New `resolverdepth` package centralizes resolver transport diagnostics rather than duplicating protocol logic in ServiceLib/UI code.
- New `dns.resolver.profile` RPC supports:
  - classic UDP;
  - classic TCP;
  - EDNS(0) UDP-size 512;
  - EDNS(0) UDP-size 1232;
  - TXT query handling;
  - DNS-over-TLS;
  - DNS-over-HTTPS.
- The profile preserves latency, RCODE, RA, truncation, answer count, EDNS observation, TXT payloads, TLS state, HTTP status/content type, and transport errors.
- EDNS behavior reports:
  - `ednsCompatible` when at least one EDNS probe responds;
  - `ednsDowngrade` when classic UDP works but both EDNS probes fail.
- UDP/TCP agreement remains separate evidence rather than being folded into a score.

### DoT identity semantics

- DoT uses DNS-over-TCP framing over TLS.
- A logical `serverName` is mandatory when DoT profiling is requested.
- TLS certificate verification and SNI use that logical identity while the physical dial target remains the supplied resolver IP.
- TLS 1.2 is the minimum accepted protocol version.
- The result exposes negotiated TLS version, SNI identity, and whether a verified certificate chain was established.
- This intentionally preserves the project's physical-endpoint versus logical-identity invariant.

### DoH identity semantics

- DoH requires an HTTPS URL and uses POST with `application/dns-message` request/accept semantics.
- When a physical resolver address is supplied, HTTP dialing can be pinned to that address while TLS verification and HTTP Host identity remain tied to the DoH URL hostname.
- HTTP response status and content type are preserved alongside parsed DNS-message evidence.
- A successful HTTPS request is not silently converted into a DNS success: the body must also parse as a matching DNS response.

### Qualification and consensus integration

- Resolver qualification now supports opt-in `checkDepth` and can embed the complete resolver profile.
- Existing cheap qualification behavior is unchanged when depth is disabled.
- Qualification requests carry optional DoT server name/port and DoH URL metadata.
- Consensus also supports opt-in deep profiles on trusted and candidate resolvers.
- Resolver endpoints now preserve encrypted-DNS identity metadata separately from IP/port.
- Each consensus resolver observation can carry its own deep profile while authority-majority and DNSSEC-rooted answer assessments remain independent.

### ServiceLib

- Added typed resolver-profile request, per-transport probe, and aggregate result DTOs.
- Added `ProfileResolverAsync`.
- Qualification and consensus DTOs now expose deep-profile toggles/results and encrypted-DNS endpoint identity metadata.
- DNSSEC domain-validation DTOs expose the composed denial proof.
- Contract tests cover DoT/DoH identity serialization, EDNS/TXT/encrypted-DNS evidence, composed NXDOMAIN proof shape, and embedded qualification depth.

### Remaining depth work

The next DNS batch should focus on evidence quality rather than adding more protocol names: deterministic real-zone DNSSEC proof fixtures, stronger NSEC3 opt-out/insecure-delegation corpus cases, DoT/DoH retry/quorum behavior, HTTP/2 and ALPN observations, EDNS option behavior, and transparent DNS-injection comparison across classic and encrypted transports. These results can then feed ranking and Reviver DNS repair strategies without introducing another DNS implementation.


## Continuation checkpoint: resolver quorum, protocol evidence, and Reviver integration

This checkpoint hardens resolver-depth measurements from single observations into repeated evidence suitable for ranking and repair decisions.

### Repeated sampling and quorum

- Resolver-depth probes now run multiple attempts per transport instead of treating a single response as definitive.
- Default depth sampling is three attempts with majority quorum; callers may request up to seven attempts and choose the minimum-success threshold.
- Every transport profile preserves:
  - attempts;
  - successes;
  - quorum result;
  - reliability ratio;
  - median latency;
  - representative observation;
  - all individual samples.
- The representative observation is selected from the dominant answer signature and median-latency sample rather than simply using the last response.
- Raw RPC callers with invalid explicit quorum settings are rejected. Zero remains the engine-level "use defaults" value; ServiceLib emits concrete defaults.

### Answer identity and cross-transport comparison

- Every successful A/TXT/DoT/DoH observation now carries the normalized DNS answer signature already used by the authority/consensus layer.
- UDP and TCP agreement is based on answer identity rather than answer count alone.
- Classic DNS and encrypted DNS are compared only when each available transport group has an internally stable signature.
- If DoT and DoH disagree with each other, they do not form an encrypted baseline and therefore cannot manufacture a classic-vs-encrypted interception signal.
- Evidence reasons distinguish:
  - `classic-transports-diverge`;
  - `encrypted-transports-diverge`;
  - `classic-encrypted-answer-divergence`;
  - `classic-transports-agree-but-encrypted-differs`;
  - `encrypted-transports-agree-against-classic`.
- `interceptionSuspected` is deliberately an evidence label, not a categorical diagnosis. DNS load balancing, policy resolvers, ECS, filtering, or resolver-specific behavior can also cause cross-transport divergence.

### DoT / DoH protocol evidence

- DoT samples now preserve negotiated ALPN in addition to TLS version, SNI identity, certificate verification, latency, and DNS answer identity.
- DoH explicitly attempts HTTP/2, records the actual HTTP protocol version and negotiated ALPN, and requires a successful DNS-message parse.
- DoH responses with a non-`application/dns-message` content type are rejected as invalid DNS-over-HTTPS observations instead of being treated as success.
- Physical address pinning remains separate from TLS/HTTP logical identity.

### Explainable quality classification

Resolver-depth output now derives an explicit quality class instead of an opaque aggregate score:

- `strong`: stable classic/encrypted agreement with encrypted DNS available;
- `usable`: quorum-backed resolver behavior without a material warning;
- `degraded`: EDNS downgrade or transport disagreement;
- `unstable`: insufficient reliability despite some responses;
- `suspicious`: internally stable classic/encrypted answer divergence;
- `unusable`: no transport reaches quorum;
- `unknown`: insufficient evidence.

The output also exposes:
- a reliability floor across attempted transports;
- number of transports meeting quorum;
- quality reason codes;
- interception evidence reason codes.

### Qualification and consensus controls

- Standalone `dns.resolver.profile` accepts `attempts` / `minSuccesses`.
- Deep resolver qualification accepts `depthAttempts` / `depthMinSuccesses`.
- Deep consensus profiling uses the same controls across candidate/trusted resolver profiles.
- Expensive repeated encrypted-DNS probes remain opt-in.

### Reviver

- `ResolverDepthRepairEvidence` converts typed Discovery depth results into ordinary explainable `RepairEvidence`.
- The adapter preserves quality, reliability floor, quorum count, EDNS behavior, encrypted-DNS availability, divergence/suspicion reasons, and per-transport quorum/latency/protocol metadata.
- `RepairCandidateRanker` now exposes a small `ResolverQuality` component when this evidence exists.
- Runtime/core validation still dominates ranking; resolver-depth evidence contributes only 5% of the final score and missing depth evidence is neutral.
- Suspicion evidence is explicitly capped at a low resolver-quality value so a superficially reliable but cross-transport-divergent resolver cannot receive a strong DNS evidence contribution.
- This does not make DNS evidence authoritative for proxy viability: candidates must still pass the existing real-core runtime quorum before they are rankable.

Next work should focus on real-world fixture capture, catalog refresh governance, and explicit UI/application of DNS recommendations; the resolver identity catalog, denial-proof corpus expansion, composite repair inspection, and first bounded DNS Reviver strategy are now landed in the following checkpoint.


## Continuation checkpoint: policy-aware resolver catalog + composite DNS repair planning

This checkpoint turns the previous DNS evidence primitives into an operational, bounded repair path while preserving a strict separation between measurement, recommendation, and mutation.

### Versioned resolver identity catalog

- `resolvercatalog` is now the single built-in source of identity metadata for default public recursive resolvers used by Discovery.
- Catalog version `2026-09-23` is emitted in RPC results and copied into Reviver evidence so a future recommendation can be traced to the exact resolver dataset that produced it.
- Each catalog identity contains:
  - stable catalog ID;
  - provider/name;
  - IPv4 and IPv6 service addresses;
  - classic DNS port;
  - DoT logical server name and port;
  - DoH HTTPS URL;
  - DNSSEC-validation capability metadata;
  - policy class;
  - whether it is eligible to act as a neutral reference;
  - source URLs;
  - verification date.
- The current built-in set contains Cloudflare standard, Google Public DNS, and Quad9 Secure.
- Cloudflare and Google are classified as neutral/reference-eligible in this context.
- Quad9 Secure is deliberately classified as `security-filtering` and is **not** reference-eligible because its standard secure service intentionally blocks domains identified as malicious.
- Catalog validation rejects:
  - duplicate/empty IDs;
  - invalid IPv4/IPv6 addresses;
  - missing or invalid DNS/DoT ports;
  - missing DoT identity;
  - non-HTTPS/invalid DoH URLs;
  - non-neutral reference-eligible entries;
  - missing/invalid source URLs;
  - invalid verification dates.
- `dns.resolver.catalog` validates the compiled catalog before exposing it and returns both the complete set and the reference-eligible subset.

### Policy-aware consensus

- Default trusted resolver definitions are now derived from the catalog instead of a separate hard-coded IP list.
- Their DoT/DoH identities and policy classes therefore remain attached to the same resolver throughout classic/deep measurements.
- Resolver observations now retain:
  - `catalogId`;
  - policy;
  - `referenceEligible`.
- `dns.consensus.compare` now exposes two deliberately different aggregates:
  - `consensus`: all valid authority/trusted/candidate evidence;
  - `referenceConsensus`: authoritative evidence plus neutral reference-eligible trusted resolvers only.
- Security-filtering resolvers and candidate resolvers remain visible in the all-evidence consensus but do not silently redefine the neutral reference.
- A real comparison regression test verifies that a filtering trusted resolver can disagree with authority/neutral recursive DNS, making the full evidence divergent while the reference consensus remains unanimous.

### Composite DNS repair diagnostic

- New `dns.repair.inspect` centralizes the network-side evidence Reviver needs for DNS failures.
- For both A and AAAA it:
  1. attempts the existing root-anchored DNSSEC domain validation;
  2. preserves the resulting trace and DNSSEC status when validation completes;
  3. falls back to ordinary iterative trace if DNSSEC validation itself is unavailable;
  4. marks that fallback explicitly instead of presenting it as authenticated evidence.
- The same result includes:
  - IPv4 family evidence;
  - IPv6 family evidence;
  - full traces;
  - optional DNSSEC validation object;
  - authentication/status;
  - fallback state/error;
  - resolver catalog version;
  - resolver recommendations.
- Resolver-catalog structural validation is part of this composite diagnostic, so repair evidence cannot be built on malformed built-in identity data.
- ServiceLib exposes the diagnostic through `InspectDnsRepairAsync`.
- `IDiscoveryDnsDiagnosticClient` intentionally exposes the composite repair inspection seam to Reviver rather than requiring repair code to coordinate four separate DNS operations.

### Reviver DNS evidence projection

- `DiscoveryDnsRepairEvidenceProvider` now only projects the composite Discovery result into Reviver models.
- It no longer duplicates DNSSEC/fallback/catalog orchestration in C#.
- `DnsRepairObservation` retains:
  - observed IPv4/IPv6 addresses;
  - trace completeness;
  - DNSSEC authentication/status;
  - per-family errors;
  - catalog version;
  - resolver recommendations.
- Resolver recommendations are ordered with neutral reference-eligible identities first.
- They remain **evidence/recommendations only**; this checkpoint does not rewrite PattN's global `DNSItem`, `NormalDNS`, `TunDNS`, or application DNS settings.

### Dedicated bounded DNS repair strategy

- New `DnsAddressFamilyStrategy` handles only:
  - `DnsResolutionFailure`;
  - `NoUsableAddressFamily`.
- It applies only to ordinary hostname-based profiles with Xray-compatible structured generators.
- Literal-IP profiles, complex/custom outbound profiles, and unrelated failure classes are rejected.
- If Discovery observes neither A nor AAAA, the strategy generates no candidate. It never guesses a family.
- Candidate planning is bounded by observed address-family evidence:
  - IPv4-only → `UseIPv4`; `ForceIPv4` is additionally considered only for `NoUsableAddressFamily`.
  - IPv6-only → `UseIPv6`; `ForceIPv6` likewise only for the explicit no-family failure.
  - dual-stack → bounded `UseIPv4v6` / `UseIPv6v4` variants, plus single-family variants only for `NoUsableAddressFamily`.
  - when IPv6 has root-anchored authenticated evidence and IPv4 does not, `UseIPv6v4` is ordered first; otherwise IPv4-first remains the conservative first dual-stack candidate.
- If the current profile is on sing-box but the exact profile shape has a structured Xray generator, the strategy may pair the target-strategy mutation with the already-supported local core fallback to Xray.
- Mutation guards enforce that this strategy can change only:
  - `ProfileItem.TargetStrategy`; or
  - `CoreType` + `TargetStrategy` when the Xray switch is required.
- SNI, Host, path/service name, credentials, Reality fields, ALPN, fingerprint, transport extras, endpoint address, and port remain untouched.
- `ProfileInvariantRegistry` now rejects unknown target-strategy values so a future strategy cannot emit arbitrary strings.

### Reviver composition and failure containment

- New `ReviverStrategyCatalog.CreateDefault` composes the built-in:
  - DNS address-family repair;
  - endpoint replacement;
  - core fallback.
- `ReviverService` still owns actual ordering by failure-specific priority, confidence, strategy ID, and global/per-strategy candidate budgets.
- DNS address-family repair has higher priority than endpoint replacement for DNS/address-family failures because it preserves the remote endpoint and changes only local resolution behavior.
- Discovery diagnostic failures are fail-closed for this strategy: cancellation propagates, but other diagnostic errors produce no DNS candidate and do not prevent the rest of Reviver from continuing.
- Every DNS candidate still has to pass the existing real-core runtime validation quorum before it can be ranked or recommended.

### Reviver evidence

DNS candidates carry explicit evidence rather than a hidden verdict:

- `discovery.dns.address-family`
  - hostname;
  - observed A/AAAA counts and addresses;
  - per-family DNSSEC authentication/status.
- `discovery.dns.resolver-recommendations`
  - resolver catalog version;
  - neutral reference-eligible catalog IDs;
  - complete catalog IDs;
  - DoH URLs;
  - DoT logical identities.
- Global DNS remains unchanged automatically. Applying one of those resolver recommendations should be an explicit future settings-level operation with its own validation and rollback semantics.

### Expanded denial-proof corpus

The DNSSEC proof tests now include deterministic edge cases beyond the initial happy paths:

- incomplete NSEC NXDOMAIN where closest-encloser and next-closer evidence exist but wildcard nonexistence is missing;
- exact-owner NSEC with CNAME, which must not be treated as NODATA;
- NSEC3 exact-owner NODATA;
- NSEC3 complete closest-encloser/next-closer/wildcard NXDOMAIN composition;
- NSEC3 opt-out evidence for insecure DS delegation;
- incompatible NSEC3 parameter sets remaining unsupported rather than being combined.

These are synthetic deterministic fixtures intended to enforce proof semantics. The next corpus step remains capture/replay of known real signed responses so wire parsing, canonicalization, chain validation, and proof composition are tested together.

### End-to-end repair regression

An integration-style ServiceLib test now exercises:

1. baseline diagnosis returns `DnsResolutionFailure`;
2. Discovery evidence reports an authenticated IPv4-only hostname;
3. the default Reviver strategy catalog plans exactly the bounded `UseIPv4` candidate;
4. static invariants accept it;
5. runtime validation reaches quorum;
6. ranking receives it;
7. Reviver recommends it.

This closes the first full Discovery → diagnosis → evidence → mutation planning → runtime validation → ranking loop for DNS repair.

### Next boundary

The next broad batch should move from internal plumbing to operational lifecycle:

- capture/replay real DNSSEC A/AAAA/NXDOMAIN/NODATA/NSEC3 fixtures;
- add a governed resolver-catalog refresh/check workflow so stale identities are detectable;
- profile catalog identities periodically and retain historical reliability rather than assuming metadata implies reachability;
- build an explicit DNS recommendation/apply workflow for `DNSItem` / TUN DNS with snapshots and rollback rather than silent global mutation;
- feed resolver-policy/reference consensus into the Discovery UI;
- add repair-history persistence for DNS evidence and compare pre/post-repair outcomes;
- let Reviver choose between address-family repair and endpoint replacement using accumulated historical evidence, while preserving the existing bounded-search and real-core validation gates.


## Continuation checkpoint: persistent DNS repair history + governed resolver lifecycle + transactional DNS settings

This checkpoint moves DNS repair from a stateless diagnostic loop into a governed operational lifecycle. The core rule remains unchanged: current measurements and real-core validation are authoritative for the present repair attempt; historical data is advisory and cannot bypass validation.

### Persistent DNS repair history

- PattN now has a dedicated SQLite entity, `DnsRepairHistoryItem`, registered with the existing application database during startup.
- History records preserve:
  - normalized hostname;
  - profile/session/candidate IDs;
  - event kind;
  - target strategy;
  - family/family-order classification;
  - candidate state;
  - resolver catalog version;
  - validation attempts/successes/consecutive successes;
  - loss and median latency;
  - runtime-quorum outcome;
  - observation timestamp;
  - serialized evidence payload.
- Discovery observations and candidate-validation outcomes remain distinct event kinds.
- `SqliteDnsRepairHistoryStore` exposes:
  - observation recording;
  - candidate validation recording;
  - bounded-window summaries;
  - age-based pruning.
- The default summary window is 30 days.
- Historical IPv4/IPv6 pass rates intentionally use only **single-family** `UseIPv4`/`ForceIPv4` and `UseIPv6`/`ForceIPv6` validations.
- Dual-stack `UseIPv4v6` / `UseIPv6v4` outcomes are still persisted, but they do not count as proof that the first family actually carried traffic because the core may have fallen back to the other family.

### History-informed planning without self-reinforcing guesses

- `DnsAddressFamilyStrategy` can consume an optional `IDnsRepairHistoryStore`.
- The current Discovery observation is recorded before historical summary lookup.
- History failures are advisory failures: they do not abort a repair attempt.
- Dual-stack candidate order may be influenced by historical single-family runtime outcomes only when:
  - both IPv4 and IPv6 have at least 3 historical validation samples; and
  - their runtime-quorum pass rates differ by **more than 20 percentage points**.
- Exact 20-point differences remain neutral.
- Sparse history remains neutral.
- If mature history is neutral, current DNS evidence determines ordering as before.
- History never creates a candidate for a family that current Discovery did not observe.
- Historical preference changes ordering only; every candidate still passes static invariants and current real-core runtime validation.

### Reviver lifecycle observers

- New `IRepairLifecycleObserver` provides explicit planned/validated hooks.
- `ReviverService` emits those hooks after a candidate enters static-valid state and after runtime validation resolves to success/failure.
- Observer failures are isolated and logged rather than aborting repair planning or validation.
- Cancellation remains authoritative and is propagated.
- `DnsRepairHistoryObserver` records DNS candidate validation outcomes into the history store.
- `ReviverStrategyCatalog.CreateDefaultObservers` binds the DNS history observer from the same store supplied to the history-aware DNS strategy, reducing composition drift between "read history" and "write history" wiring.

### Resolver catalog freshness governance

- `resolvercatalog.Audit` now reports:
  - catalog version;
  - structural validity;
  - configured maximum age;
  - per-identity verification date;
  - age in days;
  - per-identity stale state;
  - total stale count.
- New `dns.resolver.catalog.audit` exposes this through the Discovery RPC.
- ServiceLib exposes the typed audit result through `AuditResolverCatalogAsync`.
- Invalid explicit freshness windows are rejected.
- New `pattn-discovery/cmd/catalog-audit` provides a CI/operations entry point:
  - exit 0 when the catalog is valid and fresh;
  - exit 1 when any identity is stale or the catalog is invalid;
  - exit 2 for invalid command arguments/output failures.
- A scheduled GitHub workflow now runs every Monday and on manual dispatch.
- The workflow runs the catalog/unit tests and enforces a 120-day maximum verification age.
- This is deliberately a **metadata freshness gate**, not a claim that every resolver is currently reachable from every network. Live reachability/reliability remains measurement data and should be stored separately.

### Explicit transactional application-level DNS repair

Global/application DNS mutation remains separate from automatic profile Reviver planning.

- New `DnsSettingsRepairService` prepares an explicit `DnsSettingsRepairPlan` from a chosen catalog recommendation.
- Preparation is side-effect-free.
- The current `SimpleDNSItem` is deep-snapshotted before a plan is produced.
- The first implementation changes only:
  - `SimpleDNSItem.RemoteDNS`;
  - `SimpleDNSItem.BootstrapDNS`.
- `DirectDNS`, Hosts, FakeIP, routing strategies, stale-serving, parallel query settings, and all custom per-core `DNSItem` documents remain untouched.
- DoH is preferred for `RemoteDNS` when the recommendation provides one.
- A catalog IP is used for bootstrap.
- The plan records:
  - resolver catalog ID/name/policy;
  - whether the resolver is neutral-reference eligible;
  - resolver catalog version;
  - complete before/after snapshots;
  - before/after fingerprints;
  - exact changed fields.

### Stale-plan protection and compensation

`ApplyAsync` is transactional-by-compensation:

1. fingerprint the current `SimpleDNSItem`;
2. reject if it differs from the plan's original snapshot;
3. apply the detached after-snapshot in memory;
4. persist through the existing atomic `ConfigHandler.SaveConfig`;
5. if persistence fails, restore the previous in-memory snapshot and attempt to persist the restoration;
6. return a receipt only after successful persistence.

This prevents a prepared DNS recommendation from overwriting user edits made after preparation.

### Rollback conflict protection

- The receipt preserves the exact before/applied snapshots and applied fingerprint.
- Normal rollback refuses to run if current settings no longer match the applied snapshot.
- This protects newer user edits from being overwritten by an old rollback operation.
- A caller may explicitly request a forced rollback when that overwrite is intentional.
- If rollback persistence fails, the pre-rollback in-memory state is restored and a compensating save is attempted.

### Test coverage added in this checkpoint

- catalog freshness audit with deterministic fresh/stale dates;
- catalog-audit CLI exit semantics;
- RPC capability/argument coverage for `dns.resolver.catalog.audit`;
- ServiceLib catalog-audit DTO contract;
- history preference minimum-sample and minimum-delta behavior;
- mature history reordering dual-stack candidates without creating new candidates;
- Reviver lifecycle observer planned/validated notifications;
- observer-failure isolation;
- DNS settings plan side-effect isolation;
- preservation of unrelated SimpleDNS fields;
- successful apply + rollback;
- stale-plan rejection;
- rollback conflict protection;
- forced rollback;
- compensation after persistence failure;
- default composition of DNS history lifecycle observer.

### Next operational boundary

The next broad batch should focus on **live measurement lifecycle and UI visibility**, not additional hidden automation:

- scheduled resolver catalog reachability/depth profiling as non-gating telemetry;
- persistence of resolver-depth quality history separate from profile-repair history;
- historical decay/hysteresis instead of a fixed 30-day flat window;
- Discovery UI pages for resolver identity, freshness, current quality, historical reliability, and disagreement reasons;
- an explicit UI preview/apply/rollback flow for `DnsSettingsRepairPlan`;
- persisted promotion/rollback outcomes linked to repair history;
- real captured DNS wire fixtures replayed through trace/DNSSEC/denial validation;
- comparison of pre-repair and post-repair runtime outcomes before recommending long-lived promotion.


## Continuation checkpoint: resolver telemetry, DNS health UI, and repair outcome history

This checkpoint makes the previous DNS/reviver lifecycle observable instead of leaving its evidence hidden behind services.

### Resolver-depth telemetry persistence

- New `DnsResolverTelemetryItem` stores non-gating resolver profile observations independently from profile-repair history.
- Each row preserves:
  - resolver catalog ID/version;
  - provider/name/address;
  - policy and reference eligibility;
  - current resolver-depth quality/status;
  - reliability floor;
  - quorum transport count;
  - interception-suspicion state;
  - representative median latency;
  - derived decayed health score/class;
  - reason codes;
  - probe error;
  - observation timestamp.
- Telemetry history is bounded to the configured dashboard history window instead of growing indefinitely.
- Current probe failures are persisted as observations rather than disappearing from history.

### Decayed health model

Resolver health is deliberately not a simple "latest result wins" flag.

- The dashboard uses exponentially decayed historical samples.
- Default history window: 90 days.
- Default half-life: 14 days.
- Each sample combines:
  - resolver reliability floor (65%);
  - explicit Discovery quality class (35%).
- Interception-suspicion evidence caps a sample to a low score.
- A failed current probe contributes zero to the decayed score.
- Older samples lose half their influence every half-life.
- Unknown/no-history remains neutral rather than automatically healthy or unhealthy.

### Hysteretic health classes

The displayed state uses explicit hysteresis:

- `healthy`
- `watch`
- `degraded`
- `suspicious`
- `unavailable`

Examples of hysteresis behavior:

- a previously healthy resolver remains healthy through moderate score noise and must fall materially before becoming degraded;
- a previously degraded resolver must recover materially before becoming healthy;
- `watch` acts as the transition band;
- current interception suspicion always produces `suspicious`;
- a current probe failure always produces `unavailable`, regardless of historical score.

This means history damps noisy quality movement but never hides a failed live observation.

### Non-gating live resolver profiling

- `DnsHealthDashboardService` profiles the current versioned catalog using the existing `dns.resolver.profile` path.
- Each catalog resolver is isolated: one resolver failure does not abort the remaining probes.
- Profiles use repeated quorum sampling and include classic DNS plus catalog-provided DoT/DoH identities.
- Resolver diagnostics remain advisory. They do not gate PattN startup, ordinary proxy use, or DNS settings.
- The default UI behavior on open is cache-only: opening DNS settings does not start network probes.
- Live network profiling occurs only after the user explicitly selects **Refresh DNS health**.

### Persisted resolver catalog audit

Catalog freshness itself is now persisted in `DnsResolverCatalogAuditItem`.

The persisted audit contains:

- catalog version;
- structural validity;
- maximum allowed metadata age;
- stale entry count;
- audit timestamp;
- serialized per-entry freshness details;
- audit error.

This allows the UI to distinguish:

- no catalog audit has ever been recorded;
- cached catalog audit from a previous session;
- a fresh audit from the current refresh.

A catalog audit failure no longer prevents otherwise valid resolver profiling.

### DNS health UI

The existing DNS settings window now contains a dedicated **DNS health** tab in both desktop frontends:

- Avalonia;
- WPF.

The surface shows:

- an explicit advisory/non-gating explanation;
- manual **Refresh DNS health** action;
- healthy/watch/degraded counts;
- resolver catalog version/validity/freshness;
- last catalog-audit timestamp;
- last resolver telemetry timestamp;
- per-resolver health/quality/reliability/latency/policy/reference role;
- Discovery reason codes;
- per-resolver/current diagnostic errors.

The refresh button is disabled while a refresh is active so overlapping resolver profile batches cannot be launched from repeated clicks.

New text is defined in the neutral resource catalog and exposed through `ResUI.Designer.cs`; satellite cultures fall back to the neutral strings until translated.

### Baseline -> candidate outcome comparison

Reviver sessions now retain `RepairDiagnosis.RuntimeValidation` as `RepairSession.BaselineValidation`.

Promotion preparation therefore has access to the same baseline runtime evidence that diagnosed the repair attempt.

New `RepairOutcomeComparer` produces an explainable comparison with:

- baseline/candidate reliability;
- reliability delta;
- baseline/candidate median latency;
- latency delta;
- baseline/candidate loss;
- loss delta;
- verdict;
- reason codes.

Verdicts:

- `unknown`: no baseline runtime evidence;
- `improved`;
- `stable`;
- `regressed`.

The verdict is not an opaque score. Material reliability, latency, and loss changes each emit an explicit reason such as:

- `reliability-improved`;
- `reliability-regressed`;
- `latency-improved`;
- `latency-regressed`;
- `loss-improved`;
- `loss-regressed`.

Small runtime noise remains `stable`.

### Promotion/rollback history

New `RepairPromotionHistoryItem` persists lifecycle outcomes separately from DNS telemetry and candidate-validation history.

A successful promotion record preserves:

- session/candidate/original/promoted profile IDs;
- previous default profile ID;
- whether the repaired profile became default;
- candidate score;
- mutations;
- baseline validation;
- candidate validation;
- before/after outcome comparison;
- outcome verdict;
- promotion timestamp.

Rollback records preserve the linkage back to the same session/candidate/profile IDs.

`RepairPromotionService` accepts an optional `IRepairPromotionHistoryStore`:

- history is written only after the user action succeeds;
- history-write failure is logged and does not roll back a successful promotion/rollback;
- cancellation after the committed user action cannot re-label that completed action as a failure merely because history recording was skipped or failed.

### Storage registration

Application startup now creates the following lifecycle tables alongside PattN's existing SQLite entities:

- `DnsRepairHistoryItem`;
- `DnsResolverTelemetryItem`;
- `DnsResolverCatalogAuditItem`;
- `RepairPromotionHistoryItem`.

No second persistence engine is introduced.

### Regression coverage added

- decayed resolver weighting favors recent observations;
- interception suspicion caps resolver health;
- hysteresis transition boundaries;
- live unavailability overrides historical health;
- baseline runtime validation propagates through an end-to-end DNS Reviver run;
- promotion preparation retains baseline validation;
- promotion preparation emits before/after outcome comparison;
- unknown-baseline comparison;
- material improvement reasons;
- material regression reasons;
- stable/noise comparison;
- DNS health UI/resource names and bindings were statically audited across both frontends.

### CI gate correction

The absence of PR workflow runs was traced to branch filtering rather than an assumed Actions outage:

- PR #1 targets `my-releases`;
- `.github/workflows/test.yml` previously accepted pull requests only into `master`;
- the workflow now accepts both `master` and `my-releases` for pull requests and pushes;
- UI paths under both `v2rayN/v2rayN/**` and `v2rayN/v2rayN.Desktop/**` are now trigger paths;
- a Windows `ui-build` job compiles both WPF and Avalonia projects;
- `workflow_dispatch` is enabled for explicit validation runs.

The existing Linux test job still runs the full Go test/vet suite and ServiceLib test project. The additional Windows job exists specifically to catch XAML/resource/code-behind compile failures that ServiceLib-only tests cannot see.

### Next boundary

The next broad slice can build on this without adding hidden automation:

1. Add a typed recent-telemetry/history query API for richer charts/tables instead of the current compact status text.
2. Add an explicit resolver-selection + DNS repair preview flow in the DNS health tab, backed by the already-landed transactional `DnsSettingsRepairService`.
3. Add a user-visible promotion/rollback history surface with outcome comparison.
4. Capture real DNS wire fixtures and replay them through the Go trace/DNSSEC/denial stack.
5. Add endpoint-pool history/hysteresis for historically-good proxy endpoints, separate from resolver telemetry.
6. Add optional user-configurable telemetry refresh cadence only after an explicit product setting exists; do not introduce silent periodic network probes.


## Continuation checkpoint: explicit DNS resolver selection, preview, apply, and persisted rollback

This checkpoint turns resolver telemetry into a user-controlled settings workflow without allowing telemetry or Reviver to silently mutate application DNS.

### Live resolver options

- A successful live DNS health refresh now projects the current validated resolver catalog into typed `DnsResolverOption` records.
- Options preserve:
  - catalog/provider/name;
  - IPv4/IPv6 endpoints;
  - classic DNS port;
  - DoT identity/port;
  - DoH URL;
  - policy class;
  - neutral-reference eligibility;
  - current health/quality;
  - current quorum transport count;
  - interception-suspicion state.
- Resolver options are intentionally **live-session data**. Cached telemetry can still be displayed after reopening DNS settings, but a resolver cannot be selected for a new apply operation until a new live refresh supplies the current catalog/options.
- This prevents stale cached metadata from being silently promoted into a settings mutation.

### Apply safety gate

A resolver is considered eligible for application only when all of the following are true:

1. a live resolver option is explicitly selected;
2. its current profile has at least one quorum transport;
3. no interception-suspicion evidence is present;
4. its current health class is `healthy` or `watch`;
5. the resolver catalog audit from the live refresh is known, structurally valid, and has zero stale identities under the configured age policy;
6. PattN's Simple DNS path is actually enabled.

A `degraded`, `suspicious`, `unavailable`, no-quorum, stale-catalog, invalid-catalog, or custom-DNS-active state can still be inspected in Preview but cannot be applied through the UI.

### Side-effect-free preview

The DNS Health tab now exposes a resolver selector and a dedicated **Preview change** action.

Preview:

- creates the already-landed `DnsSettingsRepairPlan`;
- does not modify `Config`;
- shows resolver name/policy/current health/catalog version;
- shows exact `RemoteDNS` before -> after;
- shows exact `BootstrapDNS` before -> after;
- lists the exact changed fields;
- explains every active apply blocker.

The preview explicitly states that Apply persists immediately and that closing/canceling the DNS settings window is not rollback.

### Explicit transactional apply

**Apply DNS repair** reuses `DnsSettingsRepairService` and therefore retains the existing stale-plan and compensating-save guarantees.

The UI adds stricter preconditions than the raw service:

- fresh valid catalog;
- safe current live resolver telemetry;
- Simple DNS enabled.

Only `RemoteDNS` and `BootstrapDNS` are changed. No custom Xray/sing-box DNS document, Direct DNS, routing strategy, host map, FakeIP setting, or unrelated option is touched.

After successful persistence the bound DNS fields are immediately synchronized with the applied snapshot so a later ordinary Save cannot accidentally restore stale UI values.

### Persistent DNS settings undo history

New `DnsSettingsRepairHistoryItem` uses PattN's existing SQLite database.

Each event stores:

- apply/rollback event kind;
- plan ID;
- resolver catalog ID/version;
- complete serialized repair receipt;
- timestamp.

`SqliteDnsSettingsRepairHistoryStore` supports:

- recording successful applies;
- recording successful rollbacks;
- recovering the newest apply that has not subsequently been rolled back.

History writes occur only **after** the actual settings persistence succeeds. A history-write failure is logged but never converts an already-committed user action into an apparent failure.

### Stepwise rollback

- The DNS settings window restores the latest active repair receipt when reopened.
- Rollback continues using the existing full-SimpleDNS fingerprint conflict guard, so it refuses to overwrite newer persisted DNS edits.
- After one successful rollback, the UI reloads the next previous active receipt, if one exists.
- Multiple sequential resolver applications therefore form a conservative stepwise undo stack rather than losing the earlier before-snapshot.
- Forced rollback remains available only at the service layer; the normal UI does not bypass conflict protection.

### UI integration

Both WPF and Avalonia DNS Health tabs now contain:

- resolver selector;
- Preview change;
- Apply DNS repair;
- Rollback DNS repair;
- repair status;
- read-only exact plan preview;
- explicit persistence/rollback safety text.

Reactive command availability is driven by the same view-model safety state. Resolver refresh temporarily disables Preview/Apply/Rollback to prevent an old plan from racing a catalog/telemetry refresh.

### CI remediation discovered while enabling the UI gate

The newly-active CI gate uncovered earlier integration drift and an Avalonia checkout assumption:

- XHTTP normalization passed a `char` to the `StartsWith(string, StringComparison)` path; corrected.
- golden profile tests still referenced the pre-split logical-host API; aligned to `ResolveServerName`.
- `TransportPathParameters.Remove(...)` was restored as a loss-conscious multi-parameter convenience API expected by the golden WS metadata test.
- nullable score assertions were rewritten for the current TUnit assertion constraints.
- WPF now compiles successfully in the UI gate.
- Avalonia's `GlobalHotKeys` dependency is a repository submodule; the UI-build checkout now uses `submodules: recursive` instead of incorrectly treating the missing types as an application compile defect.

### Next boundary

After this checkpoint is green in CI, the next broad slice can focus on richer history and evidence presentation:

- typed time-series query API for resolver telemetry;
- compact resolver history table/trend presentation instead of only latest-line summaries;
- user-visible DNS apply/rollback event history;
- user-visible Reviver promotion/rollback history with baseline-vs-candidate deltas;
- real DNS packet capture/replay fixtures;
- historically-good proxy endpoint pools with age decay and hysteresis;
- optional scheduled telemetry only behind an explicit user setting.


## Continuation checkpoint: read-only resolver trends and DNS/Reviver lifecycle history

This checkpoint adds historical visibility on top of the already-persisted telemetry and repair lifecycle tables. It is intentionally read-only: loading history does not start Discovery, probe the network, create repair plans, or mutate configuration.

### Bounded local history query

New `DnsHistoryService` reads only PattN's existing SQLite lifecycle data:

- `DnsResolverTelemetryItem`;
- `DnsSettingsRepairHistoryItem`;
- `RepairPromotionHistoryItem`.

Defaults:

- history window: 90 days;
- maximum displayed telemetry points per resolver: 12;
- maximum recent lifecycle events: 30.

The query is bounded for UI use and does not expose an unbounded database dump.

### Resolver trend model

Each resolver trend preserves recent points with:

- timestamp;
- health class;
- Discovery quality;
- decayed health score;
- reliability floor;
- representative median latency;
- quorum transport count;
- interception-suspicion state;
- current error.

Trend direction is deliberately coarse and explainable rather than predictive:

- fewer than 4 samples -> `insufficient`;
- compare the newer half and older half of up to the 10 most recent samples;
- recent average at least 8 percentage points higher -> `improving`;
- recent average at least 8 points lower -> `declining`;
- otherwise -> `stable`.

This is an observational summary of recorded measurements, not a forecast and not an automatic resolver-selection signal.

### Lifecycle event stream

The local history view combines two existing transactional histories:

- application DNS:
  - DNS apply;
  - DNS rollback.
- profile Reviver:
  - Reviver promotion;
  - Reviver rollback.

Events retain their original persisted timestamps and identifying linkage. Promotion rows also display the recorded baseline-vs-candidate outcome verdict when available.

### DNS Health history surface

The DNS Health tab now has read-only historical sections in both WPF and Avalonia:

- resolver history/trend summary;
- recent DNS / Reviver actions;
- history last-updated timestamp;
- history diagnostics.

History loads from SQLite when the DNS settings window opens and refreshes after:

- explicit live DNS health refresh;
- successful DNS apply;
- successful DNS rollback.

Opening the window still performs no live resolver/network probe.

### Separation from decision logic

Historical trend direction is not wired into the DNS settings Apply gate in this checkpoint.

Apply eligibility continues to depend only on the previously-established current-state controls:

- fresh valid live resolver catalog audit;
- current live resolver quorum;
- no current interception suspicion;
- current resolver health `healthy` or `watch`;
- Simple DNS enabled;
- valid non-stale settings repair plan.

This avoids converting a convenience trend visualization into a hidden policy input.

### Regression coverage

New tests enforce the trend interpretation boundaries:

- sparse history remains `insufficient`;
- materially improving history reports `improving`;
- materially worsening history reports `declining`;
- small score movement remains `stable`.

### Next boundary

After this slice, the next broad work can move beyond compact text summaries while retaining the same semantics:

- structured resolver-history grids with selectable time windows;
- lightweight trend visualization from the typed point series;
- user-visible DNS apply/rollback rows with exact before/after details;
- user-visible Reviver promotion/rollback rows with runtime deltas and mutation summaries;
- export/diagnostic copy support for Discovery evidence;
- real DNS packet capture/replay fixtures;
- historically-good endpoint pools with independent age-decay/hysteresis.


## Continuation checkpoint: historically-good endpoint pools and source-diverse candidate composition

This checkpoint adds durable endpoint reuse without turning historical success or user pinning into implicit trust.

### Two distinct endpoint stores

Endpoint state is deliberately split into two independent concepts.

#### Automatic endpoint observation history

`EndpointObservationHistoryItem` stores results from actual Discovery endpoint probes scoped by:

- logical host;
- port;
- transport/network;
- stream security;
- physical IP address.

Each observation retains:

- source/provenance;
- provider / ASN / POP metadata;
- probe attempts/successes/consecutive successes;
- qualified state;
- measured reliability;
- median latency;
- probe errors;
- observation timestamp.

History is therefore profile-identity scoped. An IP that worked for one logical TLS/CDN identity or transport is not automatically reused for unrelated profiles.

#### Explicit endpoint pool

`EndpointPoolItem` stores user/curated endpoint membership separately from automatic history.

Pool entries retain:

- the same logical-host/port/network/security scope;
- physical IP;
- optional label;
- enabled state;
- pinned state;
- provider / ASN / POP metadata;
- creation/update timestamps.

Pool membership is explicit state. Automatic historical success does not silently create a pinned endpoint.

### Decayed historical eligibility

`SqliteEndpointHistoryStore` aggregates recent observations using a bounded policy.

Default policy:

- history window: 30 days;
- reliability half-life: 7 days;
- at least 2 qualified observations;
- at most 1 consecutive recent failed observation;
- minimum decayed reliability: 0.67;
- maximum historical candidates: 16.

Failed observations contribute only a small fraction of their raw reliability to the decayed score.

This creates failure hysteresis:

- one recent failure can retain a previously strong endpoint long enough to tolerate transient noise;
- two consecutive recent failed probes suppress it from historical nomination;
- old failures decay rapidly when newer repeated success is available.

Historical latency is derived only from qualified observations.

### Historical nomination never bypasses current validation

`EndpointHistoryCandidateSource` emits only historically-good candidates and honors `DiscoveryCandidateRequest.IncludeHistorical`.

When a historical endpoint re-enters candidate discovery:

1. it is nominated from local history;
2. `DiscoveryCandidateProvider` attempts the normal pinned logical Host/SNI endpoint probe again;
3. the new probe result is written back to endpoint history;
4. Reviver still performs current real-core candidate validation before recommendation.

If Discovery enrichment itself is unavailable, existing provider fault tolerance remains: the candidate may continue to Reviver, but current real-core validation remains authoritative.

### Explicit pool candidates

`EndpointPoolCandidateSource` honors `IncludeEndpointPools`.

Pinned pool entries receive candidate-budget retention priority, but pinning means only:

> retain this endpoint as an explicit candidate worth re-testing.

It does **not** mean:

- trust the endpoint;
- skip Discovery probing;
- skip mutation invariants;
- skip real-core validation;
- prefer a currently failing endpoint over a currently-qualified endpoint.

Final provider ordering remains current-probe-first, then pin retention, then measured reliability/latency.

### Source-diverse candidate budgeting

The old provider stopped collection once the global dictionary reached `MaxCandidates * 4`, allowing an early large source to starve every later source.

Candidate collection is now bounded per source:

- each source receives up to `max(4, MaxCandidates * 2)` accepted candidates;
- duplicate physical addresses are merged;
- only after all sources have contributed is the pre-probe set trimmed to a bounded `MaxCandidates * 8` pool.

This preserves source diversity without allowing an unbounded probe batch.

### Duplicate provenance merging

When multiple sources nominate the same IP, PattN no longer silently keeps only the first source.

Merged candidates preserve:

- all source names in `metadata.sources`;
- pin state if any source pinned the endpoint;
- best available provider / ASN / POP metadata;
- strongest pre-probe reliability;
- lowest known loss/latency;
- newest observation timestamp.

The subsequent live probe overwrites measurement fields with current evidence while preserving provenance metadata.

### Canonical endpoint composition

`DiscoveryCandidateComposition.CreateDefault` now defines the intended candidate stack:

1. explicit endpoint pool;
2. current DNS resolution;
3. historically-good endpoint observations.

The same history store is supplied to the provider as the probe-result sink.

This means history read/write composition cannot silently drift apart.

### Reviver evidence

`EndpointReplacementStrategy` now projects selected endpoint provenance into `discovery.endpoint` evidence when present:

- provider;
- ASN;
- POP;
- pinned state;
- historical state;
- merged sources;
- current probe-qualified state;
- historical sample count;
- historical qualified count;
- historical failure streak;
- historical decayed reliability;
- historical last-observed timestamp.

The actual mutation remains address-only. SNI, Host, credentials, Reality fields, paths, transports, port, and all unrelated profile semantics remain untouched.

### Persistence

PattN's existing SQLite startup now registers:

- `EndpointObservationHistoryItem`;
- `EndpointPoolItem`.

No second persistence engine or cache database is introduced.

### Regression coverage

New tests cover:

- one historical success is insufficient for automatic nomination;
- repeated historical successes are required;
- two recent consecutive probe failures suppress an otherwise strong endpoint;
- recent successes outweigh sufficiently old failed evidence through decay;
- duplicate endpoint sources merge provenance and pin state;
- current live probe results are persisted back into endpoint history;
- a large earlier source cannot starve a later pinned source;
- historical source obeys `IncludeHistorical=false`;
- endpoint-pool source obeys `IncludeEndpointPools=false`;
- Reviver endpoint evidence preserves pool/history/probe provenance while keeping the repair address-only.

### Next boundary

The next endpoint slice can add UI and richer lifecycle operations without changing the trust model:

- endpoint-pool management UI (pin/unpin, label, enable/disable, remove);
- user-visible history for a selected endpoint;
- explicit "promote validated endpoint to pool" action after successful Reviver validation;
- age/source/provider filtering;
- current-vs-history comparison;
- provider/ASN target adapters feeding the same candidate composition;
- automatic pruning on a low-frequency maintenance path rather than every repair attempt.


## Continuation checkpoint: explicit validated-endpoint promotion and pool administration

This checkpoint adds the user-directed operations needed to manage the endpoint pool created in the previous slice. Nothing here auto-promotes a Reviver result.

### Runtime-validated endpoint promotion

New `EndpointPoolPromotionService` promotes one existing Reviver candidate into the endpoint pool only when:

- the candidate belongs to the supplied repair session;
- candidate state is `RuntimeValidated`;
- runtime evidence still meets quorum;
- the candidate contains an explicit `ReplaceEndpoint` mutation on `ProfileItem.Address`;
- the repaired physical address is a literal IP;
- the original profile has a logical identity that can scope the pool entry.

The pool scope is built from the **original** profile:

- logical server name;
- original port;
- network/transport;
- stream security.

The promoted physical endpoint is built from the validated candidate.

This preserves the physical-destination vs logical-identity boundary: a successful IP is never saved as a global or unrelated endpoint.

### Promotion evidence carried into the pool

When available, promotion carries forward:

- provider;
- ASN;
- POP;
- current runtime reliability;
- runtime loss;
- runtime median latency;
- Reviver session ID;
- Reviver candidate ID;
- explicit `runtimeValidated=true` metadata.

The caller must explicitly choose whether the saved endpoint is pinned.

Promotion is not invoked by `ReviverService`, candidate ranking, or profile promotion. It is an explicit operation available to a caller/UI after the user has seen a validated repair result.

### Pool administration contracts

New `IEndpointPoolAdminStore` and typed models expose bounded pool management:

- `EndpointPoolQuery`
  - optional logical-host filter;
  - include/exclude disabled entries;
  - bounded maximum result count.
- `EndpointPoolUpdate`
  - item ID;
  - optional enabled state;
  - optional pinned state;
  - optional label.

`SqliteEndpointPoolStore` now implements both candidate-source storage and administrative storage.

Administrative listing remains bounded to at most 2000 requested entries.

### Explicit management service

`EndpointPoolManagementService` provides user-directed operations:

- list;
- pin / unpin;
- enable / disable;
- relabel;
- remove.

These operations affect only explicit pool state. They do not edit automatic endpoint observation history.

Disabling/removing a pool entry therefore does not erase the historical evidence that an endpoint once worked; conversely, historical success does not recreate an explicitly removed pool entry.

### Trust boundary remains unchanged

A promoted or pinned endpoint is still only a candidate.

On later reuse it must again pass:

1. current candidate collection;
2. current logical Host/SNI endpoint probe when available;
3. profile mutation invariants;
4. current real-core Reviver validation.

Pool promotion records operator intent and a known validated observation; it does not create a permanent trust exemption.

### Regression coverage

Tests verify:

- only runtime-validated endpoint-address repairs can be promoted;
- pool scope comes from the original logical identity rather than the replacement IP;
- provider/POP evidence is retained during promotion;
- explicit pin choice and label reach the pool store;
- static-only candidates are rejected;
- pin/unpin, enable/disable, relabel, and remove are represented as explicit administrative operations.

### Next boundary

The next product-facing endpoint slice can expose these operations in a small management surface:

- endpoint pool rows grouped by logical host;
- pin/unpin and enable/disable toggles;
- label/edit/remove;
- last current probe state;
- historical reliability/failure streak;
- explicit "save validated endpoint" action from Reviver results;
- no background auto-promotion.


## Continuation checkpoint: endpoint-pool hardening + selected history detail + DNS wire capture/replay

This checkpoint extends the existing endpoint-pool and historical-candidate work without changing its trust model.

### Endpoint-pool promotion hardening

Runtime validation alone is no longer sufficient for pool promotion. `EndpointPoolPromotionService` now additionally requires:

- the candidate to belong to the same repair session;
- exactly an endpoint-address mutation for the promoted endpoint;
- the candidate profile to differ from the original only in `ProfileItem.Address`;
- the mutation's declared destination to match the actual validated literal-IP address.

This prevents a future strategy or malformed caller from smuggling unrelated profile mutations into a "validated endpoint" pool promotion.

### Endpoint-pool label policy

Pool labels now have one storage-boundary policy:

- trim surrounding whitespace;
- maximum 160 characters;
- enforced for both upsert/promotion and administrative relabel operations.

The policy lives in `EndpointPoolPolicy` so UI/CLI callers can reuse it, while the SQLite store remains the authoritative enforcement point.

### Selected-endpoint history detail

New `EndpointHistoryQueryService` provides bounded read-only history for one physical endpoint under one logical profile identity:

- logical host;
- port;
- transport/network;
- stream security;
- literal physical IP.

Defaults:

- 30-day history window;
- maximum 50 points;
- caller may request 1..500 points.

Each point retains:

- timestamp;
- qualified state;
- attempts/successes/consecutive successes;
- measured reliability;
- median latency;
- source;
- provider / ASN / POP;
- recorded probe errors.

The response also contains the same decayed historical summary used by automatic endpoint nomination, so future UI can show both raw recent observations and the exact aggregate that drives historical eligibility.

### DNS wire fixture provenance

New `internal/dnsfixture` defines an immutable raw-packet fixture format.

Every fixture contains:

- human name;
- `captureKind`: exactly `captured` or `synthetic`;
- concrete source;
- RFC3339 capture timestamp for real captures;
- query name/type;
- transaction ID;
- exact response packet as hex;
- SHA-256 digest;
- optional notes.

Replay rejects hash-corrupted packet content before passing bytes into `dnswire.ParseMessage`.

The checked-in seed fixture is explicitly marked **synthetic** and must not be described as a real network capture.

### Explicit real-capture utility

New `cmd/dns-fixture-capture` performs one operator-requested UDP DNS exchange and writes the exact received packet into the fixture format.

Example:

```bash
go run ./cmd/dns-fixture-capture \
  -server 1.1.1.1:53 \
  -name example.com \
  -type 1 \
  -out internal/dnsfixture/testdata/example.capture.json
```

Before saving, the captured packet is immediately replayed through the normal parser. A capture that cannot be deterministically replayed is rejected.

### Offline replay utility

New `cmd/dns-fixture-replay` loads a committed fixture, validates its SHA-256, replays the packet through the DNS parser, and emits a compact JSON summary.

CI remains network-independent:

- `go test ./...` replays only committed fixtures;
- no test contacts a public resolver;
- live capture is an explicit operator action;
- real captured fixtures can later be reviewed and committed with their original provenance intact.

### Regression coverage

Added coverage for:

- endpoint-pool label trimming and maximum length;
- rejecting promotion that changes fields beyond physical address;
- rejecting mutation evidence that disagrees with the validated endpoint;
- bounded newest-first selected-endpoint history projection;
- preservation of probe errors in endpoint history detail;
- null aggregate for empty endpoint history;
- fixture seed replay;
- captured-fixture provenance construction;
- SHA-256 corruption detection;
- mandatory timestamp for real captures.

### Next boundary

The next slice can consume these contracts without changing their meaning:

- endpoint-pool management UI over the existing admin service;
- selected-endpoint history view using `EndpointHistoryQueryService`;
- commit reviewed real A/AAAA/NXDOMAIN/NODATA/DNSSEC captures and replay them through higher DNSSEC/denial layers;
- fixture manifests/tags for resolver, zone, transport, and expected semantic outcome;
- provider/ASN target adapters feeding the existing source-diverse candidate composition;
- low-frequency maintenance pruning for endpoint observations and disabled pool entries.


## Continuation checkpoint: manifest-driven DNS replay + provider/ASN adapters + bounded endpoint maintenance

This checkpoint advances the Discovery evidence layer while keeping all widening operations explicit and bounded.

### Manifest-driven DNS semantic replay

DNS fixtures can now be grouped into a versioned manifest with expected parser/denial outcomes.

A manifest entry contains:

- relative fixture path;
- optional tags;
- optional expected RCODE;
- optional answer count;
- optional answer RR types;
- optional denial mechanism;
- optional denial semantic status.

Manifest paths are constrained to safe relative paths and duplicate entries are rejected.

The checked-in manifest currently covers:

1. a synthetic positive A response;
2. a synthetic NOERROR/NODATA response carrying an exact-name NSEC record.

The NSEC fixture is replayed through:

- `dnswire.ParseMessage`;
- `dnssec.ParseNSEC`;
- `dnssec.EvaluateNSEC`.

The expected result is `nsec:nodata-evidence`.

This is intentionally semantic denial evidence only. Single-packet replay reports `authenticationScope=not-evaluated-single-packet` and must not be described as root-anchored DNSSEC authentication.

### Scripted multi-exchange DNS bundles

New `ExchangeBundle` / `BundleRuntime` map exact exchange tuples to committed fixtures:

- name-server address;
- port;
- domain;
- query type;
- UDP/TCP transport;
- recursion-desired state;
- optional synthetic latency.

The bundle exposes a `dnstrace.ExchangeFunc` and `TraceOptions`, so the unmodified iterative trace engine can consume fixture packets as if they were network observations.

The first deterministic bundle proves:

- a one-hop iterative trace can complete entirely offline;
- the parsed A answer survives through `dnstrace`;
- a missing exchange tuple fails closed as `nameserver_unreachable`;
- replay never silently falls through to live DNS.

This is the infrastructure required for later root/TLD/authoritative DS/DNSKEY/RRSIG/NSEC/NSEC3 corpora to drive the full `dnsvalidate` chain offline.

### Replay CLI expansion

`dns-fixture-replay` now accepts exactly one of:

- `-fixture <path>`;
- `-manifest <path>`.

Manifest replay emits compact per-fixture summaries including:

- query;
- RCODE;
- answer count/types;
- denial mechanism/status;
- explicit authentication scope.

### Provider / ASN candidate adapter

New `ProviderAsnCandidateSource` accepts only **pre-enumerated literal-IP catalog entries**.

It does not:

- expand CIDRs;
- enumerate provider address space;
- start a scanner;
- bypass Discovery probing;
- bypass Reviver real-core validation.

Catalog entries may carry:

- physical IP;
- optional exact port;
- provider;
- ASN;
- POP;
- source ID;
- exact logical-host scope;
- transport scope;
- stream-security scope;
- observation time;
- metadata.

Entries that are disabled, non-IP, wrong-port or outside the requested scope are ignored.

`StaticProviderAsnEndpointCatalog` filters by request scope **before** applying the item budget, preventing early unrelated entries from starving valid later entries.

### Candidate composition integration

`DiscoveryCandidateComposition.CreateDefault` now accepts an optional `IProviderAsnEndpointCatalog`.

When no provider catalog is supplied, candidate behavior is unchanged.

When supplied, the canonical source order is:

1. explicit endpoint pool;
2. injected provider/ASN endpoint catalog;
3. current DNS resolution;
4. historically-good endpoint observations.

Every resulting physical IP still goes through the existing current endpoint probe and then normal Reviver validation.

### Explicit endpoint maintenance

New `EndpointMaintenanceService` provides bounded low-frequency cleanup.

Default policy:

- prune endpoint probe observations older than 90 days;
- disabled pool-entry retention: 180 days;
- at most 100 pool deletions per run;
- scan at most 2000 pool rows.

Pool deletion requires all of:

- entry is disabled;
- entry is not pinned;
- entry has a valid update timestamp;
- entry is older than the disabled-entry retention window.

Enabled entries and pinned entries are never auto-deleted by maintenance regardless of age.

The service is explicit/on-demand in this checkpoint; no hidden timer or background network work is introduced.

### Regression coverage

New tests cover:

- provider/ASN exact host/port/security scoping;
- invalid provider catalog addresses ignored;
- provider/ASN metadata projection;
- filter-before-budget behavior;
- optional catalog composition with no behavior requirement when absent;
- endpoint observation retention delegated to the history store;
- stale disabled unpinned pool cleanup;
- pinned/enabled/recent pool preservation;
- oldest-first bounded deletion;
- manifest traversal rejection;
- manifest semantic mismatch detection;
- NSEC NODATA semantic replay;
- explicit non-authenticated single-packet trust scope;
- scripted one-hop `dnstrace` replay;
- missing scripted exchange fails closed.

### Next boundary

The next rigorous DNS corpus slice should capture and commit reviewed real packets rather than synthesizing cryptographic evidence:

- positive A/AAAA chains;
- NXDOMAIN and NODATA;
- NSEC and NSEC3;
- DS/DNSKEY/RRSIG across root -> TLD -> authoritative servers;
- unsigned delegation;
- expired/invalid signature examples where legally and practically reproducible.

Once those packets exist, the scripted bundle layer can drive `dnsvalidate.Validate` and `dnschain.Validate` entirely offline with actual wire evidence.


## Continuation checkpoint: versioned provider catalogs + offline dnsvalidate execution

This compact follow-up builds on the now-green provider/ASN + fixture-manifest checkpoint.

### Versioned provider / ASN catalog documents

New `ProviderAsnEndpointCatalogDocument` defines a governed JSON envelope around the already-landed endpoint-level catalog entries.

Required document identity:

- schema version;
- catalog ID;
- catalog version;
- source.

Optional:

- source update timestamp.

Safety bounds:

- schema version must match the supported version;
- maximum 100,000 entries;
- maximum 32 MiB document size;
- every entry address is validated as a literal IP before the catalog is admitted;
- optional ports must be within 1..65535.

The loader does not expand CIDRs or scan provider address space.

### Raw-document fingerprinting

`JsonProviderAsnEndpointCatalog` SHA-256 fingerprints the exact JSON bytes that were loaded.

Every emitted catalog entry receives provenance metadata:

- `catalogId`;
- `catalogVersion`;
- `catalogSource`;
- `catalogSha256`;
- optional `catalogUpdatedAt`.

Missing per-entry source IDs are deterministically filled as `<catalog-id>:<index>`.

The catalog therefore retains both logical source identity and exact source-document identity.

### Scope and budget behavior

The JSON-backed catalog delegates to the same request-aware static catalog used by tests and embedded callers.

Filtering occurs before truncation for:

- logical host;
- port;
- network/transport;
- stream security.

This prevents unrelated early entries from consuming the bounded candidate budget.

### Offline execution of the production DNS validator

New `ValidateDNSSECBundle` loads an exchange bundle, derives `dnstrace.Options`, and calls the existing `dnsvalidate.Validate` function unchanged.

New command:

```bash
go run ./cmd/dns-fixture-validate \
  -bundle <bundle.json> \
  -name <domain> \
  -type <rrtype>
```

The command is diagnostic:

- output is the normal typed `dnsvalidate.Result`;
- missing exchanges fail through the scripted exchange path;
- no network fallback exists;
- the command does not treat an indeterminate result as an execution error.

The current synthetic one-hop corpus reaches the validator and returns the expected unanchored/indeterminate state because no signer material exists. This proves that the real validator can execute over immutable packet evidence without claiming cryptographic authentication that the fixture does not contain.

### Regression coverage

New tests cover:

- camelCase JSON catalog parsing;
- catalog ID/version/source provenance projection;
- exact raw-document SHA-256 metadata;
- deterministic fallback source IDs;
- invalid literal address rejection;
- unsupported schema rejection;
- production `dnsvalidate.Validate` execution through a scripted offline bundle;
- preservation of the expected unanchored/indeterminate state for the signer-less synthetic corpus.

### Next DNS corpus boundary

The remaining missing ingredient is evidence, not architecture.

A rigorous next corpus should contain reviewed captured packets for complete chains:

- root DNSKEY;
- TLD DS + RRSIG;
- TLD DNSKEY + RRSIG;
- authoritative DS/DNSKEY transitions where applicable;
- signed A/AAAA answers;
- signed NXDOMAIN/NODATA denial;
- NSEC and NSEC3 denial variants;
- unsigned delegation;
- known-invalid/expired signatures where a reproducible fixture can be obtained.

Those bundles can then exercise `dnschain.Validate` and `dnsvalidate.Validate` with actual captured cryptographic wire material and no production-code fork.


## Continuation checkpoint: operational read models + maintenance preview + DNS corpus audit

This checkpoint turns several previously write-only/opaque lifecycle areas into bounded read APIs and makes destructive maintenance reviewable before mutation.

### Promotion / rollback history query surface

New `RepairPromotionHistoryQueryService` exposes typed recent promotion history over the existing SQLite lifecycle records.

Bounded query filters:

- original or promoted profile ID;
- session ID;
- candidate ID;
- event kind (`promoted` / `rolled-back`);
- maximum age;
- maximum result count.

Default retention window presented by the query is 180 days and result count is bounded.

Each projected row reconstructs, when available:

- mutations;
- baseline runtime validation;
- candidate runtime validation;
- before/after outcome comparison;
- score;
- default-profile transition;
- event timestamp.

Malformed legacy JSON payloads fail soft at the read boundary: the history event remains visible while the malformed typed sub-payload becomes unavailable rather than crashing the history view.

The summary exposes:

- total events;
- promotion count;
- rollback count;
- improved/stable/regressed/unknown outcome counts;
- latest event timestamp.

### Enriched endpoint-pool inspection

New `IEndpointHistoryQueryService` formalizes the selected-endpoint detail reader already backed by SQLite.

New `EndpointPoolInspectionService` joins explicit pool state with exactly-scoped historical evidence.

Each row contains:

- pool ID / label;
- logical host;
- port;
- transport/network;
- stream security;
- physical IP;
- enabled/pinned state;
- provider / ASN / POP;
- created/updated timestamps;
- latest observation;
- decayed historical summary;
- whether the historical summary currently meets the configured "historically good" policy;
- a compact current observation state: `qualified`, `failed`, or `unknown`.

Inspection is intentionally bounded:

- at most 250 pool rows per request;
- at most 100 historical points per row;
- historical window no greater than 365 days.

This service is read-only and does not probe, pin, promote, or mutate endpoints.

### Maintenance preview / apply separation

`EndpointMaintenanceService` now has an explicit two-stage API:

1. `PreviewAsync`
2. `ApplyAsync`

Preview performs no mutation. It records:

- creation time;
- active policy;
- scanned row count;
- disabled-entry cutoff;
- exact deletion candidates;
- each candidate's last update timestamp.

Apply then re-reads pool state before every planned deletion.

A planned row is skipped when:

- it no longer exists;
- it was pinned;
- it was re-enabled;
- its update timestamp changed;
- it no longer satisfies the original cutoff.

This makes a user's post-preview edit authoritative over cleanup.

`RunAsync` remains as the convenience path and is now implemented as preview followed by apply.

Observation-history pruning occurs only during apply/run, never during preview.

### Offline DNS corpus audit

New `dnsfixture.AuditDirectory` walks a committed corpus root and classifies JSON documents as:

- packet fixtures;
- semantic manifests;
- scripted exchange bundles.

For packet fixtures it validates:

- fixture schema/provenance;
- packet hex;
- SHA-256 integrity;
- captured-vs-synthetic requirements.

For manifests it runs the existing semantic replay and expected-result checks.

For exchange bundles it loads all fixture references through the existing offline bundle runtime.

The audit reports:

- total packet fixtures;
- captured count;
- synthetic count;
- manifest count;
- bundle count;
- semantic replay count;
- orphan packet fixtures;
- validation errors.

A packet fixture is "orphaned" when it is not referenced by either a manifest or an exchange bundle.

### Corpus-audit CLI and CI gate

New command:

```bash
go run ./cmd/dns-fixture-audit \
  -root ./internal/dnsfixture/testdata \
  -fail-on-orphans
```

Exit semantics:

- 0: corpus structurally/semantically valid and, when requested, no orphan fixtures;
- 1: validation error or forbidden orphan;
- 2: invalid CLI invocation/output failure.

The main PR workflow now runs this audit after Go tests/vet.

CI therefore checks both:

1. individual code/unit behavior;
2. integrity and reference coverage of the committed DNS evidence corpus.

No network access is introduced into tests.

### Regression coverage

Added coverage for:

- typed promotion-history reconstruction and outcome counts;
- malformed legacy history payloads remaining readable;
- endpoint pool + history join semantics;
- historically-good projection;
- unknown current state when no observation exists;
- maintenance preview producing no mutation;
- stale maintenance plan skipping a newly pinned/edited entry;
- current committed DNS corpus counts;
- zero orphan packet fixtures;
- missing corpus root rejection.

### Next boundary

The next backend/product boundary can now build on stable read contracts:

- endpoint-pool management/history UI over `EndpointPoolInspectionService`;
- repair promotion/rollback history UI over `RepairPromotionHistoryQueryService`;
- explicit maintenance preview UI before deletion;
- fixture corpus growth with reviewed real captured DNSSEC chains;
- persisted repair-history pruning/retention policy;
- richer provider catalog health/audit telemetry without turning provider catalogs into live scanners.


## Continuation checkpoint: unified lifecycle retention + provider catalog governance

This checkpoint consolidates append-only evidence retention and adds metadata-only quality auditing for provider/ASN endpoint catalogs.

### Canonical lifecycle retention ownership

New `LifecycleRetentionService` is the explicit retention path for append-only operational evidence:

- `DnsRepairHistoryItem`;
- `DnsResolverTelemetryItem`;
- `RepairPromotionHistoryItem`;
- `EndpointObservationHistoryItem`.

Default retention windows:

- DNS repair history: 180 days;
- resolver telemetry: 90 days;
- promotion/rollback history: 365 days;
- endpoint observations: 90 days.

The service is explicit/on-demand. No timer, startup sweep, DNS refresh side effect, or background task invokes it.

### Frozen preview -> apply plans

Retention uses the same reviewable two-stage model as endpoint-pool maintenance:

1. `PreviewAsync`
2. `ApplyAsync`

Preview freezes:

- plan creation time;
- active retention policy;
- absolute cutoff per table;
- exact row IDs selected for deletion.

Each table has an independent bounded delete budget, defaulting to 5000 rows.

The SQLite preview queries are bounded at the database layer and fetch only the oldest rows already beyond the relevant retention cutoff.

Apply deletes only IDs that were present in the preview plan and are still older than that exact frozen cutoff.

Records created after preview are therefore never swept into the same maintenance action.

### Retention-plan tamper protection

Before applying a plan, PattN validates:

- creation timestamp is present;
- table names are from the fixed supported set;
- each table appears at most once;
- each frozen cutoff exactly equals `plan.CreatedAt - plan.Policy.<retention>`;
- row count does not exceed the table budget;
- IDs are non-empty and unique.

A caller cannot widen a cutoff or inject an unrelated table while reusing an otherwise valid plan.

### Cleanup ownership consolidation

Endpoint-pool cleanup and evidence retention now have distinct ownership:

- `EndpointMaintenanceService` manages only explicit endpoint-pool rows;
- `LifecycleRetentionService` manages append-only historical evidence.

`EndpointMaintenanceService` no longer prunes endpoint observations.

DNS-health refresh also no longer performs automatic resolver-telemetry pruning.

This removes duplicate retention paths and ensures cleanup happens only through explicit maintenance operations.

Lower-level store prune methods remain available for compatibility/internal use, but the canonical operator-facing retention workflow is the frozen lifecycle-retention plan.

### Endpoint-pool plan hardening

Endpoint pool maintenance now also validates its own frozen plan before mutation:

- creation time must be present;
- cutoff must exactly match creation time minus configured disabled-entry retention;
- candidate count must stay within the removal budget;
- candidate IDs must be non-empty and unique.

Apply still re-reads pool state, so user edits made after preview—pin, enable, relabel/update timestamp, or removal—win over cleanup.

### Provider / ASN catalog metadata audit

New `ProviderAsnCatalogAuditService` audits an already-loaded JSON provider catalog without performing network activity.

The audit reports:

- catalog ID / version / source;
- exact source-document SHA-256;
- source `updatedAt`;
- audit timestamp;
- whether freshness is known;
- age in days;
- stale state under a configurable maximum age;
- total / enabled / disabled entries;
- IPv4 / IPv6 counts;
- unique physical addresses;
- provider / ASN / POP cardinality;
- host-scoped entry count;
- broad-scope entry count;
- missing provider / ASN metadata counts;
- duplicate source-ID groups;
- exact duplicate endpoint/scope groups;
- bounded duplicate details;
- warnings and errors.

Default maximum catalog age: 120 days.

### Freshness semantics

Provider catalog freshness is metadata quality, not current network truth.

Audit behavior:

- missing `updatedAt` -> freshness unknown + warning;
- older than maximum age -> stale + warning;
- materially future-dated beyond allowed clock skew -> invalid audit error;
- duplicate source IDs / exact duplicate entries -> warnings;
- broad unscoped entries -> warning.

No audit warning automatically removes a candidate.

### Normalized duplicate detection

Exact duplicate detection normalizes:

- literal address;
- optional port;
- network/transport token;
- stream-security token;
- logical-host scope;
- enabled state.

Logical hosts are case-insensitive and trailing-dot normalized.

This catches semantically duplicate entries even when source formatting differs.

### Catalog audit available directly from JSON catalogs

`JsonProviderAsnEndpointCatalog` now exposes:

- normalized decorated entries;
- `Audit(...)`.

Decorated catalog entries continue to retain:

- catalog ID;
- catalog version;
- source;
- source-document SHA-256;
- source update time when present.

### Audit evidence carried into candidates

When the canonical candidate composition receives a JSON provider catalog, it computes a metadata-only audit once and passes it into `ProviderAsnCandidateSource`.

Every emitted candidate can therefore carry advisory fields such as:

- audit valid/invalid;
- freshness-known;
- stale state;
- catalog age;
- duplicate source-ID group count;
- exact duplicate group count;
- broad-scope entry count;
- audit warnings/errors;
- audit timestamp.

This metadata does **not** bypass or replace:

1. request scope filtering;
2. literal-IP validation;
3. current Discovery endpoint probing;
4. Reviver invariants;
5. real-core runtime validation.

A stale catalog can still nominate an endpoint; current evidence decides whether it works.

### Regression coverage

Added coverage for:

- oldest-first bounded lifecycle-retention selection;
- strict cutoff semantics;
- duplicate table-plan rejection;
- duplicate ID rejection;
- unknown table rejection;
- altered retention cutoff rejection;
- endpoint-pool maintenance cutoff tampering;
- provider catalog stale-state reporting;
- IPv4/IPv6 and metadata coverage counts;
- normalized duplicate detection;
- broad-scope warnings;
- missing freshness remaining unknown rather than invalid;
- materially future-dated catalogs becoming invalid;
- stale catalog audit metadata remaining advisory while a scoped candidate is still emitted.

### Next boundary

The next rigorous backend slice can build on these governance contracts:

- maintenance/history UI that previews lifecycle and endpoint-pool cleanup separately;
- persisted provider-catalog audit snapshots when catalogs become user-configurable resources;
- catalog comparison/diff between versions;
- retention statistics/DB size accounting before and after maintenance;
- reviewed real DNSSEC capture corpus growth;
- explicit import/update workflow for provider catalogs with atomic replacement and rollback.


## Continuation checkpoint: semantic provider-catalog diff + atomic update / rollback

This checkpoint adds an explicit file lifecycle for provider/ASN endpoint catalogs. It does not introduce remote fetching, background refresh, or automatic replacement.

### Stable semantic catalog diffs

New `ProviderAsnCatalogDiffService` compares two already-validated JSON catalogs.

Identity rules:

- explicit source IDs are authoritative diff identities;
- rows whose source ID was generated from position are marked with `catalogSourceIdGenerated=true`;
- generated-ID rows are diffed by normalized endpoint scope instead of array index.

This prevents simple source-file reordering from appearing as mass remove/add churn.

Normalized endpoint scope includes:

- physical IP;
- optional port;
- logical-host set;
- network/transport;
- stream-security token.

Logical hosts are case-insensitive and trailing-dot normalized.

### Diff outcomes

The typed diff reports:

- catalog IDs / versions / SHA-256 values;
- added count;
- removed count;
- modified count;
- unchanged count;
- per-change before / after rows;
- changed field names.

For matching identities, changes are detected across:

- address;
- port;
- provider;
- ASN;
- POP;
- logical-host scope;
- network;
- stream security;
- enabled state;
- observation timestamp;
- user metadata.

Catalog provenance metadata itself—catalog ID/version/source/hash/update timestamp and generated-ID marker—is excluded from material row comparison so a version bump alone does not mark every endpoint modified.

Ambiguous duplicate diff identities are rejected rather than arbitrarily paired.

### Side-effect-free update preparation

New `ProviderAsnCatalogUpdateService.PrepareAsync` performs no filesystem mutation.

Preparation:

1. normalizes the destination path;
2. validates the replacement JSON catalog;
3. audits replacement metadata;
4. optionally requires fresh replacement metadata;
5. fingerprints the current destination, or records an explicit missing-file sentinel;
6. attempts to parse/audit the existing catalog;
7. enforces same catalog ID by default when the current file is valid;
8. computes a semantic diff when the existing catalog has unambiguous identities;
9. freezes exact before/after bytes and SHA-256 evidence into the plan.

A malformed/empty/duplicate-ambiguous existing catalog can still be repaired:

- previous raw bytes and hash are preserved for rollback;
- parse/diff limitations are surfaced as `BeforeCatalogError`;
- replacement remains allowed when safe identity checks are possible.

### Freshness policy

Update options include:

- `RequireSameCatalogId` (default true);
- `RequireFreshNewCatalog` (default false);
- provider catalog audit policy.

Stale metadata remains importable by default because freshness is advisory provenance.

When `RequireFreshNewCatalog=true`, missing or stale `updatedAt` blocks preparation.

A replacement whose metadata audit is invalid—for example materially future-dated beyond allowed clock skew—is rejected.

### Stale-plan protection

Before mutation, `ApplyAsync` re-fingerprints the destination. After the replacement temp file has been fully flushed, the destination is fingerprinted **again immediately before rename**.

Apply is rejected when:

- a file that existed during preparation changed;
- a prepared existing file disappeared;
- a previously missing destination appeared.

This prevents an old prepared update from overwriting newer user/editor changes.

### Atomic replacement and durability

Replacement writes use a temporary file in the destination directory:

1. create unique same-directory temp file;
2. write replacement bytes;
3. flush async buffers;
4. request disk flush;
5. close temp file;
6. atomically move/replace destination;
7. re-read and verify final SHA-256.

The same-directory move avoids cross-filesystem copy semantics.

Temporary files are removed in a `finally` path.

### Apply compensation

If replacement or post-write verification fails after the new catalog was installed, the service performs best-effort compensation.

Compensation first verifies that the destination still matches the just-applied SHA-256. If another actor has already changed the file, compensation is skipped rather than overwriting that newer edit.

When the applied bytes still own the destination:

- restore exact previous bytes when the destination existed;
- remove the newly created destination when it did not.

Compensation failure is logged separately while the original apply exception is rethrown.

### Rollback receipt and conflict protection

A successful apply returns a receipt containing:

- plan ID;
- destination path;
- before/after SHA-256;
- whether a file existed before;
- exact previous bytes;
- apply timestamp.

Normal rollback first verifies the destination still matches the applied SHA-256.

If the file changed after apply, rollback refuses to overwrite it.

A caller may explicitly request forced rollback.

Rollback semantics:

- existing-before update -> atomically restore exact previous bytes;
- missing-before update -> remove the catalog created by apply.

The restored SHA-256 is verified.

### Public plan/receipt tamper checks

Apply revalidates:

- plan timestamp;
- destination path;
- replacement bytes;
- replacement SHA-256;
- parsed replacement catalog ID/version;
- prior-file fingerprint evidence.

Rollback revalidates:

- receipt identity/path;
- prior bytes against prior SHA-256;
- missing-file sentinel consistency;
- applied SHA-256 presence.

A caller cannot mutate public plan/receipt byte arrays and still pass the mutation boundary silently.

### Regression coverage

Added tests for:

- explicit source-ID additions/modifications;
- changed-field reporting;
- anonymous-row reorder stability;
- duplicate/ambiguous identity rejection;
- preparation having zero filesystem side effects;
- apply + rollback exact byte round trip;
- stale-plan rejection without overwriting external edits;
- rollback conflict protection;
- forced rollback;
- create-new apply followed by delete rollback;
- same-catalog-ID enforcement;
- optional freshness enforcement;
- repairing an empty invalid prior file;
- repairing a duplicate/ambiguous prior catalog when diffing is unavailable;
- rollback receipt tamper rejection before mutation.

### Next boundary

The next provider-catalog slice can now safely add product orchestration rather than more file primitives:

- user-visible catalog import/update preview built from audit + diff;
- catalog registry with explicit enabled/disabled resources;
- persisted audit/update receipts;
- provider catalog history/version rollback across app restarts;
- optional external fetch adapters with ETag/content-hash caching, only behind explicit user action;
- signature/provenance verification for catalogs that publish signed release metadata.


## Continuation checkpoint: persistent provider-catalog registry + restart-safe revision ledger

This checkpoint turns provider/ASN catalog files into explicit registered resources without introducing background fetching or automatic replacement.

### Persistent registry identity

New SQLite-backed registry records preserve:

- stable registry ID;
- normalized local file path;
- user-facing display name;
- enabled/disabled state;
- catalog ID/version/source;
- exact source-document SHA-256;
- catalog `updatedAt`;
- registration/update timestamps;
- most recent audit timestamp/result;
- currently active revision ID.

The registry ID is intentionally separate from the catalog's own ID. The registry identifies a local managed resource; the catalog ID remains provenance supplied by the catalog document.

### Explicit registration and enable/disable

`ProviderAsnCatalogRegistryService.RegisterAsync`:

1. loads and validates the local JSON file;
2. runs the existing metadata audit;
3. optionally requires fresh metadata;
4. rejects invalid audit results;
5. prevents two enabled registry resources from sharing the same catalog ID;
6. persists the exact current SHA-256 and audit evidence.

Re-registering the same path can refresh metadata but cannot silently change the catalog ID. An identity change requires explicit unregister/re-register semantics rather than being smuggled through a normal refresh.

Re-enabling a disabled resource verifies that the file still matches the registered catalog ID and SHA-256. A file edited while disabled must be explicitly refreshed or re-registered first.

### On-disk drift remains authoritative

Enabled registry resources are loaded through `LoadEnabledCatalogsAsync`.

Before a registered catalog is admitted into candidate composition, PattN verifies:

- catalog ID still matches;
- exact file SHA-256 still matches the registry.

External edits therefore do not silently enter discovery. They produce an explicit reconciliation requirement.

### Persistent revision ledger

Each registry-managed update creates a `ProviderAsnCatalogRevisionItem` containing:

- stable revision ID;
- registry ID;
- original update-plan ID;
- destination path;
- before/after SHA-256;
- before/after catalog ID/version;
- exact previous file bytes required for rollback;
- apply timestamp;
- rollback timestamp/forced flag;
- applied audit result;
- semantic diff.

The exact previous bytes are persisted so rollback can be performed after an application restart without relying on the original in-memory update plan.

### Registry-managed update application

`PrepareUpdateAsync` still delegates to the existing `ProviderAsnCatalogUpdateService`.

The registry adds one more invariant: the replacement catalog ID must match the registered resource's catalog ID even when a lower-level caller disables `RequireSameCatalogId`.

`ApplyUpdateAsync`:

1. verifies the plan belongs to the registered path and catalog ID;
2. delegates file mutation to the stale-checked/atomic update service;
3. persists the exact rollback receipt as a revision row;
4. updates registry metadata/hash/audit;
5. marks that revision active.

If the file update succeeds but registry persistence fails, PattN immediately attempts the existing protected file rollback. A successfully written file is not intentionally left ahead of the persistent registry.

### Restart-safe rollback by revision ID

`RollbackRevisionAsync` reconstructs the original update receipt from the persisted revision row.

Normal rollback requires the requested revision to still be the registry's active revision. This prevents crossing a newer applied update.

The underlying update service still performs its own current-file SHA-256 conflict check, so newer external file edits remain protected.

After rollback PattN:

- reloads and re-audits the restored file;
- restores registry catalog metadata;
- resolves a prior active revision when its applied hash matches the restored hash;
- marks the rolled-back revision with time/force state.

A forced rollback remains explicit.

### Registry-backed candidate composition

New `RegistryProviderAsnEndpointCatalog` aggregates all explicitly enabled, hash-matching registered catalogs into the existing `IProviderAsnEndpointCatalog` contract.

Behavior:

- no remote fetch;
- no background refresh;
- no CIDR expansion;
- request scoping still happens inside each catalog;
- duplicate physical endpoint scope across multiple catalogs is deduplicated;
- per-catalog audit metadata is copied into candidate metadata;
- candidate probing and Reviver validation remain mandatory downstream.

New `DiscoveryCandidateComposition.CreateWithRegisteredProviderCatalogs` provides the explicit production composition path over the SQLite registry.

### Registry persistence tables

Application startup now creates:

- `ProviderAsnCatalogRegistryItem`;
- `ProviderAsnCatalogRevisionItem`.

Revision rows are intentionally not included in generic evidence retention yet because they may contain the only restart-safe rollback bytes for a still-active catalog revision. A dedicated revision-retention policy should only remove revisions after rollback safety requirements are explicitly defined.

### Query correctness

Registry SQLite queries apply enable/registry/rollback filters **before** result limits.

This prevents unrelated newer rows from consuming a bounded query and hiding the enabled resource or revision the caller actually requested.

### Regression coverage

Added tests for:

- successful registration with persisted audit evidence;
- duplicate enabled catalog-ID rejection;
- disabled duplicate registration followed by rejected enable;
- apply -> new service instance -> rollback by persisted revision ID;
- exact prior-file restoration after restart-style rollback;
- active revision tracking;
- external on-disk drift rejection;
- same-path catalog-ID replacement rejection;
- disabled-resource drift blocking re-enable;
- registry aggregate deduplication across different catalogs;
- propagation of catalog audit metadata through registry aggregation;
- registry-level catalog-ID enforcement even when the lower-level update option allows ID changes.

### Next boundary

The provider-catalog backend now supports durable local resource orchestration. The next product slice can safely add:

- registry/import/update UI using audit + semantic diff previews;
- revision history/rollback UI;
- explicit unregister workflow that preserves or intentionally discards rollback history;
- optional signed-release provenance verification;
- explicit user-triggered remote fetch adapters with ETag/content-hash caching;
- revision-retention policy that never deletes active rollback evidence.


## Continuation checkpoint: Discovery management UI over governed backend contracts

This checkpoint exposes the now-mature Discovery/Reviver operational backends through one explicit desktop management surface. It adds no background mutation, background fetching, automatic catalog replacement, automatic endpoint cleanup, or automatic rollback.

### Shared management ViewModel

New `DiscoveryManagementViewModel` is shared by WPF and Avalonia.

It orchestrates existing services rather than reimplementing their policies:

- `ProviderAsnCatalogRegistryService`;
- `SqliteProviderAsnCatalogRegistryStore`;
- `EndpointPoolManagementService`;
- `EndpointPoolInspectionService`;
- `EndpointHistoryQueryService`;
- `RepairPromotionHistoryQueryService`;
- `EndpointMaintenanceService`;
- `LifecycleRetentionService`.

All mutation flows remain explicit commands.

The management window performs an initial read refresh when opened, but that refresh only reads local registry/history state. It does not start network scans, fetch remote catalogs, update catalog files, promote endpoints, prune history, or mutate settings.

### Provider-catalog management tab

The catalog tab exposes:

- registered local catalog resources;
- enabled/disabled state;
- catalog ID/version;
- last audit time;
- local file path;
- persisted metadata audit summary;
- user-facing display name;
- revision history.

Explicit operations:

- register a local JSON catalog;
- refresh/re-audit the selected catalog;
- enable/disable the selected catalog;
- relabel the registry resource;
- select a replacement JSON file and **prepare** an update preview;
- apply only the currently prepared update plan;
- rollback the selected persisted revision.

Update preview displays:

- before -> after catalog version;
- semantic add/remove/modify/unchanged counts when available;
- post-update entry count;
- freshness state;
- shortened before/after SHA-256 values.

The UI does not bypass stale-plan, catalog-identity, hash, audit, atomic-write, compensation, active-revision, or rollback-conflict checks. Those continue to be enforced by the underlying registry/update services.

### Endpoint-pool management tab

The endpoint tab uses `EndpointPoolInspectionService`, so each visible row combines explicit pool state with the same bounded historical summary used by endpoint-history candidate logic.

Visible state includes:

- label;
- logical host;
- physical IP/port;
- enabled state;
- pinned state;
- current observation state;
- historically-good eligibility;
- provider / ASN / POP.

Explicit operations:

- refresh pool inspection;
- pin/unpin the selected endpoint;
- enable/disable the selected endpoint;
- relabel the selected endpoint.

The detail area shows:

- recent sample count;
- decayed reliability;
- decayed latency;
- recent failure streak;
- current qualification state;
- historical eligibility.

This tab intentionally does not add automatic promotion or automatic deletion.

### Repair history tab

The repair-history tab reads the typed persisted promotion/rollback history.

Summary:

- total lifecycle events;
- promotion count;
- rollback count;
- improved/stable/regressed counts.

Rows expose:

- event kind;
- event time;
- original profile;
- promoted profile;
- candidate score;
- stored outcome verdict.

Selecting an event shows the persisted before/after comparison, including reliability, latency, and loss deltas when available.

Malformed historical typed payloads remain governed by the query service's fail-soft projection semantics.

### Maintenance tab

Maintenance remains two-stage.

Endpoint pool cleanup:

1. **Preview endpoint cleanup**
2. **Apply endpoint cleanup**

The preview contains the exact frozen deletion candidates. Apply still re-reads pool state and skips any entry that was changed, pinned, enabled, removed, or otherwise no longer matches the frozen plan.

Lifecycle evidence retention:

1. **Preview lifecycle retention**
2. **Apply lifecycle retention**

Preview reports candidate counts per governed evidence table. Apply uses the frozen ID plan and cannot sweep records created after preview into the same action.

No cleanup executes merely by opening the window.

### Cross-platform UI

The same ViewModel is registered in both view locators and rendered by:

- `v2rayN/Views/DiscoveryManagementWindow.xaml`;
- `v2rayN.Desktop/Views/DiscoveryManagementWindow.axaml`.

Both frontends use the app's existing:

- window sizing persistence;
- toolbar/menu conventions;
- DataGrid patterns;
- spacing resources;
- reactive bindings;
- modal window routing.

The Settings menu now contains **Discovery management** directly after DNS settings.

### Platform-specific file selection

Only file picking remains platform-specific:

- WPF uses the existing JSON-capable `OpenFileDialog` helper;
- Avalonia uses the existing storage-provider picker with a JSON file type.

Selected files are passed back to the shared ViewModel through ReactiveUI interactions.

### Localization

New neutral resource keys cover the management surface and are exposed through `ResUI.Designer.cs`.

Existing satellite resources fall back to the neutral English text until translations are added. No existing localized key was repurposed with a different meaning.

### Safety boundaries

The management surface inherits and preserves the backend's explicit safety boundaries:

- local read on open;
- no remote provider fetch;
- no background DNS/resolver scan;
- no catalog file mutation without preview/apply;
- no catalog rollback without an explicit selected revision;
- no endpoint state mutation without an explicit command;
- no endpoint cleanup without preview/apply;
- no lifecycle evidence deletion without preview/apply;
- no bypass of active-revision or stale-plan protections.

### Next boundary

After the WPF/Avalonia build gate validates this surface, the next product slice can focus on:

- explicit unregister workflow with revision-history preservation/discard policy;
- richer per-catalog diff/revision details;
- endpoint observation timeline/table instead of the compact summary;
- maintenance policy editors and DB-size estimates before/after apply;
- reviewed real DNSSEC capture corpus growth;
- explicit signed-catalog provenance verification and user-triggered remote-fetch adapters.


## Continuation checkpoint: destructive-action confirmation + revision evidence detail

This checkpoint hardens the product-facing Discovery management surface after the first WPF/Avalonia implementation passed the full PR gate.

### Explicit confirmation before committed mutations

The following actions now require a platform-native yes/no confirmation before invoking the backend mutation:

- applying a prepared provider-catalog update;
- rolling back a selected provider-catalog revision;
- applying a prepared endpoint-pool cleanup plan;
- applying a prepared lifecycle-retention plan.

Preview, refresh, enable/disable, pin/unpin, and relabel commands keep their existing direct interaction model.

The confirmation layer is intentionally **not** a replacement for backend guards. After confirmation, the existing services still perform their own:

- stale-plan checks;
- current-file hash verification;
- catalog ID verification;
- active-revision checks;
- rollback conflict checks;
- frozen-plan validation;
- post-preview endpoint-state revalidation.

Canceling the confirmation performs no mutation and leaves the prepared plan available for further review.

### Revision evidence detail

Selecting a provider-catalog revision now exposes the persisted evidence needed to judge rollback:

- before -> after catalog version;
- active / inactive / rolled-back state;
- forced rollback marker when applicable;
- semantic diff counts:
  - added;
  - removed;
  - modified;
  - unchanged;
- applied audit validity;
- freshness-known/fresh/stale state;
- entry count;
- warning/error counts;
- shortened before/after SHA-256 values;
- apply timestamp.

This view is reconstructed from the persisted revision ledger; it does not re-fetch or mutate the catalog.

### Cross-platform confirmation routing

The shared ViewModel exposes one `Interaction<string, bool>`.

Platform handlers:

- WPF uses the existing `UI.ShowYesNo` / `MessageBoxResult.Yes`;
- Avalonia uses the existing async `UI.ShowYesNo` / `ButtonResult.Yes`.

The mutation decision remains shared; only native dialog presentation is platform-specific.

### Validation status before this checkpoint

Commit `8ec50ceb256eb1400e76e83f0e29984bca9e8214` passed the full PR workflow:

- Go tests;
- Go vet;
- DNS fixture corpus audit;
- ServiceLib tests;
- WPF build;
- Avalonia build;
- test-result publication.

The next CI run validates only the confirmation/revision-detail delta on top of that green surface.

### Next boundary

With catalog management now visible and guarded, the next product/backend slice can focus on:

- explicit unregister with preserve/discard revision-history policy;
- endpoint observation timeline/table rather than summary-only history;
- maintenance policy editors and before/after storage estimates;
- provider registry diff/revision search and filtering;
- signed catalog provenance verification;
- explicit user-triggered remote catalog fetch with ETag/content-hash caching and the same local update preview/apply path.


## Continuation checkpoint: safe provider-catalog unregister and re-registration lifecycle

This checkpoint adds an explicit retirement lifecycle for local provider/ASN catalog registry resources. Unregistering a catalog does **not** delete its JSON file.

### Retirement instead of destructive registry deletion

Provider catalog registry rows now carry a nullable `UnregisteredAt` / `UnregisteredAtUnixMs`.

Existing database rows remain active because the new column is nullable.

Default registry queries exclude retired rows. Callers may explicitly request `IncludeUnregistered` for administrative/history workflows.

A retired registry resource:

- is forced disabled;
- is excluded from enabled catalog aggregation;
- cannot be refreshed;
- cannot be enabled;
- cannot be renamed;
- cannot prepare/apply catalog updates;
- cannot roll back a revision while retired.

Re-registering its local file is the explicit transition back into active management.

### Unregister semantics

New `ProviderAsnCatalogRegistryService.UnregisterAsync`:

- marks the resource retired;
- disables it;
- records retirement time;
- never deletes or edits the catalog JSON file;
- preserves the exact catalog metadata already recorded;
- preserves revision history by default;
- preserves the active revision linkage when the on-disk bytes remain unchanged.

The returned receipt records:

- registry ID;
- local file path;
- catalog ID;
- retirement timestamp;
- whether revision history was discarded;
- revision count.

### Atomic optional revision-history discard

`ProviderAsnCatalogUnregisterOptions.DiscardRevisionHistory` is deliberately explicit and defaults to false.

When requested, retirement plus revision-ledger deletion is performed inside one SQLite transaction.

The shared SQLite helper now exposes the underlying sqlite-net transaction primitive for this purpose; no ad-hoc BEGIN/COMMIT sequence or second database connection is introduced.

Discarding history:

- does not delete the catalog file;
- clears active-revision linkage;
- irreversibly removes persisted catalog revision rows for that registry resource.

### Explicit post-retirement history discard

A user may initially unregister safely while preserving history, then later decide that the retired path should be reusable for a different catalog identity.

New `DiscardRetiredRevisionHistoryAsync` performs that irreversible ledger deletion only on an already-retired resource.

It rejects active/registered resources so revision history cannot be casually destroyed as part of ordinary catalog management.

### Re-registration behavior

Registering the same local file path after retirement reuses the existing registry resource ID rather than creating a duplicate row.

If the catalog identity is unchanged:

- preserved revisions remain attached;
- an unchanged file may reactivate the existing active revision;
- normal enabled-catalog collision checks still run.

If the bytes changed externally while retired but the catalog ID stayed the same:

- the revision ledger remains preserved as historical evidence;
- stale active-revision linkage is cleared because no persisted revision describes the newly reconciled file state.

If the catalog ID changed:

- re-registration is rejected while any preserved revision history remains;
- after explicit retired-history discard, the same path may be re-registered under the new identity.

This prevents rollback bytes and semantic history from one catalog identity being silently attached to another.

### UI behavior

The Provider Catalogs tab now exposes **Unregister catalog**.

The UI operation:

- requires explicit confirmation;
- uses the safe default that preserves revision history;
- leaves the JSON file on disk;
- removes the resource from the active catalog list after success;
- reports how many revision records were preserved.

The irreversible discard-history operation is intentionally **not** exposed as a routine management-window button in this checkpoint.

### Regression coverage

Added coverage for:

- unregister leaves the catalog file untouched;
- unregister removes the resource from normal active queries;
- administrative queries can still see retired rows;
- retired resources cannot be re-enabled directly;
- revision history is preserved by default;
- preserved revisions are not presented as active while the resource is retired;
- unchanged re-registration restores active management under the same registry ID;
- same-ID external byte drift while retired preserves history but clears stale active-revision linkage;
- unregister-with-discard clears the ledger transactionally;
- identity change on the same path succeeds only after explicit history discard;
- identity change remains blocked while preserved history exists.

### Next boundary

The next lifecycle/product slice can now safely add:

- an archived/retired-catalog view;
- explicit discard-history workflow for retired resources with stronger warning copy;
- catalog export/archive bundles containing file + audit + revision metadata;
- maintenance/storage size estimates before evidence deletion;
- endpoint observation timeline visualization;
- signed provenance metadata for provider catalogs.


## Continuation checkpoint: retired-catalog archive UI + endpoint timeline + maintenance size estimates

This checkpoint extends the already-green Discovery management surface without changing backend authority or introducing background behavior.

### Retired catalog archive

The management window now has a dedicated **Retired catalogs** tab backed by the registry's existing retirement model.

The archive lists:

- display name;
- catalog ID;
- catalog version;
- retirement timestamp;
- retained file path.

Selecting a retired catalog loads its persisted revision ledger and shows an explicit archive detail summary.

### Safe re-registration

A retired catalog can be explicitly re-registered from the archive.

The UI uses the existing local file path and display name and re-registers the resource in **disabled** state.

This keeps the existing backend invariants:

- the catalog file is never fetched remotely;
- preserved revision history remains attached to the same registry identity;
- a changed catalog identity remains blocked while preserved revision history exists;
- the user must review the re-registered resource before enabling it.

### Explicit irreversible revision-history discard

The archive exposes **Discard revision history** only for retired catalogs.

The action:

1. requires the existing cross-platform confirmation interaction;
2. calls `DiscardRetiredRevisionHistoryAsync`;
3. preserves the catalog JSON file;
4. irreversibly deletes the retired resource's persisted rollback/revision evidence.

The confirmation copy explicitly states that rollback evidence cannot be recovered.

No active catalog exposes this command.

### Endpoint observation timeline

The endpoint-pool tab now expands the existing history section into:

- compact historical summary;
- bounded recent observation timeline.

Selecting an endpoint loads up to 50 recent observations from the existing `EndpointHistoryQueryService`, scoped by:

- logical host;
- port;
- transport/network;
- stream security;
- physical endpoint IP.

The timeline shows:

- observation time;
- qualified state;
- measured reliability;
- median latency;
- successes;
- source;
- ASN;
- POP.

The timeline is read-only and does not trigger probing.

### Maintenance payload estimates

New `MaintenancePayloadEstimateService` enriches preview UX with approximate evidence payload size.

Endpoint cleanup preview estimates the UTF-8 serialized size of the exact pool rows frozen into the deletion plan.

Lifecycle retention preview estimates the UTF-8 serialized size of the exact candidate rows across:

- `DnsRepairHistoryItem`;
- `DnsResolverTelemetryItem`;
- `RepairPromotionHistoryItem`;
- `EndpointObservationHistoryItem`.

The service returns:

- total estimated bytes;
- per-category estimated bytes;
- human-readable binary units.

These values are explicitly presented as **approximate serialized payload**, not a promise about SQLite file compaction. SQLite may retain free pages after deletion until later vacuum/compaction.

### Shared cross-platform implementation

The same shared `DiscoveryManagementViewModel` drives both frontends.

WPF and Avalonia receive matching:

- retired catalog archive tab;
- archive commands;
- endpoint timeline;
- maintenance estimate text.

Existing visual conventions are preserved:

- current tab layout;
- DataGrid density;
- spacing resources;
- toolbar/menu patterns;
- confirmation routing;
- localized neutral resource fallback.

### Regression coverage

Added focused tests for:

- human-readable payload units;
- UTF-8 serialized evidence byte counting.

The full WPF/Avalonia build gate remains the authoritative compile check for the new control/resource/binding surface.

### Next boundary

After this surface passes the full PR gate, the next strong slice can focus on:

- retired-catalog export/archive bundles;
- maintenance policy editors;
- explicit SQLite vacuum/compaction as a separate opt-in operation;
- richer endpoint timeline filtering/export;
- signed provider-catalog provenance;
- user-triggered remote catalog fetch with ETag/content-hash caching through the existing preview/apply pipeline.


## Continuation checkpoint: retired-catalog archive export + explicit SQLite compaction

This checkpoint adds two explicit operational tools to the already-green Discovery management surface. Neither operation runs automatically or as a side effect of ordinary retention.

### Portable retired-catalog archive bundles

New `ProviderAsnCatalogArchiveService` exports one retired registry resource into a self-contained JSON bundle.

The bundle contains:

- archive format version;
- creation timestamp;
- full persisted registry view;
- current retained catalog filename;
- exact current catalog-file SHA-256;
- exact current catalog-file bytes as Base64;
- persisted revision metadata;
- audit/diff evidence already present in revision views.

The archive intentionally does **not** embed historical rollback file bytes from each revision ledger entry. Those remain governed by the local registry database.

### Export drift protection

Archive preparation is restricted to retired registry resources.

By default PattN verifies that the retained catalog file's current SHA-256 still matches the registry hash captured before retirement.

If the file changed after retirement, export is rejected instead of silently packaging bytes that no longer correspond to the persisted registry evidence.

An explicit `AllowFileDrift` option exists for advanced callers that intentionally want to archive the changed file; the UI does not enable that override in this checkpoint.

### Atomic archive file output

Archive saving writes a temporary sibling file and then atomically replaces/moves it into the user-selected destination.

The management UI uses the existing platform-native save-file interaction:

- WPF: existing `SaveFileDialog`;
- Avalonia: existing storage-provider save picker.

Export is read-only with respect to:

- provider registry state;
- revision history;
- original catalog JSON.

### Explicit SQLite compaction

New `SQLiteCompactionService` exposes a two-stage compaction workflow:

1. `PreviewAsync`
2. `ApplyAsync`

No startup path, lifecycle retention path, endpoint maintenance path, or ordinary settings save calls this service automatically.

Preview records:

- database path;
- database file length;
- last-write timestamp;
- SQLite page size;
- page count;
- freelist page count;
- estimated reclaimable bytes.

The estimate is `page_size * freelist_count`; it is informational, not a guaranteed final file size.

### Stale-plan protection

Before VACUUM, apply prepares a fresh database-state observation and requires the frozen preview to still match:

- normalized database path;
- file length;
- last-write timestamp;
- page size;
- page count;
- freelist count.

If any of those changed, the compaction plan is rejected and the user must preview again.

This keeps the displayed reclaim estimate tied to the state the user actually reviewed.

### WAL checkpoint and VACUUM

After stale validation, apply performs:

1. `PRAGMA wal_checkpoint(TRUNCATE)`;
2. `VACUUM`.

The result reports:

- before state;
- after state;
- observed database-file bytes reclaimed.

The UI warning explicitly notes that VACUUM rewrites the database and can temporarily require additional disk space.

### Management UI

The retired-catalog archive tab now exposes **Export archive**.

The Maintenance tab now includes a separate **SQLite database compaction** group with:

- Preview database compaction;
- Apply database compaction;
- preview/result detail text.

Apply requires the existing cross-platform confirmation interaction.

Compaction remains deliberately separate from:

- endpoint cleanup;
- lifecycle evidence deletion.

Deleting evidence never implicitly VACUUMs the database.

### Regression coverage

Added tests for:

- retired archive bundle creation;
- exact catalog bytes/Base64;
- catalog-file SHA-256 preservation;
- persisted revision metadata carry-through;
- archive-file serialization;
- default rejection of post-retirement file drift;
- explicit drift override;
- compaction reclaim arithmetic;
- overflow-safe reclaim estimation;
- stale database-state detection.

### Next boundary

Once this passes the full PR gate, the next slice can safely focus on:

- signed provider-catalog provenance metadata;
- explicit user-triggered remote fetch with ETag/content-hash caching through the same preview/apply path;
- archive import/inspection without automatic registration;
- maintenance policy editors;
- optional scheduled reminders for stale catalogs without background mutation.


## Continuation checkpoint: explicit remote provider-catalog fetch + detached signed provenance

This checkpoint adds the first remote provider/ASN catalog transport, but deliberately keeps remote I/O separate from catalog mutation.

### User-triggered HTTPS fetch only

Remote provider catalog sources are persisted per registered catalog in `ProviderAsnCatalogRemoteSourceItem`.

Stored source state includes:

- registry ID;
- catalog HTTPS URI;
- optional detached-signature HTTPS URI;
- signature policy;
- locally trusted key ID;
- locally trusted public-key SPKI;
- ETag;
- Last-Modified;
- last observed remote catalog SHA-256;
- source configuration timestamp;
- cache/check/fetch timestamps;
- last signature-validation result.

No background refresh task, timer, startup fetch, or automatic apply path is introduced.

### HTTPS and response bounds

`HttpProviderAsnCatalogRemoteTransport`:

- accepts only absolute HTTPS catalog URLs;
- validates the final URL after redirects is still HTTPS;
- supports If-None-Match and If-Modified-Since;
- uses response-header-first streaming;
- rejects Content-Length above the catalog byte budget;
- independently enforces the byte budget while streaming;
- returns 304 as metadata rather than inventing content.

Remote-source configuration rejects embedded URI credentials and fragments.

### Conditional cache semantics

A 304 response is considered sufficient only when the current local registered catalog SHA-256 still matches the cached remote-content SHA-256.

This matters after rollback:

- remote cache may still describe version N+1;
- local file may have been rolled back to N;
- server can legitimately return 304 for the cached N+1 validator.

In that case PattN performs one explicit unconditional re-fetch to recover the N+1 bytes required for a fresh preview.

A 304 never causes cached metadata alone to overwrite a local catalog.

### Detached locally trusted signature envelope

Optional signed provenance uses a detached JSON envelope with:

- schema version;
- algorithm: `ecdsa-p256-sha256`;
- signature encoding: IEEE P1363;
- key ID;
- catalog ID;
- catalog version;
- catalog SHA-256;
- signing timestamp;
- signature.

The signed payload has a fixed PattN domain separator plus canonical catalog identity/hash/time fields.

The signature is verified with a locally configured ECDSA P-256 SubjectPublicKeyInfo public key.

The public key is **not** accepted from:

- the remote catalog;
- the detached signature response;
- transport headers.

This prevents the object being verified from supplying its own trust anchor.

Signature policy:

- `None`: provenance signature not required;
- `Optional`: unsigned source is allowed only when no signature trust configuration is supplied;
- `Required`: signature URI + trusted key ID + trusted SPKI are mandatory.

If optional signature verification is configured, an invalid signature is still rejected; "optional" means signature configuration may be absent, not that a bad signature is acceptable.

### Signature scope vs freshness

A valid detached signature authenticates the configured key's statement about:

- catalog identity;
- catalog version;
- exact catalog bytes;
- signing timestamp.

It does not by itself prove that the catalog is fresh or currently preferred.

The existing catalog metadata audit remains responsible for freshness/staleness policy.

### Preview remains non-mutating

`ProviderAsnCatalogRemoteUpdateService.FetchPreviewAsync`:

1. confirms the registry resource is still active;
2. verifies the local file still matches the registry SHA-256;
3. performs the conditional HTTPS fetch;
4. performs the rollback-aware unconditional re-fetch when necessary;
5. parses/validates catalog bytes;
6. requires the remote catalog ID to match the registry catalog ID;
7. applies detached-signature policy;
8. persists remote cache/provenance metadata;
9. delegates changed bytes to the existing `PrepareUpdateAsync`.

The output is a `ProviderAsnCatalogRemoteFetchPreview`.

The registered catalog file is unchanged.

### Apply still uses the existing update pipeline

`ApplyAsync` requires a prepared remote preview and verifies before mutation that:

- remote-source trust/configuration has not changed since preview;
- cached remote-content SHA-256 still matches the preview.

It then delegates to `ProviderAsnCatalogRegistryService.ApplyUpdateAsync`.

Therefore all previously landed guards remain authoritative:

- same catalog ID;
- metadata audit;
- semantic diff;
- local stale-file detection;
- atomic replacement;
- post-write SHA verification;
- persistent revision ledger;
- compensation;
- restart-safe rollback.

Remote transport never writes the catalog file directly.

### Regression coverage

Added tests for:

- valid locally trusted ECDSA P-256 detached signatures;
- key-ID mismatch rejection;
- catalog-hash mismatch rejection;
- tampered-signature rejection;
- optional unsigned source configuration;
- rejection of partial signature trust configuration;
- normal 304 with local == cached remote producing no update;
- rollback case: 304 + local != cached remote forcing an unconditional second GET;
- required invalid signature blocking preview;
- remote-cache change invalidating an already prepared apply;
- non-HTTPS remote URI rejection.

### Persistence

Application startup now creates `ProviderAsnCatalogRemoteSourceItem` through PattN's existing SQLite database.

Remote cache/trust state is separate from the provider catalog revision ledger; the catalog revision remains the authoritative record of file mutation.

### Next boundary

After this backend passes the full PR gate, the product slice can add:

- explicit remote-source configuration UI;
- remote fetch preview and signature-status details;
- apply through the existing catalog update confirmation;
- persisted remote-apply provenance linked to catalog revision IDs;
- optional stale-catalog reminders that never fetch/apply in the background.


## Continuation checkpoint: explicit remote-source UI + revision-linked signed provenance

This checkpoint exposes the already-landed explicit remote provider-catalog transport in the shared Discovery management surface without weakening its trust or mutation boundaries.

### Remote source editor

The selected active provider catalog now has a collapsed **Remote source / signed provenance** section in both WPF and Avalonia.

The editor exposes:

- catalog HTTPS URL;
- optional detached-signature HTTPS URL;
- signature policy:
  - None;
  - Optional;
  - Required;
- locally trusted key ID;
- locally trusted ECDSA P-256 SubjectPublicKeyInfo public key as Base64.

The public key is intentionally editable/readable because it is trust configuration, not secret key material.

Saving remote-source configuration:

- validates through the existing backend HTTPS/trust-key rules;
- updates only remote source/cache/trust metadata;
- does not fetch remote bytes;
- does not mutate the registered catalog file;
- clears any previously prepared remote fetch preview because the trust/configuration snapshot changed.

### Explicit remote actions

The UI exposes four explicit actions:

1. **Save remote source**
2. **Remove remote source**
3. **Fetch preview**
4. **Apply remote preview**

No action runs automatically on startup, selection, refresh-all, or a timer.

Removing a remote source requires confirmation and removes only:

- source URI configuration;
- conditional cache validators;
- cached remote-content hash;
- current signature-check status.

It does **not** change:

- the local catalog JSON;
- the catalog revision ledger;
- previously persisted revision-linked remote provenance.

### Fetch preview

**Fetch preview** operates on the saved remote source configuration, not unsaved editor text.

The preview displays:

- check time;
- whether the server returned 304;
- whether local bytes already match remote bytes;
- remote content SHA-256;
- signature policy;
- whether signature verification was attempted;
- signature validity;
- policy-satisfied state;
- signature status;
- verified key ID;
- existing semantic local update diff/audit when content differs.

The existing backend remains authoritative for:

- HTTPS-only transport;
- response-size limits;
- conditional ETag/Last-Modified fetch;
- rollback-aware unconditional refetch;
- locally anchored signature verification;
- same-catalog-ID enforcement;
- metadata audit;
- semantic diff.

A fetch preview never changes the catalog file.

### Apply remote preview

Applying a remote preview uses the existing catalog-update confirmation interaction and delegates to `ProviderAsnCatalogRemoteUpdateService.ApplyAsync`.

The backend revalidates before mutation that:

- remote trust/source configuration did not change after preview;
- cached remote-content SHA-256 still matches the preview;
- the frozen catalog update plan still passes all existing stale-file and ownership checks.

The remote transport never writes the catalog JSON directly.

After apply:

- normal catalog metadata is refreshed;
- the created catalog revision is selected;
- revision history remains the authoritative file-mutation ledger;
- the remote fetch preview is cleared.

### Revision-linked remote provenance

A new SQLite entity, `ProviderAsnCatalogRemoteApplyProvenanceItem`, records remote origin evidence keyed directly by the resulting catalog revision ID.

Persisted fields include:

- revision ID;
- registry ID;
- source URI;
- signature URI;
- signature policy;
- locally trusted key ID;
- 304 state;
- ETag;
- Last-Modified;
- exact remote content SHA-256;
- signature attempted/valid/policy-satisfied state;
- signature status;
- signature envelope key ID;
- signed catalog SHA-256;
- signature signing time;
- remote check time;
- revision apply time.

The trusted public key bytes themselves are not duplicated into revision provenance; the revision records the trusted key ID and verified signature evidence.

### Provenance persistence timing

Remote provenance is written only after the existing catalog revision apply succeeds.

The catalog revision remains authoritative for the mutation.

If provenance persistence fails after a successful catalog apply:

- the successful catalog mutation is not rolled back merely because auxiliary provenance history failed;
- the failure is logged;
- the revision remains valid but its remote provenance detail may be unavailable.

This mirrors PattN's existing principle that auxiliary history failure must not mislabel a completed user mutation as failed.

### Revision detail UX

Selecting a catalog revision now reconstructs two layers of evidence:

1. existing local revision evidence:
   - before/after versions;
   - active/rolled-back state;
   - semantic diff;
   - audit;
   - before/after hashes;
   - apply time;
2. optional remote provenance:
   - remote source URI;
   - remote content hash;
   - ETag;
   - remote check/apply time;
   - signature status/policy/key.

A revision without remote provenance is explicitly shown as a local/manual catalog update.

Async source/provenance loads are selection-safe: if the user changes catalogs or revisions while an async read is pending, the stale result is discarded rather than overwriting the newly selected item's UI.

### Shared UI implementation

The same `DiscoveryManagementViewModel` drives both frontends.

The remote section is collapsed by default so local-file catalog management remains the primary uncluttered path.

Both WPF and Avalonia receive matching:

- editor fields;
- signature-policy selector;
- command buttons;
- source/cache/signature status;
- fetch-preview evidence.

Neutral resource strings are defined in `ResUI.resx` with generated accessors; satellite cultures fall back to those strings until translated.

### Regression coverage

Added a successful remote-apply regression proving that:

- the remote preview applies through the normal catalog revision pipeline;
- one provenance row is persisted;
- provenance is keyed to the returned revision ID;
- registry/source URI are preserved;
- exact remote content SHA-256 and ETag are preserved;
- signature-policy satisfaction is preserved;
- revision provenance can be read back through the remote update service.

The full WPF/Avalonia build gate remains the authoritative compile check for the new controls, resources, and ReactiveUI bindings.

### Next boundary

After this surface passes the full PR gate, the next slice can safely focus on:

- stale-catalog reminders that never fetch or apply in the background;
- remote source health/history views across all registered catalogs;
- explicit archive export of remote provenance alongside retired catalog bundles;
- provenance retention/pruning policy;
- optional certificate/SPKI pinning for HTTPS transport as a separate trust mechanism from catalog signatures;
- remote source configuration import/export without embedding private key material.


## Continuation checkpoint: remote-source fleet health + provenance-aware archives + guarded provenance retention

This checkpoint extends the explicit remote-catalog workflow with read-only fleet visibility and lifecycle handling for revision-linked remote provenance. It introduces no background remote I/O.

### Persisted remote-source fleet health

New `ProviderAsnCatalogRemoteHealthService` derives health exclusively from:

- active provider/ASN catalog registry rows;
- persisted remote source/cache metadata;
- persisted signature-validation status;
- persisted local catalog audit/freshness state.

It performs no HTTP request, DNS lookup, file mutation, or source refresh.

Default review policy considers a saved remote check stale after 30 days.

### Fleet health classes

Each active catalog receives one persisted-state health class:

- `unconfigured`
- `catalog-invalid`
- `catalog-stale`
- `signature-failed`
- `signature-required`
- `unchecked`
- `stale-check`
- `healthy`

Precedence is intentionally evidence-oriented:

1. invalid local catalog audit;
2. stale local catalog metadata;
3. persisted signature failure / missing required verified signature;
4. never checked;
5. old check timestamp;
6. healthy.

A durable signature failure therefore cannot be hidden by the weaker fact that the last check is old.

An unconfigured remote source is not treated as a fault because remote transport is optional; it is reported as `unconfigured` with `NeedsReview = false`.

### Remote sources fleet tab

The shared Discovery management window now has a dedicated **Remote sources** tab in both WPF and Avalonia.

It shows:

- display name;
- catalog version;
- whether a remote source is configured;
- source HTTPS URI;
- signature policy;
- last checked time;
- health class;
- review-required state.

Selecting a row shows:

- catalog ID/version;
- enabled/configured state;
- signature policy;
- last checked/fetched timestamps;
- source URI;
- cached remote SHA-256;
- local audit validity/freshness/staleness;
- concrete health reason codes.

The tab's Refresh action only rereads persisted metadata. It explicitly performs no remote fetch.

`Refresh All` also refreshes this local-only health projection; opening the management window therefore remains network-passive.

### Shared revision provenance projection

Remote revision provenance mapping is centralized in `ProviderAsnCatalogRemoteProvenanceProjector`.

Both:

- selected revision detail; and
- retired archive export

use the same projection contract so signature/cache/time semantics cannot drift between UI and archive output.

### Retired archive format v2

`ProviderAsnCatalogArchiveBundle` is now format version 2.

In addition to the existing:

- current retained catalog bytes;
- current catalog SHA-256;
- registry metadata;
- audit/revision metadata;

the archive includes all persisted remote-apply provenance rows whose revision IDs are part of the exported revision set.

Archive provenance contains:

- revision/registry IDs;
- source URI;
- signature URI;
- signature policy;
- trusted key ID;
- conditional-cache evidence;
- remote content SHA-256;
- signature validation outcome;
- signing/check/apply timestamps.

The trusted public key/private signing material is not embedded into the archive provenance payload. The archive records the key ID and verification evidence, not private key material.

Remote provenance for revisions omitted by `MaxRevisions` is also omitted from the archive.

### Explicit provenance retention

`LifecycleRetentionPolicy` now includes:

- `RemoteCatalogProvenanceRetention` — default 730 days.

Remote provenance is deliberately more protected than ordinary append-only telemetry.

An old provenance row is eligible only when:

- its applied timestamp is older than the retention cutoff; and
- its linked catalog revision is either:
  - rolled back; or
  - no longer present.

If the linked revision still exists and has not been rolled back, provenance is retained regardless of age.

This prevents explicit retention from silently removing origin/signature evidence for a surviving revision.

### Retention preview/apply integration

Remote provenance participates in the same frozen explicit lifecycle-retention workflow:

1. Preview selects exact revision IDs.
2. The maintenance UI shows candidate counts and approximate serialized payload size.
3. Apply deletes only frozen IDs that still meet the table/cutoff plan.
4. SQLite compaction remains a separate explicit operation.

No provenance pruning occurs automatically on startup, archive export, source removal, catalog unregister, or rollback.

### Payload estimates

`MaintenancePayloadEstimateService` now estimates serialized payload represented by frozen remote-provenance candidates alongside:

- DNS repair history;
- resolver telemetry;
- repair promotion history;
- endpoint observation history.

As before, this is an approximate serialized payload estimate, not a guaranteed SQLite file-size reduction.

### Regression coverage

Added coverage for:

- unconfigured remote sources remaining optional/non-failing;
- persisted signature failure outranking stale-check age;
- healthy recent required-signature evidence;
- stale persisted remote check detection;
- stale local catalog metadata outranking otherwise-current remote state;
- archive format version 2;
- archive provenance filtered to included revisions;
- archive serialization/deserialization preserving remote provenance;
- old provenance for surviving non-rolled-back revisions remaining protected;
- rolled-back/missing revision provenance becoming retention-eligible;
- strict-before-cutoff semantics;
- frozen provenance cutoff validation.

The WPF/Avalonia compile gate remains authoritative for the new fleet tab and ReactiveUI bindings.

### Next boundary

After this checkpoint passes the full PR gate, the next strong slice can focus on:

- explicit stale-catalog/reminder preferences without background fetch;
- archive inspection/import that never auto-registers or applies;
- remote source configuration export/import with public trust material only;
- provenance search/filtering across revisions;
- explicit per-source certificate/SPKI pinning as a transport trust mechanism separate from catalog signatures;
- maintenance policy editors for retention windows and delete budgets.


## Continuation checkpoint: archive inspection + portable remote-source trust config + provenance search

This checkpoint makes provider-catalog archives and remote-source trust configuration portable/readable without introducing automatic registration, remote fetch, or catalog mutation.

### Read-only archive inspection

New `ProviderAsnCatalogArchiveInspectionService` inspects exported retired-catalog archives without importing or registering them.

Inspection validates:

- bounded archive file size;
- archive format version;
- registry identity presence;
- simple/non-traversing archived catalog filename;
- embedded catalog Base64;
- embedded catalog byte-size limit;
- exact embedded catalog SHA-256;
- embedded catalog schema;
- embedded catalog ID matching archived registry catalog ID;
- remote provenance rows referencing only revisions actually present in the archive.

The embedded catalog is audited with the existing provider-catalog metadata audit policy.

Inspection returns:

- registry/catalog IDs;
- catalog version;
- archived filename;
- verified payload SHA-256;
- payload byte count;
- catalog entry count;
- revision count;
- remote provenance record count;
- catalog audit;
- warnings.

A registry SHA that differs from the archived payload is reported as a warning instead of silently treated as equal, preserving compatibility with explicit drift-allowed retired exports.

Inspection performs no:

- file extraction;
- registry insertion;
- source configuration;
- remote I/O;
- revision creation;
- catalog apply.

### Portable remote-source configuration bundle

New `ProviderAsnCatalogRemoteSourcePortableBundle` carries only the configuration required to reproduce remote source trust:

- format version;
- export timestamp;
- catalog ID/version;
- display name;
- catalog HTTPS URI;
- optional detached-signature HTTPS URI;
- signature policy;
- trusted key ID;
- trusted ECDSA P-256 public-key SPKI;
- public-key SHA-256 fingerprint.

The bundle deliberately excludes runtime/cache state:

- ETag;
- Last-Modified;
- last remote content hash;
- cached remote bytes;
- last check/fetch timestamps;
- last signature result;
- detached signature bytes;
- private signing material.

### Atomic portable bundle file I/O

`ProviderAsnCatalogRemoteSourcePortabilityService` now supports:

- `ExportAsync`;
- `SaveAsync`;
- `LoadAsync`;
- `PrepareImportAsync`;
- `ApplyImportAsync`.

Portable bundle files are bounded to 256 KiB.

Save writes a temporary sibling file and atomically moves/replaces it into the requested destination.

Load validates:

- format version;
- catalog ID;
- HTTPS-only source/signature URIs;
- no URI credentials/fragments;
- trusted-key configuration;
- public-key encoding;
- optional stored public-key SHA-256 fingerprint.

### Preview-first import

Remote source import is a two-stage operation.

`PrepareImportAsync`:

1. validates the portable bundle;
2. requires the target registry resource to remain registered;
3. requires exact catalog-ID match;
4. validates source/signature HTTPS configuration;
5. validates public trust material;
6. computes imported configuration fingerprint;
7. snapshots the existing local remote-source configuration fingerprint;
8. warns when bundle catalog version differs from the current local catalog version.

It performs no fetch and no source mutation.

### Stale-preview protection

`ApplyImportAsync` revalidates:

- target registry still registered;
- target catalog ID unchanged;
- current local remote-source configuration fingerprint still equals the preview snapshot;
- preview's imported configuration still equals its prepared fingerprint.

If another user/action changes source/trust configuration after preview, apply is rejected.

Successful apply delegates to the existing `ProviderAsnCatalogRemoteUpdateService.ConfigureAsync`, therefore imported configuration receives the same:

- HTTPS restrictions;
- trust-key validation;
- cache reset semantics.

Import does **not** fetch remote bytes or apply a catalog update.

### Searchable remote apply provenance

New `ProviderAsnCatalogRemoteProvenanceQueryService` provides a bounded read-only search over persisted revision-linked remote provenance.

Filters include:

- registry ID;
- revision ID;
- source hostname;
- trusted key ID;
- signature status;
- signature valid/invalid;
- signature-policy satisfied/unsatisfied;
- maximum age;
- maximum item count.

The default query window is 730 days and the returned item count is bounded.

The summary reports:

- total matching provenance records;
- signature-attempted count;
- valid-signature count;
- policy-satisfied count;
- server-304 count;
- latest apply timestamp;
- projected provenance entries.

Source filtering accepts a hostname only, not an arbitrary URL, reducing ambiguous filter semantics.

The query performs no:

- network access;
- source configuration;
- catalog mutation;
- retention deletion;
- archive mutation.

### Regression coverage

Added focused tests for:

- valid archive inspection;
- archive SHA-256 tamper rejection;
- archive provenance referencing a missing revision;
- portable export preserving public trust config while excluding ETag/cache content state;
- atomic save/load round trip;
- catalog-ID mismatch rejection on import preview;
- public-key fingerprint mismatch rejection;
- stale local source configuration invalidating an import preview;
- successful import applying source/trust config without fetching remote bytes;
- provenance filtering by registry/host/key/status/validity;
- newest-first provenance summary;
- invalid URL-shaped source-host filter rejection.

### Next boundary

Once this checkpoint passes the full PR gate, the next strong slice can focus on:

- archive inspection UI that never auto-registers;
- remote-source config export/import UI using the preview/apply contract;
- provenance search/filter UI across catalog revisions;
- maintenance policy editors for retention windows/delete budgets;
- optional certificate/SPKI pinning as a transport-level trust mechanism separate from detached catalog signatures;
- archive bundle signature support as a separate authenticity layer from catalog-content signatures.


## Continuation checkpoint: optional HTTPS SPKI pinning for remote provider catalogs

This checkpoint adds an optional transport-level trust constraint for remote provider/ASN catalog fetches. It is deliberately separate from detached catalog signatures.

### Trust model

SPKI pinning does **not** replace normal HTTPS validation.

For a pinned remote request PattN requires both:

1. platform TLS validation succeeds with no certificate policy errors;
2. the leaf certificate SubjectPublicKeyInfo SHA-256 matches one configured pin.

A matching pin cannot override:

- invalid certificate chains;
- hostname mismatch;
- expired/not-yet-valid certificates;
- other platform TLS policy errors.

This keeps SPKI pinning as an additional constraint rather than a custom CA/trust bypass.

### Pin format and bounds

Remote source configuration now accepts optional `TlsSpkiPinsSha256`.

Rules:

- SHA-256 over DER SubjectPublicKeyInfo;
- exactly 64 hexadecimal characters;
- normalized to lowercase;
- duplicates removed;
- canonical lexical ordering;
- maximum 8 pins.

Canonical ordering means reordering an equivalent pin set does not count as a remote-source configuration change and does not unnecessarily clear conditional-cache state.

### Persistent source configuration

`ProviderAsnCatalogRemoteSourceItem` stores the normalized pin set as JSON.

Existing rows with no pin column/value are interpreted as an empty pin set.

Changing the effective pin set is treated as a trust-configuration change and therefore resets the same remote cache/signature state already reset by changes to:

- source URI;
- signature URI;
- signature policy;
- trusted detached-signature key.

An old unpinned row and a newly configured empty pin set remain equivalent, avoiding a one-time migration cache reset.

### Transport enforcement

`ProviderAsnCatalogRemoteTransportRequest` carries the normalized pin set.

Pins are applied to:

- conditional catalog fetch;
- rollback-aware unconditional re-fetch;
- detached-signature envelope fetch.

Unpinned requests continue using the existing shared `HttpClient`.

Pinned requests use a request-scoped `HttpClientHandler` certificate callback that:

1. requires `SslPolicyErrors.None`;
2. requires a non-null certificate;
3. hashes `certificate.PublicKey.ExportSubjectPublicKeyInfo()` with SHA-256;
4. requires the hash to match one configured pin.

Automatic HTTPS redirects remain subject to the same pin set. A redirect to a host/certificate whose SPKI is not in the configured set therefore fails closed.

### Detached signature remains independent

TLS SPKI pinning and detached catalog signatures protect different layers:

- SPKI pins constrain the HTTPS server key accepted for transport;
- detached signatures authenticate the catalog content against a separately configured ECDSA signing key.

Either mechanism may be configured independently.

Using both requires both to succeed.

### Revision-linked provenance

Remote apply provenance now records the normalized SPKI pin set that was active when the remote preview was applied.

This lets a later revision/archive inspection answer both:

- which detached catalog-signature key/policy was trusted;
- which HTTPS SPKI pins constrained the transport.

Older provenance rows naturally project an empty pin set.

### Portable remote-source configuration

The portable remote-source bundle now carries SPKI pins as part of public trust configuration.

Pins therefore:

- survive explicit export/import;
- participate in import-preview fingerprints;
- trigger stale-preview rejection if local pin configuration changes after preview.

They remain public trust metadata; no private keys, cached remote bytes, ETags, or signature bytes are added to the portable format.

### Regression coverage

Added tests for:

- pin normalization;
- case canonicalization;
- deduplication;
- maximum pin count;
- invalid SHA-256 pin rejection;
- SPKI SHA-256 computed from a self-signed test certificate;
- positive pin match;
- pin mismatch rejection;
- matching pin still rejected when normal TLS reports chain errors;
- JSON persistence round trip;
- configured pins propagated into remote transport requests;
- persisted source pin set;
- portable export/import preserving pins.

### Next boundary

After this checkpoint passes the full PR gate, the next trust/product slice can focus on:

- explicit SPKI pin editor/validation UX in the remote-source section;
- helper UX to display the currently observed server SPKI only after a successful ordinary TLS connection, without auto-trusting it;
- archive inspection/import UI;
- provenance filtering by transport pin;
- pin rotation workflows that permit overlapping old/new pins;
- optional archive-bundle signatures as a separate authenticity layer from both HTTPS pins and catalog-content signatures.


## Continuation checkpoint: SPKI rotation UX + trust portability + archive/provenance inspection

This checkpoint exposes the already-landed SPKI/portability/provenance primitives through the shared management surface and fixes one stale-preview trust bug discovered during the integration pass.

### SPKI stale-preview correctness fix

The portable remote-source import preview previously fingerprinted:

- catalog URI;
- signature URI;
- signature policy;
- trusted detached-signature key ID;
- trusted detached-signature public key;

but accidentally omitted `TlsSpkiPinsSha256`.

That meant a local TLS pin-only configuration change after preview could evade the intended stale-preview check.

`ProviderAsnCatalogRemoteSourcePortabilityService.ConfigurationFingerprint` now includes the canonical normalized SPKI pin set.

Regression coverage explicitly proves:

1. prepare an import preview;
2. change only the current local TLS pin set;
3. apply the old preview;
4. stale-preview protection rejects it and preserves the newer local pin set.

### Human-editable pin set and overlap rotation

`ProviderAsnCatalogTransportPinning` now exposes editor helpers:

- `ParseEditorText`;
- `FormatEditorText`;
- `Diff`.

The editor accepts pins separated by:

- line breaks;
- commas;
- semicolons;
- spaces/tabs.

All input still passes through the canonical pin validator:

- SHA-256 hex only;
- lower-case canonicalization;
- deduplication;
- lexical ordering;
- maximum 8 pins.

The UI shows a pending pin-set diff:

- added pins;
- removed pins;
- unchanged pins.

When at least one old pin remains while a new pin is added, the UI explicitly identifies the configuration as an overlap-rotation set.

No rotation step automatically removes the old pin. The user can save an overlap set, verify the new endpoint/key behavior, and later remove the retired pin in a separate explicit save.

### Ordinary-TLS SPKI observation without trust-on-first-use

New `ProviderAsnCatalogTlsObservationService` performs an explicit HTTPS request solely to observe the leaf certificate SPKI.

Security rules:

- URI must be absolute HTTPS with no embedded credentials or fragment;
- normal platform certificate-chain and hostname validation must succeed;
- a custom callback returns false for every non-`SslPolicyErrors.None` result;
- redirects must remain HTTPS;
- the response body is not consumed beyond headers;
- observation has a bounded timeout;
- the observed SPKI is never persisted as trusted configuration automatically.

The observation contains:

- requested URL;
- final URL;
- HTTP status;
- final host;
- SPKI SHA-256;
- certificate subject;
- issuer;
- validity interval;
- observation timestamp.

The management UI has separate actions:

1. **Inspect server SPKI** — network observation only;
2. **Add observed pin to editor** — modifies only the unsaved editor;
3. **Save remote source** — the only step that persists the proposed trust set.

This deliberately avoids TOFU-style auto-trust.

### Portable remote-source trust configuration UI

The existing public-trust portability backend is now reachable from the Provider Catalogs management surface.

Actions:

- **Export trust config**
  - exports source/signature HTTPS URIs;
  - signature policy;
  - public detached-signature trust key;
  - SPKI pin set;
  - no ETag/cache content/runtime timestamps;
  - no private signing material.
- **Preview trust import**
  - loads and validates a bounded JSON bundle;
  - verifies catalog identity;
  - displays source/signature policy/key/pin count/warnings;
  - performs no remote fetch;
  - performs no source mutation;
  - performs no catalog update.
- **Apply trust import**
  - requires confirmation;
  - revalidates stale-preview fingerprints, now including SPKI pins;
  - changes source/trust configuration only;
  - resets remote cache state through the existing configuration pipeline when the effective trust/source configuration changes;
  - performs no remote fetch or catalog apply.

Native WPF/Avalonia file pickers are used for bundle import/export.

### Read-only catalog archive inspection UI

The Retired Catalogs surface now exposes **Inspect archive**.

The operation delegates to the existing bounded `ProviderAsnCatalogArchiveInspectionService`.

Displayed evidence includes:

- archive format/version/time;
- catalog ID/version;
- entry count;
- payload byte count;
- verified SHA-256;
- revision count;
- remote provenance record count;
- registry-hash/payload agreement;
- catalog metadata audit/freshness;
- warnings.

Inspection remains read-only:

- no extraction;
- no catalog registration;
- no remote-source configuration;
- no remote fetch;
- no trust import;
- no catalog apply.

### Searchable remote provenance by transport pin

`ProviderAsnCatalogRemoteProvenanceQuery` now supports an optional single `TlsSpkiPinSha256` filter.

The pin filter is:

- trimmed;
- canonicalized through the same SPKI pin validator;
- rejected when invalid;
- matched against the exact persisted pin set active for each revision-linked remote apply.

Malformed legacy pin JSON fails closed for that row rather than matching by accident.

The provenance summary now also reports how many matching applies were transport-pinned.

The Remote Sources management tab now contains a local-only provenance search surface with filters for:

- source hostname;
- detached-signature trusted key ID;
- transport SPKI pin.

It shows:

- revision ID;
- source;
- trusted signature key;
- signature status;
- apply time;
- detailed remote hash/ETag/signature evidence;
- persisted transport pin set.

Search performs no network request and no mutation.

### Cross-platform management UI

The same shared `DiscoveryManagementViewModel` owns behavior for WPF and Avalonia.

Provider Catalogs remote-source editor now includes:

- SPKI pin editor;
- validation/diff status;
- ordinary-TLS SPKI inspection;
- explicit add-observed-pin-to-editor action;
- observed certificate details;
- portable trust export/import;
- trust-import preview.

Remote Sources now has separate persisted-state views for:

- fleet health;
- searchable remote apply provenance.

Retired Catalogs now includes read-only archive inspection alongside existing archive export.

The expanded remote-source editor is placed inside its own bounded scroll surface so opening it cannot crowd the main catalog/revision grids out of the window.

### Regression coverage

Added/expanded tests for:

- editor pin parsing and canonicalization;
- overlap rotation diff;
- ordinary-TLS certificate/SPKI projection helper;
- rejection of non-HTTPS observation evidence;
- stale portable-import rejection after a pin-only configuration change;
- provenance filtering by case-insensitive/canonicalized transport SPKI pin;
- transport-pinned provenance counts.

### Validation baseline

The branch immediately before this checkpoint, `f85a7349c6f8a6b9d25a4a022207df3fe742abef`, passed the complete PR gate:

- Go tests;
- Go vet;
- DNS fixture corpus audit;
- ServiceLib tests;
- WPF build;
- Avalonia build;
- test-result publication.

The next PR run must validate this UI/trust delta on top of that green baseline.

### Next boundary

After this checkpoint is green, the next rigorous trust/lifecycle slice can focus on:

- optional signatures over portable archive bundles themselves;
- archive signature verification during read-only inspection;
- persisted trust-configuration revision history for source/key/pin changes;
- explicit pin-rotation receipts/history;
- provenance filtering/export across multiple catalogs;
- maintenance-policy editors for retention windows and deletion budgets;
- reviewed real DNSSEC capture corpus growth.


### Post-gate trust hardening

The first SPKI-management UI checkpoint (`ac29c9f7bb7ea0e4fecd336e01193c48466b003a`) passed the complete PR gate, including both desktop UI compilers.

A follow-up review tightened three trust-change edges:

- **Observed-key authority binding:** an observed SPKI may be added to the editor only while the current remote-source HTTPS authority (scheme/host/port) still matches the authority that was inspected. Changing the source authority requires a fresh observation.
- **Cross-host redirect protection:** when ordinary HTTPS inspection ends at a different host/port, the final certificate pin is displayed as evidence but cannot be added through the one-click helper. The pinned transport evaluates every TLS hop, so a single final-host pin would be incomplete. Users must choose a stable direct HTTPS source or manually enter a deliberately reviewed multi-hop pin set.
- **Explicit pin-removal confirmation:** saving a remote source that removes one or more previously persisted SPKI pins now requires confirmation before any configuration write occurs. Invalid editor text also fails before the save pipeline begins.

Portable trust-import preview now reports the exact pin delta (`added / removed / unchanged`) in addition to pin count, so trust weakening/rotation is visible before confirmation.


## Continuation checkpoint: signed archives + durable remote trust revisions + linked pin-rotation receipts

This checkpoint adds a third independent authenticity layer for retired catalog archives and a durable audit ledger for remote source/key/pin configuration changes.

### Three distinct trust layers

PattN now keeps three mechanisms deliberately separate:

1. **HTTPS/TLS validation + optional SPKI pins**
   - constrains the transport/server key accepted during remote fetch;
2. **detached provider-catalog signature**
   - authenticates fetched catalog content against a locally trusted release key;
3. **provider catalog archive signature**
   - authenticates the exported retired-catalog archive itself.

A success in one layer never bypasses failure in another.

### Provider archive format v3

New archives default to format version 3.

Format v3 adds an optional `ArchiveSignature` envelope containing:

- schema version;
- algorithm;
- signature encoding;
- key ID;
- archive-payload SHA-256;
- signing timestamp;
- ECDSA signature.

Existing unsigned format-v2 archives remain readable.

### Archive signature algorithm

Archive signatures use:

- ECDSA;
- NIST P-256 specifically;
- SHA-256;
- IEEE P1363 fixed-field signature encoding.

The signer service requires an actual NIST P-256 curve, not merely an EC key whose size happens to be 256 bits.

The private key is supplied by the caller as an `ECDsa` instance and is never serialized into the archive.

### Signed archive payload

The archive signature covers a domain-separated payload derived from:

- archive format version;
- archive creation timestamp;
- complete registry metadata hash;
- catalog file name;
- verified catalog-file SHA-256;
- revision-set hash;
- remote-provenance-set hash;
- notes hash.

The embedded catalog bytes remain protected by the existing exact catalog SHA-256 check; that hash is itself part of the signed archive payload.

Tampering with archive metadata therefore changes the signed payload even when the embedded catalog bytes are untouched.

### Signed archive export API

`ProviderAsnCatalogArchiveService.PrepareSignedAsync` composes:

1. the existing retired-catalog archive preparation;
2. optional remote provenance inclusion;
3. archive signing.

The existing `SaveAsync` persists either signed or unsigned bundles and still uses atomic sibling-temp replacement.

### Read-only signature verification

`ProviderAsnCatalogArchiveInspectionOptions` now accepts `ArchiveSignatureTrust`:

- optional/required policy;
- locally trusted archive-signing key ID;
- locally trusted ECDSA P-256 public-key SPKI.

Inspection:

- accepts archive formats 2 and 3;
- verifies embedded catalog bytes before archive-signature policy;
- verifies archive payload hash;
- verifies the locally trusted key ID;
- verifies ECDSA P-256 signature;
- reports structured signature validation evidence.

If signature verification is required and policy is not satisfied, inspection fails closed.

Unsigned v2 archives remain inspectable when signature verification is not required.

A signed archive with no configured local trust is reported as present-but-unverified rather than self-trusted from archive contents.

### Exact P-256 enforcement

The pre-existing detached catalog-signature verifier previously accepted any imported ECDSA key whose reported key size was 256 bits.

Both detached-catalog and archive signature verification now validate the actual EC curve OID against NIST P-256.

This aligns implementation with the documented algorithm contract.

### Durable remote trust-configuration revision ledger

New SQLite entity:

- `ProviderAsnCatalogRemoteSourceRevisionItem`.

Every **effective** remote source/trust configuration change can persist:

- revision ID;
- registry ID;
- change reason;
- before/after configuration fingerprints;
- before/after source URI;
- before/after signature URI;
- before/after signature policy;
- before/after trusted detached-signature key ID;
- SHA-256 fingerprints of before/after trusted detached-signature public keys;
- before/after TLS SPKI pin sets;
- added pins;
- removed pins;
- change timestamp.

Private key material is never recorded.

An identical configuration save produces no revision and no longer advances `ConfigurationUpdatedAt`, so a no-op save cannot invalidate an otherwise valid remote-fetch/import preview.

### Central mutation ownership

Revision recording occurs inside the existing `ProviderAsnCatalogRemoteUpdateService.ConfigureAsync` boundary.

This means the same ledger covers configuration changes originating from:

- existing management UI saves;
- portable trust configuration imports;
- dedicated pin rotation;
- future callers using the central configuration pipeline.

Source removal records a terminal `remove` revision with an empty after-fingerprint.

### Revision write failure semantics

The trust/source configuration mutation remains authoritative.

After a successful source configuration write:

- revision history is attempted with `CancellationToken.None`;
- revision persistence failure is logged;
- the completed trust change is not rolled back or misreported as failed solely because auxiliary history storage failed.

This matches PattN's existing promotion/provenance history principle.

### Change attribution

Callers can supply a bounded `ProviderAsnCatalogRemoteSourceChangeContext`.

Current reasons include:

- `configure`;
- `portable-import`;
- `tls-pin-rotation`;
- `remove`.

Pin rotation pre-generates the revision ID so its receipt can link directly to the corresponding trust revision.

### Trust revision queries

`ProviderAsnCatalogRemoteSourceRevisionQueryService` provides bounded read-only search by:

- registry ID;
- change reason;
- trusted detached-signature key ID;
- TLS SPKI pin;
- maximum age;
- maximum item count.

The summary reports:

- total matching revisions;
- pin changes;
- source URI changes;
- signature-trust changes;
- latest change time.

Malformed historical pin JSON fails closed for that row instead of creating a false pin match.

### Safe two-step pin rotation service

New `ProviderAsnCatalogTlsPinRotationService` formalizes the overlap workflow already exposed conceptually by the UI.

Rules:

- unchanged pin sets are rejected as no-ops;
- existing non-empty -> proposed non-empty requires at least one overlapping current pin;
- direct disjoint old->new replacement is rejected;
- removing all pins requires explicit `AllowUnpinning=true`;
- stale source/trust configuration after preview invalidates apply;
- mutated preview content/fingerprint invalidates apply;
- no remote fetch occurs as part of prepare/apply.

Recommended rotation remains:

1. current = old;
2. preview/apply = old + new;
3. explicitly validate ordinary HTTPS/server SPKI using the existing observation/fetch tools;
4. preview/apply = new;
5. old pin is retired only in the second explicit mutation.

### Pin rotation receipt

Successful apply returns `ProviderAsnCatalogTlsPinRotationReceipt` containing:

- pre-generated trust revision ID;
- registry ID;
- before pin set;
- after pin set;
- apply time;
- resulting remote source view;
- whether the linked trust revision was observable in the configured revision store.

A history-read failure after the committed change is logged and yields `RevisionRecorded=false`; it does not make the completed rotation appear failed.

### Retention integration

Remote source trust revisions participate in the same explicit lifecycle retention workflow as other append-only evidence.

New policy:

- `RemoteSourceRevisionRetention` — default 730 days.

Trust revisions therefore:

- are never pruned at startup;
- are never pruned merely because a source is removed;
- appear in retention preview;
- are deleted only through explicit retention apply;
- participate in serialized payload estimates;
- use frozen candidate IDs/cutoffs and the existing delete budget.

### Production composition

Application startup now creates the trust revision table.

`DiscoveryManagementViewModel` supplies the SQLite revision store to the central remote-update service, so existing management UI changes immediately participate in revision recording without a second mutation path.

The latest authority-binding, cross-host redirect protection, invalid-editor rejection, and explicit pin-removal confirmation from the preceding SPKI-hardening checkpoint remain unchanged.

### Regression coverage

Added/expanded coverage for:

- signed archive validation;
- archive metadata tamper detection;
- unsigned v2 backward compatibility;
- required-signature fail-closed behavior;
- signed archive export convenience path;
- rejection of non-P-256 signing keys;
- exact P-256 requirement for detached catalog trust keys;
- effective-change-only source revisions;
- no-op saves preserving configuration timestamps;
- linked pin-add/remove diffs;
- source removal terminal revision;
- best-effort revision persistence failure;
- trust-revision query by reason/pin;
- two-step overlapping pin rotation;
- disjoint rotation rejection;
- implicit unpinning rejection;
- stale rotation preview rejection;
- retention frozen-cutoff validation for trust revisions.

### Validation baseline

The live branch immediately before this checkpoint, `dc61001f75d7319b775adf9baba8c264d1cc9370`, passed the complete PR gate:

- Go tests;
- Go vet;
- DNS fixture corpus audit;
- ServiceLib tests;
- WPF build;
- Avalonia build;
- test-result publication.

### Next boundary

After this checkpoint is green, the next rigorous slice can focus on:

- exposing trust-revision history and linked pin-rotation receipts in the existing Remote Sources UI;
- optional signed-archive export/verification UX with explicit local key selection;
- archive signature key portability using public trust material only;
- retention policy editors for trust/provenance evidence;
- archive signature provenance in retired archive inspection;
- reviewed real captured DNSSEC fixtures replacing remaining synthetic coverage where appropriate.

## 2026-09-26 completion checkpoint

This is the current restart point; older "next boundary" sections above are historical.

### Integrated state

The umbrella branch contains the Discovery + Reviver runtime, deep DNS/DNSSEC evidence paths, resolver/catalog tooling, C# Discovery services, provider/ASN catalog lifecycle and trust, Reviver validation/promotion, desktop management surfaces, packaging hooks, crash-recovery hardening, and assurance workflows.

The sing-box WebSocket metadata path has also been hardened so unrelated encoded/empty query components and literal plus signs are preserved, while ed/eh metadata is always removed before the path is sent to the server.

### Review decomposition

PR #1 remains the integration umbrella.

- #2: pattn-discovery runtime and DNS engine.
- #3: Discovery RPC, endpoint, and DNS telemetry services.
- #4: sing-box WebSocket early-data path metadata fix.
- #5: evidence capture, fuzz corpus, and assurance workflows; stacked on #2.
- #6: provider/ASN catalog trust, lifecycle, and durability; stacked on #3.

The #2 + #5 tree exactly reconstructs the original pre-split #2 tree. The #3 + #6 tree exactly reconstructs the original pre-split #3 tree, so the split did not lose files from either extraction snapshot.

### Review status

Prior inline CodeRabbit findings on #1 are resolved. Three later correctness findings on #4 were verified and fixed in both #4 and the umbrella: preservation of empty query components, preservation of literal plus signs, and unconditional removal of invalid/empty ed/eh metadata from the request path. Fresh manual CodeRabbit reviews have been requested for the split PRs.

### Remaining evidence gates

These are evidence/environment gates, not hidden implementation TODOs:

- review root trust material after the real 2026-10-11 KSK rollover and IANA publication changes;
- execute the committed desktop accessibility matrix with real assistive technology;
- promote only human-reviewed real DNS/DNSSEC captures with stable provenance and replay expectations;
- retain real crash/power-loss and special-architecture proof where the required environments are available;
- extend long-duration load/soak and upgrade/downgrade evidence;
- add no deeper Reviver strategy unless a reproducible failure satisfies REVIVER_STRATEGY_EVIDENCE.md.

The next work should therefore be evidence collection and narrowly justified follow-up, not speculative feature expansion.

