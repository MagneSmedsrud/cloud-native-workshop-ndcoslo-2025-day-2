namespace Dometrain.Monolith.Api.Students;

public static class StudentMapper
{
    public static Student MapToStudent(this StudentRegistrationRequest request)
    {
        return new Student
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            FullName = request.FullName
        };
    }
}
