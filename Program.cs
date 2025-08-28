using System.Collections;
using System.Security.Cryptography;

class Logger : IDisposable
{
    private readonly object lockObj = new object();
    private StreamWriter writer;
    private bool disposed = false;
    private string logFilePath;
    private bool error = false;

    public Logger(string logFilePath)
    {
        try
        {
            writer = new StreamWriter(logFilePath, true);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error opening log file: {ex.Message}");
        }
        this.logFilePath = logFilePath;
    }

    public void Log(string message)
    {
        lock (lockObj)
        {
            try
            {
                var fmessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                Console.WriteLine(fmessage);
                writer.WriteLine(fmessage);
                writer.Flush();
            }
            catch (Exception ex)
            {
                if (!error && !File.Exists(logFilePath))
                {
                    Console.Error.WriteLine($"Error writing to log file: {ex.Message}");
                    Console.WriteLine("Trying to recreate log file...");
                    try
                    {
                        writer?.Dispose();
                        writer = new StreamWriter(logFilePath, true);
                        error = false;
                    }
                    catch (Exception ex2)
                    {
                        Console.Error.WriteLine($"Error recreating log file: {ex2.Message}");
                        throw;
                    }
                    error = true;
                }
                else
                {
                    throw;
                }
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                writer?.Dispose();
            }
            disposed = true;
        }
    }
}

class DirectorySynchronizer(string sourceDir, string replicaDir, Logger logger)
{
    private readonly string sourceDir = sourceDir;
    private readonly string replicaDir = replicaDir;
    private readonly Logger logger = logger;
    private bool error = false;

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
            if (!error && !Directory.Exists(replica))
            {
                logger.Log($"Replica directory '{replica}' does not exist. Trying to recreate it.");
                try
                {
                    Directory.CreateDirectory(replica);
                    error = false;
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
            } else
            {
                SyncRemovalInDirectories(sourceDirPath, dir);
            }
        }
    }
}

class Program
{
    const string UsageMessage = "Usage: <source> <replica> <log> <interval>";

    static void Main(string[] args)
    {

        if (args.Length != 4)
        {
            Console.WriteLine(UsageMessage);
            return;
        }

        // Validate interval
        if (!int.TryParse(args[3], out int interval) || interval <= 0)
        {
            Console.WriteLine(UsageMessage);
            Console.Error.WriteLine("Interval must be a positive integer.");
            return;
        }

        // If any of the paths are inside each other, it could lead to unexpected behavior.
        if (IsFileInsideDirectory(args[0], args[1]) || IsFileInsideDirectory(args[1], args[0])
            || IsFileInsideDirectory(args[0], args[2]) || IsFileInsideDirectory(args[2], args[0])
            || IsFileInsideDirectory(args[1], args[2]) || IsFileInsideDirectory(args[2], args[1]))
        {
            Console.WriteLine(UsageMessage);
            Console.Error.WriteLine("Source, replica Directories and log file must not be inside each other.");
            return;
        }

        // Check if source directory exists, if not create it.
        if (!Directory.Exists(args[0]))
        {
            try
            {
                Directory.CreateDirectory(args[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine(UsageMessage);
                Console.Error.WriteLine($"Error creating source Directory: {ex.Message}");
                return;
            }
        }

        // Check if replica directory exists, if not create it.
        if (!Directory.Exists(args[1]))
        {
            try
            {
                Directory.CreateDirectory(args[1]);
            }
            catch (Exception ex)
            {
                Console.WriteLine(UsageMessage);
                Console.Error.WriteLine($"Error creating replica Directory: {ex.Message}");
                return;
            }
        }

        using var logger = new Logger(args[2]);
        var synchronizer = new DirectorySynchronizer(args[0], args[1], logger);
        synchronizer.Start(interval);
    }

    // Checks if a file is inside a given directory by comparing full paths.
    public static bool IsFileInsideDirectory(string filePath, string DirectoryPath)
    {
        string fullDirectoryPath = Path.GetFullPath(DirectoryPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string fullFilePath = Path.GetFullPath(filePath);

        return fullFilePath.StartsWith(fullDirectoryPath, StringComparison.OrdinalIgnoreCase);
    }
}