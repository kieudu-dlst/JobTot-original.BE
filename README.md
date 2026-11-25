# Khởi tạo database Backend

## Chat qua WebSocket

Frontend dùng WebSocket thuần tại `/api/candidate/messages/socket` hoặc `/api/employer/messages/socket`, không còn polling tin nhắn và typing. Gọi `POST .../messages/socket-ticket` với bearer token để lấy ticket một lần, hết hạn sau 30 giây; ticket chỉ dùng để nâng cấp kết nối. Kết nối đóng khi access token hết hạn; frontend kết nối lại, lấy ticket qua cơ chế refresh token hiện có và tải bù lịch sử qua REST.

Các lệnh socket `send`, `read`, `typing` đều đi qua Application, kiểm tra phiên và quyền hội thoại. Server chỉ phát sự kiện đến các kết nối của hai thành viên; tin nhắn được lưu trước khi phát. Gửi lại giữ nguyên `clientMessageId` để không tạo bản trùng. REST vẫn hỗ trợ tải lịch sử, tìm người, tạo hội thoại và tương thích client cũ.

Proxy/IIS phải cho phép WebSocket Upgrade, HTTPS sử dụng `wss://`. Registry kết nối và ticket hiện nằm trong bộ nhớ của một API instance. Nếu chạy nhiều instance cần bổ sung distributed ticket store và pub/sub/backplane; chỉ sticky session không đủ để chuyển tin giữa các instance. Không cần migration database.

Kiểm tra kết nối WebSocket thật: `dotnet run --project tools/ExternalAuthChecks -- --root .. --architecture-only`. Kiểm tra client: từ FE chạy `node --test scripts/chat-socket.test.mjs`.

## Ranh giới Clean Architecture

- `JobTot.Api`: route, authentication/authorization HTTP, model binding, cookie, header và chuyển kết quả use case thành HTTP response. Controller gọi service của Application, không dùng DbContext, EF Core hoặc SQL.
- `JobTot.Application`: nghiệp vụ, kiểm tra dữ liệu, quyền sở hữu, xử lý đồng thời, phiên đăng nhập và điều phối transaction; khai báo các interface cần dùng.
- `JobTot.Infrastructure`: thực thi các interface bằng EF Core/SQL Server, SMTP, Google OAuth, Data Protection, JWT và lưu file.
- `JobTot.Domain`: entity và quy tắc miền; không tham chiếu các tầng khác.

Use case trả `UseCaseResult` với trạng thái nghiệp vụ; chỉ `ApplicationController` trong API ánh xạ trạng thái đó thành HTTP status, `ProblemDetails`, file tải xuống và header `Location`.

Các repository hiện có được giữ qua interface trong Application. Những use case được tách từ controller dùng `IRecruitmentData` làm unit of work và các biểu thức LINQ làm query specification. `ReadOnly`, `With`, `ThenWith` và `UpdateAsync` chỉ mô tả truy vấn; `RecruitmentData`/`DataQueryProvider` trong Infrastructure chuyển chúng thành EF Core và thực thi. Không trả `DbSet`, `EntityEntry`, transaction của EF hoặc exception SQL qua ranh giới này. Application chỉ tham chiếu Domain và .NET, không cần ASP.NET Core hay EF Core.

