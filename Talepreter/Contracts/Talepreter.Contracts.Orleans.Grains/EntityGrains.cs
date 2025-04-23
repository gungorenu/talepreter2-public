namespace Talepreter.Contracts.Orleans.Grains;

public interface IActorGrain : ITriggeredCommandGrain { }
public interface IAnecdoteGrain : ICommandGrain { }
public interface IEquipmentGrain : ICommandGrain { }
public interface ICohortGrain : ITriggeredCommandGrain { }
public interface IPersonGrain : ITriggeredCommandGrain { }
public interface ISettlementGrain : ITriggeredCommandGrain { }
public interface IWorldGrain : ICommandGrain { }
public interface IRaceGrain : ICommandGrain { }
public interface IGroupGrain : ICommandGrain { }
public interface IFactionGrain : ICommandGrain { }
public interface ICacheGrain : ICommandGrain { }
