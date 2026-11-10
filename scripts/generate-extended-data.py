"""Build a non-destructive supplement from the user's backup and runtime demo data."""
from pathlib import Path
import hashlib
import json
import re
import uuid

root = Path(__file__).resolve().parents[1]
backup = root / 'database/demo-data.sql'
text = backup.read_text(encoding='utf-16')
preview_sql = (root / 'database/preview-data.sql').read_text(encoding='utf-8-sig')
datasets = [
    {'key': key.replace("''", "'"), 'payload': json.loads(payload.replace("''", "'"))}
    for key, payload in re.findall(
        r"INSERT INTO dbo\.DemoDatasets \(\[Key\],Payload\) VALUES \(N'((?:[^']|'')*)',N'((?:[^']|'')*)'\);",
        preview_sql,
    )
]
if not datasets:
    raise ValueError('No preview datasets found in database/preview-data.sql')
tables = set(re.findall(r'CREATE TABLE \[dbo\]\.\[([^\]]+)\]', text))
namespace = uuid.UUID('b732ef27-8f99-4544-927d-22df82bb5b4f')

def uid(key):
    return str(uuid.uuid5(namespace, key))

def quote(value):
    if value is None:
        return 'NULL'
    if isinstance(value, bool):
        return '1' if value else '0'
    if isinstance(value, (int, float)):
        return str(value)
    return "N'" + str(value).replace("'", "''") + "'"

def js(value):
    return json.dumps(value, ensure_ascii=False, separators=(',', ':'))

class Raw(str):
    pass

out = [f"""-- JobTot supplemental demo data. Generated from demo-data.sql + preview-data.sql.
-- Backup SHA256: {hashlib.sha256(backup.read_bytes()).hexdigest()}
-- Run AFTER restoring demo-data.sql. This is NOT a full database backup.
-- No existing accounts, credentials, CVs, jobs, companies or sessions are replaced.
-- Demo accounts have no password; they cannot authenticate until explicitly provisioned.
-- Dates are relative to execution time (UTC+07:00). Re-runs preserve existing dates/edits.
USE [JobTot];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO
"""]

