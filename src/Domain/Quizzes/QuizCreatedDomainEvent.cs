using SharedKernel;

namespace Domain.Quizzes;

public sealed record QuizCreatedDomainEvent(Guid QuizId) : IDomainEvent;
