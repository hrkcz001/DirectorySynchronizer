namespace DirectorySynchronizer.Tests
{
    public class E2EDirectoryDeletionTest : EndToEndTest
    {
        [Theory]
        [InlineData(4)] // secondsToLive
        public void E2E_DirectoryDeletion_Test(int secondsToLive)
        {
            string[] args = [sourceDir, replicaDir, logFile, "1"];

            var testSubDir = Path.Combine(sourceDir, "subdir");
            Directory.CreateDirectory(testSubDir);
            var testFile = Path.Combine(testSubDir, "file_in_subdir");
            File.WriteAllText(testFile, "test_in_subdir");

            var thread = RunProgram(args, secondsToLive, testWriter);

            // Wait a bit to ensure the initial copy is done
            Thread.Sleep(secondsToLive * 1000 / 2);

            var replicaSubDir = Path.Combine(replicaDir, "subdir");
            var replicaFile = Path.Combine(replicaSubDir, "file_in_subdir");
            var copyExisted = Directory.Exists(replicaSubDir) && File.Exists(replicaFile);

            Directory.Delete(testSubDir, true);

            thread.Join();

            // Log verification needed

            Assert.True(copyExisted);
            Assert.False(Directory.Exists(replicaSubDir));
            Assert.False(File.Exists(replicaFile));
        }
    }
}