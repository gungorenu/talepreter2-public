using Microsoft.Extensions.Logging;
using Talepreter.Contracts.Orleans.System;
using Talepreter.Exceptions;

namespace Talepreter.Operations.Grains;

public class ValidationContext
{
    public static ValidationContext Validate(string id, ILogger logger, string grainName, string methodName) => new(id, logger, grainName, methodName);

    private readonly string _methodName;
    private readonly string _grainName;
    private readonly ILogger _logger;
    private readonly string _id;

    private ValidationContext(string id, ILogger logger, string grainName, string methodName)
    {
        _methodName = methodName;
        _grainName = grainName;
        _logger = logger;
        _id = id;
    }

    public string Id => _id;
    public string MethodName => _methodName;

    public ValidationContext TaleId(Guid value)
    {
        if (value == Guid.Empty) throw new GrainOperationException($"[{_grainName}:{_methodName} - Validation] {_id} got null/empty tale id");
        return this;
    }

    public ValidationContext TaleVersionId(Guid value)
    {
        if (value == Guid.Empty) throw new GrainOperationException($"[{_grainName}:{_methodName} - Validation] {_id} got null/empty tale version id");
        return this;
    }

    public ValidationContext Chapter(int value)
    {
        if (value < 0) throw new GrainOperationException($"[{_grainName}:{_methodName} - Validation] {_id} got negative chapter id");
        return this;
    }

    public ValidationContext Page(int value)
    {
        if (value < 0) throw new GrainOperationException($"[{_grainName}:{_methodName} - Validation] {_id} got negative page id");
        return this;
    }

    public ValidationContext Custom(bool condition, string message)
    {
        if (condition) throw new GrainOperationException($"[{_grainName}:{_methodName} - Validation] {_id} {message}");
        return this;
    }

    public ValidationContext IsHealthy(ControllerGrainStatus status, params ControllerGrainStatus[] expected)
    {
        if (!expected.Any(x => x == status)) throw new GrainOperationException($"[{_grainName}:{_methodName} - Validation] {_id} is not in healthy state");
        return this;
    }

    public ValidationContext IsNull(object objectToCheck, string argName)
    {
        if (objectToCheck == null) throw new GrainOperationException($"[{_grainName}:{_methodName} - Validation] {_id} got null/empty argument {argName}");
        return this;
    }

    public ValidationContext IsEmpty(string value, string argName)
    {
        if (string.IsNullOrEmpty(value)) throw new GrainOperationException($"[{_grainName}:{_methodName} - Validation] {_id} got null/empty argument {argName}");
        return this;
    }

    public void Debug(string message)
    {
        _logger.LogDebug($"[{_grainName}:{_methodName}] {_id} {message}");
    }

    public void Information(string message)
    {
        _logger.LogInformation($"[{_grainName}:{_methodName}] {_id} {message}");
    }

    public void Fatal(string message)
    {
        _logger.LogCritical($"[{_grainName}:{_methodName}] {_id} {message}");
    }

    public void Fatal(Exception ex, string message)
    {
        _logger.LogCritical(ex, $"[{_grainName}:{_methodName}] {_id} {message}");
    }

    public void Error(Exception ex, string message)
    {
        _logger.LogError(ex, $"[{_grainName}:{_methodName}] {_id} {message}");
    }

    public void Error(string message)
    {
        _logger.LogError($"[{_grainName}:{_methodName}] {_id} {message}");
    }

    public void Warning(Exception ex, string message)
    {
        _logger.LogWarning(ex, $"[{_grainName}:{_methodName}] {_id} {message}");
    }
    
    public void Warning(string message)
    {
        _logger.LogWarning($"[{_grainName}:{_methodName}] {_id} {message}");
    }
}
