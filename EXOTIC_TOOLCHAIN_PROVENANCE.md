# Exotic Linux toolchain provenance

PattN builds proof packages for RISC-V and LoongArch64 even though Microsoft does not currently publish .NET 10 Linux SDK binaries for those architectures.

## Trust model

- Every external SDK archive is verified against a repository-reviewed cryptographic digest in `release-assets.lock.sh` **before extraction or execution**.
- RISC-V currently defaults to the `xujiegb/dotnet-riscv` release origin.
- LoongArch64 currently defaults to the `loongson/dotnet` release origin.
- These origins are bootstrap distribution points, not independent trust roots. The digest lock is authoritative for the bytes PattN accepts.
- Release operators SHOULD mirror the exact locked archives into organization-controlled immutable storage and set:
  - `PATTN_RISCV_DOTNET_BASE`
  - `PATTN_LOONG_DOTNET_BASE`
- Changing a mirror must never require changing a digest. A digest change is a separate reviewed supply-chain update.

## Release policy

A mirror improves availability and account-compromise isolation but does not replace digest review. CI must fail closed on any checksum mismatch. Do not silently fall back to another URL or a mutable `latest` asset.

When Microsoft publishes first-party SDKs for either architecture, migrate the default source after validating compatibility and updating the lock in a dedicated review.
