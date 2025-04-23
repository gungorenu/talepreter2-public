namespace Talepreter.Operations.Workload;

public enum WorkTaskType
{
    None = 0,
    Process = 1, // each svc will do that, requires owned taskDbContext, represents process operation, pre-processing/filtering commands
    Execute = 2, // each svc will do that, requires owned taskDbContext, represents execute operation, executing pre-processed commands on entities
    Publish = 3 // has dedicated svc, represents publish operation, triggers other services to coordinate process & execute tasks sequentially
}
