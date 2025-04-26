using ConsoleTableExt;
using System.Text;

namespace APITester.Utils
{
    public static class ConsoleHelper
    {
        // Colors for different types of content
        private static readonly ConsoleColor HeaderColor = ConsoleColor.Magenta;
        private static readonly ConsoleColor KeyColor = ConsoleColor.DarkCyan;
        private static readonly ConsoleColor ValueColor = ConsoleColor.White;
        private static readonly ConsoleColor JsonKeyColor = ConsoleColor.DarkYellow;
        private static readonly ConsoleColor JsonValueColor = ConsoleColor.DarkGreen;
        private static readonly ConsoleColor JsonPunctuationColor = ConsoleColor.DarkGray;
        private static readonly ConsoleColor UrlColor = ConsoleColor.Blue;
        private static readonly ConsoleColor MethodColor = ConsoleColor.Magenta;
        private static readonly ConsoleColor TimingColor = ConsoleColor.DarkMagenta;
        private static readonly ConsoleColor StatusSuccessColor = ConsoleColor.Green;
        private static readonly ConsoleColor StatusWarningColor = ConsoleColor.Yellow;
        private static readonly ConsoleColor StatusErrorColor = ConsoleColor.Red;
        private static readonly ConsoleColor SectionColor = ConsoleColor.Cyan;
        
        public static void WriteError(string message)
        {
            Console.ForegroundColor = StatusErrorColor;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void WriteSuccess(string message)
        {
            Console.ForegroundColor = StatusSuccessColor;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void WriteWarning(string message)
        {
            Console.ForegroundColor = StatusWarningColor;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void WriteInfo(string message)
        {
            Console.ForegroundColor = SectionColor;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void WriteHeader(string header)
        {
            Console.ForegroundColor = HeaderColor;
            Console.WriteLine(header);
            Console.ResetColor();
        }

        public static void WriteKeyValue(string key, string value)
        {
            Console.ForegroundColor = KeyColor;
            Console.Write($"{key}: ");
            Console.ForegroundColor = ValueColor;
            Console.WriteLine(value);
            Console.ResetColor();
        }

        public static void WriteSection(string section)
        {
            Console.WriteLine();
            Console.ForegroundColor = SectionColor;
            Console.WriteLine(section);
            Console.ResetColor();
        }

        public static void WriteUrl(string url)
        {
            Console.ForegroundColor = UrlColor;
            Console.WriteLine(url);
            Console.ResetColor();
        }

        public static void WriteMethod(string method)
        {
            Console.ForegroundColor = MethodColor;
            Console.WriteLine(method);
            Console.ResetColor();
        }

        public static void WriteTiming(long milliseconds)
        {
            Console.Write("Response Time: ");
            Console.ForegroundColor = TimingColor;
            Console.WriteLine($"{milliseconds}ms");
            Console.ResetColor();
        }

        public static void WriteStatusCode(int statusCode)
        {
            Console.Write("Status Code: ");
            
            if (statusCode >= 200 && statusCode < 300)
            {
                Console.ForegroundColor = StatusSuccessColor;
            }
            else if (statusCode >= 300 && statusCode < 400)
            {
                Console.ForegroundColor = StatusWarningColor;
            }
            else
            {
                Console.ForegroundColor = StatusErrorColor;
            }
            
            Console.WriteLine(statusCode);
            Console.ResetColor();
        }

        public static void WriteColored(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(message);
            Console.ResetColor();
        }

        public static void WriteLineColored(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        // Format JSON with colorization
        public static void WriteColoredJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine("(empty)");
                return;
            }

            // If it's not valid JSON, just print it normally
            if (!JsonHelper.IsValidJson(json))
            {
                Console.WriteLine(json);
                return;
            }

            string formattedJson = JsonHelper.FormatJson(json);
            string[] lines = formattedJson.Split('\n');

            foreach (string line in lines)
            {
                string trimmedLine = line.TrimEnd();
                
                // Don't process empty lines
                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    Console.WriteLine();
                    continue;
                }

                // This is a simple colorizer that doesn't handle all JSON cases perfectly
                // but works well enough for basic formatted JSON
                
                string indent = new string(' ', trimmedLine.Length - trimmedLine.TrimStart().Length);
                Console.Write(indent);

                string content = trimmedLine.TrimStart();
                
                // Handle braces and brackets
                if (content == "{" || content == "}" || content == "[" || content == "]" || content == "," || content.EndsWith(","))
                {
                    WriteLineColored(content, JsonPunctuationColor);
                    continue;
                }

                // Handle key-value pairs
                if (content.Contains(":"))
                {
                    int colonIndex = content.IndexOf(':');
                    string key = content.Substring(0, colonIndex + 1);
                    string value = content.Substring(colonIndex + 1);

                    // Handle the key (with quotes and colon)
                    WriteColored(key, JsonKeyColor);

                    // Handle the value 
                    WriteLineColored(value, JsonValueColor);
                }
                else
                {
                    // Just a value (like in an array)
                    Console.WriteLine(content);
                }
            }
        }

        public static void DisplayTitle(string title)
        {
            Console.WriteLine();
            WriteHeader(title);
            WriteHeader(new string('=', title.Length));
            Console.WriteLine();
        }

        public static void DisplayTable<T>(List<T> tableData, string tableName = "") where T : class
        {
            StringBuilder sb = new StringBuilder();
            ConsoleTableBuilder
                .From(tableData)
                .WithTitle(tableName)
                .WithFormat(ConsoleTableBuilderFormat.Alternative)
                .ExportAndWriteLine();
            
            // The ExportAndWriteLine method already writes to the console
        }
    }
}
