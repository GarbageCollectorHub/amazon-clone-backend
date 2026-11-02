using AMZN.Data;
using AMZN.Data.Entities;
using Action = AMZN.Data.Entities.Action;

namespace AMZN.Repositories.Actions;

public class ActionRepository : IActionRepository
{
    private readonly DataContext _dataContext;

    public ActionRepository(DataContext dataContext)
    {
        _dataContext = dataContext;
    }
    
    public void AddProductAction(Action action)
    {
        _dataContext.Actions.Add(action);
        var product = _dataContext.Products.FirstOrDefault(p => p.Title == action.ProductTitle);
        if (product != null)
        {
            _dataContext.ProductActions.Add(new()
            {
                ProductId = product.Id,
                ActionId = action.Id
            });
        }

        _dataContext.SaveChanges();
    }

    public void AddCategoryAction(Action action)
    {
        _dataContext.Actions.Add(action);
        var category = _dataContext.Categories.FirstOrDefault(c => c.Name == action.ProductTitle);
        if (category != null)
        {
            _dataContext.CategoryActions.Add(new()
            {
                CategoryId = category.Id,
                ActionId = action.Id
            });
        }
        
        _dataContext.SaveChanges();
    }

    public void ApplyProductAction(Action action)
    {
        var productAction = _dataContext.ProductActions.FirstOrDefault(pa => pa.Action.Id == action.Id);
        if (productAction != null)
        {
            var product = _dataContext.Products.FirstOrDefault(p => p.Id == productAction.ProductId);
            if (product != null)
            {
                if (product.OriginalPrice == null)
                {
                    product.OriginalPrice = product.CurrentPrice;
                    product.CurrentPrice *= (decimal)((100 - action.Amount) * 0.01);
                }
                else
                {
                    product.CurrentPrice = (decimal)product.OriginalPrice * (decimal)((100 - action.Amount) * 0.01);
                }
                _dataContext.Products.Update(product);
                action.IsApplied = true;
                _dataContext.SaveChanges();
            }
        }
    }

    public void ApplyCategoryAction(Action action)
    {
        var categoryAction = _dataContext.CategoryActions.FirstOrDefault(ca => ca.Action.Id == action.Id);
        if (categoryAction != null)
        {
            var category = _dataContext.Categories.FirstOrDefault(c => c.Id == categoryAction.CategoryId);
            if (category != null)
            {
                var products = _dataContext.Products.Where(p => p.CategoryId == category.Id);
                if (products.Count() != 0)
                {
                    foreach (var product in products)
                    {
                        if (product.OriginalPrice == null)
                        {
                            product.OriginalPrice = product.CurrentPrice;
                            product.CurrentPrice *= (decimal)((100 - action.Amount) * 0.01);
                        }
                        else
                        {
                            product.CurrentPrice = (decimal)product.OriginalPrice * (decimal)((100 - action.Amount) * 0.01);
                        }
                        _dataContext.Products.Update(product);
                    }
                    action.IsApplied = true;
                    _dataContext.SaveChanges();
                }
            }
        }
    }

    public void RemoveProductAction(Action action)
    {
        var productAction = _dataContext.ProductActions.FirstOrDefault(pa => pa.Action.Id == action.Id);
        if (productAction != null)
        {
            var product = _dataContext.Products.FirstOrDefault(p => p.Id == productAction.ProductId);
            if (product != null)
            {
                if (product.OriginalPrice != null)
                {
                    product.CurrentPrice = (decimal)product.OriginalPrice;
                    product.OriginalPrice = null;
                    _dataContext.Products.Update(product);
                }
            }
            action.IsApplied = false;
            _dataContext.SaveChanges();
        }
    }

    public void RemoveCategoryAction(Action action)
    {
        var categoryAction = _dataContext.CategoryActions.FirstOrDefault(ca => ca.Action.Id == action.Id);
        if (categoryAction != null)
        {
            var category = _dataContext.Categories.FirstOrDefault(c => c.Id == categoryAction.CategoryId);
            if (category != null)
            {
                var products = _dataContext.Products.Where(p => p.CategoryId == category.Id);
                if (products.Count() != 0)
                {
                    foreach (var product in products)
                    {
                        if (product.OriginalPrice != null)
                        {
                            product.CurrentPrice = (decimal)product.OriginalPrice;
                            product.OriginalPrice = null;
                            _dataContext.Products.Update(product);
                        }
                    }
                }
            }
            action.IsApplied = false;
            _dataContext.SaveChanges();
        }
    }

    public bool IsValid(Action action)
    {
        DateTime now = DateTime.UtcNow;
        return action.StartDate <= now && action.EndDate > now;
    }

    public List<Action> GetActions()
    {
        return _dataContext.Actions.ToList();
    }
}
