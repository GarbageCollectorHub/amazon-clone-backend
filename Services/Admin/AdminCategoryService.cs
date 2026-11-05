using AMZN.Data.Entities;
using AMZN.Models.Category;
using AMZN.Repositories.Categories;
using AMZN.Services.Storage.Cloud;

namespace AMZN.Services.Admin;

public class AdminCategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICloudStorageService _cloudStorageService;
    
    public AdminCategoryService(ICategoryRepository categoryRepository, 
        ICloudStorageService cloudStorageService)
    {
        _categoryRepository = categoryRepository;
        _cloudStorageService = cloudStorageService;
    }

    public async Task<Dictionary<string, string>> AddCategoryAsync(CategoryFormModel formModel)
    {
        string name = formModel.Name.Trim();
        if (await _categoryRepository.NameExistsAsync(name))
            return new() { [nameof(formModel.Name)] = "Name already taken" };

        Guid? parentId = null;
        if (!string.IsNullOrWhiteSpace(formModel.ParentCategory))
        {
            if (!Guid.TryParse(formModel.ParentCategory, out Guid id) || !await _categoryRepository.ExistsAsync(id))
                return new() { [nameof(formModel.ParentCategory)] = "Parent category does not exist" };
            parentId = id;
        }

        Category category = new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = formModel.Description.Trim(),
            ParentId = parentId,
            ImageUrl = await _cloudStorageService.SaveImageAsync(formModel.Image!),
            CreatedAt = DateTime.UtcNow,
        };

        await _categoryRepository.AddCategoryAsync(category);
        return new();
    }

    public Task<List<Category>> GetAllAsync()
    {
        return _categoryRepository.GetAllAsync();
    }
}
