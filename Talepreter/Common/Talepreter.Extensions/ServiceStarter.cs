using Talepreter.Common;

namespace Talepreter.Extensions;

public static class ServiceStarter
{
    public static void StartService(Action serviceStuff)
    {
        try
        {
            LoggingHelper.SetupSerilog();
            AppDomain.CurrentDomain.UnhandledException += (o, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    Console.WriteLine("Unhandled error: " + ex.Message);
                    Console.WriteLine("Type: " + ex.GetType().Name);
                    Console.WriteLine("StackTrace: " + ex.StackTrace);
                }
                else Console.WriteLine("Unhandled and unknown error: " + e.ExceptionObject);
            };

            serviceStuff();
        }
        catch (EnvironmentVariableException ex)
        {
            Console.WriteLine("Environment variable error: " + ex.Message);
        }
        catch (Exception ex) when (ex.GetType().Name != "HostAbortedException" && ex.Source != "Microsoft.EntityFrameworkCore.Design")
        {
            Console.WriteLine("Unknown error: " + ex.Message);
            Console.WriteLine("Type: " + ex.GetType().Name);
            Console.WriteLine("StackTrace: " + ex.StackTrace);
        }
    }
}
