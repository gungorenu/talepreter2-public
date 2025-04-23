using Talepreter.Contracts.Orleans.Execute;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Contracts.Orleans.Process;
using Talepreter.Operations.Grains;

namespace Talepreter.TaleSvc.Grains.GrainStates;

[GenerateSerializer]
public class PublishGrainState : TaleEntityStateBase
{
    public PublishGrainState()
    {
        LastExecutedPage = new ChapterPagePair() { Chapter = null, Page = null };
    }

    [Id(0)] public Guid TaleVersionId { get; set; }
    [Id(1)] public Dictionary<int, ExecuteResult> ExecuteResults { get; } = [];
    [Id(2)] public Dictionary<int, ProcessResult> ProcessResults { get; } = [];
    [Id(3)] public ChapterPagePair LastExecutedPage { get; set; }

    public int ChapterCount() => ExecuteResults.Count;
}
