using APITester.Commands;
using System.CommandLine;

namespace APITester
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var rootCommand = new System.CommandLine.RootCommand
            {
                Description = "API Tester - A CLI tool for testing APIs"
            };

            // Add subcommands
            rootCommand.AddCommand(new RunCommand().Command);

            // Parse and execute
            return await rootCommand.InvokeAsync(args);
        }
    }
}
