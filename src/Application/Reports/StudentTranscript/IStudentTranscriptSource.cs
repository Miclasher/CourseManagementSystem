namespace Application.Reports.StudentTranscript;

public interface IStudentTranscriptSource
{
    /// <summary>Null when the user does not exist, is soft-deleted or is not a student.</summary>
    Task<StudentTranscriptData?> GetAsync(Guid studentId, CancellationToken cancellationToken);
}
