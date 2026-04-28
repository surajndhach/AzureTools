using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LoadPerformanceTest.Logging
{
    public static class Logger
    {
        private static readonly string _logDirectory;
        private static readonly string _infoFile;
        private static readonly string _warningFile;
        private static readonly string _errorFile;

        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        static Logger()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            string logFolder = config["LogSettings:LogFolder"] ?? "Logs";

            string basePath = Path.Combine(AppContext.BaseDirectory, logFolder);

            string today = DateTime.Now.ToString("yyyy-MM-dd");
            _logDirectory = Path.Combine(basePath, today);

            Directory.CreateDirectory(_logDirectory);

            _infoFile = Path.Combine(_logDirectory, "info.json");
            _warningFile = Path.Combine(_logDirectory, "warning.json");
            _errorFile = Path.Combine(_logDirectory, "error.json");
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
                var entry = new JsonObject
                {
                    ["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ["level"]     = level,
                    ["message"]   = message
                };

                if (exception is not null)
                {
                    var exNode = new JsonObject
                    {
                        ["type"]       = exception.GetType().FullName,
                        ["message"]    = exception.Message,
                        ["stackTrace"] = exception.StackTrace
                    };

                    if (exception.InnerException is not null)
                    {
                        exNode["innerException"] = new JsonObject
                        {
                            ["type"]    = exception.InnerException.GetType().FullName,
                            ["message"] = exception.InnerException.Message
                        };
                    }

                    entry["exception"] = exNode;
                }

                // Read existing array or start a new one
                JsonArray logs = [];

                if (File.Exists(filePath))
                {
                    var existing = File.ReadAllText(filePath);
                    if (!string.IsNullOrWhiteSpace(existing))
                        logs = JsonNode.Parse(existing)?.AsArray() ?? [];
                }

                logs.Add(entry);

                File.WriteAllText(filePath, logs.ToJsonString(_jsonOptions));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logging failed: {ex.Message}");
            }
        }

    }
}
