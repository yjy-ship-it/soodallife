namespace SoodalLife.Api.Features.Admin;

internal static class AdminPrivacy
{
    public static string? Phone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length < 7 ? "연락처 등록됨" : $"{digits[..3]}-****-{digits[^4..]}";
    }

    public static string? Email(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var at = value.IndexOf('@');
        return at <= 0 || at == value.Length - 1 ? "이메일 등록됨" : $"{value[0]}***{value[at..]}";
    }

    public static string? BusinessNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length == 10 ? $"{digits[..3]}-**-{digits[^5..]}" : "사업자번호 등록됨";
    }

    public static string? DocumentNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var visible = Math.Min(4, value.Length);
        return new string('*', Math.Max(4, value.Length - visible)) + value[^visible..];
    }

    public static string? DetailAddress(string? value) => string.IsNullOrWhiteSpace(value) ? null : "상세주소 마스킹";
}
