# Ảnh trang trí CV

Ảnh trang trí độc lập với `avatarDataUri`. `CvDocument.decorationImages` mặc định là `[]`, nên JSON cũ không cần chuyển đổi. Mỗi phần tử lưu `id`, `url`, `x`, `y`, `width`, `height`, `page`, `opacity`, `zIndex`; tọa độ/kích thước tính bằng mm trên trang A4 210 × 297 mm, trang bắt đầu từ 1. Lớp âm ở sau nội dung, lớp dương ở trước. Tối đa 5 ảnh, 2 MiB/ảnh, PNG/JPEG/WebP; ảnh giữ tỷ lệ gốc. Không lưu dữ liệu file trong JSON.

## API

- `POST /api/candidate/online-cvs/{id}/decoration-images`
- `DELETE /api/candidate/online-cvs/{id}/decoration-images/{imageId}`
- `POST /api/admin/cv-templates/{id}/decoration-images`
- `DELETE /api/admin/cv-templates/{id}/decoration-images/{imageId}`
- Mẫu chưa lưu dùng đường dẫn admin bỏ `/{id}`.

POST nhận multipart `file`, `currentCount` (số ảnh trong bản đang sửa). Upload kiểm tra quyền, giới hạn file, phần mở rộng, MIME, cấu trúc file và kích thước pixel; API lưu tài liệu kiểm tra lại toàn bộ danh sách, số lượng, quyền sử dụng file, tỷ lệ và tọa độ. Upload không đổi version tài liệu. DELETE chỉ thu hồi file không còn tham chiếu; thao tác gỡ ảnh trong tài liệu được ghi nhận qua API lưu CV/mẫu và version hiện có. Xung đột version giữ nguyên cơ chế 409.

Tạo CV từ mẫu sao chép JSON thành tài liệu riêng. File ảnh được chia sẻ bất biến, cấu hình bố trí được chỉnh độc lập. Khi lưu bỏ ảnh, backend chỉ xóa file nếu không có mẫu, CV (kể cả bản lưu trữ), nội dung CV hay snapshot ứng tuyển nào còn tham chiếu. Khóa filesystem đồng bộ upload/lưu/tạo CV/xóa file giữa các tiến trình dùng chung kho.

## Cấu hình triển khai

Không thêm migration hoặc dependency. Mặc định kho là `BE/src/JobTot.Api/App_Data/cv-decorations`, gồm `files/` và `metadata/`; chỉ `files/` được phục vụ tại `/uploads/cv-decorations/`. Có thể đặt `Media:CvDecorationDirectory` (biến môi trường `Media__CvDecorationDirectory`) thành đường dẫn tuyệt đối trên ổ lưu bền vững. Tài khoản chạy API cần quyền đọc/ghi thư mục này. Sao lưu cả hai thư mục cùng database; nhiều API instance cần dùng chung volume có hỗ trợ khóa file.

Đường dẫn file ảnh là tài nguyên media công khai với tên GUID, như ảnh công ty; không dùng kho này cho tài liệu riêng tư. Không đặt kho metadata trong webroot. Upload bỏ dở và chưa từng lưu có thể để lại file: khi bảo trì chỉ thu hồi sau khi đối chiếu toàn bộ tham chiếu, không xóa chỉ dựa theo tuổi file.

FE dùng `VITE_API_BASE_URL` hiện có cho cả API và ảnh. Vite proxy `/uploads/cv-decorations` tới backend 5049; reverse proxy production cũng cần chuyển tiếp đường dẫn này. Giới hạn request multipart tối thiểu 3 MiB. Không cần thay đổi các bản ghi CV/mẫu đang có.

FE phân trang và render cùng component `CvSheet` cho thumbnail, preview, editor và bản in. Khi in chọn A4, tỷ lệ 100%, tắt header/footer của trình duyệt; lề nằm trong trang CV, `@page` có lề 0. Nút xuất chờ font/ảnh tải xong; khung chọn và tay nắm bị ẩn trong print CSS.

## Kiểm tra hồi quy

Tại `FE`, chạy `node --test tests/cv-decoration-geometry.test.mjs` và `node tests/cv-decoration-browser.mjs` (cần Chrome; có thể đặt `CV_CHECK_CHROME` trỏ tới executable). Browser test dùng harness riêng ngoài router ứng dụng và API mô phỏng, kiểm tra các component thật, kéo/resize bằng pointer, lưu/tải lại, giới hạn ảnh, bốn bố cục nhiều trang và xuất PDF. File kết quả/fixture nằm trong `.artifacts/cv-decoration-checks` ở gốc workspace.

Tại `BE`, build `dotnet build tools/CvDecorationChecks/CvDecorationChecks.csproj --output ../.artifacts/cv-decoration-checks/bin`, rồi từ gốc workspace chạy `dotnet .artifacts/cv-decoration-checks/bin/CvDecorationChecks.dll <đường-dẫn-workspace>`. Bộ kiểm tra dùng SQL Server/cấu hình hiện có, gọi endpoint multipart với xác thực riêng của test, rồi kiểm tra repository và file storage thật. Dữ liệu nằm trong transaction được rollback; media tách khỏi kho production. Chạy browser test trước sẽ bổ sung fixture JPEG/WebP ngoài PNG tích hợp sẵn. Test không dùng tài khoản/mật khẩu demo và không thay đổi bản ghi thật.
