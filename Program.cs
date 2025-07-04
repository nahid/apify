using Apify.Commands;
using Apify.Services;
using Apify.Utils;
using System.CommandLine;

namespace Apify
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var rootCommand = new System.CommandLine.RootCommand
            {
                Description = "Apify - A CLI tool for testing APIs"
            };

            // Add the list-env command directly to the root command
            var listEnvCommand = new Command("list-env", "List available environments");
            rootCommand.AddCommand(listEnvCommand);

            // Add subcommands
            rootCommand.AddCommand(new RunCommand().Command);
            rootCommand.AddCommand(new AboutCommand());
            rootCommand.AddCommand(new InitCommand());
            rootCommand.AddCommand(new CreateRequestCommand());
            rootCommand.AddCommand(new TestsCommand().Command);
            rootCommand.AddCommand(MockServerCommand.CreateCommand());

            // Parse and execute
            return await rootCommand.InvokeAsync(args);
        }
    }
}
