using System.Diagnostics.Metrics;

namespace Blackwing.Api.Observability;

/// <summary>
/// Meter for the ingestion pipeline. Terminal job outcomes are counted here so an
/// external collector (OpenTelemetry, Prometheus) can scrape ingestion throughput
/// and failure rate without Blackwing carrying an exporter of its own. Live queue
/// depth is deliberately not a counter: it is reported on demand by the ops
/// endpoint, which reads the authoritative value from the durable job table.
/// Only aggregate numbers are recorded — never file names, user identities, or
/// image bytes.
/// </summary>
public sealed class IngestionMetrics
{
    public const string MeterName = "Blackwing.Ingestion";

    private readonly Counter<long> completed;
    private readonly Counter<long> failed;
    private readonly Counter<long> duplicate;

    public IngestionMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        completed = meter.CreateCounter<long>("blackwing.ingestion.completed", unit: "{job}", description: "Upload jobs that produced an image.");
        failed = meter.CreateCounter<long>("blackwing.ingestion.failed", unit: "{job}", description: "Upload jobs that ended in a recorded failure.");
        duplicate = meter.CreateCounter<long>("blackwing.ingestion.duplicate", unit: "{job}", description: "Upload jobs skipped as per-user duplicates.");
    }

    public void RecordCompleted() => completed.Add(1);

    /// <summary>Records a failure, tagged by its stable failure code (not the diagnostic message).</summary>
    public void RecordFailed(string code) => failed.Add(1, new KeyValuePair<string, object?>("failure.code", code));

    public void RecordDuplicate() => duplicate.Add(1);
}
