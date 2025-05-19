using Dometrain.Monolith.Api.Courses;
using Dometrain.Monolith.Api.Students;

namespace Dometrain.Monolith.Api.ShoppingCarts;

public interface IShoppingCartService
{
    Task<ShoppingCart?> AddCourseAsync(Guid studentId, Guid courseId);

    Task<ShoppingCart?> GetByIdAsync(Guid studentId);
    
    Task<ShoppingCart?> RemoveItemAsync(Guid studentId, Guid courseId);
    
    Task<ShoppingCart?> ClearAsync(Guid studentId);
}

public class ShoppingCartService : IShoppingCartService
{
    private readonly IShoppingCartRepository _shoppingCartRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ICourseRepository _courseRepository;

    public ShoppingCartService(IShoppingCartRepository shoppingCartRepository, IStudentRepository studentRepository, ICourseRepository courseRepository)
    {
        _shoppingCartRepository = shoppingCartRepository;
        _studentRepository = studentRepository;
        _courseRepository = courseRepository;
    }

    public async Task<ShoppingCart?> AddCourseAsync(Guid studentId, Guid courseId)
    {
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student is null)
        {
            return null;
        }

        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course is null)
        {
            return null;
        }
        
        await _shoppingCartRepository.AddCourseAsync(studentId, courseId);
        return await GetByIdAsync(studentId);
    }

    public async Task<ShoppingCart?> GetByIdAsync(Guid studentId)
    {
        return await _shoppingCartRepository.GetByIdAsync(studentId);
    }

    public async Task<ShoppingCart?> RemoveItemAsync(Guid studentId, Guid courseId)
    {
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student is null)
        {
            return null;
        }
        
        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course is null)
        {
            return null;
        }
        
        await _shoppingCartRepository.RemoveItemAsync(studentId, courseId);
        return await GetByIdAsync(studentId);
    }

    public async Task<ShoppingCart?> ClearAsync(Guid studentId)
    {
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student is null)
        {
            return null;
        }

        await _shoppingCartRepository.ClearAsync(studentId);
        return new ShoppingCart
        {
            StudentId = studentId
        };
    }
}
