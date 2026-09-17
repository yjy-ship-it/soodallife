using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using SoodalLife.Api.Features.CustomerAccounts;

namespace SoodalLife.Api.Tests;

public sealed class PrelaunchPhoneRegistrationApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task ExplicitPrelaunchFlagEnablesDuplicateCheckOnlyWithoutDevelopmentEnvironment()
    {
        using var application = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Prelaunch:AllowPhoneDuplicateCheckOnly"] = "true",
            })));
        using var client = application.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var status = await client.GetFromJsonAsync<IdentityVerificationStatus>(
            "/api/v1/public/customer-account/identity-verification/status");
        Assert.Equal("DUPLICATE_CHECK_ONLY", status!.StatusCode);

        var suffix = Guid.NewGuid().ToString("N");
        var customer = new RegisterCustomerRequest(
            $"customer-{suffix}"[..20],
            "사전운영 고객",
            $"customer-{suffix}@example.com",
            $"010{RandomNumberGenerator.GetInt32(10_000_000, 100_000_000)}",
            "Aa!12345678",
            "Aa!12345678",
            null,
            Array.Empty<RegistrationConsentRequest>());
        var customerResponse = await client.PostAsJsonAsync("/api/v1/public/customer-account/register", customer);
        Assert.Equal(HttpStatusCode.OK, customerResponse.StatusCode);

        var input = new
        {
            loginId = $"prelaunch-{suffix}",
            password = "Aa!12345678",
            passwordConfirmation = "Aa!12345678",
            email = (string?)null,
            phone = $"010{RandomNumberGenerator.GetInt32(10_000_000, 100_000_000)}",
            phoneVerificationToken = (string?)null,
            providerTypeCode = "BUSINESS",
            businessName = $"Prelaunch {suffix}",
            representativeName = "대표자",
            contactName = "담당자",
            businessRegistrationNumber = BusinessNumber(),
            businessAddress = "서울시 테스트구",
            businessTypeText = "서비스",
            businessItemText = "생활서비스",
            introduction = "사전운영 테스트 전문가",
            consents = Array.Empty<object>(),
        };
        var response = await client.PostAsJsonAsync("/api/v1/public/provider-registration", input);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static string BusinessNumber()
    {
        var firstNine = RandomNumberGenerator.GetInt32(100_000_000, 1_000_000_000).ToString();
        var digits = firstNine.Select(value => value - '0').ToArray();
        int[] weights = [1, 3, 7, 1, 3, 7, 1, 3, 5];
        var sum = weights.Select((weight, index) => weight * digits[index]).Sum() + digits[8] * 5 / 10;
        return firstNine + ((10 - sum % 10) % 10);
    }
}
