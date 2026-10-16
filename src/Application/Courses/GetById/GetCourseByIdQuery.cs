using Application.Abstractions.Messaging;

namespace Application.Courses.GetById;

public sealed record GetCourseByIdQuery(Guid CourseId) : IQuery<CourseResponse>;
