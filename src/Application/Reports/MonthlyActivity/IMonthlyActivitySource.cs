namespace Application.Reports.MonthlyActivity;

public interface IMonthlyActivitySource
{
    /// <summary>All (month, category) buckets with at least one event in [year-01-01, year+1-01-01) UTC.</summary>
    Task<IReadOnlyList<MonthlyActivityRaw>> GetAsync(int year, CancellationToken cancellationToken);
}
