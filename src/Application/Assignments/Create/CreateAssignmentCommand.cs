using Application.Abstractions.Messaging;

namespace Application.Assignments.Create;

public sealed record CreateAssignmentCommand(
    Guid LessonId,
    string Title,
    string Description,
    int MaxScore,
    DateTime? DueAt) : ICommand<Guid>;
