from pathlib import Path
from uuid import UUID, uuid5, NAMESPACE_URL
from html import escape
import json

ROOT = Path(__file__).resolve().parents[3]
NS = UUID('92667110-583c-4fda-81c0-c21e5803c3a7')
uid = lambda key: str(uuid5(NS, key))
quote = lambda value: "N'" + str(value).replace("'", "''") + "'"

catalog = json.loads((Path(__file__).parent / 'vietnam-provinces.json').read_text(encoding='utf-8'))
province_records = catalog['provinces']
assert len(province_records) == 34 and len({p['code'] for p in province_records}) == 34
assert all(len(p['code']) == 2 and p['code'].isdigit() for p in province_records)
assert sum(p['name'].startswith('Thành phố ') for p in province_records) == 6
provinces = [(p['code'], p['name'], 'Khu văn phòng Demo') for p in province_records]
industries = [
    ('Công nghệ thông tin', 'Công nghệ', ['Backend Developer (.NET)', 'Frontend Developer (React)', 'DevOps Engineer', 'QA Automation Engineer', 'Business Analyst']),
    ('Ngân hàng & Tài chính', 'Tài chính', ['Chuyên viên phân tích tài chính', 'Chuyên viên tín dụng', 'Chuyên viên quản trị rủi ro', 'Chuyên viên tư vấn khách hàng', 'Kế toán tổng hợp']),
    ('Thương mại điện tử', 'Thương mại', ['Chuyên viên vận hành sàn thương mại điện tử', 'Digital Marketing Specialist', 'Chuyên viên chăm sóc khách hàng', 'Chuyên viên phân tích dữ liệu', 'Quản lý ngành hàng']),
    ('Logistics & Chuỗi cung ứng', 'Logistics', ['Chuyên viên điều phối vận tải', 'Chuyên viên xuất nhập khẩu', 'Nhân viên quản lý kho', 'Chuyên viên mua hàng', 'Chuyên viên hoạch định chuỗi cung ứng']),
    ('Sản xuất & Kỹ thuật', 'Sản xuất', ['Kỹ sư tự động hóa', 'Kỹ sư cơ khí', 'Kỹ sư kiểm soát chất lượng', 'Kỹ sư bảo trì', 'Chuyên viên kế hoạch sản xuất']),
    ('Giáo dục & Đào tạo', 'Giáo dục', ['Giáo viên tiếng Anh', 'Chuyên viên phát triển học liệu', 'Chuyên viên tư vấn tuyển sinh', 'Điều phối viên đào tạo', 'Chuyên viên công nghệ giáo dục']),
    ('Y tế & Chăm sóc sức khỏe', 'Sức khỏe', ['Điều dưỡng viên', 'Dược sĩ', 'Chuyên viên vận hành dịch vụ y tế', 'Kỹ thuật viên xét nghiệm', 'Chuyên viên chăm sóc khách hàng y tế']),
    ('Du lịch & Khách sạn', 'Du lịch', ['Chuyên viên điều hành tour', 'Nhân viên lễ tân', 'Chuyên viên kinh doanh du lịch', 'Quản lý dịch vụ khách hàng', 'Chuyên viên marketing du lịch']),
    ('Xây dựng & Bất động sản', 'Xây dựng', ['Kỹ sư xây dựng', 'Kiến trúc sư', 'Chuyên viên quản lý dự án', 'Kỹ sư dự toán', 'Chuyên viên tư vấn bất động sản']),
    ('Truyền thông & Thiết kế', 'Sáng tạo', ['Graphic Designer', 'Content Marketing Specialist', 'UI/UX Designer', 'Chuyên viên truyền thông', 'Video Editor']),
]
brands = ['Bình Minh', 'Hải Đăng', 'Sao Mai', 'Thanh Hà', 'An Phú', 'Vạn An', 'Thiên Hà', 'Minh Sơn', 'Tân Việt', 'Đông Phong']
palettes = [('#1675b9', '#daf1ff'), ('#1b9373', '#e4f8ef'), ('#fa6533', '#fff0e4'), ('#4b7c8d', '#e5f3f5'), ('#c78333', '#fcf0dc'), ('#8554bd', '#f0e8fc'), ('#168b96', '#ddf5f4'), ('#bc657d', '#fce9ef'), ('#667644', '#f0f3dd'), ('#7462cc', '#ede9ff')]
assets = ROOT / 'BE/JobTot/src/JobTot.Api/wwwroot/uploads/companies'
def asset_path(name):
    folder = 'logos' if name.startswith('logo-') else 'covers'
    path = assets / folder / (uuid5(NAMESPACE_URL, 'jobtot:company-image:' + name).hex + '.svg')
    path.parent.mkdir(parents=True, exist_ok=True)
    return path
