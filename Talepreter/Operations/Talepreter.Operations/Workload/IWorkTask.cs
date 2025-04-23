namespace Talepreter.Operations.Workload;

public interface IWorkTask : IDisposable
{
    public Guid Id { get; }
}
