using Application.Abstractions.Messaging;

namespace Application.Reports.CourseDashboard;

public sealed record GetCourseDashboardQuery(ReportSource Source, Guid CourseId) : IQuery<CourseDashboardResponse>;
