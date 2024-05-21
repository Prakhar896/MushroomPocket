using System;

namespace Extensions {
    class Logger {
        public static string logFile = "logs.txt";
        public static void Log(string message) {
            FileOps.WriteTo(logFile, $"{DateTime.Now.ToUniversalTime().ToString("dd-MM-yyyy H:mm:ss")} UTC: {message}\n", FileOps.FileWriteMode.Append);
        }

        public static string[] ReadLogs() {
            if (!FileOps.FileExistAt(logFile)) {
                return new string[] {};
            }
            return FileOps.ReadFrom(logFile, FileOps.FileReadMode.ReadAllLines).lines;
        }
    }
}