using System.Net;

namespace JobTot.Application.Authentication;

public sealed record AccountEmailContent(string Text, string Html);

public static class AccountEmailTemplates
{
    public static AccountEmailContent Verification(string code) => new(
        $"Mã xác thực của bạn là: {code}\nMã có hiệu lực 1 phút. Không chia sẻ mã này cho người khác.",
        Layout("Xác thực email của bạn", "XÁC THỰC TÀI KHOẢN",
            "Chỉ một bước nữa để bắt đầu! Nhập mã bên dưới trên trang Job Tốt để xác thực địa chỉ email của bạn.",
            "MÃ XÁC THỰC", code, "1 phút", false,
            "Không chia sẻ mã xác thực với bất kỳ ai, kể cả người tự xưng là nhân viên Job Tốt.",
            "Nếu bạn không yêu cầu xác thực email, hãy bỏ qua thư này."));

    public static AccountEmailContent TemporaryPassword(string password) => new(
        $"Mật khẩu tạm của bạn: {password}\nCó hiệu lực 10 phút. Đăng nhập bằng mật khẩu này để đặt mật khẩu mới.\nNếu không yêu cầu, hãy bỏ qua email; mật khẩu hiện tại chưa thay đổi.",
        Layout("Lấy lại quyền truy cập", "KHÔI PHỤC MẬT KHẨU",
            "Chúng mình đã nhận được yêu cầu khôi phục mật khẩu của bạn. Dùng mật khẩu tạm bên dưới để đăng nhập Job Tốt và đặt mật khẩu mới.",
            "MẬT KHẨU TẠM", password, "10 phút", true,
            "Sau khi đăng nhập bằng mật khẩu tạm, bạn cần đặt mật khẩu mới để tiếp tục. Không chia sẻ mật khẩu này với bất kỳ ai.",
            "Nếu bạn không gửi yêu cầu này, hãy bỏ qua thư. Mật khẩu hiện tại của bạn chưa thay đổi."));

