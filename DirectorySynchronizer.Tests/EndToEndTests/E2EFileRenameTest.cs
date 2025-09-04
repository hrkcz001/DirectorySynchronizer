namespace DirectorySynchronizer.Tests
{
    public class E2EFileRenameTest : EndToEndTest
    {
        [Theory]
        [InlineData(4)] // secondsToLive
        public void E2E_FileRename_Test(int secondsToLive)
        {
            string[] args = [sourceDir, replicaDir, logFile, "1"];

            var testFile = Path.Combine(sourceDir, "old_test");
            File.WriteAllText(testFile, "test_old");

            var thread = RunProgram(args, secondsToLive, testWriter);

            // Wait a bit to ensure the initial copy is done
            Thread.Sleep(secondsToLive * 1000 / 2);

            var replicaFile = Path.Combine(replicaDir, "old_test");
            var copyExisted = File.Exists(replicaFile);

            var renamedFile = Path.Combine(sourceDir, "new_test");
            File.Move(testFile, renamedFile);

            thread.Join();

            // Log verification needed

            Assert.True(copyExisted);
            Assert.False(File.Exists(replicaFile));

            var renamedReplicaFile = Path.Combine(replicaDir, "new_test");
            Assert.True(File.Exists(renamedReplicaFile));
            Assert.Equal("test_old", File.ReadAllText(renamedReplicaFile));
        }
    }
}