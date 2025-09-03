using DirectorySynchronizer.src;

namespace DirectorySynchronizer.Tests
{
    public class LoggerTests : IDisposable
    {
        private readonly string tempFile;

        public LoggerTests()
        {
            tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        }

        public void Dispose()
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }

        [Fact]
        public void Logger_Dispose_Test()
        {
            File.Create(tempFile).Close();

            var logger = new Logger(tempFile);
            logger.Log("Test");
            logger.Dispose();

            string content = File.ReadAllText(tempFile);
            Assert.Contains("Test", content);
            File.Delete(tempFile);
            Assert.False(File.Exists(tempFile));
        }

        [Fact]
        public void Logger_CreatesFile_Test()
        {
            using (var logger = new Logger(tempFile))
            {
                logger.Log("Test");
            }

            Assert.True(File.Exists(tempFile));
            string content = File.ReadAllText(tempFile);
            Assert.Contains("Test", content);
        }

        [Fact]
        public void Logger_WritesTimestamp_Test()
        {
            File.Create(tempFile).Close();

            using (var logger = new Logger(tempFile))
            {
                logger.Log("Test");
            }

            string content = File.ReadAllText(tempFile);
            Assert.Contains("Test", content);
            Assert.Matches(@"\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\]", content);
        }
    }

    public class LoggerConcurrencyTests : IDisposable
    {
        private readonly string tempFile;

        public LoggerConcurrencyTests()
        {
            tempFile = Path.GetTempFileName();
        }

        public void Dispose()
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }

        [Fact]
        public async Task Logger_ConcurrentWrite_Test()
        {
            int threadCount = 10;
            int messagesPerThread = 100;

            using (var logger = new Logger(tempFile))
            {

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
            }

            var lines = File.ReadAllLines(tempFile);
            // All lines were written.
            Assert.Equal(threadCount * messagesPerThread, lines.Length);
            // Each line was written exactly one time.
            Assert.Equal(lines.Distinct().Count(), lines.Length);
        }
    }
}