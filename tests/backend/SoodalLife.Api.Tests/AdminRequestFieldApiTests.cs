using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Catalog;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class AdminRequestFieldApiTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Admin_CanReadAssignmentAndStructuredOptions()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);

        var fields = await client.GetFromJsonAsync<List<AdminRequestFieldResponse>>(FieldsPath(factory.Catalog.ServiceId));
        Assert.NotNull(fields);
        Assert.Equal(3, fields.Count);
        Assert.Equal([1, 2, 3], fields.Select(field => field.DisplayOrder).ToArray());
        Assert.All(fields, field => Assert.Contains(field.AssignmentScope, new[] { "MIDDLE_DEFAULT", "SERVICE_OVERRIDE" }));

        var selected = fields.Single(field => field.InputType == "SELECT");
        Assert.Equal(["주거", "상가"], selected.Options.Select(option => option.Value).ToArray());
        Assert.Equal([1, 2], selected.Options.Select(option => option.DisplayOrder).ToArray());
        Assert.All(selected.Options, option => Assert.True(option.IsActive));
    }

    [Theory]
    [InlineData(RoleCodes.Customer)]
    [InlineData(RoleCodes.Provider)]
    public async Task NonAdmin_CannotAccessRequestFieldAdminApi(string role)
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[role]);
        var response = await client.GetAsync(FieldsPath(factory.Catalog.ServiceId));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DefinitionUpdate_PreservesDefinitionIdentityAndExistingAnswers()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);
        var fieldId = factory.Catalog.FieldIds[2];
        var path = $"{FieldsPath(factory.Catalog.ServiceId)}/{fieldId}/definition";
        var baseline = await ReadPreservationStateAsync(fieldId);

        var unconfirmed = await client.PutAsJsonAsync(path, DefinitionUpdate("공간 유형을 선택해 주세요.", false));
        Assert.Equal(HttpStatusCode.Conflict, unconfirmed.StatusCode);

        try
        {
            var response = await client.PutAsJsonAsync(path, DefinitionUpdate("공간 유형을 선택해 주세요.", true));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updated = await response.Content.ReadFromJsonAsync<AdminRequestFieldResponse>();
            Assert.Equal("공간 유형을 선택해 주세요.", updated!.Label);
            Assert.Equal(baseline, await ReadPreservationStateAsync(fieldId));
        }
        finally
        {
            await client.PutAsJsonAsync(path, DefinitionUpdate("공간 유형", true));
        }
    }

    [Fact]
    public async Task ServiceAssignment_ChangesRequiredOrderAndActiveWithoutChangingOtherService()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);
        var fieldId = factory.Catalog.FieldIds[2];
        var path = $"{FieldsPath(factory.Catalog.ServiceId)}/{fieldId}/assignment";
        var answerCount = await CountAnswersAsync();

        try
        {
            var response = await client.PutAsJsonAsync(path, new { IsActive = false, IsRequired = false, DisplayOrder = 1 });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updated = await response.Content.ReadFromJsonAsync<AdminRequestFieldResponse>();
            Assert.False(updated!.ServiceEnabled);
            Assert.False(updated.Required);
            Assert.Equal(1, updated.DisplayOrder);
            Assert.Equal("SERVICE_OVERRIDE", updated.AssignmentScope);

            var otherFields = await client.GetFromJsonAsync<List<AdminRequestFieldResponse>>(FieldsPath(factory.Catalog.OtherServiceId));
            var other = otherFields!.Single(field => field.Id == fieldId);
            Assert.True(other.ServiceEnabled);
            Assert.True(other.Required);
            Assert.Equal(3, other.DisplayOrder);

            using var customerClient = CreateClient();
            await LoginAsync(customerClient, factory.Credentials[RoleCodes.Customer]);
            var selectedServiceFields = await customerClient.GetFromJsonAsync<List<RequestFieldResponse>>($"/api/v1/categories/{factory.Catalog.ServiceId}/request-fields");
            var otherServiceFields = await customerClient.GetFromJsonAsync<List<RequestFieldResponse>>($"/api/v1/categories/{factory.Catalog.OtherServiceId}/request-fields");
            Assert.DoesNotContain(selectedServiceFields!, field => field.Id == fieldId);
            Assert.Contains(otherServiceFields!, field => field.Id == fieldId);
            Assert.Equal(answerCount, await CountAnswersAsync());
        }
        finally
        {
            await client.PutAsJsonAsync(path, new { IsActive = true, IsRequired = true, DisplayOrder = 3 });
        }
    }

    [Fact]
    public async Task OptionUpdate_ChangesLabelOrderAndActiveButKeepsValueAndWritesAuditLog()
    {
        using var client = CreateClient();
        await LoginAsync(client, factory.Credentials[RoleCodes.Admin]);
        var fieldId = factory.Catalog.FieldIds[2];
        var field = await client.GetFromJsonAsync<AdminRequestFieldResponse>($"{FieldsPath(factory.Catalog.ServiceId)}/{fieldId}");
        var option = field!.Options.Single(item => item.Value == "주거");
        var path = $"{FieldsPath(factory.Catalog.ServiceId)}/{fieldId}/options/{option.Id}";

        try
        {
            var response = await client.PutAsJsonAsync(path, new { Label = "주거 공간", DisplayOrder = 2, IsActive = false });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updated = await response.Content.ReadFromJsonAsync<AdminRequestFieldResponse>();
            var changed = updated!.Options.Single(item => item.Id == option.Id);
            Assert.Equal("주거", changed.Value);
            Assert.Equal("주거 공간", changed.Label);
            Assert.Equal(2, changed.DisplayOrder);
            Assert.False(changed.IsActive);

            using var customerClient = CreateClient();
            await LoginAsync(customerClient, factory.Credentials[RoleCodes.Customer]);
            var customerFields = await customerClient.GetFromJsonAsync<List<RequestFieldResponse>>($"/api/v1/categories/{factory.Catalog.ServiceId}/request-fields");
            Assert.DoesNotContain("주거", customerFields!.Single(item => item.Id == fieldId).Options);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            Assert.True(await db.AuditLogs.AnyAsync(log => log.ActionCode == "REQUEST_FIELD_OPTION_UPDATED" && log.EntityPublicId == option.Id));
        }
        finally
        {
            await client.PutAsJsonAsync(path, new { Label = "주거", DisplayOrder = 1, IsActive = true });
        }
    }

    private static object DefinitionUpdate(string label, bool confirmSharedChange) => new
    {
        Label = label, InputType = "SELECT", StatusCode = "ACTIVE", Unit = (string?)null,
        ValidationRule = "선택", ConfirmSharedChange = confirmSharedChange,
    };

    private async Task<(long Id, string SourceFieldId, string FieldKey, int AnswerCount)> ReadPreservationStateAsync(Guid fieldId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var field = await db.CategoryFieldDefinitions.AsNoTracking().SingleAsync(item => item.PublicId == fieldId);
        return (field.Id, field.SourceFieldId, field.FieldKey, await db.RequestAnswers.CountAsync());
    }

    private async Task<int> CountAnswersAsync()
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>().RequestAnswers.CountAsync();
    }

    private static string FieldsPath(Guid serviceId) => $"/api/v1/admin/service-categories/services/{serviceId}/request-fields";
    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, TestCredential credential) => client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
}
