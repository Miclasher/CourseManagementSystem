namespace Domain.Enrollments;

public interface IEnrollmentRepository
{
    Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Enrollment?> GetCurrentAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default);

    void Add(Enrollment enrollment);

    void Drop(Enrollment enrollment);

    void CompleteLesson(Enrollment enrollment, Guid lessonId);
}