def asset_url(name):
    return '/uploads/companies/' + asset_path(name).relative_to(assets).as_posix()
assets.mkdir(parents=True, exist_ok=True)

for index, (name, _, _) in enumerate(industries):
    color, pale = palettes[index]
    windows = ''.join(f'<rect x="{x}" y="{y}" width="14" height="22" rx="2" fill="{pale}" opacity=".9"/>' for x in range(140, 430, 28) for y in [65, 100, 135])
    svg = f'''<svg xmlns="http://www.w3.org/2000/svg" width="640" height="300" viewBox="0 0 640 300"><rect width="640" height="300" fill="{pale}"/><circle cx="565" cy="55" r="105" fill="{color}" opacity=".06"/><path d="M0 260 Q180 185 330 240 T640 225 V300 H0" fill="{color}" opacity=".12"/><rect x="120" y="42" width="338" height="166" rx="8" fill="{color}" opacity=".72"/>{windows}<rect x="470" y="109" width="105" height="99" rx="7" fill="{color}" opacity=".3"/><path d="M80 211H588" stroke="{color}" stroke-width="4" opacity=".4"/><circle cx="79" cy="166" r="28" fill="{color}" opacity=".3"/><path d="M79 179V214" stroke="{color}" stroke-width="6" opacity=".4"/><text x="28" y="270" font-family="Arial,sans-serif" font-size="20" fill="{color}">{escape(name)}</text><text x="555" y="277" font-family="Arial,sans-serif" font-size="12" fill="{color}">DEMO</text></svg>'''
    asset_path(f'cover-{index+1:02}.svg').write_text(svg, encoding='utf-8')

industry_rows = []
province_rows = []
media_rows = []
company_rows = []
job_rows = []
job_detail_rows = []
for index, (name, _, _) in enumerate(industries):
    industry_rows.append(f"({index}, '{uid(f'industry:{index}')}', {quote(name)})")
    media_rows.append(f"('{uid(f'cover:{index}')}', N'{asset_url(f'cover-{index+1:02}.svg')}', N'CompanyCover')")
for index, (code, name, _) in enumerate(provinces):
    legacy = province_records[index].get('legacyCode')
    short_name = name.removeprefix('Thành phố ').removeprefix('Tỉnh ')
    province_id = uid(f'province:{legacy or code}')
    province_rows.append(f"({index}, '{province_id}', {quote(code)}, {quote(name)}, {quote(legacy) if legacy else 'NULL'}, {quote(short_name)})")

