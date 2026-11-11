using Application.Reports;
using Application.Reports.CourseDashboard;
using Microsoft.Extensions.DependencyInjection;

namespace Application.UnitTests.Reports;

public sealed class CourseDashboardHandlerTests
{
    private static readonly Guid CourseId = Guid.Parse("a0000000-0000-0000-0000-00000000000a");

    private static CourseDashboardData Data(bool shuffled)
    {
        List<DashboardLessonData> lessons =
        [
            new() { LessonId = Guid.Parse("e0000000-0000-0000-0000-000000000001"), Title = "L1", Position = 1, DurationMinutes = 10 },
            new() { LessonId = Guid.Parse("e0000000-0000-0000-0000-000000000002"), Title = "L2", Position = 2, DurationMinutes = 15 }
        ];
        List<DashboardModuleData> modules =
        [
            new() { ModuleId = Guid.Parse("d0000000-0000-0000-0000-000000000001"), Title = "M1", Position = 1, Lessons = shuffled ? [lessons[1], lessons[0]] : lessons },
            new() { ModuleId = Guid.Parse("d0000000-0000-0000-0000-000000000002"), Title = "M2", Position = 2 }
        ];
        List<DashboardReviewData> reviews =
        [
            // Same second, different sub-second precision (PostgreSQL microseconds vs MongoDB milliseconds).
            new() { ReviewId = Guid.Parse("97000000-0000-0000-0000-000000000002"), AuthorName = "B", Rating = 4, CreatedAt = new DateTime(2025, 3, 2, 10, 0, 0, DateTimeKind.Utc).AddTicks(shuffled ? 1230000 : 1234560) },
            new() { ReviewId = Guid.Parse("97000000-0000-0000-0000-000000000001"), AuthorName = "A", Rating = 5, Comment = "great", CreatedAt = new DateTime(2025, 3, 2, 10, 0, 0, DateTimeKind.Utc) }
        ];

        return new CourseDashboardData
        {
            CourseId = CourseId,
            Title = "Course A",
            Slug = "a",
            Level = "beginner",
            Status = "published",
            PriceCents = 1999,
            CategoryName = "Programming",
            LeadInstructorName = "Ivan Instructor",
            PublishedAt = new DateTime(2025, 1, 2, 10, 0, 0, DateTimeKind.Utc),
            ActiveEnrollments = 2,
            CompletedEnrollments = 1,
            DroppedEnrollments = 4,
            RatingCounts = [0, 1, 0, 1, 1],
            Modules = shuffled ? [modules[1], modules[0]] : modules,
            RecentReviews = shuffled ? [reviews[1], reviews[0]] : reviews
        };
    }

    [Fact]
    public void Map_Should_ProduceIdenticalJson_WhenCollectionsAreInDifferentOrder()
    {
        // Act
        CourseDashboardResponse ordered = GetCourseDashboardQueryHandler.Map(Data(shuffled: false));
        CourseDashboardResponse shuffled = GetCourseDashboardQueryHandler.Map(Data(shuffled: true));

        // Assert
        ReportTestHelpers.Json(shuffled).ShouldBe(ReportTestHelpers.Json(ordered));
        ordered.Modules.Select(m => m.Title).ShouldBe(["M1", "M2"]);
        ordered.Modules[0].Lessons.Select(l => l.Title).ShouldBe(["L1", "L2"]);
        ordered.RecentReviews.Select(r => r.AuthorName).ShouldBe(["A", "B"]);
        ordered.RecentReviews[0].CreatedAt.ShouldBe("2025-03-02T10:00:00Z");
    }

    [Fact]
    public void Map_Should_ComputeDerivedNumbers()
    {
        // Act
        CourseDashboardResponse response = GetCourseDashboardQueryHandler.Map(Data(shuffled: false));

        // Assert
        response.Price.ShouldBe(19.99m);
        response.Ratings.Count.ShouldBe(3);
        response.Ratings.Average.ShouldBe(3.67m);
        response.Enrollments.Total.ShouldBe(7);
        response.Enrollments.CompletionRate.ShouldBe(33.33m);
        response.TotalLessons.ShouldBe(2);
        response.TotalMinutes.ShouldBe(25);
        response.Modules[0].TotalMinutes.ShouldBe(25);
        response.Modules[1].TotalMinutes.ShouldBe(0);
        response.PublishedAt.ShouldBe("2025-01-02T10:00:00Z");
    }

    [Fact]
    public void Map_Should_PadHistogramToFiveLevels_WhenRatingCountsAreShort()
    {
        // Arrange
        CourseDashboardData data = Data(shuffled: false);
        data.RatingCounts = [];

        // Act
        CourseDashboardResponse response = GetCourseDashboardQueryHandler.Map(data);

        // Assert
        response.Ratings.Histogram.ShouldBe([0L, 0L, 0L, 0L, 0L]);
        response.Ratings.Average.ShouldBe(0m);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenSourceReturnsNull()
    {
        // Arrange
        ICourseDashboardSource source = Substitute.For<ICourseDashboardSource>();
        source.GetAsync(CourseId, Arg.Any<CancellationToken>()).Returns((CourseDashboardData?)null);
        IKeyedServiceProvider provider = ReportTestHelpers.ProviderWith(ReportSource.Mongo, source);

        // Act
        Result<CourseDashboardResponse> result = await new GetCourseDashboardQueryHandler(provider)
            .Handle(new GetCourseDashboardQuery(ReportSource.Mongo, CourseId), CancellationToken.None);

        // Assert
        result.Error.ShouldBe(ReportErrors.CourseNotFound(CourseId));
    }

    [Fact]
    public async Task Handle_Should_ReturnUnsupportedSource_WhenNoSourceIsRegistered()
    {
        // Arrange
        var handler = new GetCourseDashboardQueryHandler(ReportTestHelpers.EmptyProvider());

        // Act
        Result<CourseDashboardResponse> result = await handler.Handle(new GetCourseDashboardQuery(ReportSource.ClickHouse, CourseId), CancellationToken.None);

        // Assert
        result.Error.ShouldBe(ReportErrors.UnsupportedSource);
    }
}
