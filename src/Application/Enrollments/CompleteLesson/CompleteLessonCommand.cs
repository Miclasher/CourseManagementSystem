using Application.Abstractions.Messaging;

namespace Application.Enrollments.CompleteLesson;

public sealed record CompleteLessonCommand(Guid EnrollmentId, Guid LessonId) : ICommand;
