namespace Talepreter.Contracts.Api.Requests;

public class BeginProcessRequestData
{
    public Command[] Commands { get; init; } = [];
    public PageBlock PageInfo { get; init; } = default!;
}
