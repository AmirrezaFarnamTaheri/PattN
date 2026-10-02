namespace ServiceLib.Helper;

/// <summary>
/// Security boundary for PattN's legacy runtime updater.
///
/// Release hashes fetched from the same origin do not independently authenticate that origin.
/// Until PattN ships a pinned public trust root plus a signed-manifest verifier, application,
/// core and geo runtime network updates fail closed.
///
/// Do not flip this as a feature flag. Re-enable runtime updates only by replacing the legacy
/// path with the verifier contract documented in UPDATE_TRUST_BOOTSTRAP.md.
/// </summary>
public static class RuntimeUpdateTrustPolicy
{
    public static readonly bool BlockUnauthenticatedLegacyUpdater = true;
}
