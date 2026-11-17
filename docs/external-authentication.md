# Đăng ký và đăng nhập mạng xã hội

Ứng viên và Nhà tuyển dụng đều dùng email/mật khẩu và Google. Backend kiểm tra vai trò ở cả đầu luồng, callback và bước tạo/liên kết tài khoản. Không có chế độ giả lập nào trong ứng dụng production; nhà cung cấp giả lập chỉ được đăng ký trong `tools/ExternalAuthChecks`.

## Cấu hình backend

Mẫu: `src/JobTot.Api/appsettings.ExternalAuth.example.json`. Tệp này không tự được nạp: đưa giá trị vào user secrets, biến môi trường hoặc hệ thống quản lý bí mật. Không đưa client secret vào React, `VITE_*`, Git hay URL trình duyệt.

ASP.NET Core hiện không tự đọc tệp `BE/.env`. Nếu lưu biến trong tệp này, công cụ khởi chạy cần nạp chúng vào môi trường tiến trình trước khi chạy API; cách đơn giản cho local là dùng `dotnet user-secrets` bên dưới. Không thay đổi nội dung `.env` đang dùng để tránh ảnh hưởng các cấu hình khác.

| Biến môi trường | Giá trị |
| --- | --- |
| `ExternalAuth__ApiOrigin` | Origin công khai của API, ví dụ `https://api.jobtot.example` |
| `ExternalAuth__WebOrigin` | Origin frontend, ví dụ `https://jobtot.example` |
| `ExternalAuth__Google__ClientId` | Google OAuth Web client ID |
| `ExternalAuth__Google__ClientSecret` | Google OAuth client secret |
| `Cors__AllowedOrigins__0` | Origin frontend chính xác; thêm `__1`, `__2` khi cần |

Origin không chứa path/query/fragment. Production bắt buộc HTTPS. HTTP chỉ được phép với loopback trong Development. Local nên thống nhất `localhost` cho cả FE/API, không trộn `localhost` và `127.0.0.1` vì cookie không chia sẻ giữa hai hostname. Frontend và API cần cùng site, ví dụ hai subdomain cùng domain, hoặc dùng proxy `/api`. Cookie giao dịch là HttpOnly, SameSite=Lax, Secure khi API công khai là HTTPS. Nếu dùng proxy, cấu hình `ApiOrigin` là URL công khai và chuyển tiếp đầy đủ cookie.

Phiên đăng nhập vẫn dùng `Jwt:*` và cơ chế refresh/logout hiện có. Cần cấu hình `PasswordRecovery:Smtp:{Host,Port,EnableSsl,From,Username,Password}` để gửi mã xác minh email và phục hồi mật khẩu. Với các instance API, dùng chung kho ASP.NET Data Protection keys, bảo vệ quyền truy cập và đặt application discriminator thống nhất; không dùng kho khóa tạm trong production.

Ví dụ local (chạy từ BE, điền bí mật bằng công cụ quản lý cấu hình của bạn):

```powershell
dotnet user-secrets set "ExternalAuth:ApiOrigin" "http://localhost:5049" --project src/JobTot.Api
dotnet user-secrets set "ExternalAuth:WebOrigin" "http://localhost:5173" --project src/JobTot.Api
dotnet user-secrets set "ExternalAuth:Google:ClientId" "YOUR_CLIENT_ID" --project src/JobTot.Api
dotnet user-secrets set "ExternalAuth:Google:ClientSecret" "YOUR_CLIENT_SECRET" --project src/JobTot.Api
```

## Callback chính xác

Thay `{API_ORIGIN}` bằng `ExternalAuth:ApiOrigin`. Callback luôn ở backend; frontend nhận kết quả tại `{WEB_ORIGIN}/auth/external`.

| Nhà cung cấp / vai trò | Callback cần đăng ký |
| --- | --- |
| Google / Ứng viên | `{API_ORIGIN}/api/candidate/auth/external/google/callback` |
| Google / Nhà tuyển dụng | `{API_ORIGIN}/api/employer/auth/external/google/callback` |

API chỉ chấp nhận Google cho cả hai vai trò. Yêu cầu `start` và `callback` trực tiếp với Facebook, LinkedIn hoặc nhà cung cấp khác trả HTTP 400. Giao dịch cũ của nhà cung cấp đã gỡ cũng bị từ chối khi đọc hoặc hoàn tất.

### Google

Tạo OAuth client loại **Web application** trong Google Cloud, cấu hình Branding/Audience/consent và thêm hai Authorized redirect URIs ở bảng trên. Với môi trường thử nghiệm, thêm tài khoản vào danh sách test users; khi phát hành hoàn tất yêu cầu xác minh/đưa app ra production của Google. Scope dùng `openid profile email`; authorization code với PKCE S256 và nonce. Backend kiểm tra chữ ký RS256 từ discovery/JWKS, issuer, audience, lifetime, nonce, azp và đối chiếu `sub` với userinfo. Lưu `sub`, không dùng email làm khóa liên kết. Email ngoài Gmail không có hosted-domain claim vẫn cần mã email Job Tốt.

