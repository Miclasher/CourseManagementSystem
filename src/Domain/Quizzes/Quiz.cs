using SharedKernel;

namespace Domain.Quizzes;

public sealed class Quiz : Entity
{
    public Guid Id { get; set; }
    public Guid LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int PassPercent { get; set; }
    public int MaxAttempts { get; set; }
    public List<QuizQuestion> Questions { get; set; } = [];

    public static Quiz Create(Guid lessonId, string title, int passPercent, int maxAttempts, List<QuizQuestion> questions)
    {
        var quiz = new Quiz
        {
            Id = Guid.CreateVersion7(),
            LessonId = lessonId,
            Title = title,
            PassPercent = passPercent,
            MaxAttempts = maxAttempts,
            Questions = questions
        };

        quiz.Raise(new QuizCreatedDomainEvent(quiz.Id));

        return quiz;
    }
}
