using System.Collections;
using System.Security.Cryptography;

class DirectorySynchronizer(string sourceDir, string replicaDir, Logger logger)
{
    private readonly string sourceDir = sourceDir;
    private readonly string replicaDir = replicaDir;
    private readonly Logger logger = logger;

    public void Start(int interval)
    {
        InitLogging();
        while (true)
        {
            try
            {
                while (true)
                {
                    SyncDirectories(sourceDir, replicaDir);
                    System.Threading.Thread.Sleep(interval * 1000);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error during synchronization: {ex.Message}");
            }
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
        logger.Log($"File: {e.FullPath} {e.ChangeType}");
    }

    private void OnRenamed(object source, RenamedEventArgs e)
    {
        logger.Log($"File: {e.OldFullPath} renamed to {e.FullPath}");
    }

    // Files are considered equal if their MD5 hashes match.
    public static bool FilesEqual(string f1, string f2)
    {
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
                logger.Log($"Source directory '{source}' does not exist.");
                throw;
            }
            if (!Directory.Exists(replica))
            {
                logger.Log($"Replica directory '{replica}' does not exist. Trying to recreate it.");
                try
                {
                    Directory.CreateDirectory(replica);
                }
                catch (Exception ex2)
                {
                    logger.Log($"Error creating replica directory: {ex2.Message}");
                    throw;
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

        // Call SyncRemovalInDirectories recursively for each subdirectory in replica or remove if not exist in source
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