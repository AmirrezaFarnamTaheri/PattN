using ServiceLib.Reviver.Models;

namespace ServiceLib.Reviver.Validation;

public sealed class RepairValidationAccumulator
{
    private readonly List<int> _latencies = [];
    private readonly List<ERepairFailureClass> _failures = [];
    private readonly List<double> _throughputs = [];
    private int _consecutive;

    public int Attempts { get; private set; }
    public int Successes { get; private set; }
    public int ConsecutiveSuccesses { get; private set; }

    public void AddSuccess(int latencyMs, double? throughputMbps = null)
    {
        Attempts++;
        Successes++;
        _consecutive++;
        ConsecutiveSuccesses = Math.Max(ConsecutiveSuccesses, _consecutive);
        if (latencyMs > 0)
        {
            _latencies.Add(latencyMs);
        }
        if (throughputMbps is { } throughput
            && double.IsFinite(throughput)
            && throughput >= 0)
        {
            _throughputs.Add(throughput);
        }
    }

    public void AddFailure(ERepairFailureClass failure)
    {
        Attempts++;
        _consecutive = 0;
        _failures.Add(failure);
    }

    public RepairValidationEvidence Build(bool integritySuspect = false)
    {
        double? median = null;
        if (_latencies.Count > 0)
        {
            var ordered = _latencies.OrderBy(x => x).ToArray();
            var middle = ordered.Length / 2;
            median = ordered.Length % 2 == 1
                ? ordered[middle]
                : (ordered[middle - 1] + ordered[middle]) / 2.0;
        }

        double? throughput = null;
        if (_throughputs.Count > 0)
        {
            var ordered = _throughputs.OrderBy(x => x).ToArray();
            var middle = ordered.Length / 2;
            throughput = ordered.Length % 2 == 1
                ? ordered[middle]
                : (ordered[middle - 1] + ordered[middle]) / 2.0;
        }

        return new RepairValidationEvidence
        {
            Attempts = Attempts,
            Successes = Successes,
            ConsecutiveSuccesses = ConsecutiveSuccesses,
            MedianLatencyMs = median,
            LossRate = Attempts == 0 ? null : (Attempts - Successes) / (double)Attempts,
            ThroughputMbps = throughput,
            Failures = _failures.ToArray(),
            IntegritySuspect = integritySuspect,
        };
    }
}
