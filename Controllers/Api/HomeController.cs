using AMZN.DTOs.Home;
using AMZN.DTOs.Products;
using AMZN.Services.Home;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace AMZN.Controllers.Api
{
    [Route("api/home")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly HomeService _homeService;

        public HomeController(HomeService homeService)
        {
            _homeService = homeService;
        }


        [HttpGet]
        [AllowAnonymous]
        [OutputCache(Duration = 15)]
        public async Task<ActionResult<HomeResponseDto>> GetHome(CancellationToken ct)
        {
            HomeResponseDto dto = await _homeService.GetHomeAsync(ct);
            return Ok(dto);
        }

        [HttpGet("last-viewed")]
        [AllowAnonymous]
        public async Task<ActionResult<List<ProductCardDto>>> GetLastViewed([FromQuery] HomeLastViewedQueryDto query, CancellationToken ct)
        {
            List<ProductCardDto> dto = await _homeService.GetLastViewedAsync(query.ProductIds, ct);
            return Ok(dto);
        }
    }
}
