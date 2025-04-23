using Talepreter.Contracts.Orleans.Execute;
using Talepreter.Contracts.Orleans.Process;
using Talepreter.Operations.Grains;

namespace Talepreter.TaleSvc.Grains.GrainStates;

[GenerateSerializer]
public class ChapterGrainState : TaleEntityStateBase
{
    [Id(0)]
    public Guid TaleVersionId { get; set; }
    [Id(1)]
    public int ChapterId { get; set; }
    [Id(2)]
    public Dictionary<int, ExecuteResult> ExecuteResults { get; } = [];
    [Id(3)]
    public Dictionary<int, ProcessResult> ProcessResults { get; } = [];

    public int PageCount() => ExecuteResults.Count;
}
