namespace DirectorySynchronizer.Tests
{
    public class E2EFileCreationTest : EndToEndTest
    {
        [Theory]
        [InlineData(2)] // secondsToLive
        public void E2E_FileCreation_Test(int secondsToLive)
        {
            string[] args = [sourceDir, replicaDir, logFile, "1"];

            var thread = RunProgram(args, secondsToLive, testWriter);

            var testFile = Path.Combine(sourceDir, "copy_test");
            File.WriteAllText(testFile, "test_copy");

            thread.Join();

            // Log verification needed

            var replicaFile = Path.Combine(replicaDir, "copy_test");
            Assert.True(File.Exists(replicaFile));
            Assert.Equal("test_copy", File.ReadAllText(replicaFile));
        }
    }
}