using Application.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace Application.Reports.StudentTranscript;

internal sealed class GetStudentTranscriptQueryHandler(IServiceProvider serviceProvider)
    : IQueryHandler<GetStudentTranscriptQuery, StudentTranscriptResponse>
{
    public async Task<Result<StudentTranscriptResponse>> Handle(GetStudentTranscriptQuery query, CancellationToken cancellationToken)
    {
        IStudentTranscriptSource? source = serviceProvider.GetKeyedService<IStudentTranscriptSource>(query.Source);
        if (source is null)
        {
            return Result.Failure<StudentTranscriptResponse>(ReportErrors.UnsupportedSource);
        }

        StudentTranscriptData? data = await source.GetAsync(query.StudentId, cancellationToken);
        if (data is null)
        {
            return Result.Failure<StudentTranscriptResponse>(ReportErrors.StudentNotFound(query.StudentId));
        }

        return Map(data);
    }

    internal static StudentTranscriptResponse Map(StudentTranscriptData data)
    {
        var enrollments = data.Enrollments
            .OrderByDescending(e => ReportMath.Timestamp(e.EnrolledAt), StringComparer.Ordinal)
            .ThenBy(e => ReportMath.Key(e.EnrollmentId), StringComparer.Ordinal)
            .Select(e => new TranscriptEnrollment(
                e.EnrollmentId,
                e.CourseId,
                e.CourseTitle,
                e.Status,
                ReportMath.Timestamp(e.EnrolledAt),
                ReportMath.Timestamp(e.CompletedAt),
                e.CompletedLessons,
                e.TotalLessons,
                ReportMath.Percent(e.CompletedLessons, e.TotalLessons),
                e.QuizAttempts,
                e.BestQuizScoreHundredths is { } best ? ReportMath.FromHundredths(best) : null,
                e.CertificateCode))
            .ToList();

        decimal averageProgress = enrollments.Count == 0
            ? 0m
            : decimal.Round(enrollments.Sum(e => e.ProgressPercent) / enrollments.Count, 2, MidpointRounding.AwayFromZero);

        return new StudentTranscriptResponse(
            data.StudentId,
            $"{data.FirstName} {data.LastName}",
            data.Email,
            enrollments.Count,
            enrollments.Count(e => e.Status == "completed"),
            enrollments.Count(e => e.CertificateCode is not null),
            averageProgress,
            enrollments);
    }
}
