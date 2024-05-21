using System;

namespace Extensions {
    class Misc {
        public static T? TryParse<T>(string input) where T : struct {
            try {
                return (T)Convert.ChangeType(input, typeof(T));
            } catch {
                return null;
            }
        }

        public static string Input(string message) {
            Console.Write(message);
            string input = Console.ReadLine();
            return input;
        }

        public static string SafeInputWithPredicate(string message, Func<string, bool> predicate, string errMessage="Invalid input. Please enter a valid input.") {
            Console.Write(message);
            string input = Console.ReadLine();
            while (!predicate(input)) {
                Console.WriteLine(errMessage);
                Console.Write(message);
                input = Console.ReadLine();
            }
            return input;
        }

        public static T SafeInputAndParse<T>(string message, string errMessage="Invalid input. Please enter a valid input.") where T : struct {
            Console.Write(message);
            string input = Console.ReadLine();
            T? result = TryParse<T>(input);
            while (result == null) {
                Console.WriteLine(errMessage);
                Console.Write(message);
                input = Console.ReadLine();
                result = TryParse<T>(input);
            }
            return result.Value;
        }

        public static void ClearConsole() {
            Console.Clear();
        }
    }
}