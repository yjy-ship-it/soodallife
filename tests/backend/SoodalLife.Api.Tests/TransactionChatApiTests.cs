using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Chat;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class TransactionChatApiTests(AuthenticationWebApplicationFactory factory) : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task LazyCreate_IsIdempotent_AndCreatesOnlyTransactionParties()
    {
        var transactionId = await SeedTransaction();
        using var customer = Client(); await Login(customer, factory.Credentials[RoleCodes.Customer]);
        var first = await customer.GetFromJsonAsync<ChatRoomDetail>($"/api/v1/chat/transactions/{transactionId}/room");
        var second = await customer.GetFromJsonAsync<ChatRoomDetail>($"/api/v1/chat/transactions/{transactionId}/room");
        Assert.NotNull(first); Assert.Equal(first!.Id, second!.Id); Assert.Equal("ACTIVE", first.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var room = await db.ChatRooms.SingleAsync(x => x.ResourcePublicId == transactionId);
        var participants = await db.ChatParticipants.Where(x => x.ChatRoomId == room.Id).OrderBy(x => x.ParticipantRoleCode).ToListAsync();
        Assert.Equal(2, participants.Count); Assert.Equal(["CUSTOMER", "PROVIDER"], participants.Select(x => x.ParticipantRoleCode));
        Assert.All(participants, value => Assert.Equal("ACTIVE", value.StatusCode));
    }

    [Fact]
    public async Task ObjectAuthorization_HidesRoomFromUnselectedProvider_OtherCustomer_AndAdmin()
    {
        var transactionId = await SeedTransaction();
        using var customer = Client(); using var provider = Client(); using var otherProvider = Client(); using var otherCustomer = Client(); using var admin = Client();
        await Login(customer, factory.Credentials[RoleCodes.Customer]); await Login(provider, factory.Credentials[RoleCodes.Provider]);
        await Login(otherProvider, factory.AreaMismatchProviderCredential); await Login(otherCustomer, factory.OtherCustomerCredential); await Login(admin, factory.Credentials[RoleCodes.Admin]);
        var room = await customer.GetFromJsonAsync<ChatRoomDetail>($"/api/v1/chat/transactions/{transactionId}/room");
        Assert.Equal(HttpStatusCode.OK, (await provider.GetAsync($"/api/v1/chat/rooms/{room!.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherProvider.GetAsync($"/api/v1/chat/rooms/{room.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherCustomer.GetAsync($"/api/v1/chat/rooms/{room.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync($"/api/v1/chat/rooms/{room.Id}")).StatusCode);
        Assert.Empty((await otherProvider.GetFromJsonAsync<List<ChatRoomListItem>>("/api/v1/chat/rooms"))!);
    }

    [Fact]
    public async Task Text_Idempotency_Pagination_Read_AndUnread_WorkWithoutInternalIdentifiers()
    {
        var transactionId = await SeedTransaction();
        using var customer = Client(); using var provider = Client();
        await Login(customer, factory.Credentials[RoleCodes.Customer]); await Login(provider, factory.Credentials[RoleCodes.Provider]);
        var room = await customer.GetFromJsonAsync<ChatRoomDetail>($"/api/v1/chat/transactions/{transactionId}/room");
        var key = Guid.NewGuid().ToString("N");
        var firstResponse = await customer.PostAsJsonAsync($"/api/v1/chat/rooms/{room!.Id}/messages/text", new { body = "안전한 거래 채팅입니다.", idempotencyKey = key });
        firstResponse.EnsureSuccessStatusCode();
        var first = (await firstResponse.Content.ReadFromJsonAsync<ChatMessageResponse>())!;
        var repeated = (await (await customer.PostAsJsonAsync($"/api/v1/chat/rooms/{room.Id}/messages/text", new { body = "중복", idempotencyKey = key })).Content.ReadFromJsonAsync<ChatMessageResponse>())!;
        Assert.Equal(first.Id, repeated.Id);
        for (var index = 0; index < 34; index++)
            (await provider.PostAsJsonAsync($"/api/v1/chat/rooms/{room.Id}/messages/text", new { body = $"전문가 메시지 {index}", idempotencyKey = Guid.NewGuid().ToString("N") })).EnsureSuccessStatusCode();
        var unread = await customer.GetFromJsonAsync<ChatUnreadCountResponse>("/api/v1/chat/unread-count");
        Assert.Equal(34, unread!.Count);
        var pageResponse = await customer.GetAsync($"/api/v1/chat/rooms/{room.Id}/messages?pageSize=10");
        var raw = await pageResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("senderUserId", raw, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("chatRoomId", raw, StringComparison.OrdinalIgnoreCase);
        var page = await pageResponse.Content.ReadFromJsonAsync<ChatMessagePage>();
        Assert.Equal(10, page!.Items.Count); Assert.True(page.HasMore); Assert.NotNull(page.NextCursor);
        var older = await customer.GetFromJsonAsync<ChatMessagePage>($"/api/v1/chat/rooms/{room.Id}/messages?pageSize=10&before={page.NextCursor}");
        Assert.Equal(10, older!.Items.Count); Assert.Empty(page.Items.Select(x => x.Id).Intersect(older.Items.Select(x => x.Id)));
        var latest = page.Items[^1];
        Assert.Equal(HttpStatusCode.NoContent, (await customer.PostAsJsonAsync($"/api/v1/chat/rooms/{room.Id}/read", new { messageId = latest.Id })).StatusCode);
        Assert.Equal(0, (await customer.GetFromJsonAsync<ChatUnreadCountResponse>("/api/v1/chat/unread-count"))!.Count);
        Assert.Equal(HttpStatusCode.NoContent, (await customer.PostAsJsonAsync($"/api/v1/chat/rooms/{room.Id}/read", new { messageId = latest.Id })).StatusCode);
    }

    [Theory]
    [InlineData("READ_ONLY")]
    [InlineData("CLOSED")]
    public async Task NonActiveRoom_BlocksNewMessages(string status)
    {
        var transactionId = await SeedTransaction(); using var customer = Client(); await Login(customer, factory.Credentials[RoleCodes.Customer]);
        var room = await customer.GetFromJsonAsync<ChatRoomDetail>($"/api/v1/chat/transactions/{transactionId}/room");
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var row = await db.ChatRooms.SingleAsync(x => x.PublicId == room!.Id); row.StatusCode = status; await db.SaveChangesAsync(); }
        var response = await customer.PostAsJsonAsync($"/api/v1/chat/rooms/{room!.Id}/messages/text", new { body = "차단되어야 합니다.", idempotencyKey = Guid.NewGuid().ToString("N") });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task FileMessage_ReusesStoredFile_FailsClosedForCounterparty_AndBlocksAttachmentIdor()
    {
        var transactionId = await SeedTransaction();
        using var customer = Client(); using var provider = Client(); using var otherProvider = Client();
        await Login(customer, factory.Credentials[RoleCodes.Customer]); await Login(provider, factory.Credentials[RoleCodes.Provider]); await Login(otherProvider, factory.AreaMismatchProviderCredential);
        var room = await customer.GetFromJsonAsync<ChatRoomDetail>($"/api/v1/chat/transactions/{transactionId}/room");
        using var form = new MultipartFormDataContent();
        var bytes = TestFileSamples.ValidPng();
        var content = new ByteArrayContent(bytes); content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(content, "file", "현장사진.png"); form.Add(new StringContent(Guid.NewGuid().ToString("N"), Encoding.UTF8), "idempotencyKey");
        var sentResponse = await customer.PostAsync($"/api/v1/chat/rooms/{room!.Id}/messages/file", form); sentResponse.EnsureSuccessStatusCode();
        var sent = (await sentResponse.Content.ReadFromJsonAsync<ChatMessageResponse>())!;
        Assert.Equal("FILE", sent.MessageTypeCode); Assert.True(sent.Attachment!.Available); Assert.NotNull(sent.Attachment.DownloadUrl);
        Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync(sent.Attachment.DownloadUrl)).StatusCode);
        var providerPage = await provider.GetFromJsonAsync<ChatMessagePage>($"/api/v1/chat/rooms/{room.Id}/messages");
        var providerFile = Assert.Single(providerPage!.Items).Attachment!;
        Assert.False(providerFile.Available); Assert.Null(providerFile.DownloadUrl); Assert.Contains("MALWARE_SCAN_NOT_CLEAN", providerFile.PublicationStatus);
        Assert.Equal(HttpStatusCode.NotFound, (await provider.GetAsync($"/api/v1/chat/rooms/{room.Id}/attachments/{sent.Attachment.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherProvider.GetAsync($"/api/v1/chat/rooms/{room.Id}/attachments/{sent.Attachment.Id}")).StatusCode);
        var raw = await sentResponse.Content.ReadAsStringAsync(); Assert.DoesNotContain("storageKey", raw, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("fileId", raw, StringComparison.OrdinalIgnoreCase);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var stored = await (from link in db.ChatAttachments join file in db.Files on link.FileId equals file.Id
            where link.PublicId == sent.Attachment.Id select file).SingleAsync();
        Assert.Equal("CHAT_ATTACHMENT", stored.PurposeCode); Assert.Equal("NOT_INTEGRATED", stored.MalwareScanStatusCode); Assert.Equal("NOT_INTEGRATED", stored.PrivacyInspectionStatusCode);
    }

    [Fact]
    public async Task FileDataMessage_AcceptsJsonTransport_AndCreatesAttachment()
    {
        var transactionId = await SeedTransaction();
        using var customer = Client(); await Login(customer, factory.Credentials[RoleCodes.Customer]);
        var room = await customer.GetFromJsonAsync<ChatRoomDetail>($"/api/v1/chat/transactions/{transactionId}/room");
        var bytes = TestFileSamples.ValidJpeg();
        var response = await customer.PostAsJsonAsync($"/api/v1/chat/rooms/{room!.Id}/messages/file-data", new
        {
            fileName = "현장사진.jpg",
            contentType = "image/jpeg",
            base64Data = Convert.ToBase64String(bytes),
            idempotencyKey = Guid.NewGuid().ToString("N"),
        });
        response.EnsureSuccessStatusCode();
        var message = await response.Content.ReadFromJsonAsync<ChatMessageResponse>();
        Assert.NotNull(message); Assert.Equal("FILE", message!.MessageTypeCode); Assert.Equal("현장사진.jpg", message.Attachment!.FileName);
        Assert.True(message.Attachment.Available); Assert.NotNull(message.Attachment.DownloadUrl);
    }

    [Fact]
    public async Task FileChunkMessage_AssemblesSmallRequests_AndCreatesOneAttachment()
    {
        var transactionId = await SeedTransaction();
        using var customer = Client(); await Login(customer, factory.Credentials[RoleCodes.Customer]);
        var room = await customer.GetFromJsonAsync<ChatRoomDetail>($"/api/v1/chat/transactions/{transactionId}/room");
        var bytes = new byte[40_000]; TestFileSamples.ValidJpeg().CopyTo(bytes, 0);
        var uploadId = Guid.NewGuid(); var idempotencyKey = Guid.NewGuid().ToString("N");
        var first = await customer.PostAsJsonAsync($"/api/v1/chat/rooms/{room!.Id}/messages/file-chunks", new
        {
            uploadId, fileName = "분할현장사진.jpg", contentType = "image/jpeg", chunkIndex = 0, totalChunks = 2,
            base64Chunk = Convert.ToBase64String(bytes[..32_768]), idempotencyKey,
        });
        first.EnsureSuccessStatusCode();
        var pending = await first.Content.ReadFromJsonAsync<ChatFileChunkResponse>();
        Assert.NotNull(pending); Assert.False(pending!.Completed); Assert.Null(pending.Message);
        var second = await customer.PostAsJsonAsync($"/api/v1/chat/rooms/{room.Id}/messages/file-chunks", new
        {
            uploadId, fileName = "분할현장사진.jpg", contentType = "image/jpeg", chunkIndex = 1, totalChunks = 2,
            base64Chunk = Convert.ToBase64String(bytes[32_768..]), idempotencyKey,
        });
        second.EnsureSuccessStatusCode();
        var completed = await second.Content.ReadFromJsonAsync<ChatFileChunkResponse>();
        Assert.NotNull(completed); Assert.True(completed!.Completed); Assert.NotNull(completed.Message);
        Assert.Equal("분할현장사진.jpg", completed.Message!.Attachment!.FileName);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        Assert.Single(await db.ChatMessages.Where(x => x.MessageTypeCode == "FILE").ToListAsync());
    }

    [Fact]
    public async Task RealtimeJoin_UsesSameObjectAuthorization_AndMessageOutboxContainsNoBody()
    {
        var transactionId = await SeedTransaction(); using var customer = Client(); await Login(customer, factory.Credentials[RoleCodes.Customer]);
        var room = await customer.GetFromJsonAsync<ChatRoomDetail>($"/api/v1/chat/transactions/{transactionId}/room");
        var response = await customer.PostAsJsonAsync($"/api/v1/chat/rooms/{room!.Id}/messages/text", new { body = "Outbox에 원문을 복제하지 않습니다.", idempotencyKey = Guid.NewGuid().ToString("N") }); response.EnsureSuccessStatusCode();
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var service = scope.ServiceProvider.GetRequiredService<ChatService>();
        var customerPrincipal = await Principal(factory.Credentials[RoleCodes.Customer].LoginId, db);
        var otherProviderPrincipal = await Principal(factory.AreaMismatchProviderCredential.LoginId, db);
        Assert.True(await service.CanJoin(room.Id, customerPrincipal, default)); Assert.False(await service.CanJoin(room.Id, otherProviderPrincipal, default));
        var outbox = await db.OutboxEvents.SingleAsync(x => x.AggregatePublicId == room.Id && x.EventType == "CHAT.MESSAGE.CREATED");
        Assert.DoesNotContain("Outbox에 원문", outbox.PayloadJson); Assert.Contains("messageId", outbox.PayloadJson);
        Assert.Single(await db.ChatMessages.Where(x => x.ChatRoomId == db.ChatRooms.Single(r => r.PublicId == room.Id).Id).ToListAsync());
    }

    private async Task<Guid> SeedTransaction()
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var customer = await (from user in db.Users join profile in db.CustomerProfiles on user.Id equals profile.UserId where user.LoginId == factory.Credentials[RoleCodes.Customer].LoginId select profile).SingleAsync();
        var provider = await (from user in db.Users join profile in db.ProviderProfiles on user.Id equals profile.UserId where user.LoginId == factory.Credentials[RoleCodes.Provider].LoginId select profile).SingleAsync();
        var category = await db.ServiceCategories.SingleAsync(x => x.PublicId == factory.Catalog.ServiceId); var now = DateTime.UtcNow;
        var transaction = new TransactionRecord { CustomerProfileId = customer.Id, ProviderProfileId = provider.Id, CategoryId = category.Id, StatusCode = "CREATED", AgreedAmount = 100000, CurrencyCode = "KRW", QuoteSnapshotJson = "{}", CategoryPolicySnapshotJson = "{}", CompletionPolicySnapshotJson = "{}", CreatedAt = now, UpdatedAt = now };
        db.Transactions.Add(transaction); await db.SaveChangesAsync(); return transaction.PublicId;
    }

    private static async Task<ClaimsPrincipal> Principal(string loginId, SoodalLifeDbContext db)
    {
        var id = await db.Users.Where(x => x.LoginId == loginId).Select(x => x.PublicId).SingleAsync();
        return new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString())], "test"));
    }
    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static Task<HttpResponseMessage> Login(HttpClient client, TestCredential credential) => client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
}
