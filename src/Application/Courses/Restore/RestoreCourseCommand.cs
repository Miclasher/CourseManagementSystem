using Application.Abstractions.Messaging;

namespace Application.Courses.Restore;

public sealed record RestoreCourseCommand(Guid CourseId) : ICommand;
