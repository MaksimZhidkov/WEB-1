using Microsoft.AspNetCore.Mvc;
using MyApp.Application.Services;

namespace MyApp.WebApi.Controllers;

[ApiController]
[Route("api/images")]
public sealed class ImagesController : ControllerBase
{
    private readonly ImageService _service;

    public ImagesController(ImageService service)
    {
        _service = service;
    }

    public sealed class UploadImageRequest
    {
        public required string Title { get; init; }
        public required IFormFile File { get; init; }
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] UploadImageRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Title)) return BadRequest("Название обязательно.");
        if (req.File is null || req.File.Length == 0) return BadRequest("Файл обязателен.");

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        try
        {
            await using var stream = req.File.OpenReadStream();
            var dto = await _service.UploadAsync(
                req.Title,
                stream,
                req.File.FileName,
                req.File.ContentType,
                baseUrl,
                ct);

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpGet]
    public IActionResult GetPage([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = _service.GetPage(page, pageSize, baseUrl);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetById([FromRoute] Guid id)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var dto = _service.Get(id, baseUrl);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete([FromRoute] Guid id)
    {
        var ok = _service.Delete(id);
        return ok ? NoContent() : NotFound();
    }
}
