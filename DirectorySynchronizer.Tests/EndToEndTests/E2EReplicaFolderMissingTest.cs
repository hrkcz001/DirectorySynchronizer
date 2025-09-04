namespace DirectorySynchronizer.Tests
{
    public class E2EReplicaFolderMissingTest : EndToEndTest
    {
        [Theory]
        [InlineData(4)] // secondsToLive
        public void E2E_ReplicaFolderMissing_Test(int secondsToLive)
        {
            string[] args = [sourceDir, replicaDir, logFile, "1"];

            var testFile = Path.Combine(sourceDir, "file_before_replica_missing");
            File.WriteAllText(testFile, "test_before");

            var thread = RunProgram(args, secondsToLive, testWriter);

            // Wait a bit to ensure the initial copy is done
            Thread.Sleep(secondsToLive * 1000 / 2);

            // Delete the replica directory to simulate it being missing
            Directory.Delete(replicaDir, true);

            thread.Join();

            // Log verification needed

            var replicaFile = Path.Combine(replicaDir, "file_before_replica_missing");
            Assert.True(File.Exists(replicaFile));
            Assert.Equal("test_before", File.ReadAllText(replicaFile));
        }
    }
}