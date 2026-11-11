using SharedKernel;

namespace Application.Reports;

public static class ReportErrors
{
    public static readonly Error UnsupportedSource = Error.Problem(
        "Reports.UnsupportedSource", "This report is not available from the requested source.");

    public static readonly Error InvalidPeriod = Error.Problem(
        "Reports.InvalidPeriod", "The requested period is invalid.");

    public static readonly Error InvalidLimit = Error.Problem(
        "Reports.InvalidLimit", "Limit must be between 1 and 100.");

    public static Error CourseNotFound(Guid courseId) => Error.NotFound(
        "Reports.CourseNotFound", $"The course with the Id = '{courseId}' was not found.");

    public static Error StudentNotFound(Guid studentId) => Error.NotFound(
        "Reports.StudentNotFound", $"The student with the Id = '{studentId}' was not found.");
}
