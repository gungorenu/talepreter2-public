using Talepreter.Common;
using Talepreter.Exceptions;

namespace Talepreter.Contracts.Orleans.Execute;

[GenerateSerializer]
public class ExecuteCommandContext : IExecuteContext
{
    [Id(0)] public Guid TaleId { get; init; }
    [Id(1)] public Guid TaleVersionId { get; init; }
    [Id(2)] public RawCommand Command { get; init; } = default!;
    [Id(3)] public int Chapter { get; init; }
    [Id(4)] public int PageInChapter { get; init; }

    public bool MustExist => Command.NamedParameters?.FirstOrDefault(x => x.Name == "!mustexist") != null;

    public long? Today()
    {
        var start = Command.NamedParameters?.FirstOrDefault(x => x.Name == "!start")?.Value.ToLong() ?? throw new CommandValidationException(Command.ToString()!, "Command has no start date set");
        var stay = Command.NamedParameters?.FirstOrDefault(x => x.Name == "!stay")?.Value.ToLong() ?? throw new CommandValidationException(Command.ToString()!, "Command has no stay duration set");
        var voyage = Command.NamedParameters?.FirstOrDefault(x => x.Name == "!voyage")?.Value.ToLong() ?? 0;
        return start + stay + voyage;
    }
}
