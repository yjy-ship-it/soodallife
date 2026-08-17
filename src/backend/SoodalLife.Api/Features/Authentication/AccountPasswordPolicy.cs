namespace SoodalLife.Api.Features.Authentication;

public static class AccountPasswordPolicy
{
    public const int MinimumLength = 6;
    public const string Guidance = "비밀번호는 6자 이상이며 문자, 숫자, 특수문자를 각각 포함해야 합니다.";

    public static bool IsSatisfied(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length < MinimumLength || value.Any(char.IsWhiteSpace))
            return false;

        var hasLetter = value.Any(IsAsciiLetter);
        var hasDigit = value.Any(char.IsDigit);
        var hasSpecial = value.Any(character => !char.IsLetterOrDigit(character) && !char.IsWhiteSpace(character));
        return hasLetter && hasDigit && hasSpecial;
    }

    private static bool IsAsciiLetter(char value) => value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
}
