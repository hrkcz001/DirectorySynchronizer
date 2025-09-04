namespace DirectorySynchronizer.Tests
{
    public class E2EFileModificationTest : EndToEndTest
    {
        [Theory]
        [InlineData(4)] // secondsToLive
        public void E2E_FileModification_Test(int secondsToLive)
        {
            string[] args = [sourceDir, replicaDir, logFile, "1"];

            var testFile = Path.Combine(sourceDir, "modify_test");
            File.WriteAllText(testFile, "test_modify");

            var thread = RunProgram(args, secondsToLive, testWriter);

            // Wait a bit to ensure the initial copy is done
            Thread.Sleep(secondsToLive * 1000 / 2);

            File.WriteAllText(testFile, "test_modify_changed");

            thread.Join();

            // Log verification needed

            var replicaFile = Path.Combine(replicaDir, "modify_test");
            Assert.True(File.Exists(replicaFile));
            Assert.Equal("test_modify_changed", File.ReadAllText(replicaFile));
        }
    }
}