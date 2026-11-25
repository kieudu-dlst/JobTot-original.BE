# Chat AI nghề nghiệp

Backend nhận `POST /api/ai/career-search` với JSON `{ "question": "Backend Developer cần kỹ năng gì?" }`. Giữ xác thực Candidate/Employer hiện có. Gemini trả lời tiếng Việt; không có Google Search hoặc công cụ web trong request.

## Cấu hình

API project dùng User Secrets ID `jobtot-api`. Đặt model `Gemini:Model=gemini-3.5-flash-lite` và danh sách `Gemini:ApiKeys:0`, `Gemini:ApiKeys:1`, ... trong User Secrets. Cấu hình key đơn `Gemini:ApiKey` vẫn được hỗ trợ nếu không có danh sách. Không lưu key trong source. Các cấu hình không bí mật có ví dụ trong `src/JobTot.Api/appsettings.Gemini.example.json`.

`GeminiConfiguration.AddGeminiSecrets()` bổ sung các giá trị Gemini còn thiếu từ User Secrets ngay khi khởi động, kể cả khi không chạy trong Development. Cấu hình hiện có từ môi trường hoặc tham số chạy được giữ nguyên. User Secrets thuộc tài khoản hệ điều hành chạy backend; khi triển khai dưới tài khoản khác, cung cấp key bằng biến môi trường `Gemini__ApiKeys__0`, `Gemini__ApiKeys__1`, ... hoặc `Gemini__ApiKey`.

Không đặt thời gian chờ tối đa cho câu hỏi; chỉ dùng token hủy của client. Không gửi maxOutputTokens, không giới hạn số ký tự câu trả lời và không yêu cầu giới hạn 1200 từ trong prompt. Các thiết lập TimeoutSeconds, MaxOutputTokens, MaxAnswerCharacters cũ không còn tác dụng. Giới hạn tối đa riêng của model vẫn do Google quyết định. Chuyển key khi Google trả 401, 403, 429 hoặc lý do API_KEY_INVALID/API_KEY_EXPIRED. Với HTTP 500/502/503/504, chờ rồi thử lại cùng key/model tối đa 2 lần bổ sung trên toàn bộ câu hỏi (mặc định 1 rồi 2 giây; dùng Retry-After nếu có, giới hạn khoảng nghỉ 1–30 giây). Hết ngân sách retry thì thử các key còn lại, mỗi key một lượt. Không thử lại sau khi đã nhận HTTP thành công. Tổng tối đa số key + 2 lượt gọi cho một câu hỏi. Hủy từ client dừng cả thời gian nghỉ. Không đổi key cho lỗi model 404 hoặc request 400 không liên quan key. Tối đa 10 key; loại trùng. Key chỉ gửi qua header `x-goog-api-key`; không gửi trong URL hoặc ghi log. Hủy từ client dừng xử lý.

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
Xử lý hủy: provider bắt OperationCanceledException tại chỗ. Nếu token trình duyệt bị hủy, chuyển thành ai_cancelled; controller kết thúc HTTP 499 không có JSON. Nếu deadline hết, giữ ai_timeout/HTTP 504. Không tự retry yêu cầu người dùng đã hủy.

Cập nhật 25/11/2026: nếu tất cả lượt gọi vẫn bị lỗi tạm thời từ Google, trả HTTP 503/ai_provider_unavailable cùng Retry-After: 5, không gộp thành 502 chung. Không tự đặt lại thời gian chờ tối đa hoặc giới hạn độ dài câu trả lời. Smoke test controller thật: HTTP 200, nhận câu trả lời 162 ký tự. Việc kiểm tra thành công một lần không đảm bảo Google luôn sẵn sàng.

## Nhận xét ảnh CV (25/11/2026)

POST JSON có thể kèm `image: { mimeType: "image/png" | "image/jpeg", data: "<base64>" }`. Ảnh tối đa 5 MiB, chiều rộng/cao tối đa 10.000 và tổng tối đa 25 triệu điểm ảnh. Backend kiểm tra Base64, chữ ký PNG/JPEG và kích thước trước khi gọi Google; ảnh không hợp lệ trả 400/ai_invalid_image. Giới hạn request JSON là 7 MiB để chứa Base64 và câu hỏi. Không nhận URL ảnh, SVG, GIF, PDF ở chức năng này.