    private static string Layout(string title, string category, string introduction,
        string credentialLabel, string credential, string lifetime, bool isPassword,
        string securityNote, string unsolicitedNote)
    {
        static string Encode(string value) => WebUtility.HtmlEncode(value);
        const string font = "Tahoma,Arial,Helvetica,sans-serif";
        const string brandFont = "Verdana,Geneva,Arial,sans-serif";
        var heroTitle = isPassword ? "Trở lại hành trình của bạn." : "Cơ hội mới bắt đầu từ đây.";
        var fontSize = isPassword ? "19px" : "42px";
        var letterSpacing = isPassword ? "0.5px" : "7px";
        return $$"""
            <!doctype html>
            <html lang="vi">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta name="color-scheme" content="light">
              <title>{{Encode(title)}} · Job Tốt</title>
              <style>
                body, table, td, p, h1, span { font-family: {{font}}; }
                @media only screen and (max-width:480px) {
                  .email-content { padding-left:20px !important; padding-right:20px !important; }
                  .email-title { font-size:26px !important; }
                  .email-outer { padding:16px 8px !important; }
                }
              </style>
            </head>
            <body style="margin:0;padding:0;background-color:#f7f5f2;color:#203547;font-family:{{font}};-webkit-text-size-adjust:100%;">
              <div style="display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;mso-hide:all;">{{Encode(title)}}. Thông tin có hiệu lực {{Encode(lifetime)}}. Vui lòng không chia sẻ với người khác.</div>
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background-color:#f7f5f2;">
                <tr><td class="email-outer" align="center" style="padding:36px 12px;">
                  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="max-width:580px;">
                    <tr><td class="email-content" bgcolor="#c63621" style="padding:30px 32px 34px;background-color:#c63621;background-image:radial-gradient(ellipse at 100% 0%,#ffce55 0%,transparent 48%),radial-gradient(ellipse at 100% 100%,#ff7024 0%,transparent 60%),linear-gradient(115deg,#b82b22 0%,#ee4422 55%,#ff901f 100%);border-radius:20px 20px 0 0;">
                      <p style="margin:0;font-family:{{brandFont}};font-size:30px;line-height:1.5;font-weight:700;letter-spacing:-1px;color:#ffffff;"><span style="font-family:{{brandFont}};font-size:30px;line-height:1.5;font-weight:700;color:#ffffff;white-space:nowrap;">Job Tốt</span></p>
                      <p style="margin:4px 0 0;font-family:{{font}};font-size:11px;line-height:1.8;color:#fff3e9;letter-spacing:0.3px;">CƠ HỘI MỚI. HÀNH TRÌNH MỚI.</p>
                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                        <tr><td style="padding-top:26px;font-family:{{font}};color:#ffffff;"><span style="display:block;width:32px;border-top:3px solid #ffc998;font-size:1px;line-height:1px;">&nbsp;</span></td></tr>
                        <tr><td style="padding-top:13px;font-family:{{font}};font-size:23px;line-height:1.5;font-weight:700;color:#ffffff;">{{Encode(heroTitle)}}</td></tr>
                      </table>
                    </td></tr>
                    <tr><td style="background-color:#ffffff;border:1px solid #e0e6eb;border-top:0;border-radius:0 0 20px 20px;">
                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                        <tr><td class="email-content" style="padding:30px 32px 0;font-family:{{font}};">
                          <span style="display:inline-block;padding:7px 10px;background-color:#fff0ea;border:1px solid #f9ded4;border-radius:6px;font-family:{{font}};color:#b94c38;font-size:10px;line-height:1.5;font-weight:700;letter-spacing:1px;">{{Encode(category)}}</span>
                        </td></tr>
                        <tr><td class="email-content" style="padding:17px 32px 0;"><h1 class="email-title" style="margin:0;font-family:{{font}};font-size:30px;line-height:1.4;font-weight:700;letter-spacing:-0.5px;color:#183244;">{{Encode(title)}}</h1></td></tr>
                        <tr><td class="email-content" style="padding:14px 32px 26px;font-family:{{font}};font-size:14px;line-height:1.9;color:#657583;"><strong style="font-family:{{font}};color:#344b5d;">Xin chào bạn,</strong><br>{{Encode(introduction)}}</td></tr>
                        <tr><td class="email-content" style="padding:0 32px;">
                          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background-color:#fff6ef;border:1px solid #f4d4c2;border-radius:16px;">
                            <tr><td align="center" style="padding:24px 12px 10px;font-family:{{font}};color:#a56b58;font-size:10px;font-weight:700;letter-spacing:2px;">{{Encode(credentialLabel)}}</td></tr>
                            <tr><td align="center" style="padding:0 12px 17px;font-family:Consolas,'Courier New',monospace;font-size:{{fontSize}};font-weight:700;line-height:1.5;letter-spacing:{{letterSpacing}};color:#b93623;overflow-wrap:anywhere;word-break:break-all;">{{Encode(credential)}}</td></tr>
                            <tr><td align="center" style="padding:0 12px 25px;font-family:{{font}};"><span style="display:inline-block;padding:7px 13px;border-radius:20px;background-color:#ffe6dc;font-family:{{font}};font-size:12px;line-height:1.6;color:#a74430;">Hết hạn sau <strong style="font-family:{{font}};">{{Encode(lifetime)}}</strong></span></td></tr>
                          </table>
                        </td></tr>
                        <tr><td class="email-content" style="padding:26px 32px;">
                          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr><td style="padding-left:13px;border-left:3px solid #ef705a;font-family:{{font}};font-size:12px;line-height:1.9;color:#657583;"><strong style="font-family:{{font}};color:#344b5d;font-size:13px;">Một lưu ý nhỏ để an tâm hơn</strong><br>{{Encode(securityNote)}}</td></tr>
                          </table>
                        </td></tr>
                        <tr><td class="email-content" style="padding:20px 32px 26px;background-color:#fafbfc;border-top:1px solid #edf0f3;border-radius:0 0 20px 20px;font-family:{{font}};font-size:11px;line-height:1.9;color:#7b8994;">{{Encode(unsolicitedNote)}}</td></tr>
                      </table>
                    </td></tr>
                    <tr><td align="center" style="padding:22px 20px 0;font-family:{{font}};color:#8796a2;font-size:10px;line-height:1.9;">Đồng hành cùng bạn trên hành trình sự nghiệp.<br>Email tự động từ Job Tốt · Vui lòng không trả lời thư này.</td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }
}
