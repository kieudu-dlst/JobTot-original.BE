$ErrorActionPreference = 'Stop'
$backend = Split-Path -Parent $PSScriptRoot
$violations = @()

function Check-Sources([string] $folder, [string] $pattern, [string] $rule) {
    Get-ChildItem -LiteralPath (Join-Path $backend $folder) -Recurse -Filter '*.cs' |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
        ForEach-Object {
            if (Select-String -LiteralPath $_.FullName -Pattern $pattern -Quiet) {
                $script:violations += "$rule : $($_.FullName)"
            }
        }
}

Check-Sources 'src/JobTot.Api' 'Microsoft\.(EntityFrameworkCore|Data\.SqlClient)|JobTot\.Infrastructure\.Persistence|\bRecruitmentDbContext\b|\bSaveChanges(Async)?\s*\(' 'API must not access persistence'
Check-Sources 'src/JobTot.Api/Controllers' 'using (static )?JobTot\.Infrastructure|\bIRecruitmentData\b|\bIEntityCollection\b|\b(IAvatarStorage|ICompanyImageStorage)\b' 'Controllers must call application use cases'
Check-Sources 'src/JobTot.Application' 'Microsoft\.(AspNetCore|EntityFrameworkCore|Data\.SqlClient)|JobTot\.(Api|Infrastructure)|\b(IActionResult|ControllerBase|IFormFile|DbContext|DbSet|StatusCodes|HttpContext|ProblemDetails)\b|\b(Ok|BadRequest|StatusCode)\s*\(' 'Application must remain independent of HTTP and persistence adapters'
Check-Sources 'src/JobTot.Domain' 'using (Microsoft\.|JobTot\.(Api|Infrastructure|Application))' 'Domain must have no outward dependencies'

[xml] $applicationProject = Get-Content -LiteralPath (Join-Path $backend 'src/JobTot.Application/JobTot.Application.csproj')
foreach ($reference in $applicationProject.Project.ItemGroup.ProjectReference) {
    if ($reference -and $reference.Include -notmatch 'JobTot\.Domain[\\/]JobTot\.Domain\.csproj$') {
        $violations += "Unexpected Application reference: $($reference.Include)"
    }
}
if ($applicationProject.Project.ItemGroup.FrameworkReference -or $applicationProject.Project.ItemGroup.PackageReference) {
    $violations += 'Application must remain a plain .NET project with only the Domain reference.'
}
if ($violations.Count) { throw ($violations -join [Environment]::NewLine) }
Write-Output 'PASS Clean Architecture dependency boundaries'
