using SoodalLife.Api.Features.Authentication;

namespace SoodalLife.Api.Tests;

public sealed class AccountPasswordPolicyTests
{
    [Theory]
    [InlineData("abc1!x")]
    [InlineData("ABC1!X")]
    [InlineData("long-password9!")]
    public void Accepts_SixOrMore_WithLetterDigitAndSpecial_WithoutUppercaseRequirement(string value)
        => Assert.True(AccountPasswordPolicy.IsSatisfied(value));

    [Theory]
    [InlineData("ab1!x")]
    [InlineData("abcdef!")]
    [InlineData("123456!")]
    [InlineData("abc123")]
    [InlineData("abc 1!")]
    public void Rejects_MissingRequiredComponentOrWhitespace(string value)
        => Assert.False(AccountPasswordPolicy.IsSatisfied(value));
}
