using Orleans.Concurrency;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Data.DocumentDbContext;
using Talepreter.Operations.Grains;

namespace Talepreter.PersonSvc.Grains;

[GenerateSerializer, StatelessWorker]
public class PersonContainerGrain : ContainerGrain<PersonContainerGrain>, IPersonContainerGrain
{
    public PersonContainerGrain(ILogger<PersonContainerGrain> logger, IDocumentDbContext documentDbContext) : base(logger, documentDbContext) { }
}
