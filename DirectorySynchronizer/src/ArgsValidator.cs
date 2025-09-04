namespace DirectorySynchronizer.src
{
    // Checks command-line arguments, validates paths.
    public class ArgsValidator
    {
        public static void ValidateArgs(string[] args)
        {
            if (args.Length != 4)
            {
                throw new ArgumentException("Invalid number of arguments.");
            }

            var sourcePath = args[0];
            var replicaPath = args[1];
            var logPath = args[2];

            if (!int.TryParse(args[3], out int interval) || interval <= 0)
            {
                throw new ArgumentException("Interval must be a positive integer.");
            }

            FileSystemValidator.ValidatePaths(sourcePath, replicaPath, logPath);
        }
    }

    // Validates the file system paths for source, replica, and log.
    // Ensures directories exist(creates them if not) and are accessible, and that paths do not overlap.
    public class FileSystemValidator
    {
        public static void ValidatePaths(string sourcePath, string replicaPath, string logPath)
        {
            // If any of the paths are inside each other, it could lead to unexpected behavior.
            if (IsFileInsideDirectory(sourcePath, replicaPath) || IsFileInsideDirectory(replicaPath, sourcePath)
                || IsFileInsideDirectory(sourcePath, logPath) || IsFileInsideDirectory(logPath, sourcePath)
                || IsFileInsideDirectory(replicaPath, logPath) || IsFileInsideDirectory(logPath, replicaPath))
            {
                throw new ArgumentException("Source, replica, and log paths must be independent of each other.");
            }

            // Check if source directory exists, if not create it.
            if (!Directory.Exists(sourcePath))
            {
                try
                {
                    Directory.CreateDirectory(sourcePath);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Error creating source Directory: {ex.Message}");
                }
            }
            else
            {
                // Check if source directory is readable.
                try
                {
                    _ = Directory.EnumerateFiles(sourcePath).FirstOrDefault();
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Source directory is not readable: {ex.Message}");
                }
            }

            // Check if replica directory exists, if not create it.
            if (!Directory.Exists(replicaPath))
            {
                try
                {
                    Directory.CreateDirectory(replicaPath);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Error creating replica Directory: {ex.Message}");
                }
            }
            else
            {

                // Check if source directory is readable.
                try
                {
                    _ = Directory.EnumerateFiles(replicaPath).FirstOrDefault();
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Replica directory is not readable: {ex.Message}");
                }

                // Check if replica directory is writable.
                try
                {
                    var i = 0;
                    var filePath = Path.Combine(replicaPath, $"dirsync.tempfile{i}");
                    // Creating a guaranteed missing file
                    while (File.Exists(filePath))
                    {
                        i++;
                        filePath = Path.Combine(replicaPath, $"dirsync.tempfile{i}");
                    }

                    var stream = File.CreateText(filePath);
                    stream.Write("Test");
                    stream.Flush();
                    stream.Close();
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Replica directory is not writable: {ex.Message}");
                }
            }

            // Check if log file can be created or opened.
            try
            {
                using var logStream = new FileStream(logPath, FileMode.Append, FileAccess.Write);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error accessing log file: {ex.Message}");
            }
        }

        // Checks if a file is inside a given directory by comparing full paths.
        // Also returns true for same paths.
        public static bool IsFileInsideDirectory(string filePath, string DirectoryPath)
        {
            string fullDirectoryPath = Path.GetFullPath(DirectoryPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string fullFilePath = Path.GetFullPath(filePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            return fullFilePath.StartsWith(fullDirectoryPath, StringComparison.OrdinalIgnoreCase);
        }
    }
}