using Application.Abstractions.Messaging;

namespace Application.Courses.GetStats;

public sealed record GetCourseStatsQuery : IQuery<List<CourseStatsResponse>>;
