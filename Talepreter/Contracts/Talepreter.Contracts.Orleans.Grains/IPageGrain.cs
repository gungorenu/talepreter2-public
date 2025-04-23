using Orleans.Concurrency;
using Talepreter.Contracts.Orleans.Execute;
using Talepreter.Contracts.Orleans.System;
using Talepreter.Contracts.Orleans.Process;

namespace Talepreter.Contracts.Orleans.Grains
{
    public interface IPageGrain : IGrainWithStringKey
    {
        /// <summary>
        /// gets status of page, if page is Processed or Executed then it is good, otherwise will be considered failed
        /// </summary>
        [ReadOnly, AlwaysInterleave]
        Task<ControllerGrainStatus> GetStatus();

        /// <summary>
        /// sets up the base information for the grain for further operations
        /// </summary>
        Task Initialize(Guid taleId, Guid talePublishId, int chapter, int page);

        /// <summary>
        /// adds commands of the page and starts processing
        /// </summary>
        Task BeginProcess(int chapter, int page, ProcessCommand[] pageCommands);

        /// <summary>
        /// stops current operation, will not initiate next page operation
        /// </summary>
        Task Stop();

        /// <summary>
        /// call all 4 containers (Actor, Anecdote, Person, World) to execute the page commands and waits for results
        /// starts operations async, without awaiting. response will come from others when their stuff is complete
        /// it waits until all commands are executed and has a result. it will also have a timer set to check itself and check internal states a final time before raising timed out response back
        /// calling this again will call four containers but they might give quick immediate answer if they have no commands to execute (because all are done before)
        /// </summary>
        Task BeginExecute();

        // --

        /// <summary>
        /// response of process command. if all 4 containers give answer for each command then it will response back to chapter grain with total status (success, failed etc)
        /// </summary>
        /// <param name="callerContainer">grain name itself, one of the four containers only, others cannot call page grain</param>
        Task OnProcessComplete(string callerContainer, ProcessResult result);

        /// <summary>
        /// response of execute command. if all 4 containers give answer then it will response back to chapter grain with total status (success, failed etc)
        /// </summary>
        /// <param name="callerContainer">grain name itself, one of the four containers only, others cannot call page grain</param>
        Task OnExecuteComplete(string callerContainer, ExecuteResult result);
    }
}
