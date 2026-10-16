namespace Domain.Courses;

/// <summary>
/// Reads load from views. Writes only queue a stored-procedure call on the unit of work; they run when it commits.
/// </summary>
public interface ICourseRepository
{
    Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Course?> GetDeletedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> IsInstructorAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default);

    void Add(Course course);

    void Update(Course course);

    void Publish(Course course);

    void Delete(Course course);

    void Restore(Course course);
}