Tài liệu: [Google OpenID Connect](https://developers.google.com/identity/openid-connect/openid-connect).

## Hành vi tài khoản

1. POST `/api/{candidate|employer}/auth/external/google/start` với `{mode:"register"|"login",returnTo?}` và header `X-JobTot-External: 1`. Trình duyệt phải giữ cookie của giao dịch.
2. Backend ghi giao dịch 15 phút và trả authorization URL. Callback kiểm tra state, cookie, vai trò/phương thức và chỉ nhận callback một lần; sau đó đổi code bằng backend.
3. Frontend tải thông tin giao dịch bằng GET `/flows/{id}`. URL chỉ có ID giao dịch, không có JWT hay access token. Giao dịch được bảo vệ bằng cookie bí mật và dữ liệu mã hóa Data Protection.
4. Đăng nhập chỉ cấp phiên cho liên kết provider/subject hiện có và tài khoản đúng vai trò còn hoạt động. Chưa liên kết: UI hướng dẫn chuyển sang đăng ký, POST `/flows/{id}/registration`, rồi người dùng xác nhận vai trò/điều khoản. Không tự đăng ký khi đang đăng nhập.
5. Đăng ký hoàn tất dùng POST `/flows/{id}/register`. Email thiếu, chưa được nhà cung cấp xác minh hoặc bị người dùng thay đổi phải đi qua OTP email hiện có. Nhà tuyển dụng vẫn phải cung cấp HR, điện thoại, tên pháp lý, MST, trụ sở và cam kết tuyển dụng; doanh nghiệp ở trạng thái Pending.
6. Email trùng: bắt buộc đăng nhập đúng tài khoản Job Tốt có email đó rồi xác nhận liên kết. UI hỗ trợ mật khẩu, phục hồi mật khẩu hoặc đăng nhập bằng phương thức đã liên kết rồi quay lại và POST `/link-session` với JWT đúng vai trò. `/link` dùng mật khẩu và xử lý bước đổi mật khẩu tạm hiện có. Không tự ghép theo email, không chuyển vai trò và không nhận thông tin doanh nghiệp đang tồn tại.
7. Tạo account/link/refresh session, tiêu thụ mã email và tiêu thụ giao dịch nằm trong một transaction SQL Server; concurrency token và unique index ngăn dùng lại/nhận cùng provider subject. Tài khoản xã hội có password hash ngẫu nhiên không thể đoán để giữ password stamp/refresh/recovery hiện có. Người dùng muốn dùng mật khẩu có thể đi qua quên mật khẩu.

Các endpoint POST của giao dịch yêu cầu header `X-JobTot-External: 1`; `/link-session` còn yêu cầu Bearer JWT và vai trò đúng. CORS chỉ cấp credentials cho origin được cho phép. Giới hạn tốc độ xác thực hiện có áp dụng cho start/register/login/link. Có thể xóa các dòng `ExternalAuthFlows` đã hết hạn bằng tác vụ bảo trì định kỳ; không xóa `ExternalLogin` theo thời hạn giao dịch.

## Khôi phục tài khoản đã liên kết Facebook hoặc LinkedIn

Các bản ghi `Account`, hồ sơ và `ExternalLogin` cũ được bảo toàn. Người dùng có email đã xác minh và còn truy cập hộp thư chọn đúng vai trò tại `/login`, bấm **Quên mật khẩu?**, nhận mật khẩu tạm qua email, rồi đăng nhập để đặt mật khẩu mới. Luồng này hỗ trợ cả tài khoản có password hash ngẫu nhiên lẫn tài khoản cũ có `PasswordHash` rỗng. ID tài khoản, hồ sơ và liên kết cũ được giữ sau khôi phục. Người dùng có thể liên kết Google sau khi xác thực tài khoản Job Tốt hiện có.

Email khôi phục được gửi tới `Account.Email`; mật khẩu tạm xác nhận quyền truy cập hộp thư đó. Tài khoản thiếu email hoặc người dùng mất quyền truy cập hộp thư cần quản trị viên xác minh chủ sở hữu và hỗ trợ khôi phục. Tài khoản bị khóa/xóa tuân theo trạng thái tài khoản hiện có. Việc gửi email thực tế phụ thuộc cấu hình SMTP và khả năng gửi thư của môi trường triển khai.

## Migration

Migration: `20261112103518_AddExternalAuthentication`. Thêm `ExternalAuthFlows`, concurrency token và index thời hạn; đổi `ExternalLogin.ProviderSubject` sang collation BIN2 để phân biệt mã tài khoản có khác chữ hoa/thường. Giữ unique index provider/subject đã có. Không sửa hoặc xóa dữ liệu tài khoản hiện tại.

```powershell
# Từ BE, với cấu hình connection string đúng môi trường:
dotnet ef database update --project src/JobTot.Infrastructure --startup-project src/JobTot.Api
# Có thể xuất script để review/áp dụng theo quy trình triển khai:
dotnet ef migrations script --idempotent --project src/JobTot.Infrastructure --startup-project src/JobTot.Api --output external-auth-migrations.sql
```

Database local `JobTot` đã được áp dụng migration này ngày 12/11/2026. Với database triển khai khác, kiểm tra lịch sử EF và chạy migration trước khi bật Google OAuth. Cần cho phép đổi collation/cập nhật index trên bảng ExternalLogin; thao tác có thể giữ khóa bảng trong thời gian triển khai.

## Kiểm tra và trạng thái bàn giao

```powershell
# FE:
npm.cmd run check
# BE:
dotnet build src/JobTot.Api/JobTot.Api.csproj
dotnet run --project tools/ExternalAuthChecks -- --root D:/job-tot-drive/source-code
# Bộ UI dùng cùng API thật với DB InMemory và nhà cung cấp giả lập trong test harness:
dotnet run --project tools/ExternalAuthChecks -- --root D:/job-tot-drive/source-code --serve
# Sau đó từ FE, với bản build hiện tại:
node tests/external-auth-browser.mjs
```

`ExternalAuthChecks` không kết nối database thật, không gửi email thật và không đăng nhập tài khoản nhà cung cấp thật. Kiểm tra giao thức dùng RSA/JWKS và HTTP handler giả lập để kiểm chứng chữ ký, issuer, audience, lifetime, nonce, subject, client authentication và PKCE. Kiểm tra API dùng các controller/service/session thật với nhà cung cấp giả lập. DB InMemory không thay thế được kiểm thử cạnh tranh/rollback SQL Server; cần kiểm thử staging với SQL Server sau khi migration được áp dụng.

Khi liên kết bằng mật khẩu, kiểm tra mật khẩu và số lần thử mật khẩu tạm diễn ra trước transaction hoàn tất liên kết, để các lần thử sai vẫn được lưu khi liên kết bị từ chối. Transaction tạo/liên kết tài khoản không làm mất bộ đếm thử sai.

**Tài khoản thật:** cần thử Google và SMTP bằng tài khoản thật của môi trường đích. Chạy đăng ký → logout → login lại cho cả hai vai trò, hủy cấp quyền, email thiếu/trùng, đăng ký Nhà tuyển dụng và xác nhận Pending, cùng khôi phục mật khẩu qua hộp thư thật. API Facebook/LinkedIn được kiểm tra từ chối ở cả hai vai trò bằng bộ kiểm tra tự động.

Báo cáo tự động lưu tại `.artifacts/external-auth-checks/` của workspace. Các kết quả thực tế của lần bàn giao được ghi trong báo cáo JSON và phản hồi cuối.

### Phạm vi bộ kiểm tra

- Giao diện `/login` và `/register` của cả hai vai trò trên desktop 1440px và mobile 390px có một nút Google toàn chiều rộng, cùng trường email/mật khẩu.
- API từ chối `start`/`callback` Facebook và LinkedIn, kể cả tên khác chữ hoa/thường và giao dịch cũ đã sẵn sàng hoàn tất.
- Kiểm tra Google bao gồm đăng ký, đăng nhập, liên kết tài khoản, refresh/logout và kiểm chứng ID token/PKCE qua fixture.
- Kiểm tra email bao gồm OTP, đăng ký/đăng nhập, quên mật khẩu và khôi phục tài khoản xã hội cũ qua mật khẩu tạm ở cả hai vai trò, đồng thời đối chiếu ID tài khoản, hồ sơ và liên kết cũ.
- Kết quả từng lần chạy được ghi trong `backend-result.json` và `browser-result.json` tại `.artifacts/external-auth-checks/` của workspace.

### Các tệp triển khai

| Khu vực | Tệp sửa/thêm |
| --- | --- |
| UI/phương thức | `FE/src/features/authenticate/ui/{auth-panel,credentials-form,employer-credentials-form,email-auth-fields,social-auth-buttons,external-auth-panel}.jsx`, `social-auth.css` |
| API/validation FE | `FE/src/features/authenticate/api/external-auth.js`, `model/{validation,employer-validation}.js`, `features/authenticate/index.js` |
| Trang/route FE | `FE/src/pages/auth/ui/{auth-page,external-auth-page}.jsx`, `pages/auth/index.js`, `app/router.jsx`, `shared/lib/auth-return-path.js` |
| OAuth backend | `BE/src/JobTot.Api/{ExternalAuthService,ExternalIdentityClient}.cs`, `Controllers/ExternalAuthController.cs`, `Program.cs`, `CandidateAuthentication.cs` |
| Account/link | `BE/src/JobTot.Application/Authentication/{CandidateAuthService,EmployerAuthService}.cs`, `BE/src/JobTot.Infrastructure/Persistence/EmployerAuthRepository.cs` |
| Persistence | `ExternalAuthFlow.cs`, `RecruitmentDbContext.cs`, `ErdModelConfiguration.cs`, migration `20261112103518_AddExternalAuthentication` và model snapshot |
| Cấu hình/hướng dẫn | `BE/src/JobTot.Api/appsettings.ExternalAuth.example.json`, tài liệu này |
| Kiểm tra | `FE/tests/external-auth-browser.mjs`, `BE/tools/ExternalAuthChecks/{ExternalAuthChecks.csproj,Program.cs,ProtocolChecks.cs}` |
