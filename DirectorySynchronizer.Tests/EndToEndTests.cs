namespace DirectorySynchronizer.Tests
{
    public class EndToEndTests : IDisposable
    {
        private readonly string tempRoot;
        private readonly string sourceDir;
        private readonly string replicaDir;
        private readonly string logFile;
        private readonly StringWriter testWriter;
        private Thread? runThread = null;

        public EndToEndTests()
        {
            tempRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            sourceDir = Path.Combine(tempRoot, "source");
            replicaDir = Path.Combine(tempRoot, "replica");
            logFile = Path.Combine(tempRoot, "sync.log");

            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(replicaDir);

            testWriter = new StringWriter();
        }

        private Thread RunThread(string[] args, int secondsToLive, StringWriter outWriter, StringWriter? errWriter = null)
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

        [Fact]
        public void E2E_InvalidArgs_Test()
        {
            string[] args = ["arg1", "arg2"];

            var errWriter = new StringWriter();

            RunThread(args, 1, testWriter, errWriter).Join();

            var output = testWriter.ToString();
            Assert.Contains("Usage:", output);

            var errOutput = errWriter.ToString();
            Assert.Contains("Invalid number of arguments", errOutput);

            errWriter.Dispose();
        }

        [Fact]
        public void E2E_FileCreation_Test()
        {
            string[] args = [sourceDir, replicaDir, logFile, "1"];

            var thread = RunThread(args, 3, testWriter);

            var testFile = Path.Combine(sourceDir, "copy_test");
            File.WriteAllText(testFile, "test_copy");
            
            thread.Join();

            // Log verification needed

            // Verify file copied
            var replicaFile = Path.Combine(replicaDir, "copy_test");
            Assert.True(File.Exists(replicaFile));
            Assert.Equal("test_copy", File.ReadAllText(replicaFile));
        }
    }
}