# A connection-scoped helper recursively adds missing object fields and array items.
# Existing array records are matched by id, and edited values are never replaced.
out.append("""CREATE PROCEDURE #MergeDemoJson
    @Current nvarchar(max), @Incoming nvarchar(max), @Result nvarchar(max) OUTPUT,
    @Depth int = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET @Result = @Current;
    IF @Depth > 24 THROW 51001, 'Demo JSON nesting exceeds 24 levels.', 1;
    DECLARE @IsArray bit = CASE WHEN LEFT(LTRIM(@Current),1)=N'[' THEN 1 ELSE 0 END;
    IF LEFT(LTRIM(@Current),1)<>LEFT(LTRIM(@Incoming),1) RETURN;
    DECLARE @Items TABLE (Ordinal int IDENTITY, ItemKey nvarchar(4000), ItemValue nvarchar(max), ItemType int);
    INSERT @Items(ItemKey,ItemValue,ItemType) SELECT [key],[value],[type] FROM OPENJSON(@Incoming);
    DECLARE @i int=1,@count int=@@ROWCOUNT,@key nvarchar(4000),@value nvarchar(max),@type int,
        @path nvarchar(4000),@old nvarchar(max),@oldType int,@child nvarchar(max),@index int,@nextDepth int=@Depth+1;
    WHILE @i<=@count
    BEGIN
        SELECT @key=ItemKey,@value=ItemValue,@type=ItemType FROM @Items WHERE Ordinal=@i;
        SET @index=NULL; SET @old=NULL; SET @oldType=NULL;
        IF @IsArray=1
        BEGIN
            IF @type=5 AND JSON_VALUE(@value,'$.id') IS NOT NULL
                SELECT TOP(1) @index=TRY_CONVERT(int,[key]),@old=[value],@oldType=[type]
                FROM OPENJSON(@Result) WHERE [type]=5 AND JSON_VALUE([value],'$.id')=JSON_VALUE(@value,'$.id');
            ELSE IF @type=5 AND JSON_VALUE(@value,'$.title') IS NOT NULL
                SELECT TOP(1) @index=TRY_CONVERT(int,[key]),@old=[value],@oldType=[type]
                FROM OPENJSON(@Result) WHERE [type]=5 AND JSON_VALUE([value],'$.title')=JSON_VALUE(@value,'$.title')
                    AND ISNULL(JSON_VALUE([value],'$.organization'),N'')=ISNULL(JSON_VALUE(@value,'$.organization'),N'')
                    AND ISNULL(JSON_VALUE([value],'$.period'),N'')=ISNULL(JSON_VALUE(@value,'$.period'),N'');
            ELSE IF @type=5 AND JSON_VALUE(@value,'$.name') IS NOT NULL
                SELECT TOP(1) @index=TRY_CONVERT(int,[key]),@old=[value],@oldType=[type]
                FROM OPENJSON(@Result) WHERE [type]=5 AND JSON_VALUE([value],'$.name')=JSON_VALUE(@value,'$.name');
            ELSE
                SELECT TOP(1) @index=TRY_CONVERT(int,[key]),@old=[value],@oldType=[type]
                FROM OPENJSON(@Result) WHERE [type]=@type AND ([value]=@value OR ([value] IS NULL AND @value IS NULL));
            SET @path=CASE WHEN @index IS NULL THEN N'append $' ELSE N'$['+CONVERT(nvarchar(20),@index)+N']' END;
        END
        ELSE
        BEGIN
            SELECT @old=[value],@oldType=[type] FROM OPENJSON(@Result) WHERE [key]=@key COLLATE Latin1_General_100_BIN2;
            SET @path=N'$."'+STRING_ESCAPE(@key,'json')+N'"';
        END;
        IF @oldType IS NULL
        BEGIN
            IF @type IN (4,5) SET @Result=JSON_MODIFY(@Result,@path,JSON_QUERY(@value));
            ELSE IF @type=1 SET @Result=JSON_MODIFY(@Result,@path,@value);
            ELSE IF @type=2 SET @Result=JSON_MODIFY(@Result,@path,CONVERT(decimal(38,10),@value));
            ELSE IF @type=3 SET @Result=JSON_MODIFY(@Result,@path,CONVERT(bit,CASE WHEN @value=N'true' THEN 1 ELSE 0 END));
        END
        ELSE IF @type IN (4,5) AND @oldType=@type
        BEGIN
            EXEC #MergeDemoJson @old,@value,@child OUTPUT,@nextDepth;
            SET @Result=JSON_MODIFY(@Result,@path,JSON_QUERY(@child));
        END;
        SET @i+=1;
    END;
END;
GO
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @Now datetimeoffset(7)=TODATETIMEOFFSET(SYSUTCDATETIME(),'+00:00');
    DECLARE @Today date=CONVERT(date,SWITCHOFFSET(@Now,'+07:00'));
""")

def emit(sql):
    out.append('    ' + sql + '\n')

def seed(table, key, values, natural=None):
    """Return a SQL variable resolving an existing natural key or a deterministic UUID."""
    assert table in tables, table
    var = '@v' + str(seed.counter)
    seed.counter += 1
    emit(f'DECLARE {var} uniqueidentifier;')
    if natural:
        where = ' AND '.join(f'[{k}]={v if isinstance(v, Raw) else quote(v)}' for k,v in natural.items())
        emit(f'SELECT TOP(1) {var}=[Id] FROM dbo.[{table}] WITH (UPDLOCK,HOLDLOCK) WHERE {where} ORDER BY [Id];')
    emit(f'SET {var}=COALESCE({var},{quote(uid(key))});')
    values = {'Id': Raw(var), **values, 'Status': 1, 'CreatedDate': Raw('@Now')}
    columns = ','.join('['+k+']' for k in values)
    literals = ','.join(v if isinstance(v, Raw) else quote(v) for v in values.values())
    emit(f'IF NOT EXISTS (SELECT 1 FROM dbo.[{table}] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={var}) INSERT dbo.[{table}] ({columns}) VALUES ({literals});')
    return Raw(var)

