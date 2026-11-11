namespace Application.Reports.TopCourses;

public sealed record TopCoursesResponse(string From, string To, int Limit, IReadOnlyList<TopCourseRow> Courses);

public sealed record TopCourseRow(
    int Rank,
    Guid CourseId,
    string CourseTitle,
    string CategoryName,
    long LessonsCompleted,
    long QuizAttempts,
    decimal PassRate,
    decimal AvgQuizScore,
    long ActiveStudents);
