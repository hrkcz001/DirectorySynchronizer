namespace DirectorySynchronizer.Tests
{
    public class E2EDirectoryCreationTest : EndToEndTest
    {
        [Theory]
        [InlineData(2)] // secondsToLive
        public void E2E_DirectoryCreation_Test(int secondsToLive)
        {
            string[] args = [sourceDir, replicaDir, logFile, "1"];

            var thread = RunProgram(args, secondsToLive, testWriter);

            var testSubDir = Path.Combine(sourceDir, "subdir");
            Directory.CreateDirectory(testSubDir);
            var testFile = Path.Combine(testSubDir, "file_in_subdir");
            File.WriteAllText(testFile, "test_in_subdir");

            thread.Join();

            // Log verification needed

            var replicaSubDir = Path.Combine(replicaDir, "subdir");
            var replicaFile = Path.Combine(replicaSubDir, "file_in_subdir");
            Assert.True(Directory.Exists(replicaSubDir));
            Assert.True(File.Exists(replicaFile));
            Assert.Equal("test_in_subdir", File.ReadAllText(replicaFile));
        }
    }
}