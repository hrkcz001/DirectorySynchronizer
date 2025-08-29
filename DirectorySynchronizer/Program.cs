using DirectorySynchronizer.src;

namespace DirectorySynchronizer
{
    // Main program file for the Directory Synchronizer application.
    // It validates command-line arguments, initializes logging, and starts the synchronization process.
    class Program
    {
        const string UsageMessage = "Usage: <source> <replica> <log> <interval>";

        static void Main(string[] args)
        {
            try
            {
                ArgsValidator.ValidateArgs(args);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(UsageMessage);
                Console.Error.WriteLine(ex.Message);
                return;
            }

            using var logger = new Logger(args[2]);
            var synchronizer = new Synchronizer(args[0], args[1], logger);

            try
            {
                synchronizer.Start(int.Parse(args[3]));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }
    }
}