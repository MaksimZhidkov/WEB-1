using Microsoft.AspNetCore.Mvc;
using MyApp.Application.Services;

namespace MyApp.WebApi.Controllers;

[ApiController]
[Route("api/rating")]
public sealed class RatingController : ControllerBase
{
    private readonly ImageService _service;

    public RatingController(ImageService service)
    {
        _service = service;
    }

    [HttpGet]
    public IActionResult GetTop([FromQuery] int top = 50)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var items = _service.GetRatingTop(top, baseUrl);
        return Ok(items);
    }
}
