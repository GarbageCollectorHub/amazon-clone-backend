using AMZN.Data;
using AMZN.Models.Action;
using AMZN.Models.Category;
using AMZN.Models.User;

namespace AMZN.Models;

public class FormsValidators
{
    private readonly DataContext _dataContext;
    
    public FormsValidators(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public Dictionary<string, string> ValidateUser(UserSignUpFormModel? formModel)
    {
        Dictionary<string, string> errors = new();
        if (formModel != null)
        {
            if (string.IsNullOrEmpty(formModel.FirstName))
            {
                errors.Add("firstName", "First name is required");
            }

            if (!string.IsNullOrEmpty(formModel.FirstName))
            {
                if (formModel.FirstName.Length < 2 || formModel.FirstName.Length > 20)
                {
                    errors.Add("firstName", "First name must be between 2 and 20 characters");
                }
            }
            
            if (string.IsNullOrEmpty(formModel.LastName))
            {
                errors.Add("firstName", "First name is required");
            }

            if (!string.IsNullOrEmpty(formModel.LastName))
            {
                if (formModel.LastName.Length < 2 || formModel.LastName.Length > 20)
                {
                    errors.Add("firstName", "First name must be between 2 and 20 characters");
                }
            }
            
            if (string.IsNullOrEmpty(formModel.Email))
            {
                errors.Add("email", "Email is required");
            }

            if (!string.IsNullOrEmpty(formModel.Email))
            {
                if (_dataContext.Users.FirstOrDefault(u => u.Email == formModel.Email) != null)
                {
                    errors.Add("email", "Email already taken");
                }
            }
            
            if (string.IsNullOrEmpty(formModel.Password))
            {
                errors.Add("password", "password is required");
            }

            if (!(formModel.Password.Length >= 8 && formModel.Password.Length <= 20))
            {
                errors.Add("password", "Password must be between 8 and 20 characters");
            }
            
            if (string.IsNullOrEmpty(formModel.ConfirmPassword))
            {
                errors.Add("passwordConfirm", "password's confirmation is required");
            }

            if (!(formModel.ConfirmPassword.Length >= 8 && formModel.ConfirmPassword.Length <= 20))
            {
                errors.Add("passwordConfirm", "Password must be between 8 and 20 characters");
            }
            
            if (!string.Equals(formModel.Password, formModel.ConfirmPassword))
            {
                errors.Add("passwordConfirm", "Passwords do not match");
            }

            if (string.IsNullOrEmpty(formModel.Role))
            {
                errors.Add("role", "Role is required");
            }

            if (!string.IsNullOrEmpty(formModel.Role))
            {
                if (!string.Equals(formModel.Role, "User") && !string.Equals(formModel.Role, "Admin") && !string.Equals(formModel.Role, "Moderator"))
                {
                    errors.Add("role", "Role is not valid");
                }
            }
        }
        else
        {
            errors.Add("formModel", "Form model is null");
        }
        
        return errors;
    }

    public Dictionary<string, string> ValidateCategory(CategoryFormModel? formModel)
    {
        Dictionary<string, string> errors = new();
        if (formModel != null)
        {
            if (string.IsNullOrEmpty(formModel.Name))
            {
                errors.Add("name", "Name is required");
            }
            
            if (!string.IsNullOrEmpty(formModel.Name))
            {
                if (formModel.Name.Length < 2 || formModel.Name.Length > 20)
                {
                    errors.Add("name", "Name must be between 2 and 20 characters");
                }
                if (_dataContext.Categories.FirstOrDefault(c => c.Name == formModel.Name) != null)
                {
                    errors.Add("name", "Name already taken");
                }
            }

            if (string.IsNullOrEmpty(formModel.Description))
            {
                errors.Add("description", "Description is required");
            }
            
            if (!string.IsNullOrEmpty(formModel.Description))
            {
                if (formModel.Description.Length < 2 || formModel.Description.Length > 100)
                {
                    errors.Add("description", "Description must be between 2 and 100 characters");
                }
            }

            if (!string.IsNullOrEmpty(formModel.ParentCategory))
            {
                if (_dataContext.Categories.FirstOrDefault(c => c.Id.ToString() == formModel.ParentCategory) == null)
                {
                    errors.Add("parentCategory", "Parent category does not exist");
                }
            }

            if (formModel.Image == null || formModel.Image.Length == 0)
            {
                errors.Add("image", "Image is required");
            }
        }
        else
        {
            errors.Add("formModel", "Form model is null");
        }
        
        return errors;
    }

    public Dictionary<string, string> ValidateAction(ActionFormModel? formModel)
    {
        Dictionary<string, string> errors = new();
        if (formModel != null)
        {
            if (string.IsNullOrEmpty(formModel.Name))
            {
                errors.Add("name", "Name is required");
            }

            if (!string.IsNullOrEmpty(formModel.Name))
            {
                if (formModel.Name.Length < 2 || formModel.Name.Length > 20)
                {
                    errors.Add("name", "Name must be between 2 and 20 characters");
                }

                if (_dataContext.Actions.FirstOrDefault(a => a.Name == formModel.Name) != null)
                {
                    errors.Add("name", "Name already taken");
                }
            }

            if (!string.IsNullOrEmpty(formModel.Description))
            {
                if (formModel.Description.Length < 2 || formModel.Description.Length > 100)
                {
                    errors.Add("description", "Description must be between 2 and 100 characters");
                }
            }

            if (string.IsNullOrEmpty(formModel.ApplyTo))
            {
                errors.Add("applyTo", "Application type to is required");
            }

            if (!string.IsNullOrEmpty(formModel.ApplyTo))
            {
                if (formModel.ApplyTo != "product" && formModel.ApplyTo != "category")
                {
                    errors.Add("applyTo", "Application type to is not valid");
                }
            }

            if (string.IsNullOrEmpty(formModel.ProductTitle))
            {
                errors.Add("productTitle", "Product title is required");
            }

            if (!string.IsNullOrEmpty(formModel.ProductTitle))
            {
                if (formModel.ApplyTo == "product")
                {
                    if (_dataContext.Products.FirstOrDefault(p => p.Title == formModel.ProductTitle) == null)
                    {
                        errors.Add("productTitle", "Product does not exist");
                    }
                }

                if (formModel.ApplyTo == "category")
                {
                    if (_dataContext.Categories.FirstOrDefault(c => c.Name == formModel.ProductTitle) == null)
                    {
                        errors.Add("productTitle", "Category does not exist");
                    }
                }
            }

            if (formModel.Amount <= 0 || formModel.Amount >= 100)
            {
                errors.Add("amount", "Amount must be between 1 and 99");
            }

            if (formModel.StartDate == default)
            {
                errors.Add("startDate", "Start date is required");
            }

            if (formModel.EndDate == default)
            {
                errors.Add("endDate", "End date is required");
            }

            else if (formModel.StartDate >= formModel.EndDate)
            {
                errors.Add("endDate", "End date must be after start date");
            }

            else if (formModel.EndDate <= DateTime.UtcNow)
            {
                errors.Add("endDate", "End date must be in the future");
            }
        }
        else
        {
            errors.Add("formModel", "Form model is null");
        }
        
        return errors;
    }
    
}
