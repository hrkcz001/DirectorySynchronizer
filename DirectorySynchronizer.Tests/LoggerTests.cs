using DirectorySynchronizer.src;

namespace DirectorySynchronizer.Tests
{
    public class LoggerTests : IDisposable
    {
        private readonly string tempFile;
        private readonly StringWriter testWriter;
        private readonly StreamWriter logFileWriter;

        public LoggerTests()
        {
            tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            testWriter = new StringWriter();
            logFileWriter = new StreamWriter(tempFile);
        }

        public void Dispose()
        {
            testWriter.Dispose();
            logFileWriter.Dispose();
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }

        [Fact]
        public void Logger_Log_Test()
        {
            var logger = new Logger([testWriter, logFileWriter]);
            logger.Log("Test");

            logFileWriter.Close();

            string content = File.ReadAllText(tempFile);
            Assert.Equal(content, testWriter.ToString());
            Assert.Contains("Test", content);
        }

        [Fact]
        public void Logger_WritesTimestamp_Test()
        {
            var logger = new Logger([testWriter, logFileWriter]);
            logger.Log("Test");

            logFileWriter.Close();

            string content = File.ReadAllText(tempFile);
            Assert.Equal(content, testWriter.ToString());
            Assert.Contains("Test", content);
            Assert.Matches(@"\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\]", content);
        }
    }

    public class LoggerConcurrencyTests : IDisposable
    {
        private readonly string tempFile;
        private readonly StringWriter testWriter;
        private readonly StreamWriter logFileWriter;

        public LoggerConcurrencyTests()
        {
            tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            testWriter = new StringWriter();
            logFileWriter = new StreamWriter(tempFile);
        }

        public void Dispose()
        {
            testWriter.Dispose();
            logFileWriter.Dispose();
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }

        [Fact]
        public async Task Logger_ConcurrentWrite_Test()
        {
            int threadCount = 10;
            int messagesPerThread = 100;

            var logger = new Logger([testWriter, logFileWriter]);

            var barrier = new Barrier(threadCount);
            var tasks = new List<Task>();

            for (int i = 0; i < threadCount; i++)
            {
                int threadId = i;
                tasks.Add(Task.Run(async () =>
                {
                    // Barrier is a starting point of writes.
                    // It ensures that all threads will start writing at the same time.
                    barrier.SignalAndWait();

                    for (int j = 0; j < messagesPerThread; j++)
                    {
                        logger.Log($"{threadId}-{j}");
                        await Task.Yield();
                    }
                }));
            }

            await Task.WhenAll(tasks);

            logFileWriter.Close();

            var lines = File.ReadAllLines(tempFile);
            Assert.Equal(lines, testWriter.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
            // All lines were written.
            Assert.Equal(threadCount * messagesPerThread, lines.Length);
            // Each line was written exactly one time.
            Assert.Equal(lines.Distinct().Count(), lines.Length);
        }
    }
}