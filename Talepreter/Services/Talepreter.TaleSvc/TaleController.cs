using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Talepreter.Contracts.Api;
using Talepreter.Contracts.Api.Requests;
using Talepreter.Contracts.Api.Responses;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Contracts.Orleans.Process;
using Talepreter.Contracts.Orleans.System;
using Talepreter.Exceptions;
using Talepreter.Model.Command;

namespace Talepreter.TaleSvc;

[ApiController]
[Route("api/[controller]")]
public class TaleController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IGrainFactory _grainFactory;

    public TaleController(ILogger<TaleController> logger, IGrainFactory grainFactory)
    {
        _logger = logger;
        _grainFactory = grainFactory;
    }

    [HttpGet("ping")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public ActionResult<string> Ping()
    {
        return Ok("pong");
    }

    [HttpGet("{taleId}/{taleVersionId}/status")]
    [ProducesResponseType<EntityStatus>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPublishStatus(Guid taleId, Guid taleVersionId)
    {
        Stopwatch sw = new();
        sw.Start();
        try
        {
            _logger.LogDebug($"PublishStatus:{taleId},{taleVersionId} received");

            if (taleId == Guid.Empty) return BadRequest("TaleId is empty");
            if (taleVersionId == Guid.Empty) return BadRequest("TaleVersionId is empty");

            var publishGrain = _grainFactory.FetchPublish(taleId, taleVersionId);
            var res = await publishGrain.GetStatus();
            return Ok(Map(res));
        }
        catch (Exception ex)
        {
            _logger.LogError($"PublishStatus:{taleId},{taleVersionId} faulted, {ex.Message}");
            return BadRequest();
        }
        finally
        {
            sw.Stop();
            _logger.LogDebug($"PublishStatus:{taleId},{taleVersionId} completed in {sw.ElapsedMilliseconds} ms");
        }
    }

    [HttpGet("{taleId}/{taleVersionId}/{chapter}/status")]
    [ProducesResponseType<EntityStatus>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetChapterStatus(Guid taleId, Guid taleVersionId, int chapter)
    {
        Stopwatch sw = new();
        sw.Start();
        try
        {
            _logger.LogDebug($"ChapterStatus:{taleId},{taleVersionId},{chapter} received");

            if (taleId == Guid.Empty) return BadRequest("TaleId is empty");
            if (taleVersionId == Guid.Empty) return BadRequest("TaleVersionId is empty");
            if (chapter < 0) return BadRequest("Chapter cannot be negative");

            var chapterGrain = _grainFactory.FetchChapter(taleId, taleVersionId, chapter);
            var res = await chapterGrain.GetStatus();
            return Ok(Map(res));
        }
        catch (Exception ex)
        {
            _logger.LogError($"ChapterStatus:{taleId},{taleVersionId},{chapter} faulted, {ex.Message}");
            return BadRequest();
        }
        finally
        {
            sw.Stop();
            _logger.LogDebug($"ChapterStatus:{taleId},{taleVersionId},{chapter} completed in {sw.ElapsedMilliseconds} ms");
        }
    }

    [HttpGet("{taleId}/{taleVersionId}/{chapter}/{page}/status")]
    [ProducesResponseType<EntityStatus>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPageStatus(Guid taleId, Guid taleVersionId, int chapter, int page)
    {
        Stopwatch sw = new();
        sw.Start();
        try
        {
            _logger.LogDebug($"PageStatus:{taleId},{taleVersionId},{chapter},{page} received");

            if (taleId == Guid.Empty) return BadRequest("TaleId is empty");
            if (taleVersionId == Guid.Empty) return BadRequest("TaleVersionId is empty");
            if (chapter < 0) return BadRequest("Chapter cannot be negative");
            if (page < 0) return BadRequest("Page cannot be negative");

            var pageGrain = _grainFactory.FetchPage(taleId, taleVersionId, chapter, page);
            var res = await pageGrain.GetStatus();
            return Ok(Map(res));
        }
        catch (Exception ex)
        {
            _logger.LogError($"PageStatus:{taleId},{taleVersionId},{chapter},{page} faulted, {ex.Message}");
            return BadRequest(ex.Message);
        }
        finally
        {
            sw.Stop();
            _logger.LogDebug($"PageStatus:{taleId},{taleVersionId},{chapter},{page} completed in {sw.ElapsedMilliseconds} ms");
        }
    }

    [HttpPost("{taleId}/{taleVersionId}/stop")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Stop(Guid taleId, Guid taleVersionId)
    {
        Stopwatch sw = new();
        sw.Start();
        try
        {
            _logger.LogDebug($"Stop:{taleId},{taleVersionId} received");

            if (taleId == Guid.Empty) return BadRequest("TaleId is empty");
            if (taleVersionId == Guid.Empty) return BadRequest("TaleVersionId is empty");

            var publishGrain = _grainFactory.FetchPublish(taleId, taleVersionId);
            await publishGrain.Stop();
            return Accepted();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Stop:{taleId},{taleVersionId} faulted, {ex.Message}");
            return BadRequest();
        }
        finally
        {
            sw.Stop();
            _logger.LogDebug($"Stop:{taleId},{taleVersionId} completed in {sw.ElapsedMilliseconds} ms");
        }
    }

    [HttpPost("{taleId}/{taleVersionId}/purge")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PurgePublish(Guid taleId, Guid taleVersionId)
    {
        Stopwatch sw = new();
        sw.Start();
        try
        {
            _logger.LogDebug($"Purge:{taleId},{taleVersionId} received");

            if (taleId == Guid.Empty) return BadRequest("TaleId is empty");
            if (taleVersionId == Guid.Empty) return BadRequest("TaleVersionId is empty");

            var taleGrain = _grainFactory.FetchTale(taleId);
            await taleGrain.PurgePublish(taleVersionId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Purge:{taleId},{taleVersionId} faulted, {ex.Message}");
            return BadRequest();
        }
        finally
        {
            sw.Stop();
            _logger.LogDebug($"Purge:{taleId},{taleVersionId} completed in {sw.ElapsedMilliseconds} ms");
        }
    }

    [HttpPost("{taleId}/purge")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PurgeTale(Guid taleId)
    {
        Stopwatch sw = new();
        sw.Start();
        try
        {
            _logger.LogDebug($"Purge:{taleId} received");

            if (taleId == Guid.Empty) return BadRequest("TaleId is empty");

            var taleGrain = _grainFactory.FetchTale(taleId);
            await taleGrain.Purge();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Purge:{taleId} faulted, {ex.Message}");
            return BadRequest();
        }
        finally
        {
            sw.Stop();
            _logger.LogDebug($"Purge:{taleId} completed in {sw.ElapsedMilliseconds} ms");
        }
    }

    [HttpPost("{taleId}/{taleVersionId}/{chapter}/{page}/add")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddPage(Guid taleId, Guid taleVersionId, int chapter, int page)
    {
        Stopwatch sw = new();
        sw.Start();
        try
        {
            _logger.LogDebug($"AddPage:{taleId},{taleVersionId},{chapter},{page} received");

            if (taleId == Guid.Empty) return BadRequest("TaleId is empty");
            if (taleVersionId == Guid.Empty) return BadRequest("TaleVersionId is empty");
            if (chapter < 0) return BadRequest("Chapter cannot be negative");
            if (page < 0) return BadRequest("Page cannot be negative");

            var taleGrain = _grainFactory.FetchTale(taleId);
            var result = await taleGrain.AddChapterPage(taleVersionId, chapter, page);
            if (result) return Ok();
            else return BadRequest("New page is not added");
        }
        catch (Exception ex)
        {
            _logger.LogError($"AddPage:{taleId},{taleVersionId},{chapter},{page} faulted, {ex.Message}");
            return BadRequest();
        }
        finally
        {
            sw.Stop();
            _logger.LogDebug($"AddPage:{taleId},{taleVersionId},{chapter},{page} completed in {sw.ElapsedMilliseconds} ms");
        }
    }

    [HttpPost("{taleId}/{taleVersionId}/execute")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Execute(Guid taleId, Guid taleVersionId)
    {
        Stopwatch sw = new();
        sw.Start();
        try
        {
            _logger.LogDebug($"Execute:{taleId},{taleVersionId} received");

            if (taleId == Guid.Empty) return BadRequest("TaleId is empty");
            if (taleVersionId == Guid.Empty) return BadRequest("TaleVersionId is empty");

            var taleGrain = _grainFactory.FetchTale(taleId);
            await taleGrain.BeginExecute(taleVersionId);
            return Accepted();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Execute:{taleId},{taleVersionId} faulted, {ex.Message}");
            return BadRequest();
        }
        finally
        {
            sw.Stop();
            _logger.LogDebug($"Execute:{taleId},{taleVersionId} completed in {sw.ElapsedMilliseconds} ms");
        }
    }

    [HttpPost("{taleId}/{taleVersionId}/{chapter}/{page}/process")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Process(Guid taleId, Guid taleVersionId, int chapter, int page, [FromBody] BeginProcessRequestData data)
    {
        Stopwatch sw = new();
        sw.Start();
        try
        {
            _logger.LogDebug($"Process:{taleId},{taleVersionId},{chapter},{page} received");

            if (taleId == Guid.Empty) return BadRequest("TaleId is empty");
            if (taleVersionId == Guid.Empty) return BadRequest("TaleVersionId is empty");
            if (chapter < 0) return BadRequest("Chapter cannot be negative");
            if (page < 0) return BadRequest("Page cannot be negative");
            if (data == null) return BadRequest("Command data is empty or invalid");
            if (data.PageInfo == null) return BadRequest("Page info is empty or invalid");
            if ((data.Commands?.Length ?? 0) == 0) return BadRequest("Command list is empty, at least one command must exist");

            var taleGrain = _grainFactory.FetchTale(taleId);
            var commands = Map(data);
            await taleGrain.BeginProcess(taleVersionId, chapter, page, commands);
            return Accepted();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Process:{taleId},{taleVersionId},{chapter},{page} faulted, {ex.Message}");
            return BadRequest();
        }
        finally
        {
            sw.Stop();
            _logger.LogDebug($"Process:{taleId},{taleVersionId},{chapter},{page} completed in {sw.ElapsedMilliseconds} ms");
        }
    }

    [HttpPost("{taleId}/{taleVersionId}/initialize")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Initialize(Guid taleId, Guid taleVersionId, [FromBody] InitializePublishRequestData data)
    {
        Stopwatch sw = new();
        sw.Start();
        try
        {
            _logger.LogDebug($"Initialize:{taleId},{taleVersionId} received");

            if (taleId == Guid.Empty) return BadRequest("TaleId is empty");
            if (taleVersionId == Guid.Empty) return BadRequest("TaleVersionId is empty");
            if (data == null) return BadRequest("Initialize data is empty or invalid");

            var taleGrain = _grainFactory.FetchTale(taleId);
            await taleGrain.Initialize(taleVersionId, data.BackupOfVersionId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Initialize:{taleId},{taleVersionId} faulted, {ex.Message}");
            return BadRequest();
        }
        finally
        {
            sw.Stop();
            _logger.LogDebug($"Initialize:{taleId},{taleVersionId} completed in {sw.ElapsedMilliseconds} ms");
        }
    }

    [HttpGet("{taleId}/versions")]
    [ProducesResponseType<GetVersionsResponseData>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Versions(Guid taleId)
    {
        Stopwatch sw = new();
        sw.Start();
        try
        {
            _logger.LogDebug($"Versions:{taleId} received");

            if (taleId == Guid.Empty) return BadRequest("TaleId is empty");

            var taleGrain = _grainFactory.FetchTale(taleId);
            var res = await taleGrain.GetVersions();

            var response = new GetVersionsResponseData { Versions = res ?? [] };
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Versions:{taleId} faulted, {ex.Message}");
            return BadRequest();
        }
        finally
        {
            sw.Stop();
            _logger.LogDebug($"Versions:{taleId} completed in {sw.ElapsedMilliseconds} ms");
        }
    }

    [HttpGet("{taleId}/{taleVersionId}/lastexecutedpage")]
    [ProducesResponseType<GetLastExecutedPageResponseData>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LastExecutedPage(Guid taleId, Guid taleVersionId)
    {
        Stopwatch sw = new();
        sw.Start();
        try
        {
            _logger.LogDebug($"LastExecutedPage:{taleId},{taleVersionId} received");

            if (taleId == Guid.Empty) return BadRequest("TaleId is empty");
            if (taleVersionId == Guid.Empty) return BadRequest("TaleVersionId is empty");

            var publishGrain = _grainFactory.FetchPublish(taleId, taleVersionId);
            var result = await publishGrain.LastExecutedPage();
            return Ok(new GetLastExecutedPageResponseData { Chapter = result?.Chapter ?? -1, Page = result?.Page ?? -1 });
        }
        catch (Exception ex)
        {
            _logger.LogError($"LastExecutedPage:{taleId},{taleVersionId} faulted, {ex.Message}");
            return BadRequest();
        }
        finally
        {
            sw.Stop();
            _logger.LogDebug($"LastExecutedPage:{taleId},{taleVersionId} completed in {sw.ElapsedMilliseconds} ms");
        }
    }

    [HttpGet("{taleId}/{taleVersionId}/summary")]
    [ProducesResponseType<GetVersionSummaryResponseData>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSummary(Guid taleId, Guid taleVersionId)
    {
        Stopwatch sw = new();
        sw.Start();
        try
        {
            _logger.LogDebug($"GetSummary:{taleId},{taleVersionId} received");

            if (taleId == Guid.Empty) return BadRequest("TaleId is empty");
            if (taleVersionId == Guid.Empty) return BadRequest("TaleVersionId is empty");

            var publishGrain = _grainFactory.FetchPublish(taleId, taleVersionId);
            var lastExecPage = await publishGrain.LastExecutedPage();
            var status = await publishGrain.GetStatus();

            var res = new GetVersionSummaryResponseData
            {
                LastPage = new LastExecutedPage { Chapter = lastExecPage?.Chapter ?? -1, Page = lastExecPage?.Page ?? -1 },
                Status = Map(status),
                VersionId = taleVersionId
            };

            return Ok(res);
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetSummary:{taleId},{taleVersionId} faulted, {ex.Message}");
            return BadRequest();
        }
        finally
        {
            sw.Stop();
            _logger.LogDebug($"GetSummary:{taleId},{taleVersionId} completed in {sw.ElapsedMilliseconds} ms");
        }
    }

    // --

    private EntityStatus Map(ControllerGrainStatus status) => status switch
    {
        ControllerGrainStatus.Idle => EntityStatus.Idle,
        ControllerGrainStatus.Processing => EntityStatus.Processing,
        ControllerGrainStatus.Processed => EntityStatus.Processed,
        ControllerGrainStatus.Executing => EntityStatus.Executing,
        ControllerGrainStatus.Executed => EntityStatus.Executed,
        ControllerGrainStatus.Cancelled => EntityStatus.Cancelled,
        ControllerGrainStatus.Faulted => EntityStatus.Faulted,
        ControllerGrainStatus.Timedout => EntityStatus.Timedout,
        ControllerGrainStatus.Purged => EntityStatus.Purged,
        _ => throw MissingMapperException.Fault<ControllerGrainStatus, EntityStatus>(status)
    };

    private ProcessCommand[] Map(BeginProcessRequestData data)
    {
        List<ProcessCommand> commands = [];
        foreach (var cmd in data.Commands)
        {
            var newCmd = new ProcessCommand
            {
                Phase = cmd.Phase,
                Index = cmd.Index,
                Tag = cmd.Tag,
                Target = cmd.Target,
                Parent = cmd.Parent,
                ArrayParameters = cmd.ArrayParameters,
                Comment = cmd.Comment,
                NamedParameters = Map(cmd.NamedParameters, data.PageInfo)
            };
            commands.Add(newCmd);
        }

        return [.. commands];
    }

    private Contracts.Orleans.NamedParameter[] Map(NamedParameter[]? arr, PageBlock pageInfo)
    {
        List<Contracts.Orleans.NamedParameter> @params =
        [
            new(){ Name = CommandIds.CommandAttributes.Start, Type = Contracts.Orleans.NamedParameterType.Set, Value = pageInfo.Date.ToString()},
            new(){ Name = CommandIds.CommandAttributes.StartLocation, Type = Contracts.Orleans.NamedParameterType.Set, Value = pageInfo.Location.ToString()!},
            new(){ Name = CommandIds.CommandAttributes.Stay, Type = Contracts.Orleans.NamedParameterType.Set, Value = pageInfo.Stay.ToString()}
        ];
        if (pageInfo.Voyage.HasValue) @params.Add(new() { Name = CommandIds.CommandAttributes.Voyage, Type = Contracts.Orleans.NamedParameterType.Set, Value = pageInfo.Voyage.Value.ToString() });
        if (pageInfo.Travel != null) @params.Add(new() { Name = CommandIds.CommandAttributes.TravelTo, Type = Contracts.Orleans.NamedParameterType.Set, Value = pageInfo.Travel.ToString() });

        @params.AddRange(arr!.Select(x => new Contracts.Orleans.NamedParameter()
        {
            Name = x.Name,
            Value = x.Value,
            Type = x.Type switch
            {
                NamedParameterType.Set => Contracts.Orleans.NamedParameterType.Set,
                NamedParameterType.Add => Contracts.Orleans.NamedParameterType.Add,
                NamedParameterType.Reset => Contracts.Orleans.NamedParameterType.Reset,
                NamedParameterType.Remove => Contracts.Orleans.NamedParameterType.Remove,
                _ => throw MissingMapperException.Fault<NamedParameterType, Contracts.Orleans.NamedParameterType>(x.Type)
            }
        }));
        return [.. @params];
    }
}
