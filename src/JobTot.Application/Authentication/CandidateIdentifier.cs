using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace JobTot.Application.Authentication;

public sealed record CandidateIdentifier(string? Email, string? Phone)
{
    public static CandidateIdentifier Parse(string value)
    {
        if (TryParse(value, out var identifier))
            return identifier!;
        throw new ArgumentException(
            value.Contains('@')
                ? "Email không hợp lệ."
                : "Nhập email hoặc số điện thoại di động Việt Nam hợp lệ."
        );
    }

    public static bool TryParse(string? value, out CandidateIdentifier? identifier)
    {
        identifier = null;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        value = value.Trim();
        if (value.Contains('@'))
        {
            if (value.Length > 320 || !new EmailAddressAttribute().IsValid(value))
                return false;
            identifier = new(value.ToLowerInvariant(), null);
            return true;
        }

        // Vietnamese mobile numbers: 09..., +849..., 849... share one identity.
        var phone = Regex.Replace(value, @"[\s().-]", "");
        if (phone.StartsWith("+84"))
            phone = "0" + phone[3..];
        else if (phone.StartsWith("84"))
            phone = "0" + phone[2..];
        if (!Regex.IsMatch(phone, @"\A0[35789][0-9]{8}\z"))
            return false;
        identifier = new(null, "+84" + phone[1..]);
        return true;
    }
}
