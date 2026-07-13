namespace Segaris.Api.Modules.Wellness.Domain;

internal static class WellnessScore
{
    public static int? Compute(int completed, int total)
    {
        if (total == 0)
        {
            return null;
        }

        var percentage = completed * 100m / total;
        return (int)Math.Round(percentage, MidpointRounding.AwayFromZero);
    }
}
