using System.Runtime.InteropServices;
using DirectorySynchronizer.src;

namespace DirectorySynchronizer.Tests
{
    public class ArgsValidatorTests : IDisposable
    {
        private readonly string tempSource;
        private readonly string tempReplica;
        private readonly string tempLog;
        private bool disposed = false;

        public ArgsValidatorTests()
        {
            tempSource = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            tempReplica = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            tempLog = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (Directory.Exists(tempSource)) Directory.Delete(tempSource, true);
                    if (Directory.Exists(tempReplica)) Directory.Delete(tempReplica, true);
                    if (File.Exists(tempLog)) File.Delete(tempLog);
                }
                disposed = true;
            }
        }

        [Fact]
        public void ValidateArgs_CorrectArgs_DoesNotThrow()
        {
            string[] args = [tempSource, tempReplica, tempLog, "5"];
            Exception ex = Record.Exception(() => ArgsValidator.ValidateArgs(args));
            Assert.Null(ex);

            Assert.True(Directory.Exists(tempSource));
            Assert.True(Directory.Exists(tempReplica));
            Assert.True(File.Exists(tempLog));
        }

        [Theory]
        [InlineData()]
        [InlineData("arg1")]
        [InlineData("arg1", "arg2")]
        [InlineData("arg1", "arg2", "arg3")]
        [InlineData("arg1", "arg2", "arg3", "arg4", "arg5")]
        public void ValidateArgs_WrongNumberOfArgs_Throws(params string[] args)
        {
            var ex = Assert.Throws<ArgumentException>(() => ArgsValidator.ValidateArgs(args));
            Assert.Equal("Invalid number of arguments.", ex.Message);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-5")]
        [InlineData("abc")]
        [InlineData("5.5")]
        public void ValidateArgs_InvalidInterval_Throws(string interval)
        {
            string[] args = [tempSource, tempReplica, tempLog, interval];
            var ex = Assert.Throws<ArgumentException>(() => ArgsValidator.ValidateArgs(args));
            Assert.Equal("Interval must be a positive integer.", ex.Message);
        }

        [Fact]
        public void ValidateArgs_SourceInsideReplica_Throws()
        {
            string subDir = Path.Combine(tempReplica, "sub");

            string[] args = [subDir, tempReplica, tempLog, "5"];
            var ex = Assert.Throws<ArgumentException>(() => ArgsValidator.ValidateArgs(args));
            Assert.Equal("Source, replica, and log paths must be independent of each other.", ex.Message);
        }

        [Fact]
        public void ValidateArgs_ReplicaInsideSource_Throws()
        {
            string subDir = Path.Combine(tempSource, "sub");

            string[] args = [tempSource, subDir, tempLog, "5"];
            var ex = Assert.Throws<ArgumentException>(() => ArgsValidator.ValidateArgs(args));
            Assert.Equal("Source, replica, and log paths must be independent of each other.", ex.Message);
        }

        [Fact]
        public void ValidateArgs_LogInsideSource_Throws()
        {
            string logPath = Path.Combine(tempSource, "log.txt");

            string[] args = [tempSource, tempReplica, logPath, "5"];
            var ex = Assert.Throws<ArgumentException>(() => ArgsValidator.ValidateArgs(args));
            Assert.Equal("Source, replica, and log paths must be independent of each other.", ex.Message);
        }

        [Fact]
        public void ValidateArgs_LogInsideReplica_Throws()
        {
            string logPath = Path.Combine(tempReplica, "log.txt");

            string[] args = [tempSource, tempReplica, logPath, "5"];
            var ex = Assert.Throws<ArgumentException>(() => ArgsValidator.ValidateArgs(args));
            Assert.Equal("Source, replica, and log paths must be independent of each other.", ex.Message);
        }

        [Fact]
        public void ValidateArgs_SourceInsideLog_Throws()
        {
            string newSorce = Path.Combine(tempLog, "source");

            string[] args = [newSorce, tempReplica, tempLog, "5"];
            var ex = Assert.Throws<ArgumentException>(() => ArgsValidator.ValidateArgs(args));
            Assert.Equal("Source, replica, and log paths must be independent of each other.", ex.Message);
        }

        [Fact]
        public void ValidateArgs_ReplicaInsideLog_Throws()
        {
            string newReplica = Path.Combine(tempLog, "replica");

            string[] args = [tempSource, newReplica, tempLog, "5"];
            var ex = Assert.Throws<ArgumentException>(() => ArgsValidator.ValidateArgs(args));
            Assert.Equal("Source, replica, and log paths must be independent of each other.", ex.Message);
        }

        [Fact]
        public void ValidateArgs_SameSourceAndReplica_Throws()
        {
            string[] args = [tempSource, tempSource, tempLog, "5"];
            var ex = Assert.Throws<ArgumentException>(() => ArgsValidator.ValidateArgs(args));
            Assert.Equal("Source, replica, and log paths must be independent of each other.", ex.Message);
        }

        [Fact]
        public void ValidateArgs_SameSourceAndLog_Throws()
        {
            string[] args = [tempSource, tempReplica, tempSource, "5"];
            var ex = Assert.Throws<ArgumentException>(() => ArgsValidator.ValidateArgs(args));
            Assert.Equal("Source, replica, and log paths must be independent of each other.", ex.Message);
        }

        [Fact]
        public void ValidateArgs_SameReplicaAndLog_Throws()
        {
            string[] args = [tempSource, tempReplica, tempReplica, "5"];
            var ex = Assert.Throws<ArgumentException>(() => ArgsValidator.ValidateArgs(args));
            Assert.Equal("Source, replica, and log paths must be independent of each other.", ex.Message);
        }

        // This test is platform dependent and works only on Windows due to AccessControl usage
        [Fact]
        public void ValidateArgs_ReplicaNoWritePermission_Throws()
        {
            // AccessControl works only on Windows
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            try
            {
                // Remove write permissions from the source directory
                Directory.CreateDirectory(tempReplica);
                Util.RemoveDirectoryWritePermission(tempReplica);

                string[] args = [tempSource, tempReplica, tempLog, "5"];
                var ex = Assert.Throws<ArgumentException>(() => ArgsValidator.ValidateArgs(args));
                Assert.StartsWith("Replica directory is not writable:", ex.Message);
            }
            finally
            {
                // Restore write permissions to the source directory
                Util.RestoreDirectoryWritePermission(tempReplica);
            }
        }

        // This test is platform dependent and works only on Windows due to AccessControl usage
        [SkippableFact]
        public void ValidateArgs_LogNoWritePermission_Throws()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Test runs only on Windows.");

            try
            {
                // Remove write permissions from the log file
                File.WriteAllText(tempLog, "Test log content");
                Util.RemoveFileWritePermission(tempLog);

                string[] args = [tempSource, tempReplica, tempLog, "5"];
                var ex = Assert.Throws<ArgumentException>(() => ArgsValidator.ValidateArgs(args));
                Assert.StartsWith("Error accessing log file:", ex.Message);
            }
            finally
            {
                // Restore write permissions to the log file
                Util.RestoreFileWritePermission(tempLog);
            }
        }

        // This test is platform dependent and works only on Windows due to AccessControl usage
        [SkippableFact]
        public void ValidateArgs_SourceNoReadPermission_Throws()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Test runs only on Windows.");

            try
            {
                // Remove read permissions from the source directory
                Directory.CreateDirectory(tempSource);
                Util.RemoveDirectoryReadPermission(tempSource); // Using write permission removal to simulate lack of access

                string[] args = [tempSource, tempReplica, tempLog, "5"];
                var ex = Assert.Throws<ArgumentException>(() => ArgsValidator.ValidateArgs(args));
                Assert.StartsWith("Source directory is not readable:", ex.Message);
            }
            finally
            {
                // Restore read permissions to the source directory
                Util.RestoreDirectoryReadPermission(tempSource);
            }
        }
    }

    public class IsFileInsideDirectory_Tests
    {
        // Windows test for IsFileInsideDirectory method
        [SkippableTheory]
        [InlineData("C:\\Dir\\File", "C:\\Dir", true)]
        [InlineData("C:\\Dir", "C:\\Dir", true)]
        [InlineData("C:\\Dir\\Subdir\\File", "C:\\Dir", true)]
        [InlineData("C:\\dir\\File", "C:\\Dir", true)]
        [InlineData("C:\\File", "C:\\Dir", false)]
        [InlineData("C:\\Dir1", "C:\\Dir", false)]
        public void IsFileInsideDirectory_WindowsTests(string filePath, string dirPath, bool expected)
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Test runs only on Windows.");

            bool result = ArgsValidator.IsFileInsideDirectory(filePath, dirPath);
            Assert.Equal(expected, result);
        }

        // Unix, FreeBSD and MacOS test for IsFileInsideDirectory method
        [SkippableTheory]
        [InlineData("/dir/file", "/dir", true)]
        [InlineData("/dir", "/dir", true)]
        [InlineData("/dir/subdir/file", "/dir", true)]
        [InlineData("/dir/file", "/Dir", true)]
        [InlineData("/file", "/dir", false)]
        [InlineData("/dir1", "/dir", false)]
        public void IsFileInsideDirectory_UnixTests(string dirPath, string filePath, bool expected)
        {

            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                       RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD) ||
                       RuntimeInformation.IsOSPlatform(OSPlatform.OSX),
                       "Test runs only on Unix, FreeBSD and MacOS systems.");

            bool result = ArgsValidator.IsFileInsideDirectory(filePath, dirPath);
            Assert.Equal(expected, result);
        }
    }
}