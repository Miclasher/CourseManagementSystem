using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests.Enrollments;

public sealed class EnrollmentTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Enroll_Should_ReturnConflict_WhenAlreadyEnrolled_AndKeepCounterAtOne()
    {
        // Arrange
        (Guid courseId, _) = await CreatePublishedCourseAsync();
        ActAs(StudentId);
        (await HttpClient.PostAsync($"api/courses/{courseId}/enrollments", null)).EnsureSuccessStatusCode();

        // Act
        HttpResponseMessage response = await HttpClient.PostAsync($"api/courses/{courseId}/enrollments", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await EnrolledCountAsync(courseId)).ShouldBe(1);
    }

    [Fact]
    public async Task Enroll_Should_ReturnConflict_WhenCourseIsFull()
    {
        // Arrange
        (Guid courseId, _) = await CreatePublishedCourseAsync(maxStudents: 1);
        ActAs(StudentId);
        (await HttpClient.PostAsync($"api/courses/{courseId}/enrollments", null)).EnsureSuccessStatusCode();
        ActAs(AdminId);

        // Act
        HttpResponseMessage response = await HttpClient.PostAsync($"api/courses/{courseId}/enrollments", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Enroll_Should_ReturnBadRequest_WhenCourseIsDraft()
    {
        // Arrange
        Guid courseId = await CreateCourseAsync();
        ActAs(StudentId);

        // Act
        HttpResponseMessage response = await HttpClient.PostAsync($"api/courses/{courseId}/enrollments", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompletingAllLessons_Should_CompleteEnrollmentAndIssueCertificate()
    {
        // Arrange
        (Guid courseId, List<Guid> lessonIds) = await CreatePublishedCourseAsync();
        ActAs(StudentId);
        HttpResponseMessage enroll = await HttpClient.PostAsync($"api/courses/{courseId}/enrollments", null);
        Guid enrollmentId = await enroll.Content.ReadFromJsonAsync<Guid>();

        // Act
        foreach (Guid lessonId in lessonIds)
        {
            HttpResponseMessage complete = await HttpClient.PostAsync(
                $"api/enrollments/{enrollmentId}/lessons/{lessonId}/complete",
                null);
            complete.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        // Assert
        string mine = await HttpClient.GetStringAsync("api/enrollments/mine");
        mine.ShouldContain("\"status\":\"completed\"");
        mine.ShouldContain("\"progressPercent\":100");
        mine.ShouldContain("CERT-");
    }

    [Fact]
    public async Task Drop_Should_FreeThePlace()
    {
        // Arrange
        (Guid courseId, _) = await CreatePublishedCourseAsync(maxStudents: 1);
        ActAs(StudentId);
        HttpResponseMessage enroll = await HttpClient.PostAsync($"api/courses/{courseId}/enrollments", null);
        Guid enrollmentId = await enroll.Content.ReadFromJsonAsync<Guid>();

        // Act
        HttpResponseMessage drop = await HttpClient.DeleteAsync($"api/enrollments/{enrollmentId}");

        // Assert
        drop.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await EnrolledCountAsync(courseId)).ShouldBe(0);
    }

    [Fact]
    public async Task Review_Should_UpdateCourseRating_ThroughTrigger()
    {
        // Arrange
        (Guid courseId, _) = await CreatePublishedCourseAsync();
        ActAs(StudentId);
        (await HttpClient.PostAsync($"api/courses/{courseId}/enrollments", null)).EnsureSuccessStatusCode();

        // Act
        HttpResponseMessage review = await HttpClient.PostAsJsonAsync(
            $"api/courses/{courseId}/reviews",
            new { rating = 4, comment = "good" });

        // Assert
        review.StatusCode.ShouldBe(HttpStatusCode.OK);
        string course = await HttpClient.GetStringAsync($"api/courses/{courseId}");
        course.ShouldContain("\"ratingAvg\":4");
    }

    private async Task<int> EnrolledCountAsync(Guid courseId) =>
        await ScalarAsync<int>($"SELECT enrolled_count FROM courses WHERE id = '{courseId}'");
}
