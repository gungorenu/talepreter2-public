using Talepreter.Data.DocumentDbContext;
using Talepreter.Data.BaseTypes;
using ProcessCommand = Talepreter.Data.BaseTypes.Command;

namespace Talepreter.Operations;

public interface ICommandProcessor
{
    Task<ProcessCommand[]> Process(ProcessCommand command, ITaskDbContext taskDbContext, CancellationToken token);
}

public interface IBatchCommandProcessor
{
    Task<ProcessCommand[]> BatchProcess(ProcessCommand[] commands, IDocumentDbContext dbContext, ITaskDbContext taskDbContext, CancellationToken token);
}