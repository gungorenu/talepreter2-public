using Talepreter.Extensions;

namespace Talepreter.Common.RabbitMQ;

public static class TalepreterTopology
{
    public static class Exchanges
    {
        public static string WorkExchange => "work"; // direct
        public static string EventExchange => "events"; // direct
    }

    // --

    public static class Queues
    {
        public static string WorkQueue(ServiceId svcId) => "work-" + svcId.ToString().ToLower();

        // for web
        public static string CommandResultQueue => "command-result";
        public static string EventsQueue => "events";
        public static string StatusUpdateQueue => "status";

        // for wpf
        public static string CommandResultQueueTaleSpecific(Guid taleId) => $"command-result.{taleId:N}".ToLower();
        public static string EventsQueueTaleSpecific(Guid taleId) => $"events.{taleId:N}".ToLower();
        public static string StatusUpdateQueueTaleSpecific(Guid taleId) => $"status.{taleId:N}".ToLower();
    }

    // --

    public static class RoutingKeys
    {
        public static string WorkRoutingKey(ServiceId svcId) => "work-" + svcId.ToString().ToLower();
        public static string CancelWorkRoutingKey => "cancel-work";
        
        // for wpf
        public static string ProcessCommandResultRoutingKey(Guid taleId) => $"commandprocess-result.{taleId:N}".ToLower();
        public static string ExecuteCommandResultRoutingKey(Guid taleId) => $"commandexecute-result.{taleId:N}".ToLower();
        public static string EventRoutingKey(Guid taleId) => $"event.{taleId:N}".ToLower();
        public static string StatusUpdateRoutingKey(Guid taleId) => $"status-update.{taleId:N}".ToLower();

        // for web
        public static string ProcessCommandResultRoutingKeyFilter => $"commandprocess-result.*";
        public static string ExecuteCommandResultRoutingKeyFilter => "commandexecute-result.*";
        public static string EventRoutingKeyFilter => "event.*";
        public static string StatusUpdateRoutingKeyFilter => "status-update.*";
    }
}