Kiểm tra ranh giới tầng và chạy kiểm tra hồi quy từ thư mục `BE`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/check-clean-architecture.ps1
dotnet build JobTot.sln
dotnet run --project tools/ExternalAuthChecks -- --root .. --architecture-only
dotnet run --project tools/ExternalAuthChecks -- --root ..
```

Bộ kiểm tra dùng database InMemory riêng và provider giả lập. Kiểm tra dịch query/update sang SQL Server chặn mở kết nối; không sửa database thật hoặc gửi email thật.

## Format C#

Chạy từ thư mục `BE/JobTot`. CSharpier được ghim phiên bản trong manifest tool;
quy tắc thụt lề và xuống dòng nằm trong `.editorconfig` và `.csharpierrc.json`.

```powershell
dotnet tool restore
dotnet csharpier src
dotnet csharpier src --check
```

## Quản lý cẩm nang và báo cáo

Admin viết bài tại `/admin/career-articles`: nhập tiêu đề, danh mục, mô tả và nội dung; lưu nháp, xem trước hoặc xuất bản. Có thể chuyển bài đã xuất bản về nháp để ẩn khỏi trang ứng viên. Trang `/career-articles` và khối cẩm nang hiển thị bài đã xuất bản; khối cẩm nang lấy 4 bài mới cập nhật nhất.

Áp dụng migration `AddCareerArticles` bằng lệnh cập nhật database bên dưới rồi khởi động lại API. Migration chỉ tạo bảng bài viết, không xuất bản các nội dung minh họa cũ. Nội dung hỗ trợ `##`/`###` cho tiêu đề, `**chữ đậm**` và `-` cho dòng liệt kê; HTML hiển thị dạng chữ.

## Tạo Migrations
dotnet ef migrations add Ten-Migration `
  --project src/JobTot.Infrastructure `
  --startup-project src/JobTot.Api `
  --configuration CodeFirst

## Update Database Structure
dotnet ef database update `
  --project src/JobTot.Infrastructure `
  --startup-project src/JobTot.Api `
  --configuration CodeFirst

## Dữ liệu mẫu
Mật khẩu mặc định của tất cả tài khoản mẫu là JobTotDemo2026!.
Ví dụ:
- Admin: admin@demo.jobtot.test
- Nhà tuyển dụng: employer001@demo.jobtot.test
- Ứng viên: candidate0001@demo.jobtot.test 
# AI public job retrieval

Only explicit company/job/recruitment requests in the CURRENT question trigger
`AiJobLookup` and job cards. Industry, AI-impact, skills, CV and opportunity advice
receive ordinary answers without cards or no-match notes, even after a job-search turn.
Job matching uses actual position titles and role aliases, never the company's industry
alone: a marketing job at an IT company is not an IT vacancy. General advice never
requires matching catalogue records. For explicit searches, Gemini advises first;
a no-match note is appended afterwards if necessary.
The assistant stays within careers, work-related study/skills, CVs and interviews;
off-topic requests should receive a brief explanation of that mission.
For follow-ups, backend reads up to four prior user questions from the authenticated
owner's conversation. These are contextual data, not system instructions. Earlier
images and stale job snapshots are never replayed. References such as "ngành này"
resolve the earlier subject; new-graduate requests filter internships/junior or
entry-level jobs. Returned examples are not statistics for the whole labour market.
The authenticated actor is required; both candidate and employer chat receive only the
public catalogue, never private employer records. Prompt claims cannot change this scope.
There is no SQL execution or arbitrary table access granted to Gemini.

The allow-list includes public job title, company name/description/founding year,
industry, location and published salary. Tax codes, registration documents, recruiter
contacts, applications and account details are excluded from the projection.
Closed/expired jobs, inactive companies, draft/non-running campaigns and mismatched
campaign/company bindings are excluded. Hidden campaign salaries are returned as null.

Retrieval matches stored industry names or meaningful adjacent keyword pairs and returns
at most six recent matches. It is bounded keyword retrieval, not an exhaustive semantic
search: a missing result is explicitly reported by backend. Gemini still answers the
general advice portion, but cannot invent alternative companies or vacancies. Gemini is instructed to
summarize only supplied records; UI job cards and URLs always come from backend IDs.
History preserves the supplied job links as a dated conversation, not a guarantee that
a vacancy remains open later. No database schema change is needed for this feature.

Checks: `ExternalAuthChecks --ai-lookup-only` (isolated data) and
`--ai-lookup-sql-read` (read-only local SQL translation/execution probe).

