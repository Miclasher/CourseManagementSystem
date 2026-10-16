using SharedKernel;

namespace Domain.Quizzes;

public sealed class QuizAttempt : Entity
{
    public Guid Id { get; set; }
    public Guid QuizId { get; set; }
    public Guid EnrollmentId { get; set; }
    public List<int> Answers { get; set; } = [];

    public static QuizAttempt Create(Guid quizId, Guid enrollmentId, List<int> answers)
    {
        var attempt = new QuizAttempt
        {
            Id = Guid.CreateVersion7(),
            QuizId = quizId,
            EnrollmentId = enrollmentId,
            Answers = answers
        };

        attempt.Raise(new QuizAttemptedDomainEvent(attempt.Id, quizId));

        return attempt;
    }
}