seed.counter=0
emit("IF OBJECT_ID(N'dbo.Companies',N'U') IS NULL OR OBJECT_ID(N'dbo.CvTemplate',N'U') IS NULL THROW 51002,'Restore demo-data.sql before running extened.sql.',1;")
emit("IF OBJECT_ID(N'dbo.DemoDatasets',N'U') IS NULL CREATE TABLE dbo.DemoDatasets ([Key] nvarchar(100) NOT NULL PRIMARY KEY,Payload nvarchar(max) NOT NULL CHECK(ISJSON(Payload)=1));")
emit('DECLARE @Incoming nvarchar(max),@Current nvarchar(max),@Merged nvarchar(max);')
for dataset in datasets:
    key=quote(dataset['key'])
    emit(f'SET @Incoming={quote(js(dataset["payload"]))}; SET @Current=NULL;')
    emit(f'SELECT @Current=Payload FROM dbo.DemoDatasets WITH (UPDLOCK,HOLDLOCK) WHERE [Key]={key};')
    emit(f'IF @Current IS NULL INSERT dbo.DemoDatasets([Key],Payload) VALUES ({key},@Incoming); ELSE BEGIN EXEC #MergeDemoJson @Current,@Incoming,@Merged OUTPUT; IF @Merged<>@Current UPDATE dbo.DemoDatasets SET Payload=@Merged WHERE [Key]={key}; END;')

# Reference data is resolved from the current restored database, rather than copying IDs.
emit("DECLARE @Province uniqueidentifier=(SELECT TOP(1) Id FROM dbo.Province WHERE Status=1 AND ProvinceName LIKE N'%Hà Nội%' ORDER BY Id);")
emit("DECLARE @Industry uniqueidentifier=(SELECT TOP(1) Id FROM dbo.Industry WHERE Status=1 AND (IndustryName LIKE N'%Công nghệ%' OR IndustryName LIKE N'%IT%') ORDER BY Id);")
emit("SET @Industry=COALESCE(@Industry,(SELECT TOP(1) Id FROM dbo.Industry WHERE Status=1 ORDER BY Id));")
emit("SET @Province=COALESCE(@Province,(SELECT TOP(1) Id FROM dbo.Province WHERE Status=1 ORDER BY Id));")
emit("IF @Industry IS NULL OR @Province IS NULL THROW 51003,'Active Industry and Province records are required.',1;")
employment=seed('EmploymentType','employment:full-time',{'TypeName':'Toàn thời gian'}, {'TypeName':'Toàn thời gian'})
profile=next(x['payload'] for x in datasets if x['key']=='profile')
candidate_chat=next(x['payload'] for x in datasets if x['key']=='messages-candidate')
employer_chat=next(x['payload'] for x in datasets if x['key']=='messages-employer')
skills=[seed('Skill','skill:'+name,{'SkillName':name},{'SkillName':name}) for name in ['Kubernetes','Docker','Terraform','AWS Cloud','Python','CI/CD','Golang']]
benefits=[seed('Benefit','benefit:'+name,{'BenefitName':name,'Detail':detail},{'BenefitName':name}) for name,detail in [
    ('Thưởng hiệu quả công việc (Demo)','Thưởng theo kết quả công việc và hiệu quả dự án.'),
    ('Bảo hiểm sức khỏe (Demo)','Chăm sóc sức khỏe bổ sung cho nhân viên.'),
    ('Làm việc kết hợp (Demo)','Linh hoạt kết hợp làm việc tại văn phòng và từ xa.')]]
people=[x['name'] for x in employer_chat]+[profile['fullName']]
candidates=[]
cvs=[]
for i,name in enumerate(people):
    email=f'candidate-{i+1}@demo.jobtot.example.invalid'
    account=seed('Account','candidate-account:'+str(i),{'Email':email,'FullName':name,'AccountType':'Candidate','AccountStatus':'Active','PasswordHash':None},{'Email':email})
    details={**profile,'fullName':name,'headline':employer_chat[i%4]['job'],'email':email,'phone':'','views':0,'invitations':0,'visitors':[]}
    candidate=seed('CandidateProfile','candidate-profile:'+str(i),{'AccountId':account,'ProvinceId':Raw('@Province'),'Headline':details['headline'],'YearsExperience':5+i,'ExpectedSalaryMin':35000000,'ExpectedSalaryMax':55000000,'ReadyStatus':'looking','Summary':profile['summary'],'DetailsJson':js(details)},{'AccountId':account})
    candidates.append(candidate)
    cv=seed('CandidateCv','candidate-cv:'+str(i),{'CandidateId':candidate,'Title':'CV '+name+' (Demo)','ContentJson':js(details),'IsDefault':True,'DocumentVersion':uid('cv-version:'+str(i))})
    cvs.append(cv)
    for j,skill in enumerate(skills):
        seed('CandidateSkill',f'candidate-skill:{i}:{j}',{'CandidateId':candidate,'SkillId':skill,'Proficiency':4},{'CandidateId':candidate,'SkillId':skill})

