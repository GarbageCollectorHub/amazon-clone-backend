using AMZN.Data.Entities;
using AMZN.Models;
using AMZN.Models.Category;
using AMZN.Repositories.Categories;
using AMZN.Services.Storage.Cloud;

namespace AMZN.Services.Admin;

public class AdminCategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly FormsValidators _formsValidator;
    private readonly ICloudStorageService _cloudStorageService;
    
    public AdminCategoryService(ICategoryRepository categoryRepository, 
        FormsValidators formsValidator, 
        ICloudStorageService cloudStorageService)
    {
        _categoryRepository = categoryRepository;
        _formsValidator = formsValidator;
        _cloudStorageService = cloudStorageService;
    }

    public async Task<(bool, object)> AddCategoryAsync(CategoryFormModel formModel)
    {
        Dictionary<string, string> errors = _formsValidator.ValidateCategory(formModel);
        if (errors.Count == 0)
        {
            Guid id = Guid.NewGuid();
            Category? parent = _categoryRepository.GetById(formModel.ParentCategory);
            Guid? parentId = null;

            if (parent != null)
                parentId = parent.Id;

            Category category = new()
            {
                Id = id,
                Name = formModel.Name,
                Description = formModel.Description,
                ImageUrl = await _cloudStorageService.SaveImageAsync(formModel.Image),
                CreatedAt = DateTime.UtcNow,
            };

            if (parent != null)
            {
                category.ParentId = parentId;
                category.ParentCategory = parent;
            }

            _categoryRepository.AddCategory(category);
            return (true, "Category added successfully");
        }

        return (false, errors.Values);
    }

    public List<Category> GetAll()
    {
        return _categoryRepository.GetAll();
    }
}
