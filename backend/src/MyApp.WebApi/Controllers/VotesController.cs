using Microsoft.AspNetCore.Mvc;
using MyApp.Application.Services;

namespace MyApp.WebApi.Controllers;

[ApiController]
[Route("api/images/{id:guid}/vote")]
public sealed class VotesController : ControllerBase
{
    private readonly ImageService _service;

    public VotesController(ImageService service)
    {
        _service = service;
    }

    public sealed class VoteRequest
    {
        public required string UserName { get; init; }
        public required bool IsLike { get; init; }
    }

    [HttpPost]
    public IActionResult Vote([FromRoute] Guid id, [FromBody] VoteRequest req)
    {
        try
        {
            _service.Vote(id, req.UserName, req.IsLike);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpDelete]
    public IActionResult Unvote([FromRoute] Guid id, [FromQuery] string userName)
    {
        try
        {
            _service.Unvote(id, userName);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }
}
