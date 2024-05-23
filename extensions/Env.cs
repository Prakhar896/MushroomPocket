using System;
using System.Collections.Generic;

namespace Extensions {
    class Env {
        public static string envFileName = ".env.txt";
        public static Dictionary<string, string> loadedEnv;
        public static void Load() {
            loadedEnv = new Dictionary<string, string>();
            string[] lines = FileOps.ReadFrom(envFileName, FileOps.FileReadMode.ReadAllLines).lines;
            if (lines == null) {
                throw new Exception($"Failed to load environment variables from environment file {envFileName}");
            }

            foreach (string line in lines) {
                string[] parts = line.Split("=");
                if (parts.Length == 2) {
                    loadedEnv.Add(parts[0], parts[1]);
                } else {
                    throw new Exception($"Invalid environment variable line: {line}");
                }
            }
        }
    }
}