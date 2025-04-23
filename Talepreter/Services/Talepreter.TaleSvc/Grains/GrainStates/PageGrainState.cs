using Talepreter.Contracts.Orleans.Execute;
using Talepreter.Contracts.Orleans.Process;
using Talepreter.Extensions;
using Talepreter.Operations.Grains;

namespace Talepreter.TaleSvc.Grains.GrainStates;

[GenerateSerializer]
public class PageGrainState : TaleEntityStateBase
{
    public PageGrainState()
    {
        ExecuteResults = new Dictionary<string, ExecuteResult>
        {
            [ServiceId.ActorSvc.ToString()] = ExecuteResult.None,
            [ServiceId.AnecdoteSvc.ToString()] = ExecuteResult.None,
            [ServiceId.PersonSvc.ToString()] = ExecuteResult.None,
            [ServiceId.WorldSvc.ToString()] = ExecuteResult.None
        };

        ProcessResults = new Dictionary<string, ProcessResult>
        {
            [ServiceId.ActorSvc.ToString()] = ProcessResult.None,
            [ServiceId.AnecdoteSvc.ToString()] = ProcessResult.None,
            [ServiceId.PersonSvc.ToString()] = ProcessResult.None,
            [ServiceId.WorldSvc.ToString()] = ProcessResult.None
        };
    }

    [Id(0)] public Guid TaleVersionId { get; set; } = Guid.Empty;
    [Id(1)] public int ChapterId { get; set; } = -1;
    [Id(2)] public int PageId { get; set; } = -1;
    [Id(3)] public Dictionary<string, ExecuteResult> ExecuteResults { get; }
    [Id(4)] public Dictionary<string, ProcessResult> ProcessResults { get; }
}
