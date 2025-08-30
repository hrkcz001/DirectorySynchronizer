namespace DirectorySynchronizer.src
{
    // Logger class for logging messages to both console and a log file with thread safety.
    public class Logger : IDisposable
    {
        private readonly Lock lockObj = new();
        private StreamWriter writer;
        private readonly string logFilePath;
        private bool disposed = false;

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
                    if (!File.Exists(logFilePath))
                    {
                        Console.Error.WriteLine($"Error writing to log file: {ex.Message}");
                        Console.WriteLine("Trying to recreate log file...");
                        try
                        {
                            writer?.Dispose();
                            writer = new StreamWriter(logFilePath, true);
                        }
                        catch (Exception ex2)
                        {
                            Console.Error.WriteLine($"Error recreating log file: {ex2.Message}");
                            throw;
                        }
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
}