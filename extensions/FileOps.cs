using System.IO;

namespace Extensions {
    class FileOps {
        public enum FileWriteMode {
            Overwrite,
            Append
        }

        public enum FileReadMode {
            ReadAll,
            ReadAllLines
        }

        #nullable enable
        public static (string? text, string[]? lines) ReadFrom(string path, FileReadMode mode = FileReadMode.ReadAll) {
            if (mode == FileReadMode.ReadAll) {
                return (text: File.ReadAllText(path), lines: null);
            } else if (mode == FileReadMode.ReadAllLines) {
                return (text: null, lines: File.ReadAllLines(path));
            } else {
                return (text: null, lines: null);
            }
        }
        #nullable disable

        public static bool FileExistAt(string path) {
            return File.Exists(path);
        }

        public static bool CreateFileAt(string path) {
            if (FileExistAt(path)) {
                return true;
            }

            File.Create(path).Close();
            return true;
        }

        public static void WriteTo(string path, string content, FileWriteMode mode = FileWriteMode.Overwrite) {
            if (mode == FileWriteMode.Overwrite) {
                File.WriteAllText(path, content);
            } else if (mode == FileWriteMode.Append) {
                File.AppendAllText(path, content);
            }
        }

        public static void DeleteTempDBFiles() {
            if (File.Exists("database.db-wal")) {
                    File.Delete("database.db-wal");
            }
            if (File.Exists("database.db-shm")) {
                File.Delete("database.db-shm");
            }
        }
    }
}