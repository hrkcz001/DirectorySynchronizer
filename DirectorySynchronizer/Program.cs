using DirectorySynchronizer.src;

namespace DirectorySynchronizer
{
    public class StopWrapper
    {
        public Func<bool> Func { get; set; } = () => { return false; };
        public bool Invoke() => Func();
    }

    // Main program file for the Directory Synchronizer application.
    // It validates command-line arguments, initializes logging, and starts the synchronization process.
    public class Program
    {
        const string UsageMessage = "Usage: <source> <replica> <log> <interval>";

        public static void Main(string[] args)
        {
            var stopWrapper = new StopWrapper();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = stopWrapper.Invoke();
            };
            Run(args, stopWrapper);
        }

        // stopWrapper is used to pass the stop function, for testing or for handling Ctrl+C etc.
        public static void Run(string[] args, StopWrapper stopWrapper)
        {
            try
            {
                ArgsValidator.ValidateArgs(args);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(UsageMessage);
                Console.Error.WriteLine(ex.Message);
                return;
            }

            using var logger = new Logger(args[2]);
            var synchronizer = new Synchronizer(args[0], args[1], logger);
            stopWrapper.Func = () =>
            {
                synchronizer.Stop();
                return true;
            };

            try
            {
                synchronizer.Start(int.Parse(args[3]));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }
    }
}