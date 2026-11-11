using Application.Reports;
using Application.Reports.StudentTranscript;
using Microsoft.Extensions.DependencyInjection;

namespace Application.UnitTests.Reports;

public sealed class StudentTranscriptHandlerTests
{
    private static readonly Guid StudentId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    private static StudentTranscriptData Data(bool shuffled)
    {
        List<TranscriptEnrollmentData> enrollments =
        [
            new()
            {
                EnrollmentId = Guid.Parse("f0000000-0000-0000-0000-000000000002"),
                CourseId = Guid.Parse("a0000000-0000-0000-0000-000000000001"),
                CourseTitle = "Newest",
                Status = "completed",
                EnrolledAt = new DateTime(2025, 2, 1, 8, 0, 0, DateTimeKind.Utc),
                CompletedAt = new DateTime(2025, 3, 1, 8, 0, 0, DateTimeKind.Utc),
                TotalLessons = 3,
                CompletedLessons = 3,
                QuizAttempts = 2,
                BestQuizScoreHundredths = 8725,
                CertificateCode = "CERT-1"
            },
            new()
            {
                EnrollmentId = Guid.Parse("f0000000-0000-0000-0000-000000000001"),
                CourseId = Guid.Parse("a0000000-0000-0000-0000-000000000002"),
                CourseTitle = "Oldest",
                Status = "active",
                EnrolledAt = new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc),
                TotalLessons = 3,
                CompletedLessons = 1
            },
            new()
            {
                EnrollmentId = Guid.Parse("f0000000-0000-0000-0000-000000000003"),
                CourseId = Guid.Parse("a0000000-0000-0000-0000-000000000003"),
                CourseTitle = "Empty",
                Status = "dropped",
                EnrolledAt = new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc)
            }
        ];

        return new StudentTranscriptData
        {
            StudentId = StudentId,
            FirstName = "Stella",
            LastName = "Student",
            Email = "student@cms.local",
            Enrollments = shuffled ? [enrollments[2], enrollments[0], enrollments[1]] : enrollments
        };
    }

    [Fact]
    public void Map_Should_ProduceIdenticalJson_WhenEnrollmentsAreInDifferentOrder()
    {
        // Act
        StudentTranscriptResponse ordered = GetStudentTranscriptQueryHandler.Map(Data(shuffled: false));
        StudentTranscriptResponse shuffled = GetStudentTranscriptQueryHandler.Map(Data(shuffled: true));

        // Assert
        ReportTestHelpers.Json(shuffled).ShouldBe(ReportTestHelpers.Json(ordered));
        ordered.Enrollments.Select(e => e.CourseTitle).ShouldBe(["Newest", "Oldest", "Empty"]);
    }

    [Fact]
    public void Map_Should_ComputeSummaryAndProgress()
    {
        // Act
        StudentTranscriptResponse response = GetStudentTranscriptQueryHandler.Map(Data(shuffled: false));

        // Assert
        response.FullName.ShouldBe("Stella Student");
        response.Courses.ShouldBe(3);
        response.CompletedCourses.ShouldBe(1);
        response.Certificates.ShouldBe(1);
        response.Enrollments[0].BestQuizScore.ShouldBe(87.25m);
        response.Enrollments[1].ProgressPercent.ShouldBe(33.33m);
        response.Enrollments[1].BestQuizScore.ShouldBeNull();
        response.Enrollments[2].ProgressPercent.ShouldBe(0m);
        response.AverageProgress.ShouldBe(44.44m);
        response.Enrollments[0].CompletedAt.ShouldBe("2025-03-01T08:00:00Z");
        response.Enrollments[1].CompletedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenSourceReturnsNull()
    {
        // Arrange
        IStudentTranscriptSource source = Substitute.For<IStudentTranscriptSource>();
        source.GetAsync(StudentId, Arg.Any<CancellationToken>()).Returns((StudentTranscriptData?)null);
        IKeyedServiceProvider provider = ReportTestHelpers.ProviderWith(ReportSource.Postgres, source);

        // Act
        Result<StudentTranscriptResponse> result = await new GetStudentTranscriptQueryHandler(provider)
            .Handle(new GetStudentTranscriptQuery(ReportSource.Postgres, StudentId), CancellationToken.None);

        // Assert
        result.Error.ShouldBe(ReportErrors.StudentNotFound(StudentId));
    }

    [Fact]
    public async Task Handle_Should_ReturnUnsupportedSource_WhenNoSourceIsRegistered()
    {
        // Arrange
        var handler = new GetStudentTranscriptQueryHandler(ReportTestHelpers.EmptyProvider());

        // Act
        Result<StudentTranscriptResponse> result = await handler.Handle(new GetStudentTranscriptQuery(ReportSource.ClickHouse, StudentId), CancellationToken.None);

        // Assert
        result.Error.ShouldBe(ReportErrors.UnsupportedSource);
    }
}
