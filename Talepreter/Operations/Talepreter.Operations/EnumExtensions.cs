using Talepreter.Contracts.Messaging;
using Talepreter.Exceptions;
using Talepreter.Extensions;

namespace Talepreter.Operations;

public static class EnumExtensions
{
    public static ResponsibleService Map(this ServiceId self)
        => self switch
        {
            ServiceId.ActorSvc => ResponsibleService.ActorSvc,
            ServiceId.AnecdoteSvc => ResponsibleService.AnecdoteSvc,
            ServiceId.PersonSvc => ResponsibleService.PersonSvc,
            ServiceId.WorldSvc => ResponsibleService.WorldSvc,
            _ => throw MissingMapperException.Fault<ServiceId, ResponsibleService>(self)
        };
}
