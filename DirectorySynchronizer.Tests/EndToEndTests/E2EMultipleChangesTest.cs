namespace DirectorySynchronizer.Tests
{
    public class E2EMultipleChangesTest : EndToEndTest
    {
        [Theory]
        [InlineData(6, 3)] // secondsToLive, numberOfFiles
        public void E2E_MultipleChanges_Test(int secondsToLive, int numberOfFiles)
        {
            string[] args = [sourceDir, replicaDir, logFile, "1"];

            var thread = RunProgram(args, secondsToLive, testWriter);

            // Creation
            for (int i = 0; i < numberOfFiles; i++)
            {
                var testFile = Path.Combine(sourceDir, $"file_{i}");
                File.WriteAllText(testFile, $"content_{i}");
            }

            // Wait a bit to ensure the initial copy is done
            Thread.Sleep(secondsToLive * 1000 / 3);

            for (int i = 0; i < numberOfFiles; i++)
            {
                var replicaFile = Path.Combine(replicaDir, $"file_{i}");
                Assert.False(!File.Exists(replicaFile) ||
                              File.ReadAllText(replicaFile) != $"content_{i}"
                             , $"Initial creation of {replicaFile} failed");
            }

            // Modification
            for (int i = 0; i < numberOfFiles; i++)
            {
                var testFile = Path.Combine(sourceDir, $"file_{i}");
                File.WriteAllText(testFile, $"content_{i}_modified");
            }

            // SubDirs Creation
            for (int i = 0; i < numberOfFiles; i++)
            {
                var testSubDir = Path.Combine(sourceDir, $"subdir_{i}");

                Directory.CreateDirectory(testSubDir);

                var testFile = Path.Combine(testSubDir, $"file_in_subdir_{i}");
                File.WriteAllText(testFile, $"content_in_subdir_{i}");

                for (int j = 0; j < numberOfFiles; j++)
                {
                    var nestedSubDir = Path.Combine(testSubDir, $"nested_subdir_{j}");
                    Directory.CreateDirectory(nestedSubDir);
                    var nestedFile = Path.Combine(nestedSubDir, $"file_in_nested_subdir_{j}");
                    File.WriteAllText(nestedFile, $"content_in_nested_subdir_{j}");
                }
            }


            // Wait a bit to ensure the modifications are processed
            Thread.Sleep(secondsToLive * 1000 / 3);

            for (int i = 0; i < numberOfFiles; i++)
            {
                var replicaFile = Path.Combine(replicaDir, $"file_{i}");
                Assert.False(!File.Exists(replicaFile) ||
                              File.ReadAllText(replicaFile) != $"content_{i}_modified"
                             , $"Modification of {replicaFile} failed");

                var replicaSubDir = Path.Combine(replicaDir, $"subdir_{i}");
                var replicaSubDirFile = Path.Combine(replicaSubDir, $"file_in_subdir_{i}");
                Assert.False(!Directory.Exists(replicaSubDir) ||
                             !File.Exists(replicaSubDirFile) ||
                              File.ReadAllText(replicaSubDirFile) != $"content_in_subdir_{i}"
                             , $"Modification of {replicaSubDirFile} failed");

                for (int j = 0; j < numberOfFiles; j++)
                {
                    var nestedReplicaSubDir = Path.Combine(replicaSubDir, $"nested_subdir_{j}");
                    var nestedReplicaFile = Path.Combine(nestedReplicaSubDir, $"file_in_nested_subdir_{j}");
                    Assert.False(!Directory.Exists(nestedReplicaSubDir) ||
                                 !File.Exists(nestedReplicaFile)        ||
                                  File.ReadAllText(nestedReplicaFile) != $"content_in_nested_subdir_{j}"
                                 , $"Modification of {nestedReplicaFile} failed");
                    
                }
            }

            // Deletion
            for (int i = 0; i < numberOfFiles; i++)
            {
                var testFile = Path.Combine(sourceDir, $"file_{i}");
                File.Delete(testFile);

                var testSubDir = Path.Combine(sourceDir, $"subdir_{i}");
                Directory.Delete(testSubDir, true);
            }

            thread.Join();

            for (int i = 0; i < 3; i++)
            {
                var replicaFile = Path.Combine(replicaDir, $"file_{i}");
                Assert.False(File.Exists(replicaFile), $"Deletion of {replicaFile} failed");

                var replicaSubDir = Path.Combine(replicaDir, $"subdir_{i}");
                Assert.False(Directory.Exists(replicaSubDir), $"Deletion of {replicaSubDir} failed");
            }
        }
    }
}