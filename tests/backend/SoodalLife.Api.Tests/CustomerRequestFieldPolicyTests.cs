using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.ServiceRequests;

namespace SoodalLife.Api.Tests;

public sealed class CustomerRequestFieldPolicyTests
{
    [Theory]
    [InlineData("service_address", "서비스 주소")]
    public void Structural_duplicates_are_retired(string key, string label)
    {
        Assert.True(CustomerRequestFieldPolicy.IsRetiredStructuralDuplicate(Field(key, label)));
    }

    [Theory]
    [InlineData("request_content", "요청 내용")]
    [InlineData("desired_cost", "희망 비용")]
    [InlineData("symptom_detail", "요청·증상 상세")]
    [InlineData("quantity_scale", "수량·규모")]
    [InlineData("site_condition", "현장 조건")]
    public void Guided_request_questions_remain_visible(string key, string label)
    {
        Assert.False(CustomerRequestFieldPolicy.IsRetiredStructuralDuplicate(Field(key, label)));
    }

    [Fact]
    public void Service_specific_question_remains_visible()
    {
        Assert.False(CustomerRequestFieldPolicy.IsRetiredStructuralDuplicate(Field("space_type", "공간 유형")));
    }

    private static CategoryFieldDefinition Field(string key, string label) => new()
    {
        FieldKey = key,
        Label = label,
        FieldTypeCode = "TEXT",
        SourceFieldId = Guid.NewGuid().ToString("N"),
        StatusCode = "ACTIVE",
    };
}
