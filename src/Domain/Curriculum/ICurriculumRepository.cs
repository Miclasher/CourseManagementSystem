namespace Domain.Curriculum;

public interface ICurriculumRepository
{
    Task<Module?> GetModuleByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Lesson?> GetLessonByIdAsync(Guid id, CancellationToken cancellationToken = default);

    void AddModule(Module module);

    void AddLesson(Lesson lesson);

    void MoveLesson(Lesson lesson, int newPosition);
}
