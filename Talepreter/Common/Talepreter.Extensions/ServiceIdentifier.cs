namespace Talepreter.Extensions;

public interface ITalepreterServiceIdentifier
{
    string Name { get; }
    string LowerCaseName { get; }

    ServiceId ServiceId { get; }
}

public class TalepreterServiceIdentifier : ITalepreterServiceIdentifier
{
    public TalepreterServiceIdentifier(ServiceId serviceId)
    {
        ServiceId = serviceId;
        Name = serviceId.ToString();
    }

    public string Name { get; private set; }
    public string LowerCaseName => Name.ToLower();
    public ServiceId ServiceId { get; private set; }
}

public enum ServiceId
{
    None = 0,
    TaleSvc = 1,
    WorldSvc = 2,
    ActorSvc = 3,
    AnecdoteSvc = 4,
    PersonSvc = 5,
    OrleansClustering = 6, // not a service but needed for migrations
    GUI = 7,
}
