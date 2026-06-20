namespace Segaris.Api.Modules.Projects.Domain;

/// <summary>The low/medium/high band a project risk falls into based on its score.</summary>
internal enum RiskBand
{
    Low,
    Medium,
    High,
}

/// <summary>
/// Frozen risk scoring and banding. The score is the product of the three 1-5 factors
/// (range 1-125), is always computed by the backend, and is never accepted from the
/// client. The band thresholds are high at <c>&gt;= 100</c>, medium at <c>&gt;= 60</c>,
/// and low otherwise.
/// </summary>
internal static class ProjectRiskScoring
{
    public const int HighThreshold = 100;
    public const int MediumThreshold = 60;

    public static int Score(int probability, int impact, int mitigation) =>
        probability * impact * mitigation;

    public static RiskBand BandFor(int score) =>
        score >= HighThreshold ? RiskBand.High
        : score >= MediumThreshold ? RiskBand.Medium
        : RiskBand.Low;
}
