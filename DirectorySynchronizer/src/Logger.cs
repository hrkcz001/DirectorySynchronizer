namespace DirectorySynchronizer.src
{
    // Logger class for logging messages to both console and a log file with thread safety.
    public class Logger(TextWriter[] textWriters)
    {
        private readonly Lock lockObj = new();
        private readonly TextWriter[] textWriters = textWriters;


        public void Log(string message)
        {
            lock (lockObj)
            {
                try
                {
                    var fmessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                    foreach (var writer in textWriters)
                    {
                        writer.WriteLine(fmessage);
                        writer.Flush();
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        foreach (var writer in textWriters)
                        {
                            writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Logging error: {ex.Message}");
                            writer.Flush();
                        }
                    }
                    catch { }
                    throw;
                }
            }
        }
    }
}