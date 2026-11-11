using Application.Reports;
using Application.Reports.MonthlyActivity;
using Microsoft.Extensions.DependencyInjection;

namespace Application.UnitTests.Reports;

public sealed class MonthlyActivityHandlerTests
{
    private static readonly Guid CategoryA = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid CategoryB = Guid.Parse("10000000-0000-0000-0000-000000000002");

    private static List<MonthlyActivityRaw> Rows() =>
    [
        new() { YearMonth = 202502, CategoryId = CategoryB, CategoryName = "Databases", LessonsCompleted = 1, QuizAttempts = 3, QuizPassed = 2, ScoreSumHundredths = 8725 + 3333 + 6050, ActiveStudents = 2 },
        new() { YearMonth = 202501, CategoryId = CategoryA, CategoryName = "Programming", LessonsCompleted = 2, ActiveStudents = 1 },
        new() { YearMonth = 202502, CategoryId = CategoryA, CategoryName = "Programming", LessonsCompleted = 5, QuizAttempts = 1, QuizPassed = 1, ScoreSumHundredths = 7000, ActiveStudents = 1 }
    ];

    private static async Task<Result<MonthlyActivityResponse>> Run(IReadOnlyList<MonthlyActivityRaw> raw, ReportSource source = ReportSource.ClickHouse)
    {
        IMonthlyActivitySource activity = Substitute.For<IMonthlyActivitySource>();
        activity.GetAsync(2025, Arg.Any<CancellationToken>()).Returns(raw);
        IKeyedServiceProvider provider = ReportTestHelpers.ProviderWith(source, activity);

        return await new GetMonthlyActivityQueryHandler(provider).Handle(new GetMonthlyActivityQuery(source, 2025), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_Should_ProduceIdenticalJson_WhenRawRowsAreInDifferentOrder()
    {
        // Arrange
        List<MonthlyActivityRaw> rows = Rows();
        var reversed = Enumerable.Reverse(Rows()).ToList();

        // Act
        Result<MonthlyActivityResponse> first = await Run(rows);
        Result<MonthlyActivityResponse> second = await Run(reversed, ReportSource.Postgres);

        // Assert
        ReportTestHelpers.Json(first.Value).ShouldBe(ReportTestHelpers.Json(second.Value));
        first.Value.Rows.Select(r => r.Month).ShouldBe(["2025-01", "2025-02", "2025-02"]);
        first.Value.Rows.Select(r => r.CategoryName).ShouldBe(["Programming", "Databases", "Programming"]);
    }

    [Fact]
    public async Task Handle_Should_RoundRatesAndAverages()
    {
        // Act
        Result<MonthlyActivityResponse> result = await Run(Rows());

        // Assert
        MonthlyActivityRow row = result.Value.Rows.Single(r => r.CategoryId == CategoryB);
        row.PassRate.ShouldBe(66.67m);
        row.AvgQuizScore.ShouldBe(60.36m);

        MonthlyActivityRow noQuizzes = result.Value.Rows.Single(r => r.Month == "2025-01");
        noQuizzes.PassRate.ShouldBe(0m);
        noQuizzes.AvgQuizScore.ShouldBe(0m);
    }

    [Fact]
    public async Task Handle_Should_ReturnUnsupportedSource_WhenNoSourceIsRegistered()
    {
        // Arrange
        var handler = new GetMonthlyActivityQueryHandler(ReportTestHelpers.EmptyProvider());

        // Act
        Result<MonthlyActivityResponse> result = await handler.Handle(new GetMonthlyActivityQuery(ReportSource.Mongo, 2025), CancellationToken.None);

        // Assert
        result.Error.ShouldBe(ReportErrors.UnsupportedSource);
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidPeriod_WhenYearIsOutOfRange()
    {
        // Arrange
        var handler = new GetMonthlyActivityQueryHandler(ReportTestHelpers.EmptyProvider());

        // Act
        Result<MonthlyActivityResponse> result = await handler.Handle(new GetMonthlyActivityQuery(ReportSource.Postgres, 1999), CancellationToken.None);

        // Assert
        result.Error.ShouldBe(ReportErrors.InvalidPeriod);
    }
}