Nếu chỉ gửi ảnh, service tạo câu hỏi mặc định yêu cầu nhận xét CV. Provider gửi text và inlineData trong cùng contents.parts của Gemini, không dùng Files API hoặc Google Search. Ảnh chỉ đi cùng câu hỏi hiện tại; frontend không tự gửi lại ảnh ở lượt tiếp theo. Không có ghi file ảnh lên backend/database hoặc log nội dung request; ảnh vẫn được truyền tới Google để xử lý. Không đặt lại maxOutputTokens hoặc timeout câu trả lời.

Kiểm tra có ảnh thật dùng ảnh mẫu không có thông tin cá nhân:

    dotnet run --project tools/ExternalAuthChecks -- --gemini-live-image <path-to-synthetic-cv.png>

Kiểm tra Google thật cho tính năng ảnh chưa thực hiện do bộ duyệt công cụ tự động chạm hạn mức; không coi các kiểm tra HTTP giả lập là xác nhận Gemini thực tế đã đọc ảnh.
## Lịch sử hội thoại theo tài khoản (25/11/2026)

Dùng một bảng mới `AiChatMessages`; không dùng chung bảng tin nhắn giữa ứng viên và nhà tuyển dụng. Mỗi lượt có hai dòng user/assistant, cùng ConversationId và TurnId. Owner lấy từ JWT, không nhận AccountId từ request. Tin người dùng và dòng assistant Pending được lưu trước khi gọi Gemini; kết thúc cập nhật Completed/Failed/Cancelled. Câu trả lời không bị cắt giới hạn ký tự. Câu hỏi đã lưu vẫn còn khi nhà cung cấp lỗi hoặc người dùng hủy. Lịch sử đã tồn tại trong bộ nhớ trình duyệt trước bản cập nhật không được tự nhập vào database.

POST `/api/ai/career-search` nhận ConversationId tùy chọn. Bỏ trống tạo hội thoại mới bằng mã server sinh; ID đã có phải thuộc tài khoản hiện tại, nếu không trả 404 trước khi gọi Google. Response thành công và lỗi provider có ConversationId để giao diện giữ đúng nhóm tin nhắn.

GET `/api/ai/conversations` liệt kê hội thoại của tài khoản. GET `/api/ai/conversations/{id}` trả 100 tin mới nhất theo thứ tự, HasMore và tham số beforeId để tải trang cũ hơn. GET `/api/ai/conversations/{id}/messages/{messageId}/image` tải ảnh CV sau kiểm tra quyền; không đưa đường dẫn file vật lý vào response. Các endpoint lịch sử yêu cầu đăng nhập Candidate/Employer và trả no-store. Tài khoản khác nhận 404 cho cả tin nhắn và ảnh.

Ảnh được lưu trong `App_Data/ai-chat` của API, không nằm trong thư mục public uploads/wwwroot và được Git bỏ qua. Database chỉ lưu tên file ngẫu nhiên và MIME; không lưu Base64. Khi sao lưu/triển khai cần giữ cả database lẫn thư mục ảnh riêng này. Không tự gửi lịch sử hoặc ảnh cũ cho Gemini; chức năng hiện tại phục vụ lưu và xem lại, chưa phải bộ nhớ hội thoại hoặc RAG.

Migration: `20261125010000_AddAiChatHistory`. Database phát triển đã được cập nhật qua `database/fixes/add-ai-chat-history.sql`, script chỉ tạo bảng/index/FK mới và ghi nhận migration tương ứng, không chạy migration không liên quan. Môi trường khác áp dụng migration trước khi chạy backend mới.

Kiểm tra history và quyền truy cập (InMemory, fake Gemini, không sửa SQL thật):

    dotnet run --project tools/ExternalAuthChecks -- --ai-history-only

Kiểm tra SQL Server chỉ đọc và model snapshot:

    dotnet run --project tools/ExternalAuthChecks -- --ai-history-sql-read

`AddCareerSearch(persistHistory: false)` chỉ dành cho fixture kiểm tra Gemini độc lập; cấu hình ứng dụng mặc định bật lưu lịch sử.