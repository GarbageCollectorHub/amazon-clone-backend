using AMZN.DTOs.Search;
using AMZN.Services.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AMZN.Controllers.Api
{

    [Route("api/search")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly SearchService _searchService;

        public SearchController(SearchService searchService)
        {
            _searchService = searchService;
        }


        [HttpGet("suggestions")]
        [AllowAnonymous]
        public async Task<ActionResult<SearchSuggestionsResponseDto>> GetSuggestions([FromQuery(Name = "q"), StringLength(100)] string? query, CancellationToken ct)
        {
            var dto = await _searchService.GetSuggestionsAsync(query, ct);
            return Ok(dto);
        }
    }

}