for i,conversation in enumerate(candidate_chat):
    name=conversation['company']
    email=f'company-{i+1}@demo.jobtot.example.invalid'
    company=seed('Companies','company:'+name,{'DisplayName':name+' (Demo)','About':'Dữ liệu minh họa từ giao diện Job Tốt; không phải thông tin tuyển dụng thực tế.','Email':email,'AddressLine':conversation['location'],'CompanySizeMin':500,'CompanySizeMax':1000,'HeadOfficeProvinceId':Raw('@Province'),'IndustryId':Raw('@Industry'),'VerificationStatus':'Unverified','Slug':'jobtot-demo-company-'+str(i+1)}, {'Email':email})
    seed('CompanyIndustry','company-industry:'+str(i),{'CompanyId':company,'IndustryId':Raw('@Industry')},{'CompanyId':company,'IndustryId':Raw('@Industry')})
    hr_email=f'hr-{i+1}@demo.jobtot.example.invalid'
    hr=seed('Account','hr-account:'+str(i),{'Email':hr_email,'FullName':conversation['name'],'AccountType':'Employer','AccountStatus':'Active','PasswordHash':None},{'Email':hr_email})
    recruiter=seed('RecruiterProfile','recruiter:'+str(i),{'AccountId':hr,'CompanyId':company,'JobTitle':conversation['subtitle'],'IsCompanyAdmin':True,'JoinedAt':Raw('@Now')},{'AccountId':hr,'CompanyId':company})
    campaign=seed('RecruitmentCampaign','campaign:'+str(i),{'CompanyId':company,'OwnerRecruiterId':recruiter,'IndustryId':Raw('@Industry'),'ProvinceId':Raw('@Province'),'EmploymentTypeId':employment,'Title':conversation['job']+' (Demo)','Department':'Kỹ thuật & Công nghệ','Description':conversation['description'],'Requirements':'\n'.join(conversation['requirements']),'WorkAddress':conversation['location'],'WorkMode':'Hybrid','Seniority':'Senior','ExperienceMinYears':3,'SalaryMin':35000000,'SalaryMax':55000000,'SalaryVisible':True,'Openings':3,'StartDate':Raw('@Today'),'EndDate':Raw('DATEADD(day,60,@Today)'),'CampaignStatus':'Running','Slug':'jobtot-demo-campaign-'+str(i+1),'PostingStatus':'Draft'})
    # Mirror the same ID into Jobs, as required by existing campaign/job integration.
    emit(f"IF NOT EXISTS(SELECT 1 FROM dbo.Jobs WITH(UPDLOCK,HOLDLOCK) WHERE Id={campaign}) INSERT dbo.Jobs(Id,CompanyId,Title,Description,Location,SalaryMin,SalaryMax,CreatedAt,ExpiresAt,IsClosed,SalaryPeriod,ProvinceId,EmploymentType,Requirements,Vacancies,RecruiterEmail,RecruiterName,RecruiterTitle) VALUES({campaign},{company},{quote(conversation['job']+' (Demo)')},{quote(conversation['description'])},{quote(conversation['location'])},35000000,55000000,@Now,DATEADD(day,60,@Now),0,N'monthly',@Province,N'Toàn thời gian',{quote(chr(10).join(conversation['requirements']))},3,{quote(hr_email)},{quote(conversation['name'])},{quote(conversation['subtitle'])});")
    stages=[]
    for j,(stage,kind) in enumerate([('Lọc CV & AI','screening'),('Sơ loại HR','hr'),('Đánh giá kỹ thuật','technical'),('Phỏng vấn quản lý','interview'),('Thư mời nhận việc','offer')]):
        stages.append(seed('CampaignStage',f'stage:{i}:{j}',{'CampaignId':campaign,'StageName':stage,'StageOrder':j+1,'StageKind':kind},{'CampaignId':campaign,'StageOrder':j+1}))
    for j,skill in enumerate(skills):
        seed('CampaignSkill',f'campaign-skill:{i}:{j}',{'CampaignId':campaign,'SkillId':skill,'IsRequired':True},{'CampaignId':campaign,'SkillId':skill})
    for j,benefit in enumerate(benefits):
        seed('CampaignBenefit',f'campaign-benefit:{i}:{j}',{'CampaignId':campaign,'BenefitId':benefit},{'CampaignId':campaign,'BenefitId':benefit})
    tag=seed('TalentTag','tag:'+str(i),{'CompanyId':company,'TagName':'Demo / Cloud & DevOps'},{'CompanyId':company,'TagName':'Demo / Cloud & DevOps'})
    for j,candidate in enumerate(candidates):
        state=['Interview','Applied','Screening','Offer','Hired'][j]
        stage=stages[[2,0,1,4,4][j]]
        application=seed('Application',f'application:{i}:{j}',{'CampaignId':campaign,'CandidateId':candidate,'CvId':cvs[j],'StageId':stage,'Source':'Demo','ApplicationStatus':state,'AppliedAt':Raw('DATEADD(day,-2,@Now)'),'CoverLetter':'Hồ sơ minh họa dùng để kiểm thử giao diện tuyển dụng.','MatchScore':96-j*3},{'CampaignId':campaign,'CandidateId':candidate})
        seed('ApplicationStageHistory',f'history:{i}:{j}',{'ApplicationId':application,'StageId':stage,'ChangedByRecruiterId':recruiter,'Note':'Khởi tạo dữ liệu demo; không có thông báo được gửi.'})
        talent=seed('TalentPoolEntry',f'talent:{i}:{j}',{'CompanyId':company,'CandidateId':candidate,'AddedByRecruiterId':recruiter,'ReadinessStatus':'ready' if j<3 else 'unknown','Notes':'Ứng viên minh họa từ dữ liệu demo.','AddedAt':Raw('@Now')},{'CompanyId':company,'CandidateId':candidate})
        seed('TalentPoolTag',f'talent-tag:{i}:{j}',{'TalentPoolEntryId':talent,'TalentTagId':tag},{'TalentPoolEntryId':talent,'TalentTagId':tag})
        if j==0:
            seed('Interview',f'interview:{i}:{j}',{'ApplicationId':application,'StageId':stage,'ScheduledStart':Raw(f"TODATETIMEOFFSET(DATEADD(hour,{10+i*2},CAST(DATEADD(day,1,@Today) AS datetime2)),'+07:00')"),'ScheduledEnd':Raw(f"TODATETIMEOFFSET(DATEADD(hour,{11+i*2},CAST(DATEADD(day,1,@Today) AS datetime2)),'+07:00')"),'Location':'Phòng phỏng vấn Demo '+str(i+1),'InterviewStatus':'pending'})

