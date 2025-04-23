using Talepreter.Common;
using Talepreter.Data.BaseTypes;
using Talepreter.Exceptions;
using Talepreter.Model.Command;

namespace Talepreter.Operations;

public static class CommandExtensions
{
    public static Command Remap(this Command command, string newTag, string newTarget, string? newParent = null, string[]? newArrayParams = null, NamedParameter[]? newNamedParams = null, int? phase = null)
    {
        List<NamedParameter> namedParams =
        [
            command.RawData.NamedParameters.First(x => x.Name == CommandIds.CommandAttributes.Start),
            command.RawData.NamedParameters.First(x => x.Name == CommandIds.CommandAttributes.StartLocation),
            command.RawData.NamedParameters.First(x => x.Name == CommandIds.CommandAttributes.Stay),
        ];

        var travelTo = command.RawData.NamedParameters.FirstOrDefault(x => x.Name == CommandIds.CommandAttributes.TravelTo);
        if (travelTo != null) namedParams.Add(travelTo);
        var voyage = command.RawData.NamedParameters.FirstOrDefault(x => x.Name == CommandIds.CommandAttributes.Voyage);
        if (voyage != null) namedParams.Add(voyage);

        if (newNamedParams != null) namedParams.AddRange(newNamedParams);

        return new Command
        {
            OperationTime = command.OperationTime,
            TaleId = command.TaleId,
            TaleVersionId = command.TaleVersionId,
            ChapterId = command.ChapterId,
            PageId = command.PageId,
            Phase = phase ?? command.Phase,
            Index = command.Index,
            
            Tag = newTag ?? command.Tag,
            Target = newTarget ?? command.Target,
            RawData = new CommandData
            {
                Tag = newTag ?? command.Tag,
                Target = newTarget ?? command.Target,
                Comment = null,
                Parent = newParent,
                ArrayParameters = newArrayParams ?? [],
                NamedParameters = [.. namedParams]
            }
        };
    }

    public static Command MapWith(this Command command, string? newTag = null, string? newTarget = null, string? newParent = null, string[]? newArrayParams = null, NamedParameter[]? newNamedParams = null)
    {
        return new Command
        {
            OperationTime = command.OperationTime,
            TaleId = command.TaleId,
            TaleVersionId = command.TaleVersionId,
            ChapterId = command.ChapterId,
            PageId = command.PageId,
            Phase = command.Phase,
            Index = command.Index,

            Tag = newTag ?? command.Tag,
            Target = newTarget ?? command.Target,
            RawData = new CommandData
            {
                Tag = newTag ?? command.Tag,
                Target = newTarget ?? command.Target,
                Comment = command.RawData.Comment,
                Parent = newParent ?? command.RawData.Parent,
                ArrayParameters = newArrayParams ?? command.RawData.ArrayParameters,
                NamedParameters = newNamedParams ?? command.RawData.NamedParameters
            }
        };
    }

    public static Command MapTrigger(this Command command, Trigger trigger)
    {
        return new Command
        {
            OperationTime = command.OperationTime,
            TaleId = command.TaleId,
            TaleVersionId = command.TaleVersionId,
            ChapterId = command.ChapterId,
            PageId = command.PageId,
            Phase = 0, // all triggers execute on phase 0
            Index = command.Index,

            Tag = CommandIds.Trigger,
            Target = trigger.Target,
            RawData = new CommandData
            {
                Tag = CommandIds.Trigger,
                Target = trigger.Target,
                Comment = null,
                Parent = null,
                ArrayParameters = [],
                NamedParameters = [NamedParameter.Create(CommandIds.TriggerCommand.Id, value: trigger.Id),
                    NamedParameter.Create(CommandIds.TriggerCommand.Type, value: trigger.Type),
                    NamedParameter.Create(CommandIds.TriggerCommand.Parameter, value: trigger.Parameter),
                    NamedParameter.Create(CommandIds.TriggerCommand.Grain, value: trigger.GrainType),
                    NamedParameter.Create(CommandIds.TriggerCommand.At, value: trigger.TriggerAt.ToString())],
            }
        };
    }

