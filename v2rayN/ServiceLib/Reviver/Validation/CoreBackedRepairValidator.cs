using ServiceLib.Reviver.Models;

namespace ServiceLib.Reviver.Validation;

/// <summary>
/// Validates an in-memory repair candidate through PattN's real temporary speed-test core path. Nothing is
/// persisted. A candidate is considered revived only after repeated application-level requests traverse the
/// generated local SOCKS inbound successfully.
/// </summary>
public sealed class CoreBackedRepairValidator(RepairPolicy? policy = null) : IRepairCandidateValidator
{
    private readonly RepairPolicy _policy = policy ?? new RepairPolicy();

    public async Task<RepairValidationEvidence> ValidateAsync(
        RepairCandidate candidate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var profile = JsonUtils.DeepCopy(candidate.Profile)
            ?? throw new InvalidOperationException("Could not clone repair candidate for validation.");

        var testItem = new ServerTestItem
        {
            IndexId = profile.IndexId,
            Address = profile.Address,
            ConfigType = profile.ConfigType,
            QueueNum = 0,
            Profile = profile,
            CoreType = profile.CoreType ?? ECoreType.Xray,
            AllowTest = true,
        };

        ProcessService? process = null;
        var accumulator = new RepairValidationAccumulator();
        try
        {
            process = await CoreManager.Instance.LoadCoreConfigSpeedtest(testItem);
            if (process is null)
            {
                accumulator.AddFailure(ERepairFailureClass.CoreStartupFailure);
                return accumulator.Build();
            }

            if (!await WaitForPortAsync(testItem.Port, cancellationToken))
            {
                accumulator.AddFailure(ERepairFailureClass.CoreStartupFailure);
                return accumulator.Build();
            }

            // A bound port is not yet evidence that *this* core bound it: if the spawn already
            // died (the usual reason is that the port was taken), anything answering on it is a
            // third-party listener and its behaviour says nothing about the repaired profile.
            if (CoreHasExited(process))
            {
                Logging.SaveLog(nameof(CoreBackedRepairValidator),
                    $"core exited while validating (port {testItem.Port}); treating as startup failure");
                accumulator.AddFailure(ERepairFailureClass.CoreStartupFailure);
                return accumulator.Build(integritySuspect: true);
            }

            var proxy = new WebProxy($"socks5://{Global.Loopback}:{testItem.Port}");
            for (var attempt = 0; attempt < _policy.RuntimeAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var latency = await ConnectionHandler.GetRealPingTime(proxy, cancellationToken);
                if (latency > 0)
                {
                    accumulator.AddSuccess(latency);
                }
                else
                {
                    accumulator.AddFailure(ERepairFailureClass.ApplicationProbeFailure);
                }

                var evidence = accumulator.Build();
                var remaining = _policy.RuntimeAttempts - evidence.Attempts;
                if (evidence.Successes >= _policy.MinimumRuntimeSuccesses
                    || evidence.Successes + remaining < _policy.MinimumRuntimeSuccesses)
                {
                    break;
                }
            }

            // Re-assert both signals once the evidence is complete. A core that died mid-run would
            // otherwise leave a successful-looking record whose last probes were served by whoever
            // else holds the port.
            var suspect = CoreHasExited(process);
            if (!suspect && !await IsPortListeningAsync(testItem.Port, cancellationToken))
            {
                suspect = true;
            }

            if (suspect)
            {
                // Whatever answered on that port may not have been our core. The successes are real
                // measurements, so they are kept, but they are marked as unattributable rather than
                // deleted -- a promotion gate must not be able to accept them.
                Logging.SaveLog(nameof(CoreBackedRepairValidator),
                    $"core no longer serving port {testItem.Port} after validation");
            }

            return accumulator.Build(integritySuspect: suspect);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logging.SaveLog(nameof(CoreBackedRepairValidator), ex);
            accumulator.AddFailure(ERepairFailureClass.CoreStartupFailure);
            return accumulator.Build();
        }
        finally
        {
            if (process is not null)
            {
                try
                {
                    await process.StopAsync();
                }
                catch (Exception ex)
                {
                    // Cleanup must not leak the process handle or mask the validation result when
                    // a core has already exited during cancellation/startup failure.
                    Logging.SaveLog(nameof(CoreBackedRepairValidator), ex);
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// <see cref="ProcessService.HasExited"/> throws once the underlying handle is disposed, and a
    /// disposed handle here means the core is gone -- but the caller must not see an exception on a
    /// cleanup race, so the answer degrades to "alive" and the port probe decides instead.
    /// </summary>
    private static bool CoreHasExited(ProcessService process)
    {
        try
        {
            return process.HasExited;
        }
        catch (Exception ex)
        {
            Logging.SaveLog(nameof(CoreBackedRepairValidator), ex);
            return false;
        }
    }

    /// <summary>
    /// One-shot version of the readiness probe, used to detect a core that stopped serving during
    /// validation.
    /// </summary>
    private static async Task<bool> IsPortListeningAsync(int port, CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        attemptCts.CancelAfter(TimeSpan.FromMilliseconds(200));
        try
        {
            await client.ConnectAsync(Global.Loopback, port, attemptCts.Token);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logging.SaveLog(nameof(CoreBackedRepairValidator), ex);
            return false;
        }
    }

    private static async Task<bool> WaitForPortAsync(int port, CancellationToken cancellationToken)
    {
        if (port <= 0)
        {
            return false;
        }

        var deadline = DateTime.UtcNow.AddSeconds(3);
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var client = new TcpClient();
            using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            attemptCts.CancelAfter(TimeSpan.FromMilliseconds(200));
            try
            {
                await client.ConnectAsync(Global.Loopback, port, attemptCts.Token);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                await Task.Delay(100, cancellationToken);
            }
        }
        return false;
    }
}
