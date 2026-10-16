using Application.Abstractions.Messaging;

namespace Application.Curriculum.AddLesson;

public sealed record AddLessonCommand(Guid ModuleId, string Title, string Content, int DurationMinutes)
    : ICommand<Guid>;
