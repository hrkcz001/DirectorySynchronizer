using System.Collections;
using System.Security.Cryptography;

namespace DirectorySynchronizer.src
{

    // Class responsible for synchronizing two directories.
    // It periodically checks for changes in the source directory and updates the replica directory accordingly.
    public class Synchronizer(string sourceDir, string replicaDir, Logger logger)
    {
        private readonly string sourceDir = sourceDir;
        private readonly string replicaDir = replicaDir;
        private readonly Logger logger = logger;
        public bool Running { get; private set; } = false;

        public void Start(int interval)
        {
            InitLogging();
            Running = true;
            try
            {
                while (Running)
                {
                    SyncDirectories(sourceDir, replicaDir);
                    var timeLeft = interval;

                    while (Running && timeLeft > 0) // Sleep in 1 second to respond to stop request
                    {
                        Thread.Sleep(1000);
                        timeLeft--;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error during synchronization: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (Running)
            {
                logger.Log("Synchronization cancelled.");
                Running = false;
            }
        }

        // Directories could be synchronized in the same way,
        // but since synchronization is periodic and it is more reliable to fully check the directories,
        // we use FileSystemWatcher only for logging changes.
        // Additionally, this approach provides more accurate timestamps in the logs.
        private void InitLogging()
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = sourceDir;
            watcher.IncludeSubdirectories = true;

            watcher.Created += OnChanged;
            watcher.Changed += OnChanged;
            watcher.Deleted += OnChanged;
            watcher.Renamed += OnRenamed;

            watcher.NotifyFilter = NotifyFilters.FileName |
                                NotifyFilters.DirectoryName |
                                NotifyFilters.LastWrite |
                                NotifyFilters.Size;

            watcher.Filter = "*.*";

            watcher.EnableRaisingEvents = true;
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            if (Directory.Exists(e.FullPath))
            {
                logger.Log($"Directory: {e.FullPath} ({e.ChangeType})");
            }
            else if (File.Exists(e.FullPath))
            {
                logger.Log($"File: {e.FullPath} ({e.ChangeType})");
            }
            else
            {
                logger.Log($"File/Directory: {e.FullPath} ({e.ChangeType})");
            }
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            if (Directory.Exists(e.FullPath))
            {
                logger.Log($"Directory: {e.OldFullPath} renamed to {e.FullPath}");
            }
            else if (File.Exists(e.FullPath))
            {
                logger.Log($"File: {e.OldFullPath} renamed to {e.FullPath}");
            }
            else
            {
                logger.Log($"File/Directory: {e.OldFullPath} renamed to {e.FullPath}");
            }
        }

        // Files are considered equal if their MD5 hashes match.
        public static bool FilesEqual(string f1, string f2)
        {

            // First check file size and last write time for a quick comparison
            var fileInfo1 = new FileInfo(f1);
            var fileInfo2 = new FileInfo(f2);
            if (fileInfo1.Length != fileInfo2.Length || fileInfo1.LastWriteTime != fileInfo2.LastWriteTime)
            {
                return false;
            }

            // If sizes and last write times are the same, do a deeper comparison using MD5 hashes
            using var md5 = MD5.Create();
            using var stream1 = File.OpenRead(f1);
            using var stream2 = File.OpenRead(f2);

            byte[] hash1 = md5.ComputeHash(stream1);
            byte[] hash2 = md5.ComputeHash(stream2);

            return StructuralComparisons.StructuralEqualityComparer.Equals(hash1, hash2);
        }

        private void SyncDirectories(string source, string replica)
        {
            try
            {
                SyncCreationInDirectories(source, replica);
                SyncRemovalInDirectories(source, replica);
            }
            catch (Exception)
            {
                if (!Directory.Exists(source))
                {
                    logger.Log($"Error: Source directory '{source}' does not exist.");
                    Stop();
                    return;
                }
                if (!Directory.Exists(replica))
                {
                    logger.Log($"Warning: Replica directory '{replica}' does not exist. Trying to recreate it.");
                    try
                    {
                        Directory.CreateDirectory(replica);
                        SyncCreationInDirectories(source, replica);
                        SyncRemovalInDirectories(source, replica);
                    }
                    catch (Exception ex2)
                    {
                        logger.Log($"Error: Failed to create replica directory: {ex2.Message}");
                        Stop();
                        return;
                    }
                }
                else
                {
                    throw;
                }
            }
        }

        public static void SyncCreationInDirectories(string source, string replica)
        {
            source = source.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            replica = replica.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            // Copy new and updated files from source to replica
            foreach (var file in Directory.GetFiles(source))
            {
                var relativePath = Path.GetRelativePath(source, file);
                var replicaFilePath = replica + relativePath;

                if (!File.Exists(replicaFilePath) || !FilesEqual(file, replicaFilePath))
                {
                    File.Copy(file, replicaFilePath, true);
                }
            }

            // Call SyncDirectories recursively for each subdirectory in source
            foreach (var dir in Directory.GetDirectories(source))
            {
                var relativePath = Path.GetRelativePath(source, dir);
                var replicaDirPath = Path.Combine(replica, relativePath);

                if (!Directory.Exists(replicaDirPath))
                {
                    Directory.CreateDirectory(replicaDirPath);
                }

                SyncCreationInDirectories(dir, replicaDirPath);
            }
        }

        public static void SyncRemovalInDirectories(string source, string replica)
        {
            source = source.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            replica = replica.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            // Remove files from replica that do not exist in source
            foreach (var file in Directory.GetFiles(replica))
            {
                var relativePath = Path.GetRelativePath(replica, file);
                var sourceFilePath = Path.Combine(source, relativePath);

                if (!File.Exists(sourceFilePath))
                {
                    File.Delete(file);
                }
            }

            // Call SyncRemovalInDirectories recursively for each subdirectory in replica or remove if doesn't exist in source
            foreach (var dir in Directory.GetDirectories(replica))
            {
                var relativePath = Path.GetRelativePath(replica, dir);
                var sourceDirPath = Path.Combine(source, relativePath);

                if (!Directory.Exists(sourceDirPath))
                {
                    Directory.Delete(dir, true);
                }
                else
                {
                    SyncRemovalInDirectories(sourceDirPath, dir);
                }
            }
        }
    }
}