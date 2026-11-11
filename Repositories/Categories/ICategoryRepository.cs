using AMZN.Data.Entities;
using AMZN.DTOs.Categories;

namespace AMZN.Repositories.Categories;

public interface ICategoryRepository
{
    Category? GetById(string id);
    Task AddCategoryAsync(Category category);
    Task<bool> NameExistsAsync(string name);

    List<Category> GetAll();

    Task<List<Category>> GetAllAsync();
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    
    Category? GetByName(string name);

    Task<List<CategoryListItemDto>> GetRootAsync(CancellationToken ct = default);
    Task<List<CategoryListItemDto>> GetByParentAsync(Guid parentId, CancellationToken ct = default);
    Task SaveChangesAsync();

    Task<List<Guid>> GetCategoryTreeIdsAsync(Guid categoryId, CancellationToken ct = default);
}
