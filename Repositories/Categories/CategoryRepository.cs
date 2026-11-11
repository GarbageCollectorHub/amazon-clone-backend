using AMZN.Data;
using AMZN.Data.Entities;
using AMZN.DTOs.Categories;
using Microsoft.EntityFrameworkCore;

namespace AMZN.Repositories.Categories;

public class CategoryRepository : ICategoryRepository
{
    private readonly DataContext _dataContext;

    public CategoryRepository(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public Category? GetById(string id)
    {
        return _dataContext.Categories.FirstOrDefault(x => x.Id.ToString() == id);
    }
    
    public async Task AddCategoryAsync(Category category)
    {
        _dataContext.Categories.Add(category);
        await _dataContext.SaveChangesAsync();
    }

    public Task<bool> NameExistsAsync(string name)
    {
        return _dataContext.Categories.AnyAsync(c => c.Name == name);
    }
    
    public List<Category> GetAll()
    {
        return _dataContext.Categories.ToList();
    }


    public Task<List<Category>> GetAllAsync()
    {
        return _dataContext.Categories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        return _dataContext.Categories
            .AsNoTracking()
            .AnyAsync(x => x.Id == id, ct);
    }
    
    public Category? GetByName(string name)
    {
        return _dataContext.Categories.FirstOrDefault(x => x.Name == name);
    }

    public Task<List<CategoryListItemDto>> GetRootAsync(CancellationToken ct = default)
    {
        return _dataContext.Categories
            .AsNoTracking()
            .Where(x => x.ParentId == null)
            .OrderBy(x => x.Name)
            .Select(x => new CategoryListItemDto
            {
                Id = x.Id,
                ParentId = x.ParentId,
                Name = x.Name,
                Description = x.Description,
                ImageUrl = x.ImageUrl,
                HasChildren = _dataContext.Categories.Any(c => c.ParentId == x.Id)
            })
            .ToListAsync(ct);
    }

    public Task<List<CategoryListItemDto>> GetByParentAsync(Guid parentId, CancellationToken ct = default)
    {
        return _dataContext.Categories
            .AsNoTracking()
            .Where(x => x.ParentId == parentId)
            .OrderBy(x => x.Name)
            .Select(x => new CategoryListItemDto
            {
                Id = x.Id,
                ParentId = x.ParentId,
                Name = x.Name,
                Description = x.Description,
                ImageUrl = x.ImageUrl,
                HasChildren = _dataContext.Categories.Any(c => c.ParentId == x.Id)
            })
            .ToListAsync(ct);
    }

    public async Task<List<Guid>> GetCategoryTreeIdsAsync(Guid categoryId, CancellationToken ct = default)
    {
        List<Category> categories = await _dataContext.Categories
            .AsNoTracking()
            .Select(x => new Category
            {
                Id = x.Id,
                ParentId = x.ParentId
            })
            .ToListAsync(ct);

        Dictionary<Guid, List<Category>> subcategoriesByParentId = categories
            .Where(x => x.ParentId != null)
            .GroupBy(x => x.ParentId!.Value)
            .ToDictionary(x => x.Key, x => x.ToList());

        List<Guid> result = new List<Guid>();

        CollectCategoryTreeIds(categoryId, subcategoriesByParentId, result);

        return result;
    }

    private static void CollectCategoryTreeIds(
            Guid categoryId,
            Dictionary<Guid, List<Category>> subcategoriesByParentId,
            List<Guid> result
            )
    {
        result.Add(categoryId);

        if (!subcategoriesByParentId.TryGetValue(categoryId, out List<Category>? subcategories))
            return;

        foreach (Category subcategory in subcategories)
        {
            CollectCategoryTreeIds(subcategory.Id, subcategoriesByParentId, result);
        }
    }



    public Task SaveChangesAsync()
    {
        return _dataContext.SaveChangesAsync();
    }

}
