using SharedKernel;

namespace Domain.Quizzes;

public sealed record QuizAttemptedDomainEvent(Guid AttemptId, Guid QuizId) : IDomainEvent;
