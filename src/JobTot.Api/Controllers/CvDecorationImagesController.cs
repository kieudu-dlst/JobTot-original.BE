using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using JobTot.Application.Cvs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobTot.Api.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CvDecorationImagesController(CvDecorationService service) : ControllerBase
{
    private Guid AccountId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [Authorize(Policy = CandidateAuthentication.Policy)]
    [HttpPost("api/candidate/online-cvs/{id:guid}/decoration-images")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public Task<CvDecorationImage> UploadCandidate(Guid id, [FromForm] UploadInput input, CancellationToken ct) =>
        Upload(id, false, input, ct);

    [Authorize(Policy = AdminAuthentication.Policy)]
    [HttpPost("api/admin/cv-templates/decoration-images")]
    [HttpPost("api/admin/cv-templates/{id:guid}/decoration-images")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public Task<CvDecorationImage> UploadTemplate(Guid? id, [FromForm] UploadInput input, CancellationToken ct) =>
        Upload(id, true, input, ct);

    private Task<CvDecorationImage> Upload(Guid? id, bool template, UploadInput input, CancellationToken ct) =>
        service.UploadAsync(AccountId, id, template,
            new UploadFile(input.File.Length, input.File.FileName, input.File.ContentType, input.File.CopyToAsync), input.CurrentCount, ct);

    [Authorize(Policy = CandidateAuthentication.Policy)]
    [HttpDelete("api/candidate/online-cvs/{id:guid}/decoration-images/{imageId:guid}")]
    public async Task<IActionResult> DeleteCandidate(Guid id, Guid imageId, CancellationToken ct)
    {
        await service.DeleteAsync(AccountId, id, false, imageId, ct);
        return NoContent();
    }
    [Authorize(Policy = AdminAuthentication.Policy)]
    [HttpDelete("api/admin/cv-templates/decoration-images/{imageId:guid}")]
    [HttpDelete("api/admin/cv-templates/{id:guid}/decoration-images/{imageId:guid}")]
    public async Task<IActionResult> DeleteTemplate(Guid? id, Guid imageId, CancellationToken ct)
    {
        await service.DeleteAsync(AccountId, id, true, imageId, ct);
        return NoContent();
    }
    public sealed class UploadInput
    {
        [Required]
        public IFormFile File { get; set; } = null!;
        [Range(0, 4, ErrorMessage = "Mỗi CV có tối đa 5 ảnh trang trí.")]
        public int CurrentCount
        {
            get; set;
        }
    }
}
