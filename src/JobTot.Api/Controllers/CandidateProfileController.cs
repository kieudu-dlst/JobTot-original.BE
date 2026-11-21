using JobTot.Application.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobTot.Application.Features;
using static JobTot.Application.Features.CandidateProfileService;

namespace JobTot.Api.Controllers;

[ApiController]
[Route("api/candidate/profile")]
[Authorize(Policy = CandidateAuthentication.Policy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CandidateProfileController(CandidateProfileService service) : ApplicationController
{
    [HttpPut("cvs/{id:guid}/default")]
    public async Task<IActionResult> SetDefault(Guid id, CvDefaultInput input, CancellationToken ct) =>
        ToAction(await service.SetDefault(id, input, ct));
    [HttpDelete("cvs/{id:guid}")]
    public async Task<ActionResult<IReadOnlyList<ProfileCv>>> DeleteCv(Guid id, CancellationToken ct) =>
        ToAction(await service.DeleteCv(id, ct));
    [HttpGet]
    public async Task<ActionResult<CandidateProfileResponse>> Get(CancellationToken ct) =>
        ToAction(await service.Get(ct));
    [HttpPut]
    public async Task<ActionResult<CandidateProfileResponse>> Put(CandidateProfileUpdateRequest request, CancellationToken ct) =>
        ToAction(await service.Put(request, ct));
    [HttpPost("cvs")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<ProfileCv>> Upload(IFormFile file, CancellationToken ct) =>
        ToAction(await service.Upload(new UploadFile(file.Length, file.FileName, file.ContentType, file.CopyToAsync), ct));
    [HttpGet("cvs/{id:guid}")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct) =>
        ToAction(await service.Download(id, ct));
    [HttpPost("avatar")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<ActionResult<CandidateProfileResponse>> Avatar(IFormFile file, CancellationToken ct) =>
        ToAction(await service.Avatar(new UploadFile(file.Length, file.FileName, file.ContentType, file.CopyToAsync), ct));
    [HttpDelete("avatar")]
    public async Task<ActionResult<CandidateProfileResponse>> RemoveAvatar(CancellationToken ct) =>
        ToAction(await service.RemoveAvatar(ct));
}
