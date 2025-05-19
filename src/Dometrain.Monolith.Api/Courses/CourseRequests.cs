namespace Dometrain.Monolith.Api.Courses;

public record CreateCourseRequest(string Name, string Description, string Author);

public record UpdateCourseRequest(string Name, string Description, string Author);
