namespace Domain.Quizzes;

public interface IQuizRepository
{
    Task<Quiz?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(Quiz quiz);

    void AddAttempt(QuizAttempt attempt);
}
