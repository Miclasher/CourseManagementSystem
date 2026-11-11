using Application.Abstractions.Messaging;

namespace Application.Reports.TopCourses;

public sealed record GetTopCoursesQuery(ReportSource Source, DateOnly From, DateOnly To, int Limit) : IQuery<TopCoursesResponse>;
