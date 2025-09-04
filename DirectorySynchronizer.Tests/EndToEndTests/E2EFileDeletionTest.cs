namespace DirectorySynchronizer.Tests
{
    public class E2EFileDeletionTest : EndToEndTest
    {
        [Theory]
        [InlineData(4)] // secondsToLive
        public void E2E_FileDeletion_Test(int secondsToLive)
        {
            string[] args = [sourceDir, replicaDir, logFile, "1"];

            var testFile = Path.Combine(sourceDir, "delete_test");
            File.WriteAllText(testFile, "test_delete");

            var thread = RunProgram(args, secondsToLive, testWriter);

            // Wait a bit to ensure the initial copy is done
            Thread.Sleep(secondsToLive * 1000 / 2);

            var replicaFile = Path.Combine(replicaDir, "delete_test");
            var copyExisted = File.Exists(replicaFile);

            File.Delete(testFile);

            thread.Join();

            // Log verification needed

            Assert.True(copyExisted);
            Assert.False(File.Exists(replicaFile));
        }
    }
}