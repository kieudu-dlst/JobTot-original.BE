# Khởi tạo database Backend

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
