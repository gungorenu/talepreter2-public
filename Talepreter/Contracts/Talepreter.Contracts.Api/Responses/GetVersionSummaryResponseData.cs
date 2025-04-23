namespace Talepreter.Contracts.Api.Responses;

public class GetVersionSummaryResponseData
{
    public Guid VersionId { get; init; } = default!;
    public EntityStatus Status { get; init; } = default!;
    public LastExecutedPage LastPage { get; init; } = default!;
}

public class LastExecutedPage
{
    public int Chapter { get; init; } = -1;
    public int Page { get; init; } = -1;
}
