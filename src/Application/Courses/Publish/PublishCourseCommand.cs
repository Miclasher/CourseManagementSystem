using Application.Abstractions.Messaging;

namespace Application.Courses.Publish;

public sealed record PublishCourseCommand(Guid CourseId) : ICommand;