for index in range(100):
    sector = index % 10
    brand = brands[index // 10]
    province_index = (index * 3 + index // 10) % len(provinces)
    _, province, district = provinces[province_index]
    sector_name, prefix, roles = industries[sector]
    company_name = f'Công ty {prefix} {brand} (Demo)'
    company_id = uid(f'company:{index+1}')
    logo_id = uid(f'logo:{index+1}')
    color, pale = palettes[sector]
    initials = ''.join(word[0] for word in brand.split()).upper()
    asset_path(f'logo-{index+1:03}.svg').write_text(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 120 120"><rect width="120" height="120" rx="22" fill="{pale}"/><text x="60" y="70" text-anchor="middle" font-family="Arial,sans-serif" font-size="43" font-weight="700" fill="{color}">{initials}</text><text x="60" y="99" text-anchor="middle" font-family="Arial,sans-serif" font-size="10" letter-spacing="2" fill="{color}">DEMO {index+1:03}</text></svg>', encoding='utf-8')
    media_rows.append(f"('{logo_id}', N'{asset_url(f'logo-{index+1:03}.svg')}', N'CompanyLogo')")
    size_min, size_max = [(15, 45), (60, 180), (220, 450), (550, 950), (1200, 3500)][(index // 2) % 5]
    description = f'Dữ liệu minh họa phục vụ phát triển và kiểm thử Job Tốt. {company_name} là doanh nghiệp giả lập trong lĩnh vực {sector_name.lower()}, có trụ sở tại {province}. Không phải thông tin tuyển dụng thực tế.'
    culture = 'Môi trường hợp tác, đào tạo chuyên môn, đánh giá năng lực định kỳ và hỗ trợ phát triển sự nghiệp. Toàn bộ thông tin là dữ liệu demo.'
    values = [f"'{company_id}'", quote(company_name), quote(description), str(sector), str(province_index), quote(f'{20+index} đường Minh Khai, {district}'), str(size_min), str(size_max), f"'{logo_id}'", f"'{uid(f'cover:{sector}')}'", quote(f'demo-company-{index+1:03}'), quote(f'careers-{index+1:03}@example.invalid'), str(2000 + index % 24), quote(culture), quote('Pending' if index % 5 == 0 else 'Verified')]
    company_rows.append('(' + ', '.join(values) + ')')
    for role_index, role in enumerate(roles):
        job_id = uid(f'job:{index+1}:{role_index+1}')
        salary_min = (10 + sector + role_index * 3 + (index // 10) % 4) * 1000000
        salary_max = salary_min + (6 + role_index % 3) * 1000000
        job_description = f'''VỊ TRÍ DEMO — KHÔNG PHẢI TIN TUYỂN DỤNG THỰC TẾ

Doanh nghiệp: {company_name}
Vị trí: {role}
Địa điểm: {district}, {province}
Hình thức: {'Hybrid' if sector in [0, 2, 9] and role_index % 2 == 0 else 'Làm việc tại văn phòng'} · Toàn thời gian

MÔ TẢ CÔNG VIỆC
- Thực hiện các nhiệm vụ chuyên môn của vị trí {role} trong lĩnh vực {sector_name.lower()}.
- Phối hợp với các bộ phận liên quan để triển khai dự án và cải tiến quy trình.
- Theo dõi chất lượng công việc, báo cáo kết quả và đề xuất giải pháp.

YÊU CẦU
- Kinh nghiệm từ {1 + role_index % 3} năm trong lĩnh vực liên quan.
- Có kỹ năng giao tiếp, làm việc nhóm và chủ động giải quyết vấn đề.
- Thành thạo các công cụ chuyên môn và có tinh thần học hỏi.

QUYỀN LỢI MINH HỌA
- Thu nhập {salary_min//1000000}–{salary_max//1000000} triệu VND/tháng tùy năng lực.
- Bảo hiểm theo quy định, nghỉ phép và hoạt động đào tạo nội bộ.
- Lộ trình phát triển nghề nghiệp và đánh giá hiệu quả định kỳ.

Đây là dữ liệu thử nghiệm. Không gửi hồ sơ hoặc thông tin cá nhân đến địa chỉ demo.'''
        job_rows.append(f"('{job_id}', '{company_id}', {quote(role)}, {quote(job_description)}, {quote(province)}, {salary_min}, {salary_max}, {index % 14 + role_index}, {60 + index % 30})")
        employment = 'flexible' if sector in [0, 2, 9] and role_index % 2 == 0 else 'full-time'
        position = 'manager' if 'Quản lý' in role else 'employee'
        job_detail_rows.append(f"('{job_id}', N'1-3', {quote(position)}, {quote(employment)})")

join_rows = lambda rows: ',\n    '.join(rows)
province_sql = f'''    -- Official 34-province catalog; preserve existing IDs referenced by company/profile/campaign records.
    DECLARE @ProvinceLock int;
    EXEC @ProvinceLock = sp_getapplock @Resource = N'JobTot.ProvinceCatalog2025.v1', @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 10000;
    IF @ProvinceLock < 0 THROW 51010, 'Could not acquire the province-catalog lock.', 1;
    DECLARE @Provinces TABLE (SeedKey int PRIMARY KEY, Id uniqueidentifier NOT NULL, Code nvarchar(2) NOT NULL,
        Name nvarchar(200) NOT NULL, LegacyCode nvarchar(32), ShortName nvarchar(200));
    INSERT @Provinces VALUES
    {join_rows(province_rows)};
    IF EXISTS (SELECT seed.SeedKey FROM @Provinces seed JOIN dbo.Province actual
        ON actual.ProvinceCode = seed.Code OR actual.ProvinceCode = seed.LegacyCode
        OR actual.ProvinceName = seed.Name OR actual.ProvinceName = seed.ShortName
        GROUP BY seed.SeedKey HAVING COUNT(*) > 1)
        THROW 51011, 'Duplicate province matches must be reconciled before importing the catalog.', 1;
    UPDATE seed SET Id = existing.Id FROM @Provinces seed CROSS APPLY
        (SELECT TOP (1) Id FROM dbo.Province WHERE ProvinceCode = seed.Code OR ProvinceCode = seed.LegacyCode
            OR ProvinceName = seed.Name OR ProvinceName = seed.ShortName ORDER BY Id) existing;
    UPDATE actual SET ProvinceCode = seed.Code, ProvinceName = seed.Name, Status = 1, UpdatedDate = @Now
    FROM dbo.Province actual JOIN @Provinces seed ON actual.Id = seed.Id
    WHERE actual.ProvinceCode <> seed.Code OR actual.ProvinceName <> seed.Name OR actual.Status <> 1;
    DECLARE @UpdatedProvinces int = @@ROWCOUNT;
    INSERT dbo.Province (Id, ProvinceCode, ProvinceName, Status, CreatedDate)
    SELECT Id, Code, Name, 1, @Now FROM @Provinces seed WHERE NOT EXISTS (SELECT 1 FROM dbo.Province actual WHERE actual.Id = seed.Id);
    DECLARE @InsertedProvinces int = @@ROWCOUNT;
    IF (SELECT COUNT(*) FROM dbo.Province actual JOIN @Provinces seed ON actual.Id = seed.Id
        WHERE actual.ProvinceCode = seed.Code AND actual.ProvinceName = seed.Name AND actual.Status = 1) <> 34
        THROW 51012, 'Expected 34 active provinces with official names and codes.', 1;
'''
province_standalone = f'''-- Official province catalog, Decision 19/2025/QD-TTg, effective 2025-07-01.
-- Source: {catalog['source']}
-- Catalog: {catalog['catalogSource']}
-- Updates the eight old demo codes in place and inserts missing provinces; no records or foreign keys are deleted.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() <> N'JobTot' THROW 51000, 'This seed is intended for the JobTot database.', 1;
DECLARE @Now datetimeoffset = TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00');
BEGIN TRY
    BEGIN TRANSACTION;
{province_sql}
    COMMIT TRANSACTION;
    SELECT @InsertedProvinces AS InsertedProvinces, @UpdatedProvinces AS UpdatedProvinces,
        (SELECT COUNT(*) FROM dbo.Province actual JOIN @Provinces seed ON actual.Id = seed.Id WHERE actual.Status = 1) AS OfficialProvinces;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
'''
(Path(__file__).parent / 'seed-vietnam-provinces.sql').write_text(province_standalone, encoding='utf-8')
sql = f'''-- JobTot development fixture: 100 fictional companies and 500 linked job posts.
-- Generated IDs are deterministic. Re-running inserts missing rows only.
-- No existing accounts, companies, jobs, or application data are deleted or updated.
-- Run from BE/JobTot with sqlcmd -S '.\\SQLEXPRESS' -d JobTot -E -C -b -f 65001 -i scripts/seed-company-directory.sql
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() <> N'JobTot' THROW 51000, 'This development seed is intended for the JobTot database.', 1;
DECLARE @Now datetimeoffset = TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00');
DECLARE @AddedCompanies int = 0, @AddedJobs int = 0;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @LockResult int;
    EXEC @LockResult = sp_getapplock @Resource = N'JobTot.CompanyDirectoryDemoSeed.v1', @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 10000;
    IF @LockResult < 0 THROW 51001, 'Could not acquire the demo-seed lock.', 1;

    DECLARE @Industries TABLE (SeedKey int PRIMARY KEY, Id uniqueidentifier NOT NULL, Name nvarchar(200) NOT NULL);
    INSERT @Industries VALUES
    {join_rows(industry_rows)};
    UPDATE seed SET Id = existing.Id FROM @Industries seed CROSS APPLY
        (SELECT TOP (1) Id FROM dbo.Industry WHERE IndustryName = seed.Name ORDER BY Id) existing;
    INSERT dbo.Industry (Id, IndustryName, Status, CreatedDate)
    SELECT Id, Name, 1, @Now FROM @Industries seed WHERE NOT EXISTS (SELECT 1 FROM dbo.Industry actual WHERE actual.Id = seed.Id);

{province_sql}

    DECLARE @Images TABLE (Id uniqueidentifier PRIMARY KEY, Url nvarchar(2048), Type nvarchar(200));
    INSERT @Images VALUES
    {join_rows(media_rows)};
    INSERT dbo.Media (Id, Url, Type, Status, CreatedDate)
    SELECT Id, Url, Type, 1, @Now FROM @Images seed WHERE NOT EXISTS (SELECT 1 FROM dbo.Media actual WHERE actual.Id = seed.Id);

    DECLARE @Companies TABLE (Id uniqueidentifier PRIMARY KEY, Name nvarchar(200), About nvarchar(4000), IndustryKey int, ProvinceKey int,
        Address nvarchar(200), SizeMin int, SizeMax int, LogoId uniqueidentifier, CoverId uniqueidentifier, Slug nvarchar(200),
        Email nvarchar(320), FoundedYear int, Culture nvarchar(max), Verification nvarchar(200));
    INSERT @Companies VALUES
    {join_rows(company_rows)};
    INSERT dbo.Companies (Id, DisplayName, LegalName, About, IndustryId, HeadOfficeProvinceId, AddressLine, CompanySizeMin, CompanySizeMax,
        LogoFileId, CoverFileId, Slug, Email, FoundedYear, Culture, VerificationStatus, Status, CreatedDate)
    SELECT seed.Id, seed.Name, seed.Name, seed.About, industry.Id, province.Id, seed.Address, seed.SizeMin, seed.SizeMax,
        seed.LogoId, seed.CoverId, seed.Slug, seed.Email, seed.FoundedYear, seed.Culture, seed.Verification, 1, @Now
    FROM @Companies seed JOIN @Industries industry ON industry.SeedKey = seed.IndustryKey JOIN @Provinces province ON province.SeedKey = seed.ProvinceKey
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Companies actual WHERE actual.Id = seed.Id);
    SET @AddedCompanies = @@ROWCOUNT;

    DECLARE @Jobs TABLE (Id uniqueidentifier PRIMARY KEY, CompanyId uniqueidentifier NOT NULL, Title nvarchar(200), Description nvarchar(max),
        Location nvarchar(200), SalaryMin decimal(18,2), SalaryMax decimal(18,2), AgeDays int, ExpiryDays int);
    INSERT @Jobs VALUES
    {join_rows(job_rows)};
    INSERT dbo.Jobs (Id, CompanyId, Title, Description, Location, ProvinceId, SalaryMin, SalaryMax, CreatedAt, ExpiresAt, IsClosed)
    SELECT seed.Id, CompanyId, Title, Description, Location, province.Id, SalaryMin, SalaryMax, DATEADD(day, -AgeDays, @Now), DATEADD(day, ExpiryDays, @Now), 0
    FROM @Jobs seed LEFT JOIN @Provinces province ON province.Name = seed.Location
        OR province.Name = N'Thành phố ' + seed.Location OR province.Name = N'Tỉnh ' + seed.Location
        OR (seed.Location LIKE N'TP. %' AND province.Name = N'Thành phố ' + SUBSTRING(seed.Location, 5, 200))
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Jobs actual WHERE actual.Id = seed.Id);
    SET @AddedJobs = @@ROWCOUNT;

    IF (SELECT COUNT(*) FROM dbo.Companies c JOIN @Companies s ON c.Id = s.Id) <> 100 THROW 51002, 'Expected 100 seeded companies.', 1;
    IF (SELECT COUNT(*) FROM dbo.Jobs j JOIN @Jobs s ON j.Id = s.Id) <> 500 THROW 51003, 'Expected 500 seeded job posts.', 1;
    IF EXISTS (SELECT 1 FROM @Companies c LEFT JOIN dbo.Jobs j ON j.CompanyId = c.Id GROUP BY c.Id HAVING COUNT(j.Id) < 5)
        THROW 51004, 'Each demo company must have at least five jobs.', 1;
    COMMIT TRANSACTION;
    SELECT @AddedCompanies AS InsertedCompanies, @AddedJobs AS InsertedJobs,
        (SELECT COUNT(*) FROM dbo.Companies c JOIN @Companies s ON c.Id = s.Id) AS SeededCompanies,
        (SELECT COUNT(*) FROM dbo.Jobs j JOIN @Jobs s ON j.Id = s.Id) AS SeededJobs;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
'''
(ROOT / 'BE/JobTot/scripts/seed-company-directory.sql').write_text(sql, encoding='utf-8')
details_sql = f'''-- Enrich only the original deterministic JobTot demo jobs with missing directory fields.
-- Requires AddJobDirectoryDetails. Does not overwrite existing details or change salaries, dates or descriptions.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() <> N'JobTot' THROW 51000, 'This seed is intended for the JobTot database.', 1;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @Details TABLE (Id uniqueidentifier PRIMARY KEY, ExperienceLevel nvarchar(20), PositionLevel nvarchar(20), EmploymentType nvarchar(20));
    INSERT @Details VALUES
    {join_rows(job_detail_rows)};
    UPDATE job SET ExperienceLevel = seed.ExperienceLevel, PositionLevel = seed.PositionLevel,
        EmploymentType = seed.EmploymentType, WorkSchedule = N'Giờ hành chính',
        Benefits = N'Bảo hiểm theo quy định|Đào tạo nội bộ'
    FROM dbo.Jobs job JOIN @Details seed ON seed.Id = job.Id
    JOIN dbo.Companies company ON company.Id = job.CompanyId
    WHERE company.DisplayName LIKE N'%(Demo)' AND job.Description LIKE N'VỊ TRÍ DEMO%'
        AND job.ExperienceLevel IS NULL AND job.PositionLevel IS NULL AND job.EmploymentType IS NULL
        AND job.WorkSchedule IS NULL AND job.Benefits IS NULL;
    DECLARE @UpdatedJobs int = @@ROWCOUNT;
    COMMIT TRANSACTION;
    SELECT @UpdatedJobs AS EnrichedDemoJobs;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
'''
(ROOT / 'BE/JobTot/scripts/seed-job-directory-details.sql').write_text(details_sql, encoding='utf-8')
candidate_details_sql = f'''-- Optional candidate-detail fixture for the original fictional demo jobs only.
-- Requires AddCandidateJobDetailsAndSubmissions. Fills empty fields without changing existing descriptions.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() <> N'JobTot' THROW 51000, 'This seed is intended for the JobTot database.', 1;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @Demo TABLE (Id uniqueidentifier PRIMARY KEY, ExperienceLevel nvarchar(20), PositionLevel nvarchar(20), EmploymentType nvarchar(20));
    INSERT @Demo VALUES
    {join_rows(job_detail_rows)};
    UPDATE job SET Requirements = COALESCE(job.Requirements,
        SUBSTRING(job.Description, CHARINDEX(N'YÊU CẦU', job.Description) + LEN(N'YÊU CẦU'),
            CHARINDEX(N'QUYỀN LỢI MINH HỌA', job.Description) - CHARINDEX(N'YÊU CẦU', job.Description) - LEN(N'YÊU CẦU'))),
        HiringProcess = COALESCE(job.HiringProcess, N'Sàng lọc hồ sơ (Demo)|Đánh giá CV và kinh nghiệm phù hợp.' + CHAR(10)
            + N'Trao đổi với bộ phận tuyển dụng (Demo)|Tìm hiểu định hướng nghề nghiệp và mong muốn của ứng viên.' + CHAR(10)
            + N'Phỏng vấn chuyên môn (Demo)|Trao đổi về kinh nghiệm, kỹ năng và công việc thực tế.' + CHAR(10)
            + N'Trao đổi với quản lý (Demo)|Thảo luận mục tiêu và cách phối hợp trong đội ngũ.' + CHAR(10)
            + N'Thông báo kết quả (Demo)|Trao đổi về đề nghị công việc nếu phù hợp.'),
        Vacancies = COALESCE(job.Vacancies, 2)
    FROM dbo.Jobs job JOIN @Demo seed ON seed.Id = job.Id JOIN dbo.Companies company ON company.Id = job.CompanyId
    WHERE company.DisplayName LIKE N'%(Demo)' AND job.Description LIKE N'VỊ TRÍ DEMO%'
        AND CHARINDEX(N'YÊU CẦU', job.Description) > 0
        AND CHARINDEX(N'QUYỀN LỢI MINH HỌA', job.Description) > CHARINDEX(N'YÊU CẦU', job.Description)
        AND (job.Requirements IS NULL OR job.HiringProcess IS NULL OR job.Vacancies IS NULL);
    DECLARE @UpdatedJobs int = @@ROWCOUNT;
    COMMIT TRANSACTION;
    SELECT @UpdatedJobs AS EnrichedDemoCandidateDetails;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
'''
(ROOT / 'BE/JobTot/scripts/seed-job-candidate-details.sql').write_text(candidate_details_sql, encoding='utf-8')
print(f'Generated 100 companies, 500 jobs, 10 industries, 34 official provinces, 110 media records; SQL file: {len(sql):,} characters.')
