namespace DirectorySynchronizer.Tests
{

    // This tests don't include logging verification.
    // Because very strangly, the FileSystemWatcher events are coming when the test finishes.
    // In a regular run, the events are processed correctly.
    // By the same reason, there are no unit tests for the FileSystemLogging class.

    // Splitting the tests into multiple classes to allow parallel execution.
    public abstract class EndToEndTest : IDisposable
    {
        protected readonly string tempRoot;
        protected readonly string sourceDir;
        protected readonly string replicaDir;
        protected readonly string logFile;
        protected readonly StringWriter testWriter;
        private Thread? runThread = null;

        public EndToEndTest()
        {
            tempRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            sourceDir = Path.Combine(tempRoot, "source");
            replicaDir = Path.Combine(tempRoot, "replica");
            logFile = Path.Combine(tempRoot, "sync.log");

            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(replicaDir);

            testWriter = new StringWriter();
        }

        // Runs the Program in a separate thread.
        // Program will be gracefully stopped after secondsToLive seconds.
        protected Thread RunProgram(string[] args, int secondsToLive, StringWriter outWriter, StringWriter? errWriter = null)
        {
            errWriter ??= outWriter;
            runThread = new Thread(() =>
                {
                    var stopWrapper = new StopWrapper();
                    Task.Run(async () =>
                    {
                        await Task.Delay(secondsToLive * 1000);
                        stopWrapper.Invoke();
                    });
                    Program.Run(args, stopWrapper, outWriter, errWriter);
                });
            runThread.Start();
            return runThread;
        }

        public void Dispose()
        {
            runThread?.Interrupt();
            runThread?.Join();
            testWriter.Dispose();
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
        }
    }
}