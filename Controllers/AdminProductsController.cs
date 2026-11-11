using AMZN.Extensions;
using AMZN.Models.Product;
using AMZN.Services.Admin;
using AMZN.Shared.Limits;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AMZN.Controllers
{
    [Route("Admin/Products")]
    public class AdminProductsController : Controller
    {
        private readonly AdminProductService _adminProductService;

        public AdminProductsController(AdminProductService adminProductService)
        {
            _adminProductService = adminProductService;
        }



        [HttpGet("")]
        [Authorize]
        public async Task<IActionResult> Index(string? search, string? sort, int page, CancellationToken ct)
        {
            Guid? sellerId = User.GetUserIdOrNull();
            if (sellerId == null)
                return Forbid();

            MyProductsListViewModel vm = await _adminProductService.BuildMyProductsListVmAsync(
                sellerId.Value, search, sort, page, ct);
            return View(vm);
        }



        [HttpGet("Create")]
        [Authorize]
        public async Task<IActionResult> Create()
        {
            ProductCreateViewModel vm = await _adminProductService.BuildCreateVmAsync();
            return View(vm);
        }


        [HttpPost("Create")]
        [Authorize]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimitingExtensions.ImageUploadsPolicy)]
        [RequestSizeLimit(ImageUploadLimits.MaxRequestBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = ImageUploadLimits.MaxRequestBytes)]
        public async Task<IActionResult> Create(ProductCreateFormModel form, CancellationToken cancellationToken)
        {
            Guid? sellerId = User.GetUserIdOrNull();
            if (sellerId == null)
                return Forbid();

            if (!ModelState.IsValid)
            {
                TempData.Remove("Success");

                ProductCreateViewModel vmInvalid = await _adminProductService.BuildCreateVmAsync(form);
                return View(vmInvalid);
            }

            try
            {
                Guid productId = await _adminProductService.CreateAsync(form, sellerId.Value, cancellationToken);

                TempData["Success"] = "Product created";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                TempData.Remove("Success");

                ProductCreateViewModel vmError = await _adminProductService.BuildCreateVmAsync(form, ex.Message);
                return View(vmError);
            }
        }


        [HttpGet("Edit/{id:guid}")]
        [Authorize]
        public async Task<IActionResult> Edit(Guid id)
        {
            Guid? sellerId = User.GetUserIdOrNull();
            if (sellerId == null)
                return Forbid();

            try
            {
                ProductEditViewModel vm = await _adminProductService.BuildProductEditVmAsync(id, sellerId.Value);
                return View(vm);
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }


        [HttpPost("Edit/{id:guid}")]
        [Authorize]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimitingExtensions.ImageUploadsPolicy)]
        [RequestSizeLimit(ImageUploadLimits.MaxRequestBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = ImageUploadLimits.MaxRequestBytes)]
        public async Task<IActionResult> Edit(Guid id, ProductEditFormModel form, CancellationToken cancellationToken)
        {
            Guid? sellerId = User.GetUserIdOrNull();
            if (sellerId == null)
                return Forbid();

            if (!ModelState.IsValid)
            {
                TempData.Remove("Success");

                try
                {
                    ProductEditViewModel vmInvalid = await _adminProductService.BuildProductEditVmAsync(id, sellerId.Value, form);

                    return View(vmInvalid);
                }
                catch (InvalidOperationException)
                {
                    return NotFound();
                }
            }

            try
            {
                await _adminProductService.UpdateAsync(id, form, sellerId.Value, cancellationToken);

                TempData["Success"] = "Product updated";
                return RedirectToAction(nameof(Edit), new { id });
            }
            catch (InvalidOperationException ex)
            {
                TempData.Remove("Success");

                try
                {
                    ProductEditViewModel vmError = await _adminProductService.BuildProductEditVmAsync(id, sellerId.Value, form, ex.Message);

                    return View(vmError);
                }
                catch (InvalidOperationException)
                {
                    return NotFound();
                }
            }
        }


        [HttpPost("Delete/{id:guid}")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            Guid? sellerId = User.GetUserIdOrNull();
            if (sellerId == null)
                return Forbid();

            try
            {
                await _adminProductService.DeleteAsync(id, sellerId.Value, cancellationToken);
                TempData["Success"] = "Product deleted";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
