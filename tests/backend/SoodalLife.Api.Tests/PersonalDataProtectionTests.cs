using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.CustomerAccounts;
using SoodalLife.Api.Infrastructure.Persistence;
using SoodalLife.Api.Infrastructure.Security;

namespace SoodalLife.Api.Tests;

public sealed class PersonalDataProtectionTests
{
    [Fact]
    public void Normalizers_are_stable_and_purpose_specific()
    {
        Assert.Equal("CUSTOMER@EXAMPLE.KR", PersonalDataNormalizer.Email(" customer@example.kr "));
        Assert.Equal("01012345678", PersonalDataNormalizer.Phone("010-1234-5678"));
        var hasher = Hasher();
        Assert.Equal(hasher.Email("customer@example.kr"), hasher.Email(" CUSTOMER@EXAMPLE.KR "));
        Assert.Equal(hasher.Phone("010-1234-5678"), hasher.Phone("01012345678"));
        Assert.False(hasher.Email("01012345678").SequenceEqual(hasher.Phone("01012345678")));
    }

    [Fact]
    public void Data_protection_round_trips_without_exposing_plaintext()
    {
        var collection = new ServiceCollection();
        collection.AddDataProtection().UseEphemeralDataProtectionProvider();
        using var services = collection.BuildServiceProvider();
        var protector = new DataProtectionPersonalDataProtector(services.GetRequiredService<IDataProtectionProvider>());
        const string value = "\uC11C\uC6B8\uC2DC \uD14C\uC2A4\uD2B8 \uC0C1\uC138\uC8FC\uC18C 101\uB3D9 1203\uD638";
        var encrypted = protector.Protect(value);
        Assert.NotEqual(value, System.Text.Encoding.UTF8.GetString(encrypted));
        Assert.Equal(value, protector.Unprotect(encrypted));
        encrypted[^1] ^= 0xff;
        Assert.ThrowsAny<CryptographicException>(() => protector.Unprotect(encrypted));
    }

    [Theory]
    [InlineData(PrivacyAudience.Public, PrivacyField.Phone, false, false, false)]
    [InlineData(PrivacyAudience.CustomerSelf, PrivacyField.Email, false, true, false)]
    [InlineData(PrivacyAudience.SelectedProvider, PrivacyField.Phone, true, true, false)]
    [InlineData(PrivacyAudience.SelectedProvider, PrivacyField.Email, true, false, false)]
    [InlineData(PrivacyAudience.AdminMasked, PrivacyField.DetailAddress, false, true, true)]
    public void Privacy_contract_enforces_audience(string audience, PrivacyField field, bool assigned, bool access, bool mask)
    {
        var decision = new PrivacyContract().Decide(audience, field, assigned);
        Assert.Equal(access, decision.CanAccess);
        Assert.Equal(mask, decision.MustMask);
    }

    private static HmacPersonalDataSearchHasher Hasher() => new(Options.Create(new PrivacyProtectionOptions
    {
        SearchHashKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
    }));
}

public sealed class PersonalDataDualWriteTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Protected_fields_are_dual_written_without_entering_api_contracts()
    {
        _ = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var protector = scope.ServiceProvider.GetRequiredService<SoodalLife.Api.Infrastructure.Security.IPersonalDataProtector>();
        var customer = db.CustomerProfiles.Single(x => x.UserId == db.Users.Single(u => u.LoginId == factory.Credentials[RoleCodes.Customer].LoginId).Id);
        var user = db.Users.Single(x => x.Id == customer.UserId);
        user.Email = "privacy-customer@example.kr";
        user.NormalizedEmail = PersonalDataNormalizer.Email(user.Email);
        user.Phone = "010-1234-5678";

        var address = new CustomerAddress
        {
            CustomerProfileId = customer.Id,
            AddressName = "test",
            RecipientName = "customer",
            PostalCode = "12345",
            RoadAddress = "test road 1",
            DetailAddress = "101-1203",
        };
        db.CustomerAddresses.Add(address);
        await db.SaveChangesAsync();

        Assert.Equal((short)1, user.PrivacyProtectionVersion);
        Assert.NotNull(user.EmailEncrypted);
        Assert.NotNull(user.EmailSearchHash);
        Assert.NotNull(user.PhoneEncrypted);
        Assert.NotNull(user.PhoneSearchHash);
        Assert.Equal(user.Email, protector.Unprotect(user.EmailEncrypted!));
        Assert.Equal(user.Phone, protector.Unprotect(user.PhoneEncrypted!));
        Assert.Equal((short)1, address.PrivacyProtectionVersion);
        Assert.Equal(address.RecipientName, protector.Unprotect(address.RecipientNameEncrypted!));
        Assert.Equal(address.RoadAddress, protector.Unprotect(address.RoadAddressEncrypted!));
        Assert.Equal(address.DetailAddress, protector.Unprotect(address.DetailAddressEncrypted!));

        var apiFields = typeof(CustomerProfileResponse).GetProperties().Select(x => x.Name).ToArray();
        Assert.DoesNotContain(apiFields, x => x.Contains("Encrypted", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(apiFields, x => x.Contains("SearchHash", StringComparison.OrdinalIgnoreCase));
    }
}
