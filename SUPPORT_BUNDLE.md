# Privacy-safe Discovery / Reviver support bundles

`ReviverSupportBundleBuilder` exports diagnostic shape, not a reusable proxy configuration.

The bundle retains protocol/core/transport/security/port information, failure classes, numeric runtime validation, mutation kinds/fields, and evidence categories. Sensitive endpoint identities are replaced with bundle-local HMAC-SHA256 tokens so equality relationships remain visible inside one bundle without allowing cross-bundle correlation.

The exporter does **not** serialize raw `ProfileItem` objects. It omits credentials, usernames, profile/subscription IDs, remarks, raw share links, raw protocol/transport JSON, certificates, Reality/WireGuard keys, raw paths, service names, and free-form evidence summaries. Non-whitelisted evidence values are tokenized.

A bundle is still diagnostic material and should be reviewed before public disclosure.
