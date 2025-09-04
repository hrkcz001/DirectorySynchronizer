using DirectorySynchronizer.src;

namespace DirectorySynchronizer.Tests
{
    public class FilesEqualTests : IDisposable
    {

        private readonly string file1;
        private readonly string file2;

        public FilesEqualTests()
        {
            file1 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            file2 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        }

        public void Dispose()
        {
            if (File.Exists(file1)) File.Delete(file1);
                    if (File.Exists(file2)) File.Delete(file2);
        }

        [Fact]
        public void FilesEqual_True_Test()
        {
            File.WriteAllText(file1, "test");
            File.Copy(file1, file2, true);

            Assert.True(Synchronizer.FilesEqual(file1, file2));
        }

        [Fact]
        public void FilesEqual_False_Test()
        {
            File.WriteAllText(file1, "test1");
            File.WriteAllText(file2, "test2");

            Assert.False(Synchronizer.FilesEqual(file1, file2));
        }
    }

    public class SyncTests : IDisposable
    {
        private readonly string sourceDir;
        private readonly string replicaDir;

        public SyncTests()
        {
            sourceDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            replicaDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(replicaDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(sourceDir)) Directory.Delete(sourceDir, true);
            if (Directory.Exists(replicaDir)) Directory.Delete(replicaDir, true);
        }

        [Fact]
        public void SyncCreation_Test()
        {
            var srcFile = Path.Combine(sourceDir, "file");
            File.WriteAllText(srcFile, "test");

            Synchronizer.SyncCreationInDirectories(sourceDir, replicaDir);

            var replicaFile = Path.Combine(replicaDir, "file");
            Assert.True(File.Exists(replicaFile));
            Assert.Equal("test", File.ReadAllText(replicaFile));
        }

        [Fact]
        public void SyncRemoval_Test()
        {
            // This file exists only in replicaDir, so it should be removed
            var replicaFile = Path.Combine(replicaDir, "file");
            File.WriteAllText(replicaFile, "test");

            Synchronizer.SyncRemovalInDirectories(sourceDir, replicaDir);

            Assert.False(File.Exists(replicaFile));
        }
    }
}