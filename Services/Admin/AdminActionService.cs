using AMZN.Models;
using AMZN.Models.Action;
using AMZN.Repositories.Actions;
using AMZN.Repositories.Categories;
using AMZN.Repositories.Products;
using Action = AMZN.Data.Entities.Action;

namespace AMZN.Services.Admin;

public class AdminActionService
{
    private readonly IActionRepository _actions;
    private readonly FormsValidators _formsValidator;
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    public AdminActionService(IActionRepository actions,
        FormsValidators formsValidator,
        IProductRepository products,
        ICategoryRepository categories)
    {
        _actions = actions;
        _formsValidator = formsValidator;
        _products = products;
        _categories = categories;
    }

    public (bool, object) AddAction(ActionFormModel formModel)
    {
        Dictionary<string, string> errors = _formsValidator.ValidateAction(formModel);
        if (errors.Count == 0)
        {
            Guid id = Guid.NewGuid();
            Action action = new()
            {
                Id = id,
                Name = formModel.Name,
                ProductTitle = formModel.ProductTitle,
                Amount = formModel.Amount,
                StartDate = formModel.StartDate,
                EndDate = formModel.EndDate,
                CreatedAt = DateTime.UtcNow,
            };
            if (formModel.Description != null)
            {
                action.Description = formModel.Description;
            }
            if (formModel.ApplyTo == "product")
            {
                _actions.AddProductAction(action);
                if (_actions.IsValid(action))
                {
                    _actions.ApplyProductAction(action);
                }
            }
            if (formModel.ApplyTo == "category")
            {
                _actions.AddCategoryAction(action);
                if (_actions.IsValid(action))
                {
                    _actions.ApplyCategoryAction(action);
                }
            }
            return (true, "Action added successfully");
        }
        else
        {
            return (false, errors.Values);
        }
    }

    public void CheckActionsValidity()
    {
        var actions = _actions.GetActions()
            .OrderBy(a => a.CreatedAt)
            .ThenBy(a => a.Id)
            .ToList();
        var inactiveActions = actions.Where(a => a.IsApplied && !_actions.IsValid(a)).ToList();

        foreach (var action in inactiveActions)
        {
            _actions.RemoveProductAction(action);
            _actions.RemoveCategoryAction(action);
        }

        foreach (var action in actions.Where(a => _actions.IsValid(a)))
        {
            if (!action.IsApplied || inactiveActions.Count > 0)
            {
                _actions.ApplyProductAction(action);
                _actions.ApplyCategoryAction(action);
            }
        }
    }
}
