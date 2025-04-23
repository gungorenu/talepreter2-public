using Orleans.Concurrency;
using Talepreter.Contracts.Orleans.Execute;
using Talepreter.Contracts.Orleans.System;
using Talepreter.Contracts.Orleans.Process;

namespace Talepreter.Contracts.Orleans.Grains
{
    public interface IPublishGrain : IGrainWithStringKey
    {
        /// <summary>
        /// gets status of publish version, if page is Processed or Executed then it is good, otherwise will be considered failed
        /// </summary>
        [ReadOnly, AlwaysInterleave]
        Task<ControllerGrainStatus> GetStatus();

        /// <summary>
        /// gets the last successfully executed page, state must be healthy or returns -1/-1
        /// </summary>
        [ReadOnly, AlwaysInterleave]
        Task<ChapterPagePair> LastExecutedPage();

        /// <summary>
        /// sets up the base information for the grain for further operations
        /// </summary>
        Task Initialize(Guid taleId, Guid talePublishId);

        /// <summary>
        /// adds a new page to tale (may also add a new chapter if it does not exist)
        /// can be called again for existing chapters and pages
        /// </summary>
        Task<bool> AddChapterPage(int chapter, int page);

        /// <summary>
        /// adds commands of the page and starts processing
        /// </summary>
        Task BeginProcess(int chapter, int page, ProcessCommand[] pageCommands);

        /// <summary>
        /// stops current operation, will not initiate next page operation
        /// </summary>
        Task Stop();

        /// <summary>
        /// deletes all command data on containers
        /// </summary>
        Task Purge();

        /// <summary>
        /// call current chapter execution operation, it will also track it and initiate next chapter if previous one is completed
        /// calling this again will execute from scratch but chapter might give success result immediately if they already have done it before
        /// starts operations async, without awaiting. response will come from others when their stuff is complete
        /// </summary>
        Task BeginExecute();

        /// <summary>
        /// copies entire data of version to another one
        /// </summary>
        Task BackupTo(Guid newVersionId);

        /// <summary>
        /// counterpart of BackupTo, gets the data from an existing version and then initializes itself as if it already executed everything to success
        /// </summary>
        Task BackupFrom(Guid taleId, Guid talePublishId, ChapterPagePair lastExecutedPage);

        // --

        /// <summary>
        /// response of process command. if all chapters give answer then it will response back to tale grain with total status (success, failed etc)
        /// </summary>
        Task OnProcessComplete(int callerChapter, int callerPage, ProcessResult result);

        /// <summary>
        /// response of execute command. if chapter gives success then will continue with next chapter
        /// if all chapters are succesful then will respond back to tale grain with result
        /// </summary>
        Task OnExecuteComplete(int callerChapter, int callerPage, ExecuteResult result);
    }
}