    public static PageTimelineInfo Timeline(this Command command)
    {
        var start = command.RawData.NamedParameters?.FirstOrDefault(x => x.Name == CommandIds.CommandAttributes.Start)?.Value.ToLong()
            ?? throw new CommandValidationException(command.ToString()!, $"Command misses system parameter {CommandIds.CommandAttributes.Start}");
        var stay = command.RawData.NamedParameters?.FirstOrDefault(x => x.Name == CommandIds.CommandAttributes.Stay)?.Value.ToLong()
            ?? throw new CommandValidationException(command.ToString()!, $"Command misses system parameter {CommandIds.CommandAttributes.Stay}");
        var voyage = command.RawData.NamedParameters?.FirstOrDefault(x => x.Name == CommandIds.CommandAttributes.Voyage)?.Value.ToLong() ?? 0;
        var startLocationPar = command.RawData.NamedParameters?.FirstOrDefault(x => x.Name == CommandIds.CommandAttributes.StartLocation)
            ?? throw new CommandValidationException(command.ToString()!, $"Command misses system parameter {CommandIds.CommandAttributes.StartLocation}");
        var travelToPar = command.RawData.NamedParameters?.FirstOrDefault(x => x.Name == CommandIds.CommandAttributes.TravelTo);

        var timeline = new PageTimelineInfo
        {
            Start = start,
            Voyage = voyage,
            Stay = stay,
            StartLocation = startLocationPar.ParseLocation() ?? throw new CommandValidationException(command.ToString()!, $"Command could not parse system parameter {CommandIds.CommandAttributes.StartLocation}"),
            TravelTo = travelToPar.ParseLocation()
        };
        return timeline;
    }

    public static PageTimelineInfo Timeline(this Contracts.Orleans.RawCommand command)
    {
        var start = command.NamedParameters?.FirstOrDefault(x => x.Name == CommandIds.CommandAttributes.Start)?.Value.ToLong()
            ?? throw new CommandValidationException(command.ToString()!, $"Command misses system parameter {CommandIds.CommandAttributes.Start}");
        var stay = command.NamedParameters?.FirstOrDefault(x => x.Name == CommandIds.CommandAttributes.Stay)?.Value.ToLong()
            ?? throw new CommandValidationException(command.ToString()!, $"Command misses system parameter {CommandIds.CommandAttributes.Stay}");
        var voyage = command.NamedParameters?.FirstOrDefault(x => x.Name == CommandIds.CommandAttributes.Voyage)?.Value.ToLong() ?? 0;
        var startLocationPar = command.NamedParameters?.FirstOrDefault(x => x.Name == CommandIds.CommandAttributes.StartLocation)
            ?? throw new CommandValidationException(command.ToString()!, $"Command misses system parameter {CommandIds.CommandAttributes.StartLocation}");
        var travelToPar = command.NamedParameters?.FirstOrDefault(x => x.Name == CommandIds.CommandAttributes.TravelTo);

        var timeline = new PageTimelineInfo
        {
            Start = start,
            Voyage = voyage,
            Stay = stay,
            StartLocation = startLocationPar.ParseLocation() ?? throw new CommandValidationException(command.ToString()!, $"Command could not parse system parameter {CommandIds.CommandAttributes.StartLocation}"),
            TravelTo = travelToPar.ParseLocation()
        };
        return timeline;
    }

    private static PageTimelineInfo.Location? ParseLocation(this NamedParameter? par)
    {
        if (par == null) return null;
        string[] args = string.IsNullOrEmpty(par.Value) ? [] : par.Value.SplitInto($",");
        PageTimelineInfo.Location? startLocation = default!;
        if (args.Length == 2) startLocation = new PageTimelineInfo.Location(args[0], args[1]);
        else if (args.Length == 1) startLocation = new PageTimelineInfo.Location(args[0], null);
        return startLocation;
    }

    private static PageTimelineInfo.Location? ParseLocation(this Contracts.Orleans.NamedParameter? par)
    {
        if (par == null) return null;
        string[] args = string.IsNullOrEmpty(par.Value) ? [] : par.Value.SplitInto($",");
        PageTimelineInfo.Location? startLocation = default!;
        if (args.Length == 2) startLocation = new PageTimelineInfo.Location(args[0], args[1]);
        else if (args.Length == 1) startLocation = new PageTimelineInfo.Location(args[0], null);
        return startLocation;
    }
}
