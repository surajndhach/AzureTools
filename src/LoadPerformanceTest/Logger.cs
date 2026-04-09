using Microsoft.Extensions.Configuration;
using System.Runtime.CompilerServices;

namespace LoadPerformanceTest
{
    public static class Logger
    {
        private static readonly string _logDirectory;
        private static readonly string _infoFile;
        private static readonly string _warningFile;
        private static readonly string _errorFile;

        static Logger()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            string logFolder = config["LogSettings:LogFolder"] ?? "Logs";

            string projectDir = GetSourceDirectory();

            string basePath = Path.Combine(projectDir, logFolder);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
            _logDirectory = Path.Combine(basePath, timestamp);

            Directory.CreateDirectory(_logDirectory);

            _infoFile = Path.Combine(_logDirectory, "info.log");
            _warningFile = Path.Combine(_logDirectory, "warning.log");
            _errorFile = Path.Combine(_logDirectory, "error.log");
        }

        public static void LogInfo(string message)
        {
            WriteLog(_infoFile, "INFO", message);
        }

        public static void LogWarning(string message, Exception? exception = null)
        {
            WriteLog(_warningFile, "WARNING", message, exception);
        }

        public static void LogError(string message, Exception? exception = null)
        {
            WriteLog(_errorFile, "ERROR", message, exception);
        }

        private static void WriteLog(string filePath, string level, string message, Exception? exception = null)
        {
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";

                if (exception is not null)
                {
                    logEntry += Environment.NewLine +
                                $"  Exception: {exception.GetType().FullName}: {exception.Message}" +
                                Environment.NewLine +
                                $"  StackTrace: {exception.StackTrace}";

                    if (exception.InnerException is not null)
                    {
                        logEntry += Environment.NewLine +
                                    $"  InnerException: {exception.InnerException.GetType().FullName}: {exception.InnerException.Message}";
                    }
                }

                File.AppendAllText(filePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logging failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns the directory of this source file, embedded at compile time via [CallerFilePath].
        /// This resolves to the project source folder regardless of where the binary runs from.
        /// </summary>
        private static string GetSourceDirectory([CallerFilePath] string sourceFilePath = "")
        {
            return Path.GetDirectoryName(sourceFilePath)!;
        }
    }
}
