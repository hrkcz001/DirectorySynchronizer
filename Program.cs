using System;

using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

class Logger : IDisposable
{
    private readonly object lockObj = new object();
    private StreamWriter writer;
    private bool disposed = false;

    public Logger(string logFilePath)
    {
        if (!File.Exists(logFilePath))
        {
            try
            {
                using (File.Create(logFilePath)) { }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating log file: {ex.Message}");
            }
        }
        writer = new StreamWriter(logFilePath, true);
    }

    public void Log(string message)
    {
        lock (lockObj)
        {
            var fmessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            Console.WriteLine(fmessage);
            writer.WriteLine(fmessage);
            writer.Flush();
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

class DirectorySynchronizer
{
    private readonly string sourceDir;
    private readonly string replicaDir;
    private readonly Logger logger;

    public DirectorySynchronizer(string sourceDir, string replicaDir, Logger logger)
    {
        this.sourceDir = sourceDir;
        this.replicaDir = replicaDir;
        this.logger = logger;
    }

    public void Start(int interval)
    {
        InitLogging();
        while (true)
        {
            try
            {
                //SyncDirectories(sourceDir, replicaDir);
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                logger.Log($"Error during synchronization: {ex.Message}");
            }
            System.Threading.Thread.Sleep(interval * 1000);
        }
    }

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

            if (!int.TryParse(args[3], out int interval) || interval <= 0)
            {
                Console.WriteLine(UsageMessage);
                Console.WriteLine("Interval must be a positive integer.");
                return;
            }

            // If any of the paths are inside each other, it could lead to unexpected behavior.
            if (IsFileInsideDirectory(args[0], args[1]) || IsFileInsideDirectory(args[1], args[0])
                || IsFileInsideDirectory(args[0], args[2]) || IsFileInsideDirectory(args[2], args[0])
                || IsFileInsideDirectory(args[1], args[2]) || IsFileInsideDirectory(args[2], args[1]))
            {
                Console.WriteLine(UsageMessage);
                Console.WriteLine("Source, replica Directories and log file must not be inside each other.");
                return;
            }

            if (!Directory.Exists(args[0]))
            {
                try
                {
                    Directory.CreateDirectory(args[0]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(UsageMessage);
                    Console.WriteLine($"Error creating source Directory: {ex.Message}");
                    return;
                }
            }

            if (!Directory.Exists(args[1]))
            {
                try
                {
                    Directory.CreateDirectory(args[1]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(UsageMessage);
                    Console.WriteLine($"Error creating replica Directory: {ex.Message}");
                    return;
                }
            }

            try
            {
                using (var logger = new Logger(args[2]))
                {
                    var synchronizer = new DirectorySynchronizer(args[0], args[1], logger);
                    synchronizer.Start(interval);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(UsageMessage);
                Console.WriteLine($"Error initializing logger: {ex.Message}");
            }

        }

        private static bool IsFileInsideDirectory(string filePath, string DirectoryPath)
        {
            string fullDirectoryPath = Path.GetFullPath(DirectoryPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string fullFilePath = Path.GetFullPath(filePath);

            return fullFilePath.StartsWith(fullDirectoryPath, StringComparison.OrdinalIgnoreCase);
        }
    }