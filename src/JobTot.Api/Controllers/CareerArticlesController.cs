using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobTot.Application.Features;
using static JobTot.Application.Features.CareerArticlesService;

namespace JobTot.Api.Controllers;

[ApiController]
public sealed class CareerArticlesController(CareerArticlesService service) : ApplicationController
{
    [HttpGet("api/career-articles")]
    public async Task<IActionResult> Published(CancellationToken ct) =>
        ToAction(await service.Published(ct));
    [Authorize(Policy = AdminAuthentication.Policy)]
    [HttpPost("api/admin/career-articles/{id:guid}/cover")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadCover(Guid id, [FromForm] IFormFile file, [FromForm] Guid version, CancellationToken ct) =>
        ToAction(await service.UploadCover(id, new UploadFile(file.Length, file.FileName, file.ContentType, file.CopyToAsync), version, ct));
    [HttpGet("api/career-articles/{id:guid}")]
    public async Task<IActionResult> Read(Guid id, CancellationToken ct) =>
        ToAction(await service.Read(id, ct));
    [Authorize(Policy = AdminAuthentication.Policy)]
    [HttpGet("api/admin/career-articles")]
    public async Task<IActionResult> List(CancellationToken ct) =>
        ToAction(await service.List(ct));
    [Authorize(Policy = AdminAuthentication.Policy)]
    [HttpPost("api/admin/career-articles")]
    public async Task<IActionResult> Create(ArticleInput input, CancellationToken ct) =>
        ToAction(await service.Create(input, ct));
    [Authorize(Policy = AdminAuthentication.Policy)]
    [HttpPut("api/admin/career-articles/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ArticleInput input, CancellationToken ct) =>
        ToAction(await service.Update(id, input, ct));
}
