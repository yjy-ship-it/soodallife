using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Security;

public sealed class PersonalDataReadInterceptor(IPersonalDataReader reader, IOptions<PrivacyProtectionOptions> options) : IMaterializationInterceptor
{
    public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
    {
        if (!options.Value.EncryptedReadEnabled) return entity;
        switch (entity)
        {
            case User value:
                value.Email = reader.Read(value.EmailEncrypted, value.Email);
                value.NormalizedEmail = value.Email is null ? null : PersonalDataNormalizer.Email(value.Email);
                value.Phone = reader.Read(value.PhoneEncrypted, value.Phone);
                break;
            case CustomerAddress value:
                value.RecipientName = reader.Read(value.RecipientNameEncrypted, value.RecipientName);
                value.RoadAddress = reader.Read(value.RoadAddressEncrypted, value.RoadAddress) ?? value.RoadAddress;
                value.DetailAddress = reader.Read(value.DetailAddressEncrypted, value.DetailAddress) ?? value.DetailAddress;
                break;
            case ProviderProfile value:
                value.BusinessAddress = reader.Read(value.BusinessAddressEncrypted, value.BusinessAddress);
                break;
            case ServiceRequest value:
                value.DetailAddress = reader.Read(value.DetailAddressEncrypted, value.DetailAddress);
                break;
            case SubscriptionRequest value:
                value.DetailAddress = reader.Read(value.DetailAddressEncrypted, value.DetailAddress);
                break;
        }
        return entity;
    }
}
