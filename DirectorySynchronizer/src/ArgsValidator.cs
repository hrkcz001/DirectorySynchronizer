// Checks command-line arguments, validates paths.
class ArgsValidator
{
    public static void ValidateArgs(string[] args)
    {
        // Check for correct number of arguments
        if (args.Length != 4)
        {
            throw new ArgumentException("Incorrect number of arguments.");
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
            throw new ArgumentException("Source, replica, and log paths must not be inside each other.");
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
        string fullDirectoryPath = Path.GetFullPath(DirectoryPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string fullFilePath = Path.GetFullPath(filePath);

        return fullFilePath.StartsWith(fullDirectoryPath, StringComparison.OrdinalIgnoreCase);
    }
}