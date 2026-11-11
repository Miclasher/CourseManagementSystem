namespace Seeder.Seeding;

internal enum EnrollmentStatus : byte
{
    Active = 0,
    Completed = 1,
    Dropped = 2,
}

/// <summary>Per-course facts later generators need (structure, owner, publication window).</summary>
internal sealed class CourseInfo
{
    public Guid Id { get; init; }
    public long CreatedAt { get; init; }

    /// <summary>0 for drafts (never published, never enrolled).</summary>
    public long PublishedAt { get; init; }

    public bool Enrollable => PublishedAt > 0;
    public int LeadInstructor { get; init; }

    /// <summary>Lessons of the course are the contiguous range [LessonStart, LessonStart + LessonCount) in course order.</summary>
    public int LessonStart { get; set; }

    public int LessonCount { get; set; }
    public List<QuizInfo> Quizzes { get; } = [];
    public List<AssignmentInfo> Assignments { get; } = [];
}

internal sealed record QuizInfo(Guid Id, int PassPercent, int MaxAttempts);

internal sealed record AssignmentInfo(Guid Id, int MaxScore);

/// <summary>Compact enrollment record (1M of them stay in memory to drive the streamed child tables).</summary>
internal struct EnrollmentRec
{
    public Guid Id;
    public int User;
    public int Course;
    public EnrollmentStatus Status;
    public long EnrolledAt;

    /// <summary>0 unless completed.</summary>
    public long CompletedAt;
}

internal sealed class SeedModel
{
    public List<Guid> UserIds { get; } = [];
    public List<long> UserCreatedAt { get; } = [];
    public List<int> Instructors { get; } = [];
    public List<int> Students { get; } = [];
    public List<Guid> CategoryIds { get; } = [];
    public List<CourseInfo> Courses { get; } = [];
    public List<Guid> LessonIds { get; } = [];
    public List<bool> LessonDeleted { get; } = [];
    public EnrollmentRec[] Enrollments { get; set; } = [];
}
