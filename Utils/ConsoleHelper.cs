using ConsoleTableExt;
using System.Text;

namespace Apify.Utils
{
    public static class ConsoleHelper
    {
        // Colors for different types of content
        private readonly static ConsoleColor HeaderColor = ConsoleColor.Magenta;
        private readonly static ConsoleColor KeyColor = ConsoleColor.DarkCyan;
        private readonly static ConsoleColor ValueColor = ConsoleColor.White;
        private readonly static ConsoleColor JsonKeyColor = ConsoleColor.DarkYellow;
        private readonly static ConsoleColor JsonValueColor = ConsoleColor.DarkGreen;
        private readonly static ConsoleColor JsonPunctuationColor = ConsoleColor.DarkGray;
        private readonly static ConsoleColor UrlColor = ConsoleColor.Blue;
        private readonly static ConsoleColor MethodColor = ConsoleColor.Magenta;
        private readonly static ConsoleColor TimingColor = ConsoleColor.DarkMagenta;
        private readonly static ConsoleColor StatusSuccessColor = ConsoleColor.Green;
        private readonly static ConsoleColor StatusWarningColor = ConsoleColor.Yellow;
        private readonly static ConsoleColor StatusErrorColor = ConsoleColor.Red;
        private readonly static ConsoleColor SectionColor = ConsoleColor.Cyan;
        private readonly static ConsoleColor TipColor = ConsoleColor.DarkCyan;
        private readonly static ConsoleColor PromptColor = ConsoleColor.Yellow;
        
        public static void WriteError(string message, bool strong = false)
        {
            Console.ForegroundColor = StatusErrorColor;
            if (strong)
            {
                Console.Write("\x1b[1m");
            }
            Console.WriteLine(message);
            if (strong)
            {
                Console.Write("\x1b[0m");
            }
            Console.ResetColor();
        }

        public static void WriteSuccess(string message, bool strong = false)
        {
            if (strong)
            {
                Console.Write("\x1b[1m");
            }
            Console.ForegroundColor = StatusSuccessColor;
            Console.WriteLine(message);
            if (strong)
            {
                Console.Write("\x1b[0m");
            }
            Console.ResetColor();
        }

        public static void WriteWarning(string message, bool strong = false)
        {
            if (strong)
            {
                Console.Write("\x1b[1m");
            }
            Console.ForegroundColor = StatusWarningColor;
            Console.WriteLine(message);
            if (strong)
            {
                Console.Write("\x1b[0m");
            }
            Console.ResetColor();
        }

        public static void WriteInfo(string message, bool strong = false)
        {
            if (strong)
            {
                Console.Write("\x1b[1m");
            }
            
            Console.ForegroundColor = SectionColor;
            Console.WriteLine(message);
            
            if (strong)
            {
                Console.Write("\x1b[0m");
            }
            
            Console.ResetColor();
        }
        
        public static void WriteDebug(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"[DEBUG] {message}");
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
            Console.ResetColor();
            Console.ForegroundColor = ValueColor;
            Console.WriteLine(value);
            Console.ResetColor();
        }
        
        
        public static void WriteRepeatChar(char ch = '-', int repeats = 100, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(new string(ch, repeats));
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
        
        public static void WriteFeatures(string key, object value)
        {
            Console.Write($"{key}: ");
            Console.ForegroundColor = JsonKeyColor;
            Console.WriteLine(value.ToString());
            Console.ResetColor();
        }

        public static void WriteStatusCode(int statusCode)
        {
            Console.Write("Status Code: ");
            
            if (statusCode is >= 200 and < 300)
            {
                Console.ForegroundColor = StatusSuccessColor;
            }
            else if (statusCode is >= 300 and < 400)
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
        
        public static T PromptInput<T>(string message, T? defaultValue = default, bool required = false)
        {
            while (true)
            {
                string prompt = defaultValue is null || defaultValue.Equals(default(T))
                ? $"{message}: "
                : $"{message} [{defaultValue}]: ";

                Console.Write(prompt);
                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    if (!required && defaultValue is not null)
                        return defaultValue;

                    if (required)
                    {
                        Console.WriteLine("Input is required. Please try again.");
                        continue;
                    }

                    return default!;
                }

                try
                {
                    return (T)Convert.ChangeType(input, typeof(T));
                }
                catch
                {
                    Console.WriteLine($"Invalid input. Please enter a value of type {typeof(T).Name}.");
                }
            }
        }
        public static string PromptMultiLineInput(string message, string? defaultValue = null)
        {
            Console.WriteLine(message);
            Console.WriteLine("(Press Enter on an empty line to finish input)");
            StringBuilder sb = new StringBuilder();

            while (true)
            {
                string? line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (sb.Length == 0 && defaultValue != null)
                    {
                        return defaultValue;
                    }
                    break;
                }
                sb.AppendLine(line);
            }

            return sb.ToString().TrimEnd();
        }
        
        public static T PromptChoice<T>(string question, IEnumerable<T> options)
        {
            var optionList = options.ToList();

            if (!optionList.Any())
                throw new ArgumentException("Option list cannot be empty.");

            Console.WriteLine(question);
            for (int i = 0; i < optionList.Count; i++)
            {
                Console.WriteLine($"  {i + 1}) {optionList[i]}");
            }

            while (true)
            {
                Console.Write("Enter choice number: ");
                string? input = Console.ReadLine();

                if (int.TryParse(input, out int choice) &&
                    choice >= 1 && choice <= optionList.Count)
                {
                    return optionList[choice - 1];
                }

                Console.WriteLine("Invalid selection. Please try again.");
            }
        }
        
