> Historical reference: standalone SQL seed/export files and test suites were removed at the project owner's request. Commands referencing those deleted files are no longer available. Application source and EF migrations are retained; existing SQL Server data is unchanged.

# Seed dữ liệu demo trên SQL Server

Thư mục này chứa `seed-demo.sql`, chạy trực tiếp bằng SSMS hoặc sqlcmd.
Không cần chạy seeder C# hay đặt biến môi trường.

## 1. Tạo database và bảng bằng EF Core

Kiểm tra connection string trong cấu hình API, rồi chạy trong `BE/JobTot`:

```powershell
dotnet ef database update --project src/JobTot.Infrastructure --startup-project src/JobTot.Api --configuration CodeFirst
```

Migration tạo schema theo model C# hiện có. Script seed chỉ INSERT dữ liệu, không
tạo database/bảng, không DROP hoặc xóa dữ liệu. Không cần tạo migration mới nếu
entity chưa thay đổi.

## 2. Chạy seed bằng SSMS

Kết nối SQL Server, mở `SQL/seed-demo.sql`, chọn đúng database JobTot của bạn
trong danh sách database phía trên cửa sổ query, rồi nhấn **Execute / F5**.
Script không cố định tên database để có thể dùng với `JobTot` hoặc `JobTotDemo`.

Hoặc chạy bằng terminal:

```powershell
sqlcmd -S '.\SQLEXPRESS' -d JobTot -E -C -b -f 65001 -i SQL/seed-demo.sql
```

Thay instance và tên database cho đúng môi trường. Sau đó chạy API với connection
string trỏ đến cùng database.

## Dữ liệu

| Dữ liệu | Số lượng |
|---|---:|
| Công ty | 100 |
| Việc làm / chiến dịch | 1.000 / 1.000 |
| Admin / nhà tuyển dụng / ứng viên | 1 / 200 / 500 |
| CV online / mẫu CV | 500 / 12 |
| Nhóm ngành / kỹ năng / địa điểm demo | 20 / 20 / 10 |
| Hồ sơ ứng tuyển | 4.000 |
| Giai đoạn / lịch phỏng vấn | 4.000 / 1.000 |
| Việc làm đã lưu / theo dõi công ty | 4.000 / 1.500 |

Đây là dữ liệu hư cấu dành cho demo. Có trạng thái active/inactive, công ty chờ
xác minh, tin tuyển dụng đóng và hồ sơ ở nhiều giai đoạn. 4.000 hồ sơ được biểu diễn
trong cả `CandidateJobSubmissions` và `JobApplication` với cùng ID để API không đếm
hai lần. Logo và ảnh bìa dùng `/uploads/companies/...` từ backend `wwwroot`.

Ngày hết hạn và lịch phỏng vấn được tính theo thời điểm chạy script. Chạy lại không
ghi đè hoặc gia hạn bản ghi đã tồn tại. ID ổn định, transaction và application lock
giúp tránh tạo trùng; lỗi sẽ rollback và được báo lại, không bị bỏ qua.

## Đăng nhập demo

Mật khẩu tất cả tài khoản demo mới: **`JobTotDemo2026!`**.
SQL chứa hash ASP.NET Identity PBKDF2, không lưu mật khẩu plaintext trong bảng Account.

| Vai trò | Email |
|---|---|
| Admin | `admin@demo.jobtot.test` |
| Nhà tuyển dụng | `employer001@demo.jobtot.test` |
| Nhân viên tuyển dụng | `employer001.staff@demo.jobtot.test` |
| Ứng viên | `candidate0001@demo.jobtot.test` |

Chạy lại giữ nguyên mật khẩu đã sửa. Email `.test` không nhận email thực.

Không chạy kèm bản export cũ `database/demo-data.sql`, vì bản đó còn chứa DDL và
đường dẫn file database phụ thuộc máy. Dùng EF migration cho schema, script trong
thư mục này cho dữ liệu demo mới. Không tự chạy script seed khi API khởi động.

Đã kiểm tra cú pháp bằng SQL Server `SET PARSEONLY` và build API thành công sau khi
bỏ seeder C#. Chưa thực thi INSERT vào database của bạn; kiểm tra cú pháp không
thay thế kiểm tra constraints/schema trên database đã migrate.

`seed-demo.sql` also updates legacy company image URLs in the same transaction before inserting demo data.
