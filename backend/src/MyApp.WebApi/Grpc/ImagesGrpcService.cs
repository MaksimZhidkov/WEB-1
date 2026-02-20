using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using MyApp.Application.Services;
using MyApp.WebApi.Grpc;

namespace MyApp.WebApi.Grpc;

[Authorize(Roles = "user,admin")]
public sealed class ImagesGrpcService : ImagesGrpc.ImagesGrpcBase
{
    private readonly ImageService _service;

    public ImagesGrpcService(ImageService service) => _service = service;

    public override async Task<ListImagesResponse> List(ListImagesRequest request, ServerCallContext context)
    {
        var list = await _service.ListAsync(context.CancellationToken);

        var resp = new ListImagesResponse();
        resp.Items.AddRange(list.Select(x => new ImageDto
        {
            Id = x.Id.ToString(),
            Title = x.Title,
            FileName = x.FileName,
            ContentType = x.ContentType,
            SizeBytes = x.SizeBytes,
            UploadedAt = x.UploadedAt.ToString("O")
        }));

        return resp;
    }

    public override async Task<ImageDto> Get(GetImageRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid id"));

        var img = await _service.GetAsync(id, context.CancellationToken);
        if (img is null)
            throw new RpcException(new Status(StatusCode.NotFound, "Not found"));

        return new ImageDto
        {
            Id = img.Id.ToString(),
            Title = img.Title,
            FileName = img.FileName,
            ContentType = img.ContentType,
            SizeBytes = img.SizeBytes,
            UploadedAt = img.UploadedAt.ToString("O")
        };
    }
}
