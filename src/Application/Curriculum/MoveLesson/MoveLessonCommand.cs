using Application.Abstractions.Messaging;

namespace Application.Curriculum.MoveLesson;

public sealed record MoveLessonCommand(Guid LessonId, int Position) : ICommand;
