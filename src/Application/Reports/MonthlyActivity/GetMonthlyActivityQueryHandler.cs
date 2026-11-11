using Application.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace Application.Reports.MonthlyActivity;

internal sealed class GetMonthlyActivityQueryHandler(IServiceProvider serviceProvider)
    : IQueryHandler<GetMonthlyActivityQuery, MonthlyActivityResponse>
{
    public async Task<Result<MonthlyActivityResponse>> Handle(GetMonthlyActivityQuery query, CancellationToken cancellationToken)
    {
        if (query.Year is < 2000 or > 2100)
        {
            return Result.Failure<MonthlyActivityResponse>(ReportErrors.InvalidPeriod);
        }

        IMonthlyActivitySource? source = serviceProvider.GetKeyedService<IMonthlyActivitySource>(query.Source);
        if (source is null)
        {
            return Result.Failure<MonthlyActivityResponse>(ReportErrors.UnsupportedSource);
        }

        IReadOnlyList<MonthlyActivityRaw> raw = await source.GetAsync(query.Year, cancellationToken);

        var rows = raw
            .OrderBy(r => r.YearMonth)
            .ThenBy(r => r.CategoryName, StringComparer.Ordinal)
            .ThenBy(r => ReportMath.Key(r.CategoryId), StringComparer.Ordinal)
            .Select(r => new MonthlyActivityRow(
                ReportMath.Month(r.YearMonth),
                r.CategoryId,
                r.CategoryName,
                r.LessonsCompleted,
                r.QuizAttempts,
                r.QuizPassed,
                ReportMath.Percent(r.QuizPassed, r.QuizAttempts),
                ReportMath.AverageHundredths(r.ScoreSumHundredths, r.QuizAttempts),
                r.ActiveStudents))
            .ToList();

        return new MonthlyActivityResponse(query.Year, rows);
    }
}
