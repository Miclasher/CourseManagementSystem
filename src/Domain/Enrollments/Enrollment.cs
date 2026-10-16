using SharedKernel;

namespace Domain.Enrollments;

public sealed class Enrollment : Entity
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid UserId { get; set; }
    public EnrollmentStatus Status { get; set; }
    public DateTime EnrolledAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public static Enrollment Create(Guid courseId, Guid userId)
    {
        var enrollment = new Enrollment
        {
            Id = Guid.CreateVersion7(),
            CourseId = courseId,
            UserId = userId,
            Status = EnrollmentStatus.Active
        };

        enrollment.Raise(new StudentEnrolledDomainEvent(enrollment.Id, courseId, userId));

        return enrollment;
    }

    public void Drop()
    {
        Status = EnrollmentStatus.Dropped;

        Raise(new EnrollmentDroppedDomainEvent(Id));
    }

    public void CompleteLesson(Guid lessonId) => Raise(new LessonCompletedDomainEvent(Id, lessonId));
}
