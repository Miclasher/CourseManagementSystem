using Application.Abstractions.Messaging;

namespace Application.Quizzes.SubmitAttempt;

/// <summary>Answers are the chosen option indexes, in question order.</summary>
public sealed record SubmitQuizAttemptCommand(Guid QuizId, Guid EnrollmentId, List<int> Answers) : ICommand<Guid>;