# Import all editorial records from the shared career resources component.
source=(root.parents[1]/'FE/src/shared/ui/career-resources/career-knowledge.jsx').read_text(encoding='utf-8')
articles=re.findall(r'\["([^"\n]+)", "([^"\n]+)", "([^"\n]+)"\]',source)
assert len(articles)==4, 'Expected four editorial records.'
for i,(category,title,summary) in enumerate(articles):
    category_id=seed('ArticleCategory','article-category:'+str(i),{'CategoryName':category,'Slug':'jobtot-demo-category-'+str(i+1)},{'CategoryName':category})
    seed('Article','article:'+str(i),{'ArticleCategoryId':category_id,'AuthorAccountId':hr,'Title':title,'Slug':'jobtot-demo-article-'+str(i+1),'Summary':summary,'Body':summary+'\n\nBài viết minh họa dùng để phát triển giao diện Job Tốt.','PublishedAt':Raw('@Now')},{'Slug':'jobtot-demo-article-'+str(i+1)})

out.append("""    COMMIT TRANSACTION;
    SELECT N'DemoDatasets' AS Entity, COUNT(*) AS Total FROM dbo.DemoDatasets
    UNION ALL SELECT N'RecruitmentCampaign',COUNT(*) FROM dbo.RecruitmentCampaign
    UNION ALL SELECT N'Application',COUNT(*) FROM dbo.Application
    UNION ALL SELECT N'TalentPoolEntry',COUNT(*) FROM dbo.TalentPoolEntry
    UNION ALL SELECT N'Interview',COUNT(*) FROM dbo.Interview
    UNION ALL SELECT N'Article',COUNT(*) FROM dbo.Article;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
DROP PROCEDURE #MergeDemoJson;
GO
""")
(root/'database/extened.sql').write_text(''.join(out),encoding='utf-8-sig')
print(f'Generated database/extened.sql: {seed.counter} relational seed statements, {len(datasets)} JSON datasets.')
