using Application.Abstractions.Messaging;

namespace Application.Courses.Delete;

public sealed record DeleteCourseCommand(Guid CourseId) : ICommand;
