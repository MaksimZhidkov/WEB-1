using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Application.Services;

namespace MyApp.WebApi.Controllers;

[ApiController]
[Route("api/images")]
public sealed class ImagesController : ControllerBase
{
    private readonly ImageService _service;

    public ImagesController(ImageService service) => _service = service;

    public sealed class UploadImageRequest
    {
        public required string Title { get; init; }
        public required IFormFile File { get; init; }
    }

    [Authorize(Roles = "user,admin")]
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] UploadImageRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Title)) return BadRequest("Название обязательно");
        if (req.File is null || req.File.Length == 0) return BadRequest("Файл обязателен");

        var userId = User.Claims.FirstOrDefault(c => c.Type.EndsWith("nameidentifier", StringComparison.OrdinalIgnoreCase))?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        await using var stream = req.File.OpenReadStream();

        var created = await _service.UploadAsync(
            title: req.Title,
            contentType: req.File.ContentType ?? "application/octet-stream",
            sizeBytes: req.File.Length,
            fileStream: stream,
            originalFileName: req.File.FileName,
            userId: userId,
            ct: ct);

        return Ok(new
        {
            created.Id,
            created.Title,
            created.FileName,
            created.ContentType,
            created.SizeBytes,
            created.UploadedAt
        });
    }

    [Authorize(Roles = "user,admin")]
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var list = await _service.ListAsync(ct);
        return Ok(list.Select(x => new
        {
            x.Id,
            x.Title,
            x.FileName,
            x.ContentType,
            x.SizeBytes,
            x.UploadedAt
        }));
    }

    [Authorize(Roles = "user,admin")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var img = await _service.GetAsync(id, ct);
        if (img is null) return NotFound();

        return Ok(new
        {
            img.Id,
            img.Title,
            img.FileName,
            img.ContentType,
            img.SizeBytes,
            img.UploadedAt
        });
    }

    // скачивание — только admin (пример разграничения)
    [Authorize(Roles = "admin")]
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var img = await _service.GetAsync(id, ct);
        if (img is null) return NotFound();

        var path = _service.GetFilePath(img);
        if (!System.IO.File.Exists(path)) return NotFound("File missing on disk");

        return PhysicalFile(path, img.ContentType, fileDownloadName: img.FileName);
    }
}
