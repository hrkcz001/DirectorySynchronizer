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
        const int MaxLongShutdownWait = 10 /*seconds*/ * 1000; // ms

        public static void Main(string[] args)
        {
            var stopWrapper = new StopWrapper();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = stopWrapper.Invoke();
                // If it takes too long to shutdown, force exit.
                Task.Run(() => Task.Delay(MaxLongShutdownWait).ContinueWith(t => Environment.Exit(0)));
            };
            Run(args, stopWrapper);
        }

        // stopWrapper is used to pass the stop function, for testing or for handling Ctrl+C etc.
        // consoleOut is used to redirect console output, for testing.
        public static void Run(string[] args, StopWrapper stopWrapper, TextWriter? consoleOut = null, TextWriter? consoleErr = null)
        {
            consoleOut ??= Console.Out;
            consoleErr ??= Console.Error;

            try
            {
                ArgsValidator.ValidateArgs(args);
            }
            catch (ArgumentException ex)
            {
                consoleOut.WriteLine(UsageMessage);
                consoleErr.WriteLine(ex.Message);
                return;
            }

            var sourceDir = args[0];
            var replicaDir = args[1];
            var logFile = args[2];
            var interval = int.Parse(args[3]);

            using var logFileWriter = new StreamWriter(logFile, true);
            var logger = new Logger([consoleOut, logFileWriter]);
            var fsLogging = new FileSystemLogging(logger, sourceDir);
            var synchronizer = new Synchronizer(sourceDir, replicaDir, logger);
            stopWrapper.Func = () =>
            {
                synchronizer.Stop();
                return true;
            };

            try
            {
                var fsLoggingThread = new Thread(fsLogging.InitLogging);
                fsLoggingThread.Start();
                synchronizer.Start(interval);
                fsLogging.Stop();
                fsLoggingThread.Join();
            }
            catch (Exception ex)
            {
                consoleErr.WriteLine(ex.Message);
            }
        }
    }
}