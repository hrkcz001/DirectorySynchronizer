namespace DirectorySynchronizer.src
{
    // Checks command-line arguments, validates paths.
    public class ArgsValidator
    {
        public static void ValidateArgs(string[] args)
        {
            // Check for correct number of arguments
            if (args.Length != 4)
            {
                throw new ArgumentException("Invalid number of arguments.");
            }

            // Validate interval
            if (!int.TryParse(args[3], out int interval) || interval <= 0)
            {
                throw new ArgumentException("Interval must be a positive integer.");
            }

            // If any of the paths are inside each other, it could lead to unexpected behavior.
            if (IsFileInsideDirectory(args[0], args[1]) || IsFileInsideDirectory(args[1], args[0])
                || IsFileInsideDirectory(args[0], args[2]) || IsFileInsideDirectory(args[2], args[0])
                || IsFileInsideDirectory(args[1], args[2]) || IsFileInsideDirectory(args[2], args[1]))
            {
                throw new ArgumentException("Source, replica, and log paths must be independent of each other.");
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
                    throw new ArgumentException($"Error creating source Directory: {ex.Message}");
                }
            }
            else 
            {
                try
                {
                    _ = Directory.EnumerateFiles(args[0]).FirstOrDefault();
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Source directory is not readable: {ex.Message}");
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
                    throw new ArgumentException($"Error creating replica Directory: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    var file = File.Create(Path.Combine(args[1], "tempfile.tmp"));
                    File.Delete(file.Name);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Replica directory is not writable: {ex.Message}");
                }
            }

            // Check if log file can be created or opened.
            try
            {
                using var logStream = new FileStream(args[2], FileMode.Append, FileAccess.Write);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error accessing log file: {ex.Message}");
            }
        }

        // Checks if a file is inside a given directory by comparing full paths.
        public static bool IsFileInsideDirectory(string filePath, string DirectoryPath)
        {
            string fullDirectoryPath = Path.GetFullPath(DirectoryPath).TrimEnd(Path.DirectorySeparatorChar);
            string fullFilePath = Path.GetFullPath(filePath);

            return fullFilePath.StartsWith(fullDirectoryPath, StringComparison.OrdinalIgnoreCase);
        }
    }
}