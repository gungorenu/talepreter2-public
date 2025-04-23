namespace Talepreter.Contracts.Orleans.Execute;

public interface IExecuteContext
{
    Guid TaleId { get; }
    Guid TaleVersionId { get; }
    int Chapter { get; }
    int PageInChapter { get; }
    long? Today();
}
