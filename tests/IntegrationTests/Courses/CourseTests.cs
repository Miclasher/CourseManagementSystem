using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests.Courses;

public sealed class CourseTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Request_Should_ReturnUnauthorized_WhenUserHeaderIsMissing()
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync("api/enrollments/mine");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Publish_Should_ReturnBadRequest_WhenCourseHasNoLessons()
    {
        // Arrange
        Guid courseId = await CreateCourseAsync();

        // Act
        HttpResponseMessage response = await HttpClient.PutAsync($"api/courses/{courseId}/publish", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Publish_Should_ReturnBadRequest_WhenUserIsNotInstructor()
    {
        // Arrange
        (Guid courseId, _) = await CreatePublishedCourseAsync(lessons: 1);
        ActAs(StudentId);

        // Act
        HttpResponseMessage response = await HttpClient.PutAsync($"api/courses/{courseId}/publish", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Should_ReturnConflict_WhenSlugIsTaken_AndLeaveNothingBehind()
    {
        // Arrange
        ActAs(InstructorId);
        var request = new { categoryId = DatabasesCategoryId, title = "Dup", slug = "dup-slug", description = "d" };
        (await HttpClient.PostAsJsonAsync("api/courses", request)).EnsureSuccessStatusCode();

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("api/courses", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ScalarAsync<long>("SELECT count(*) FROM courses WHERE slug = 'dup-slug'")).ShouldBe(1);
    }

    [Fact]
    public async Task Publish_Should_MakeCourseVisibleInCatalogAndSearch()
    {
        // Arrange
        (Guid courseId, _) = await CreatePublishedCourseAsync();

        // Act
        string catalog = await HttpClient.GetStringAsync("api/courses");
        string details = await HttpClient.GetStringAsync($"api/courses/{courseId}");

        // Assert
        catalog.ShouldContain(courseId.ToString());
        details.ShouldContain("\"lessonCount\":2");
    }

    [Fact]
    public async Task Delete_Should_SoftDeleteCascadeToLessons_AndRestoreShouldUndoIt()
    {
        // Arrange
        (Guid courseId, _) = await CreatePublishedCourseAsync();

        // Act
        (await HttpClient.DeleteAsync($"api/courses/{courseId}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Assert
        (await HttpClient.GetAsync($"api/courses/{courseId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await HttpClient.GetStringAsync("api/courses/deleted")).ShouldContain(courseId.ToString());
        (await ScalarAsync<long>(
            $"SELECT count(*) FROM lessons l JOIN modules m ON m.id = l.module_id WHERE m.course_id = '{courseId}' AND l.is_deleted"))
            .ShouldBe(2);

        (await HttpClient.PutAsync($"api/courses/{courseId}/restore", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await HttpClient.GetAsync($"api/courses/{courseId}")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ScalarAsync<long>(
            $"SELECT count(*) FROM lessons l JOIN modules m ON m.id = l.module_id WHERE m.course_id = '{courseId}' AND l.is_deleted"))
            .ShouldBe(0);
    }

    [Fact]
    public async Task HardDelete_Should_BeTurnedIntoSoftDelete_ByTrigger()
    {
        // Arrange
        Guid courseId = await CreateCourseAsync();

        // Act
        await ExecuteAsync($"DELETE FROM courses WHERE id = '{courseId}'");

        // Assert
        (await ScalarAsync<bool>($"SELECT is_deleted FROM courses WHERE id = '{courseId}'")).ShouldBeTrue();
    }

    [Fact]
    public async Task Update_Should_StoreActorAndTimestamp_AndWriteAuditLog()
    {
        // Arrange
        Guid courseId = await CreateCourseAsync();
        DateTime before = await ScalarAsync<DateTime>($"SELECT updated_at FROM courses WHERE id = '{courseId}'");

        // Act
        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/courses/{courseId}", new
        {
            categoryId = DatabasesCategoryId,
            title = "Renamed",
            description = "d",
            level = 1,
            price = 10
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ScalarAsync<Guid>($"SELECT updated_by FROM courses WHERE id = '{courseId}'")).ShouldBe(InstructorId);
        (await ScalarAsync<DateTime>($"SELECT updated_at FROM courses WHERE id = '{courseId}'")).ShouldBeGreaterThan(before);
        (await ScalarAsync<long>(
            $"SELECT count(*) FROM audit_log WHERE table_name = 'courses' AND record_id = '{courseId}' AND operation = 'U'"))
            .ShouldBeGreaterThanOrEqualTo(1);
    }
}
