# Khởi tạo database Backend

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