        public static int PromptChoiceWithIndex<T>(string question, IEnumerable<T> options)
        {
            var optionList = options.ToList();

            if (!optionList.Any())
                throw new ArgumentException("Option list cannot be empty.");

            Console.WriteLine(question);
            for (int i = 0; i < optionList.Count; i++)
            {
                Console.WriteLine($"  {i + 1}) {optionList[i]}");
            }

            while (true)
            {
                Console.Write("Enter choice number: ");
                string? input = Console.ReadLine();

                if (int.TryParse(input, out int choice) &&
                    choice >= 1 && choice <= optionList.Count)
                {
                    return choice - 1; // Return 0-based index
                }

                Console.WriteLine("Invalid selection. Please try again.");
            }
        }



        public static bool PromptYesNo(string question, bool? defaultValue = null)
        {
            string options = defaultValue == true ? "Y/n"
            : defaultValue == false ? "y/N"
            : "y/n";

            while (true)
            {
                Console.Write($"{question} [{options}]: ");
                string? input = Console.ReadLine()?.Trim().ToLower();

                if (string.IsNullOrEmpty(input))
                {
                    if (defaultValue.HasValue)
                        return defaultValue.Value;

                    Console.WriteLine("Please enter 'y' or 'n'.");
                    continue;
                }

                if (input == "y" || input == "yes")
                    return true;

                if (input == "n" || input == "no")
                    return false;

                Console.WriteLine("Invalid input. Please enter 'y' or 'n'.");
            }
        }

        public static void WritePrompt(string prompt)
        {
            Console.ForegroundColor = PromptColor;
            Console.Write(prompt);
            Console.ResetColor();
        }

        /// <summary>
        /// Displays a tip to the user with prominent formatting
        /// </summary>
        private static void WriteTip(string tipText)
        {
            Console.WriteLine();
            Console.ForegroundColor = TipColor;
            Console.WriteLine($"💡 Tip: {tipText}");
            Console.ResetColor();
            Console.WriteLine();
        }

        /// <summary>
        /// Displays the quick start guide for users after project initialization
        /// </summary>
        public static void DisplayQuickStartGuide(string configFilePath, string apiDirectoryName, bool isCompiledExecutable)
        {
            Console.WriteLine();
            WriteHeader("🚀 Quick Start Guide");
            Console.WriteLine();
            
            string exeName = Path.GetFileName(Environment.ProcessPath ?? "apify");
            string exeCommand = isCompiledExecutable ? $"./{exeName}" : "apify";
            
            WriteSuccess("Your API testing project is ready to use!");
            Console.WriteLine();
            
            WriteInfo("Project Structure:");
            WriteKeyValue("  Configuration", configFilePath);
            WriteKeyValue("  API Definitions & Mock Definitions", apiDirectoryName);
            WriteKeyValue("  Mock Definitions Extension", "*.mock.json");
            Console.WriteLine();
            
            WriteInfo("Try these commands:");
            WriteKeyValue($"  {exeCommand} call users.get", "Run the command to get users from the API");
            WriteKeyValue($"  {exeCommand} call users.", "Run the command to create new user");
            WriteKeyValue($"  {exeCommand} list-env", "List all configured environments");
            WriteKeyValue($"  {exeCommand} create:request users.get", "Create a new API request file");
            WriteKeyValue($"  {exeCommand} server:mock --port 1988 --verbose", "Start the mock API server");
            WriteKeyValue($"  {exeCommand} tests", "Run all tests with progress indicators");
            WriteWarning("We set https://reqres.in/api as the default API endpoint for testing purposes.");
            Console.WriteLine();
            
            WriteTip("You can use shortened paths like 'users.all' instead of '.apify/users/all.json'");
            
            WriteInfo("Next Steps:");
            Console.WriteLine("1. Explore the sample API tests in the .apify directory");
            Console.WriteLine("2. Create your own API tests using the 'create:request' command");
            Console.WriteLine("3. Configure additional environment variables in apify-config.json");
            Console.WriteLine("4. Run your API tests using the 'tests' command");
            Console.WriteLine();
            
            // Add the mock server section
            WriteInfo("Mock Server:");
            Console.WriteLine("- Create .mock.json files in subdirectories of .apify to define mock endpoints");
            Console.WriteLine("- Mock server configuration is in the MockServer section of apify-config.json");
            Console.WriteLine("- Start the mock server with 'server:mock' command for offline development");
            Console.WriteLine("- Use Fake Data like {{Faker.Name.FirstName()}} or {{ expr|> Faker.Name.FirstName()}} in mock responses");
            Console.WriteLine("- Define route parameters with {param} syntax (e.g., /users/{id})");
            Console.WriteLine();
            
            WriteTip("Check the https://apifyapp.com/docs site for detailed usage instructions");
            Console.WriteLine();
        }

        /// <summary>
        /// Prompts the user with a highlighted message and returns their input.
        /// Optionally adds a default value hint.
        /// </summary>
        public static string PromptInput(string message, string? defaultValue = null)
        {
            WritePrompt(defaultValue != null ? $"{message} [{defaultValue}]: " : $"{message}: ");

            string? input = Console.ReadLine();
            
            // Return the default value if input is empty and there is a default
            if (string.IsNullOrWhiteSpace(input) && defaultValue != null)
            {
                return defaultValue;
            }
            
            return input ?? string.Empty;
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
                if (content == "{" || content == "}" || content == "[" || content == "]" || content == ",")
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
            ConsoleTableBuilder
                .From(tableData)
                .WithTitle(tableName)
                .WithFormat(ConsoleTableBuilderFormat.Alternative)
                .ExportAndWriteLine();

        }
    }
}
