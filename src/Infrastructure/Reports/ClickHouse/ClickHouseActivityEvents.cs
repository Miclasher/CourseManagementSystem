namespace Infrastructure.Reports.ClickHouse;

/// <summary>Aggregates over <c>activity_events</c> (database from the connection string) matching the PostgreSQL event definition exactly.</summary>
internal static class ClickHouseActivityEvents
{
    public const string Aggregates = """
        toInt64(countIf(event_type = 'lesson_completed')) AS lessons_completed,
        toInt64(countIf(event_type = 'quiz_attempt')) AS quiz_attempts,
        toInt64(countIf(event_type = 'quiz_attempt' AND passed = 1)) AS quiz_passed,
        toInt64(sumIf(score_hundredths, event_type = 'quiz_attempt')) AS score_sum_hundredths,
        toInt64(uniqExact(user_id)) AS active_students
        """;

    public const string PeriodFilter = "event_at >= {from:DateTime('UTC')} AND event_at < {to:DateTime('UTC')}";
}
