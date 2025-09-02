using DirectorySynchronizer.src;

namespace DirectorySynchronizer.Tests
{
    public class FilesEqualTests : IDisposable
    {

        private readonly string file1;
        private readonly string file2;
        private bool disposed = false;

        public FilesEqualTests()
        {
            file1 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            file2 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
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
                    if (File.Exists(file1)) File.Delete(file1);
                    if (File.Exists(file2)) File.Delete(file2);
                }
                disposed = true;
            }
        }

        [Fact]
        public void FilesEqual_True()
        {
            File.WriteAllText(file1, "test");
            File.Copy(file1, file2, true);

            Assert.True(Synchronizer.FilesEqual(file1, file2));

            File.Delete(file1);
            File.Delete(file2);
        }

        [Fact]
        public void FilesEqual_False()
        {
            File.WriteAllText(file1, "test1");
            File.WriteAllText(file2, "test2");

            Assert.False(Synchronizer.FilesEqual(file1, file2));

            File.Delete(file1);
            File.Delete(file2);
        }
    }

    public class SyncTests : IDisposable
    {
        private readonly string sourceDir;
        private readonly string replicaDir;
        private bool disposed = false;

        public SyncTests()
        {
            sourceDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            replicaDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(replicaDir);
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
                    if (Directory.Exists(sourceDir)) Directory.Delete(sourceDir, true);
                    if (Directory.Exists(replicaDir)) Directory.Delete(replicaDir, true);
                }
                disposed = true;
            }
        }

        [Fact]
        public void SyncCreation_Test()
        {
            if (Directory.Exists(sourceDir)) Directory.Delete(sourceDir, true);
            if (Directory.Exists(replicaDir)) Directory.Delete(replicaDir, true);
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(replicaDir);

            var srcFile = Path.Combine(sourceDir, "file");
            File.WriteAllText(srcFile, "test");

            Synchronizer.SyncCreationInDirectories(sourceDir, replicaDir);

            var replicaFile = Path.Combine(replicaDir, "file");
            Assert.True(File.Exists(replicaFile));
            Assert.Equal("test", File.ReadAllText(replicaFile));

            Directory.Delete(sourceDir, true);
            Directory.Delete(replicaDir, true);
        }

        [Fact]
        public void SyncRemoval_Test()
        {
            if (Directory.Exists(sourceDir)) Directory.Delete(sourceDir, true);
            if (Directory.Exists(replicaDir)) Directory.Delete(replicaDir, true);
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(replicaDir);

            // This file exists only in replicaDir, so it should be removed
            var replicaFile = Path.Combine(replicaDir, "file");
            File.WriteAllText(replicaFile, "test");

            Synchronizer.SyncRemovalInDirectories(sourceDir, replicaDir);

            Assert.False(File.Exists(replicaFile));

            Directory.Delete(sourceDir, true);
            Directory.Delete(replicaDir, true);
        }
    }
}