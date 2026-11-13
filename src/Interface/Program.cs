using Engine;
namespace Chess;

public class Program
{
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                Logger.Error($"Unhandled exception: {ex}");
            else
                Logger.Error("Unhandled exception occurred.");
        };

        // Console.Clear();
        UCI uci = new();
        Logger.Info("Starting Chess Engine...");

        while (true)
        {
            var input = Console.ReadLine();
            string command = input ?? string.Empty;
            if (string.Equals(command.Trim(), "quit", StringComparison.OrdinalIgnoreCase))
                break;

            if (!string.IsNullOrWhiteSpace(command))
            {
                try
                {
                    uci.ReceiveCommand(command);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Exception occurred: {ex}");
                }
            }
        }
    }
}