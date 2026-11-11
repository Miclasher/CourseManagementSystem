using Application.Reports;
using Application.Reports.TopCourses;
using Microsoft.Extensions.DependencyInjection;

namespace Application.UnitTests.Reports;

public sealed class TopCoursesHandlerTests
{
    private static readonly DateOnly From = new(2025, 1, 1);
    private static readonly DateOnly To = new(2025, 12, 31);

    private static List<TopCourseRaw> Rows() =>
    [
        new() { CourseId = Guid.Parse("b0000000-0000-0000-0000-000000000000"), CourseTitle = "B", CategoryName = "X", LessonsCompleted = 10 },
        new() { CourseId = Guid.Parse("c0000000-0000-0000-0000-000000000000"), CourseTitle = "C", CategoryName = "X", LessonsCompleted = 20, QuizAttempts = 3, QuizPassed = 1, ScoreSumHundredths = 20000 },
        new() { CourseId = Guid.Parse("a0000000-0000-0000-0000-000000000000"), CourseTitle = "A", CategoryName = "Y", LessonsCompleted = 10 }
    ];

    private static (GetTopCoursesQueryHandler Handler, ITopCoursesSource Source) Create(IReadOnlyList<TopCourseRaw> raw)
    {
        ITopCoursesSource source = Substitute.For<ITopCoursesSource>();
        source.GetAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(raw);
        IKeyedServiceProvider provider = ReportTestHelpers.ProviderWith(ReportSource.Postgres, source);
        return (new GetTopCoursesQueryHandler(provider), source);
    }

    [Fact]
    public async Task Handle_Should_RankDeterministically_RegardlessOfRawOrder()
    {
        // Arrange
        (GetTopCoursesQueryHandler first, _) = Create(Rows());
        (GetTopCoursesQueryHandler second, _) = Create(Enumerable.Reverse(Rows()).ToList());
        var query = new GetTopCoursesQuery(ReportSource.Postgres, From, To, 20);

        // Act
        Result<TopCoursesResponse> a = await first.Handle(query, CancellationToken.None);
        Result<TopCoursesResponse> b = await second.Handle(query, CancellationToken.None);

        // Assert
        ReportTestHelpers.Json(a.Value).ShouldBe(ReportTestHelpers.Json(b.Value));
        a.Value.Courses.Select(c => c.CourseTitle).ShouldBe(["C", "A", "B"]);
        a.Value.Courses.Select(c => c.Rank).ShouldBe([1, 2, 3]);
        a.Value.Courses[0].PassRate.ShouldBe(33.33m);
        a.Value.Courses[0].AvgQuizScore.ShouldBe(66.67m);
        a.Value.From.ShouldBe("2025-01-01");
        a.Value.To.ShouldBe("2025-12-31");
    }

    [Fact]
    public async Task Handle_Should_PassUtcHalfOpenPeriodToSource()
    {
        // Arrange
        (GetTopCoursesQueryHandler handler, ITopCoursesSource source) = Create([]);

        // Act
        await handler.Handle(new GetTopCoursesQuery(ReportSource.Postgres, From, To, 5), CancellationToken.None);

        // Assert
        await source.Received(1).GetAsync(
            Arg.Is<DateTime>(d => d == new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) && d.Kind == DateTimeKind.Utc),
            Arg.Is<DateTime>(d => d == new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) && d.Kind == DateTimeKind.Utc),
            5,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Handle_Should_ReturnInvalidLimit_WhenLimitIsOutOfRange(int limit)
    {
        // Arrange
        (GetTopCoursesQueryHandler handler, _) = Create([]);

        // Act
        Result<TopCoursesResponse> result = await handler.Handle(new GetTopCoursesQuery(ReportSource.Postgres, From, To, limit), CancellationToken.None);

        // Assert
        result.Error.ShouldBe(ReportErrors.InvalidLimit);
    }

    [Fact]
    public async Task Handle_Should_ReturnUnsupportedSource_WhenNoSourceIsRegistered()
    {
        // Arrange
        var handler = new GetTopCoursesQueryHandler(ReportTestHelpers.EmptyProvider());

        // Act
        Result<TopCoursesResponse> result = await handler.Handle(new GetTopCoursesQuery(ReportSource.Mongo, From, To, 20), CancellationToken.None);

        // Assert
        result.Error.ShouldBe(ReportErrors.UnsupportedSource);
    }
}
