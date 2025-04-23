namespace Talepreter.Exceptions;

/// <summary>
/// naming is bad, this is used internally during publishing entities
/// </summary>
[GenerateSerializer]
public class EntityPublishException: Exception
{
    public EntityPublishException() { }
    public EntityPublishException(string message) : base(message) { }
    public EntityPublishException(string message, Exception innerException) : base(message, innerException) { }
    public EntityPublishException(string message, Guid taleId, Guid taleVersionId, string entityId) : base(message) 
    {
        TaleId = taleId;
        TaleVersionId = taleVersionId;
        EntityId = entityId;
    }

    public Guid TaleId { get => Guid.Parse(Data["TaleId"]!.ToString()!); private set => Data["TaleId"] = value; }
    public Guid TaleVersionId { get => Guid.Parse(Data["TaleVersion"]!.ToString()!); private set => Data["TaleVersion"] = value; }
    public string EntityId { get => Data["Entity"]!.ToString()!; private set => Data["Entity"] = value; }
}
