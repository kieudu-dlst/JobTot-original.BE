# Đăng ký và đăng nhập mạng xã hội

Ứng viên dùng email/mật khẩu, Google, Facebook và LinkedIn. Nhà tuyển dụng dùng email/mật khẩu và Google. Backend kiểm tra vai trò ở cả đầu luồng, callback và bước tạo/liên kết tài khoản. Không có chế độ giả lập nào trong ứng dụng production; nhà cung cấp giả lập chỉ được đăng ký trong `tools/ExternalAuthChecks`.

## Cấu hình backend

Mẫu: `src/JobTot.Api/appsettings.ExternalAuth.example.json`. Tệp này không tự được nạp: đưa giá trị vào user secrets, biến môi trường hoặc hệ thống quản lý bí mật. Không đưa client secret vào React, `VITE_*`, Git hay URL trình duyệt.

ASP.NET Core hiện không tự đọc tệp `BE/.env`. Nếu lưu biến trong tệp này, công cụ khởi chạy cần nạp chúng vào môi trường tiến trình trước khi chạy API; cách đơn giản cho local là dùng `dotnet user-secrets` bên dưới. Không thay đổi nội dung `.env` đang dùng để tránh ảnh hưởng các cấu hình khác.

| Biến môi trường | Giá trị |
| --- | --- |
| `ExternalAuth__ApiOrigin` | Origin công khai của API, ví dụ `https://api.jobtot.example` |
| `ExternalAuth__WebOrigin` | Origin frontend, ví dụ `https://jobtot.example` |
| `ExternalAuth__Google__ClientId` | Google OAuth Web client ID |
| `ExternalAuth__Google__ClientSecret` | Google OAuth client secret |
| `ExternalAuth__Facebook__ClientId` | Meta app ID |
| `ExternalAuth__Facebook__ClientSecret` | Meta app secret |
| `ExternalAuth__Facebook__GraphApiVersion` | Phiên bản Graph API được app Meta hỗ trợ, dạng `vNN.N`; không tự chọn phiên bản mới nhất |
| `ExternalAuth__LinkedIn__ClientId` | LinkedIn client ID |
| `ExternalAuth__LinkedIn__ClientSecret` | LinkedIn client secret |
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
| Facebook / Ứng viên | `{API_ORIGIN}/api/candidate/auth/external/facebook/callback` |
| LinkedIn / Ứng viên | `{API_ORIGIN}/api/candidate/auth/external/linkedin/callback` |

Không đăng ký callback Facebook/LinkedIn cho Nhà tuyển dụng: backend từ chối với 403 kể cả gọi trực tiếp.

### Google

Tạo OAuth client loại **Web application** trong Google Cloud, cấu hình Branding/Audience/consent và thêm hai Authorized redirect URIs ở bảng trên. Với môi trường thử nghiệm, thêm tài khoản vào danh sách test users; khi phát hành hoàn tất yêu cầu xác minh/đưa app ra production của Google. Scope dùng `openid profile email`; authorization code với PKCE S256 và nonce. Backend kiểm tra chữ ký RS256 từ discovery/JWKS, issuer, audience, lifetime, nonce, azp và đối chiếu `sub` với userinfo. Lưu `sub`, không dùng email làm khóa liên kết. Email ngoài Gmail không có hosted-domain claim vẫn cần mã email Job Tốt.

