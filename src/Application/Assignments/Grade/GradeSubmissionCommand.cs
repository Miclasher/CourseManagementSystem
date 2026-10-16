using Application.Abstractions.Messaging;

namespace Application.Assignments.Grade;

public sealed record GradeSubmissionCommand(Guid SubmissionId, int Score, string? Feedback) : ICommand;
