using AMZN.Data.Entities;
using AMZN.Extensions;
using AMZN.Models.Brands;
using AMZN.Services.BrandService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AMZN.Controllers
{

    [Authorize(Roles = "Admin")]
    [Route("Admin/Brands")]
    public class AdminBrandsController : Controller
    {
        private readonly BrandService _brandService;

        public AdminBrandsController(BrandService brandService)
        {
            _brandService = brandService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var list  = await _brandService.GetAllAsync();
            return View(list);
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View(new BrandCreateViewModel());
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimitingExtensions.CatalogWritesPolicy)]
        public async Task<IActionResult> Create(BrandCreateViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            try
            {
                await _brandService.CreateAsync(vm.Name);
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(vm.Name), ex.Message);
                return View(vm);
            }
        }

        [HttpPost("Delete/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _brandService.DeleteAsync(id);
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }


    }
}
