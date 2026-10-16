using Application.Abstractions.Messaging;

namespace Application.Courses.Search;

public sealed record SearchCoursesQuery(string? Text, Guid? CategoryId, int Page, int PageSize)
    : IQuery<List<CourseSearchResponse>>;
