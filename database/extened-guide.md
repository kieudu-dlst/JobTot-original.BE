# Dữ liệu bổ sung JobTot

Mở `extened.sql` trong SQL Server Management Studio và chạy trên database `JobTot`
sau khi đã restore `demo-data.sql`. Đây là script bổ sung, không phải bản backup đầy đủ.
File SQL dùng UTF-8 có BOM và chuỗi Unicode `N'...'` để giữ nguyên tiếng Việt.

Nguồn dữ liệu: backup `demo-data.sql`, bốn bộ dữ liệu trong `preview-data.sql`,
và bốn bài viết trong thành phần CareerKnowledge. Các danh mục ngành nghề,
tỉnh/thành hiện có được tái sử dụng. Backup đã có 100 doanh nghiệp, 500 việc làm,
9 mẫu CV, 9 danh mục CV nên script không chép lại các bản ghi này.

Các bản ghi bổ sung khi chạy trên bản backup hiện tại:

- 4 doanh nghiệp minh họa từ các hội thoại, 4 liên kết ngành nghề.
- 4 tài khoản HR và 5 tài khoản ứng viên demo, email thuộc `example.invalid`.
- 4 hồ sơ HR, 5 hồ sơ ứng viên, 5 CV minh họa.
- 1 loại hình làm việc, 7 kỹ năng, 3 phúc lợi.
- 4 chiến dịch và 4 việc làm có cùng ID để phù hợp API hiện tại.
- 20 vòng tuyển dụng, 28 liên kết kỹ năng chiến dịch, 12 liên kết phúc lợi.
- 35 liên kết kỹ năng ứng viên.
- 20 hồ sơ ứng tuyển, 20 bản ghi lịch sử vòng tuyển dụng.
- 20 bản ghi talent pool, 4 nhãn talent, 20 liên kết nhãn.
- 4 lịch phỏng vấn vào ngày kế tiếp, khung giờ không chồng nhau.
- 4 danh mục bài viết, 4 bài viết minh họa.

Tài khoản demo không có mật khẩu nên chưa dùng để đăng nhập. Script không cấp
quyền Admin hay sửa thông tin đăng nhập của tài khoản có sẵn.
Doanh nghiệp mới mang nhãn `(Demo)` và chưa được xác minh. Bản ghi campaign
được đặt Running để thử màn quản lý; PostingStatus là Draft, chưa công bố chiến dịch.
Các Jobs tương ứng là bản ghi demo mở để thử giao diện danh sách việc làm.

Bốn DemoDatasets được hợp nhất theo key; trường đã có giữ nguyên, trường còn thiếu
được bổ sung. Hội thoại được đối chiếu theo `id`; mục hồ sơ theo title/organization/period;
doanh nghiệp xem gần đây theo name. Không thay thế nội dung người dùng đã chỉnh sửa.
Các bảng nghiệp vụ dùng ID ổn định hoặc khóa tự nhiên để chạy lại không tạo bản ghi trùng.
Script thực hiện trong transaction và rollback nếu có lỗi.

Đã kiểm tra thực thi trên SQL Server, chạy lại và so sánh số dòng toàn bộ bảng;
toàn bộ dữ liệu của lượt kiểm tra đã rollback. Database hiện tại chưa được cập nhật.
Tạo lại script bằng `python scripts/generate-extended-data.py` nếu nguồn demo thay đổi.

Lưu ý về hiển thị: bổ sung bản ghi Article không tự thay đổi component CareerKnowledge
đang dùng nội dung tĩnh; chức năng chat hiện đọc DemoDatasets, chưa đọc bảng tin nhắn thật.
