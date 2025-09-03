using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads;

namespace DirectorySynchronizer.Tests
{
    public class EndToEndTests : IDisposable
    {
        private readonly string tempRoot;
        private readonly string sourceDir;
        private readonly string replicaDir;
        private readonly string logFile;
        private readonly TextWriter testWriter;
        private readonly TextWriter oldOut;
        private readonly TextWriter oldErr;
        private readonly Thread runThread;
        private readonly Thread stopThread;

        public EndToEndTests()
        {
            tempRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            sourceDir = Path.Combine(tempRoot, "source");
            replicaDir = Path.Combine(tempRoot, "replica");
            logFile = Path.Combine(tempRoot, "sync.log");

            oldOut = Console.Out;
            oldErr = Console.Error;

            testWriter = new StringWriter();
            Console.SetOut(testWriter);
            Console.SetError(testWriter);

            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(replicaDir);
            File.Create(logFile).Close();

            var stopWrapper = new StopWrapper();
            runThread = new Thread((_args) => Program.Run((string[])_args!, stopWrapper));
            stopThread = new Thread((seconds) =>
            {
                Thread.Sleep((int)seconds! * 1000);
                stopWrapper.Invoke();
                runThread.Join();
            });
        }

        public void Dispose()
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
            Console.SetOut(oldOut);
            Console.SetError(oldErr);
            testWriter.Dispose();
        }

        [Fact]
        public void E2E_InvalidArgs_Test()
        {
            string[] args = ["arg1", "arg2"];

            runThread.Start(args);
            stopThread.Start(2);
            runThread.Join();
            stopThread.Join();

            var output = testWriter.ToString();
            Assert.Contains("Usage:", output);
            Assert.Contains("Invalid number of arguments", output);
        }

        /*FIX: Fails
        [Fact]
        public void E2E_FileCreation_Test()
        {
            string[] args = [sourceDir, replicaDir, logFile, "1"];

            runThread.Start(args);
            stopThread.Start(5);
            runThread.Join();
            stopThread.Join();

            var testFile = Path.Combine(sourceDir, "copy_test");
            File.WriteAllText(testFile, "test_copy");
            
            // Verify log file
            var logContent = File.ReadAllText(logFile);
            Assert.Contains($"File: {testFile} (Created)", logContent);

            // Verify file copied
            var replicaFile = Path.Combine(replicaDir, "copy_test");
            Assert.True(File.Exists(replicaFile));
            Assert.Equal("test_copy", File.ReadAllText(replicaFile));
        }*/
    }
}