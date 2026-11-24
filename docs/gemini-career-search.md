# Chat AI nghề nghiệp

Backend nhận `POST /api/ai/career-search` với JSON `{ "question": "Backend Developer cần kỹ năng gì?" }`. Giữ xác thực Candidate/Employer hiện có. Gemini trả lời tiếng Việt; không có Google Search hoặc công cụ web trong request.

## Cấu hình

API project dùng User Secrets ID `jobtot-api`. Đặt model `Gemini:Model=gemini-3.5-flash-lite` và danh sách `Gemini:ApiKeys:0`, `Gemini:ApiKeys:1`, ... trong User Secrets. Cấu hình key đơn `Gemini:ApiKey` vẫn được hỗ trợ nếu không có danh sách. Không lưu key trong source. Các cấu hình không bí mật có ví dụ trong `src/JobTot.Api/appsettings.Gemini.example.json`.

`GeminiConfiguration.AddGeminiSecrets()` bổ sung các giá trị Gemini còn thiếu từ User Secrets ngay khi khởi động, kể cả khi không chạy trong Development. Cấu hình hiện có từ môi trường hoặc tham số chạy được giữ nguyên. User Secrets thuộc tài khoản hệ điều hành chạy backend; khi triển khai dưới tài khoản khác, cung cấp key bằng biến môi trường `Gemini__ApiKeys__0`, `Gemini__ApiKeys__1`, ... hoặc `Gemini__ApiKey`.

Default timeout 45 giây dùng chung cho toàn bộ câu hỏi. Mỗi key được thử tối đa một lần khi Google trả 401, 403, 429, 500, 502, 503, 504 hoặc lý do API_KEY_INVALID/API_KEY_EXPIRED. Không đổi key cho lỗi model 404 hoặc request 400 không liên quan key. Tối đa 10 key; loại trùng. Key chỉ gửi qua header `x-goog-api-key`; không gửi trong URL hoặc ghi log. Hủy từ client dừng xử lý.

## Chẩn đoán và xác minh

`ai_not_configured` nghĩa là backend không đọc được key/model; yêu cầu chưa đến Google. HTTP 429 từ Google không đủ để kết luận đã dùng hết quota chat. Kiểm tra thực tế ngày 24/11/2026: cùng key/model, chat thường trả 200, bật Search trả 429 có thông báo billing. Google Search đã được bỏ hoàn toàn khỏi request và prompt; cấu hình GoogleSearchEnabled cũ không còn tác dụng.

Kiểm tra offline (không Google/database):

    dotnet run --project tools/ExternalAuthChecks -- --gemini-only

Kiểm tra tích hợp thật, chỉ chạy khi được phép dùng key/quota:

    dotnet run --project tools/ExternalAuthChecks -- --gemini-live

Chế độ live khởi động host loopback riêng ở môi trường Production, dùng loader cấu hình và provider Gemini thật, gửi POST qua controller. JWT của host thử nghiệm bỏ kiểm tra session database, không thay đổi xác thực của ứng dụng thật. Chỉ in trạng thái và số ký tự, không in key hoặc câu trả lời. Kết quả 24/11/2026: HTTP 200, nhận câu trả lời 180 ký tự, không Google Search.

Chạy backend phục vụ frontend:

    dotnet run --project src/JobTot.Api --launch-profile http

Frontend Vite proxy `/api` đến `http://localhost:5049`. Sau khi đổi code, backend phải chạy bản build mới.