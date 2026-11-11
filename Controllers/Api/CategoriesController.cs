using AMZN.DTOs.Categories;
using AMZN.Services.Categories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace AMZN.Controllers.Api
{

    [Route("api/categories")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly CategoryService _categories;

        public CategoriesController(CategoryService categories)
        {
            _categories = categories;
        }


        [HttpGet("root")]
        [AllowAnonymous]
        [OutputCache(Duration = 20)]
        public async Task<ActionResult<List<CategoryListItemDto>>> GetRoot(CancellationToken ct)
        {
            var dto = await _categories.GetRootAsync(ct);
            return Ok(dto);
        }

        [HttpGet]
        [AllowAnonymous]
        [OutputCache(Duration = 20)]
        public async Task<ActionResult<List<CategoryListItemDto>>> GetByParent([FromQuery] Guid parentId, CancellationToken ct)
        {
            var dto = await _categories.GetByParentAsync(parentId, ct);
            return Ok(dto);
        }

    }
}
