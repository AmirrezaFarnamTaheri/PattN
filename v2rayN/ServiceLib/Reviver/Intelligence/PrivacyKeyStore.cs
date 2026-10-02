using System.Security.Cryptography;
using ServiceLib.Discovery.Services;

namespace ServiceLib.Reviver.Intelligence;

/// <summary>
/// Per-installation secret used to derive the local "anonymous" keys in
/// <see cref="NetworkIntelligenceService"/>.
/// </summary>
/// <remarks>
/// The keys used to be an unsalted SHA-256 over low-cardinality material (carrier name, ASN,
/// country code), which is not a privacy control: a 48-hex-digit digest of a value from a set of a
/// few hundred strings is a dictionary lookup, so anyone holding a record could recover the carrier
/// and could correlate records across users by digest equality.
/// <para>
/// A keyed digest removes both properties. It is unforgeable and not invertible by enumeration, and
/// it is only comparable inside this installation. The trade-off is deliberate and documented:
/// because the secret is local, a future exchange between installations cannot correlate keys
/// without an explicit agreed key. Introducing such an exchange is a policy decision (see
/// AUDIT_RECONCILIATION_2026-10-02.md, "network-fingerprint privacy policy") and must come with one.
/// </para>
/// <para>
/// The secret is 32 random bytes in the per-user config directory. A read-only or roaming profile
/// where it cannot be written degrades to a per-process ephemeral key rather than failing: keys
/// then do not correlate across runs, which is the safe direction for privacy. The file is never
/// logged and never included in a support bundle.
/// </para>
/// </remarks>
internal static class PrivacyKeyStore
{
    private const string FileName = "reviver-privacy.key";
    private const int KeyBytes = 32;

    private static readonly byte[] EphemeralKey = RandomNumberGenerator.GetBytes(KeyBytes);
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private static byte[]? _cached;

    public static byte[] GetKey()
    {
        if (_cached is { } cached)
        {
            return cached;
        }

        Gate.Wait();
        try
        {
            return _cached ??= LoadOrCreate();
        }
        finally
        {
            Gate.Release();
        }
    }

    private static byte[] LoadOrCreate()
    {
        var path = Utils.GetConfigPath(FileName);
        try
        {
            if (File.Exists(path))
            {
                var stored = File.ReadAllBytes(path);
                if (stored.Length >= KeyBytes)
                {
                    return stored;
                }
            }
        }
        catch (IOException)
        {
            return EphemeralKey;
        }
        catch (UnauthorizedAccessException)
        {
            return EphemeralKey;
        }

        var created = RandomNumberGenerator.GetBytes(KeyBytes);
        try
        {
            // The durable writer is async by design; this is a one-shot, one-file write on the
            // construction path, so waiting on it is bounded and cannot deadlock on our own thread.
            DurableAtomicFile
                .WriteAsync(path, created, cancellationToken: CancellationToken.None)
                .AsTask()
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            // Persistence failed (read-only volume, concurrent user profile). Keep working with an
            // in-memory key instead of refusing to classify the network.
            return EphemeralKey;
        }

        return created;
    }
}
