namespace Talepreter.Data.BaseTypes;

// SQL:EntityFramework Object
public class Command
{
    // last operation time, we are not interested in creation time
    public DateTime OperationTime { get; set; }

    // owner
    public Guid TaleId { get; init; }
    public Guid TaleVersionId { get; init; }

    // command in tale location
    public int ChapterId { get; init; }
    public int PageId { get; init; }
    public int Phase { get; set; } = 1; // yes 1, not 0, 0 means something else
    public int Index { get; init; }
    public long SubIndex { get; set; } // do not set this

    // command identifier, copy from RawData but for SQL readibility and handling grains
    public string Tag { get; init; } = default!;
    public string Target { get; init; } = default!;

    // raw data, not mapped to real command objects, still in raw format
    public CommandData RawData { get; init; } = default!;

    // commands will be executed and will produce a result always, sometimes will not succeed and for reporting back we need to keep some values here for simplicity
    public string? Error { get; set; } // TODO: do we really need it? errors will be published
    public CommandExecuteResult Result { get; set; } = CommandExecuteResult.None;
    public long Duration { get; set; }

    public override string ToString() => $"{TaleId}.{TaleVersionId}:[{ChapterId}#{PageId}.{Index}\\{Tag}] {Target}";
}
