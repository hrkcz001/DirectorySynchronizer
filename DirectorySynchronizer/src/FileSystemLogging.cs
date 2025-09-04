namespace DirectorySynchronizer.src
{
    public class FileSystemLogging(Logger logger, string sourceDir)
    {
        private readonly Logger logger = logger;
        private readonly FileSystemWatcher watcher = new();
        public bool Running { get; private set; } = false;

        // Directories could be synchronized in the same way,
        // but since synchronization is periodic and it is more reliable to fully check the directories,
        // we use FileSystemWatcher only for logging changes.
        // Additionally, this approach provides more accurate timestamps in the logs.
        public void InitLogging()
        {
            Running = true;

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

        public void Stop()
        {
            if (Running)
            {
                watcher.EnableRaisingEvents = false;
                Running = false;
            }
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {   
            if (!Running) return;
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
            if (!Running) return;
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
    }
}