Tài liệu: [Google OpenID Connect](https://developers.google.com/identity/openid-connect/openid-connect).

### Facebook

Tạo app trong Meta for Developers, thêm use case **Facebook Login**, bật Web OAuth và nhập Valid OAuth Redirect URI của Ứng viên. Cấu hình App Domains, Site URL, chính sách bảo mật và các yêu cầu xóa dữ liệu theo dashboard Meta. Trong Development thêm test users/developers; trước khi phát hành hoàn tất quyền/review/business verification mà dashboard yêu cầu. Scope: `public_profile,email`. Chọn phiên bản Graph API đang được app hỗ trợ và đặt `GraphApiVersion` tương ứng.

Backend đổi code bằng app ID/secret, gọi Graph `/me?fields=id,name,email` bằng access token và `appsecret_proof`. Lưu app-scoped `id`. Email có thể không được cấp; luồng hoàn tất sẽ yêu cầu nhập và xác minh email Job Tốt. Facebook không được coi là nguồn `email_verified` trong triển khai này. Access token của nhà cung cấp không được lưu.

Tài liệu: [Facebook server login flow](https://developers.facebook.com/docs/facebook-login/guides/advanced/manual-flow/). Quyền và khả năng phát hành thực tế cần kiểm tra trong app dashboard.

### LinkedIn

Tạo ứng dụng trong LinkedIn Developer Portal, liên kết Page nếu dashboard yêu cầu. Trong **Products**, xin quyền **Sign In with LinkedIn using OpenID Connect**. Thêm callback Ứng viên vào **Auth → Authorized redirect URLs**, lấy client ID/secret. Scope dùng `openid profile email`, không dùng các scope legacy `r_liteprofile`/`r_emailaddress`. Backend xác thực ID token/JWKS, nonce và subject như Google; email và email_verified là tùy chọn, thiếu thông tin sẽ chuyển sang bước hoàn tất/xác minh email.

Tài liệu: [LinkedIn OIDC](https://learn.microsoft.com/en-us/linkedin/consumer/integrations/self-serve/sign-in-with-linkedin-v2).

## Hành vi tài khoản

1. POST `/api/{candidate|employer}/auth/external/{provider}/start` với `{mode:"register"|"login",returnTo?}` và header `X-JobTot-External: 1`. Trình duyệt phải giữ cookie của giao dịch.
2. Backend ghi giao dịch 15 phút và trả authorization URL. Callback kiểm tra state, cookie, vai trò/phương thức và chỉ nhận callback một lần; sau đó đổi code bằng backend.
3. Frontend tải thông tin giao dịch bằng GET `/flows/{id}`. URL chỉ có ID giao dịch, không có JWT hay access token. Giao dịch được bảo vệ bằng cookie bí mật và dữ liệu mã hóa Data Protection.
4. Đăng nhập chỉ cấp phiên cho liên kết provider/subject hiện có và tài khoản đúng vai trò còn hoạt động. Chưa liên kết: UI hướng dẫn chuyển sang đăng ký, POST `/flows/{id}/registration`, rồi người dùng xác nhận vai trò/điều khoản. Không tự đăng ký khi đang đăng nhập.
5. Đăng ký hoàn tất dùng POST `/flows/{id}/register`. Email thiếu, chưa được nhà cung cấp xác minh hoặc bị người dùng thay đổi phải đi qua OTP email hiện có. Nhà tuyển dụng vẫn phải cung cấp HR, điện thoại, tên pháp lý, MST, trụ sở và cam kết tuyển dụng; doanh nghiệp ở trạng thái Pending.
6. Email trùng: bắt buộc đăng nhập đúng tài khoản Job Tốt có email đó rồi xác nhận liên kết. UI hỗ trợ mật khẩu, phục hồi mật khẩu hoặc đăng nhập bằng phương thức đã liên kết rồi quay lại và POST `/link-session` với JWT đúng vai trò. `/link` dùng mật khẩu và xử lý bước đổi mật khẩu tạm hiện có. Không tự ghép theo email, không chuyển vai trò và không nhận thông tin doanh nghiệp đang tồn tại.
7. Tạo account/link/refresh session, tiêu thụ mã email và tiêu thụ giao dịch nằm trong một transaction SQL Server; concurrency token và unique index ngăn dùng lại/nhận cùng provider subject. Tài khoản xã hội có password hash ngẫu nhiên không thể đoán để giữ password stamp/refresh/recovery hiện có. Người dùng muốn dùng mật khẩu có thể đi qua quên mật khẩu.

Các endpoint POST của giao dịch yêu cầu header `X-JobTot-External: 1`; `/link-session` còn yêu cầu Bearer JWT và vai trò đúng. CORS chỉ cấp credentials cho origin được cho phép. Giới hạn tốc độ xác thực hiện có áp dụng cho start/register/login/link. Có thể xóa các dòng `ExternalAuthFlows` đã hết hạn bằng tác vụ bảo trì định kỳ; không xóa `ExternalLogin` theo thời hạn giao dịch.

## Migration

Migration: `20261112103518_AddExternalAuthentication`. Thêm `ExternalAuthFlows`, concurrency token và index thời hạn; đổi `ExternalLogin.ProviderSubject` sang collation BIN2 để phân biệt mã tài khoản có khác chữ hoa/thường. Giữ unique index provider/subject đã có. Không sửa hoặc xóa dữ liệu tài khoản hiện tại.

```powershell
# Từ BE, với cấu hình connection string đúng môi trường:
dotnet ef database update --project src/JobTot.Infrastructure --startup-project src/JobTot.Api
# Có thể xuất script để review/áp dụng theo quy trình triển khai:
dotnet ef migrations script --idempotent --project src/JobTot.Infrastructure --startup-project src/JobTot.Api --output external-auth-migrations.sql
```

Migration đã được tạo, chưa áp dụng vào database đang dùng. Kiểm tra script và chạy migration trước khi bật các nút OAuth trên môi trường triển khai. Cần cho phép đổi collation/cập nhật index trên bảng ExternalLogin; thao tác có thể giữ khóa bảng trong thời gian triển khai.

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

**Tài khoản thật:** chưa thử Google, Facebook hoặc LinkedIn với tài khoản thật. Cần điền OAuth credentials/callback, quyền app và SMTP của môi trường đích; không đưa bí mật vào hội thoại. Sau cấu hình, chạy smoke test bằng test users của từng dashboard: đăng ký → logout → login lại; từ chối quyền/hủy; email thiếu/trùng; đăng ký Nhà tuyển dụng và xác nhận Pending; đổi vai trò; gọi trực tiếp employer Facebook/LinkedIn và xác nhận 403.

Báo cáo tự động lưu tại `.artifacts/external-auth-checks/` của workspace. Các kết quả thực tế của lần bàn giao được ghi trong báo cáo JSON và phản hồi cuối.

### Kết quả lần kiểm tra này

- Frontend: `npm.cmd run check` đạt ESLint và build. Build còn cảnh báo có sẵn về `/images/profile-reference.png` và kích thước bundle.
- Backend: build API và test harness thành công, 0 warning và 0 error.
- 118 kiểm tra backend/giao thức đạt. Bao gồm tạo tài khoản, đăng nhập, refresh/logout, email thiếu/trùng, liên kết bằng mật khẩu hoặc phiên đã đăng nhập, đổi mật khẩu tạm, số lần thử sai, replay/state/cookie, từ chối sai vai trò và ID token không hợp lệ.
- 62 kiểm tra Chrome đạt trên desktop 1440px và mobile 390px, không có lỗi runtime. Bao gồm đăng ký và đăng nhập bằng từng phương thức được phép, email OTP, thông tin doanh nghiệp, liên kết khi trùng email, quên mật khẩu/đổi mật khẩu tạm, hủy và lỗi/chưa cấu hình nhà cung cấp.
- Đã xem ảnh desktop/mobile; nút mạng xã hội và tab giữ đúng vai trò, email dài trong OTP xuống dòng trong màn hình.
- Database và nhà cung cấp trong kiểm tra là giả lập. Chưa chạy kiểm tra SQL Server staging, SMTP thật hoặc OAuth bằng tài khoản thật. Test harness bỏ giới hạn tốc độ để chạy toàn bộ kịch bản liên tiếp; policy production hiện có vẫn được giữ trong API.
- Script migration đã được xuất và kiểm tra tại `.artifacts/external-auth-checks/migration.sql`; chưa thực thi trên database đang dùng.

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
