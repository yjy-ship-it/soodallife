namespace SoodalLife.Api.Features.Authentication;

public static class AccountPasswordPolicy
{
    public const int MinimumLength = 6;
    public const int MaximumLength = 128;
    public const string Guidance = "비밀번호는 6~128자이며 영문자, 숫자, 특수문자를 각각 포함해야 합니다.";

    public static bool IsSatisfied(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length < MinimumLength || value.Length > MaximumLength || value.Any(char.IsWhiteSpace))
            return false;

        var hasLetter = value.Any(IsAsciiLetter);
        var hasDigit = value.Any(IsAsciiDigit);
        var hasSpecial = value.Any(IsAsciiSpecial);
        return hasLetter && hasDigit && hasSpecial;
    }

    private static bool IsAsciiLetter(char value) => value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    private static bool IsAsciiDigit(char value) => value is >= '0' and <= '9';
    private static bool IsAsciiSpecial(char value) => value is >= '!' and <= '/' or >= ':' and <= '@' or >= '[' and <= '`' or >= '{' and <= '~';
}
