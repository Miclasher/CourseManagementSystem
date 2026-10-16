using Application.Abstractions.Messaging;

namespace Application.Courses.GetDeleted;

public sealed record GetDeletedCoursesQuery : IQuery<List<DeletedCourseResponse>>;
