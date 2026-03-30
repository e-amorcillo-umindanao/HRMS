namespace HRMS.Helpers;

public static class EngagementCalculator
{
    public static (double Score, string Label, string Color) ComputeScore(
        int eventsAttended,
        int totalEvents,
        int interactionsInSemester,
        DateTime? lastInteractionDate)
    {
        var attendanceScore = totalEvents == 0
            ? 0
            : ((double)eventsAttended / totalEvents) * 50;

        var cappedInteractions = Math.Min(interactionsInSemester, 10);
        var interactionScore = (cappedInteractions / 10d) * 30;

        double recencyScore;
        if (!lastInteractionDate.HasValue)
        {
            recencyScore = 0;
        }
        else
        {
            var daysSinceLast = (DateTime.UtcNow.Date - lastInteractionDate.Value.Date).TotalDays;
            recencyScore = daysSinceLast <= 30
                ? 20
                : Math.Max(0, 20 - (((daysSinceLast - 30) / 30d) * 20));
        }

        var score = Math.Round(attendanceScore + interactionScore + recencyScore, 2);

        return score switch
        {
            >= 75 => (score, "High Engagement", "#22C55E"),
            >= 40 => (score, "Medium Engagement", "#F59E0B"),
            _ => (score, "Low Engagement", "#EF4444")
        };
    }
}
