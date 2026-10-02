# Reviver strategy evidence gate

Reviver mutations are failure-driven. Do not add TLS, SNI, transport, or network-adaptation strategies merely because they are plausible.

A new strategy is eligible only when all of these exist:

1. A reproducible failing profile with secrets removed and a stable failure classification.
2. Repeated baseline validation through the real supported proxy core showing the failure.
3. One narrowly defined mutation hypothesis tied to that evidence.
4. Explicit invariants for every field that must remain unchanged, especially logical Host/SNI/Reality identity when only a physical endpoint changes.
5. Repeated real-core validation after mutation, including success quorum and outcome comparison against baseline.
6. A regression fixture/test that fails without the strategy and passes with it.
7. Detached/reversible promotion; subscription-owned source profiles remain unchanged.

If no reproducible failure satisfies this gate, the correct change is no new strategy. The default catalog remains DNS address-family repair, evidence-backed endpoint replacement, and conservative core fallback until evidence justifies another bounded mutation.
