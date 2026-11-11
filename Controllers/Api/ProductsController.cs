using AMZN.DTOs.Brands;
using AMZN.DTOs.Common;
using AMZN.DTOs.Products;
using AMZN.DTOs.Products.Reviews;
using AMZN.Extensions;
using AMZN.Services.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace AMZN.Controllers.Api
{
    [Route("api/products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ProductService _productService;

        public ProductsController(ProductService productService) 
        {
            _productService = productService;
        }


        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<ActionResult<ProductDetailsDto>> GetById([FromRoute] Guid id, CancellationToken ct)
        {
            var dto = await _productService.GetByIdAsync(id, ct);
            return Ok(dto);
        }


        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResult<ProductCardDto>>> GetCatalogPage([FromQuery] ProductListQueryDto q, CancellationToken ct)
        {
            var dto = await _productService.GetCatalogPageAsync(q, ct);
            return Ok(dto);
        }


        [HttpGet("brands")]
        [AllowAnonymous]
        [OutputCache(Duration = 30, VaryByQueryKeys = new[] { "categoryId" })]
        public async Task<ActionResult<List<BrandDto>>> GetCatalogBrands([FromQuery] Guid? categoryId, CancellationToken ct)
        {
            var dto = await _productService.GetCatalogBrandsAsync(categoryId, ct);
            return Ok(dto);
        }

        [HttpPut("{productId:guid}/rating")]
        [Authorize]
        public async Task<ActionResult<ProductRatingResponseDto>> SetRating([FromRoute] Guid productId, [FromBody] SetProductRatingRequestDto request)
        {
            var userId = User.GetRequiredUserId();

            var dto = await _productService.SetRatingAsync(productId, userId, request.Rating);
            return Ok(dto);
        }

        [HttpGet("{productId:guid}/reviews")]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResult<ReviewDto>>> GetReviews([FromRoute] Guid productId, [FromQuery] ReviewParamsDto q, CancellationToken ct)
        {
            var dto = await _productService.GetReviewsAsync(productId, q, ct);
            return Ok(dto);
        }

        [HttpPut("{productId:guid}/review")]
        [Authorize]
        public async Task<ActionResult<ReviewDto>> CreateOrUpdateReview([FromRoute] Guid productId, [FromBody] ReviewRequestDto request)
        {
            var userId = User.GetRequiredUserId();

            var dto = await _productService.CreateOrUpdateReviewAsync(productId, userId, request);
            return Ok(dto);
        }


    }


}
