// Main program file for the Directory Synchronizer application.
// Checks command-line arguments, validates paths, and starts the synchronization process.
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