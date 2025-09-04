namespace DirectorySynchronizer.Tests
{
    public class E2EDirectoryRenameTest : EndToEndTest
    {
        [Theory]
        [InlineData(4)] // secondsToLive
        public void E2E_DirectoryRename_Test(int secondsToLive)
        {
            string[] args = [sourceDir, replicaDir, logFile, "1"];

            var testSubDir = Path.Combine(sourceDir, "old_subdir");
            Directory.CreateDirectory(testSubDir);
            var testFile = Path.Combine(testSubDir, "file_in_subdir");
            File.WriteAllText(testFile, "test_in_subdir");

            var thread = RunProgram(args, secondsToLive, testWriter);

            // Wait a bit to ensure the initial copy is done
            Thread.Sleep(secondsToLive * 1000 / 2);

            var replicaSubDir = Path.Combine(replicaDir, "old_subdir");
            var replicaFile = Path.Combine(replicaSubDir, "file_in_subdir");
            var copyExisted = Directory.Exists(replicaSubDir) && File.Exists(replicaFile);

            var renamedSubDir = Path.Combine(sourceDir, "new_subdir");
            Directory.Move(testSubDir, renamedSubDir);

            thread.Join();

            // Log verification needed

            Assert.True(copyExisted);
            Assert.False(Directory.Exists(replicaSubDir));
            Assert.False(File.Exists(replicaFile));

            var renamedReplicaSubDir = Path.Combine(replicaDir, "new_subdir");
            var renamedReplicaFile = Path.Combine(renamedReplicaSubDir, "file_in_subdir");
            Assert.True(Directory.Exists(renamedReplicaSubDir));
            Assert.True(File.Exists(renamedReplicaFile));
            Assert.Equal("test_in_subdir", File.ReadAllText(renamedReplicaFile));
        }
    }
}