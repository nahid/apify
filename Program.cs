using Apify.Commands;
using System.CommandLine;

namespace Apify
{
    public class Program
    {
        public async static Task<int> Main(string[] args)
        {
            var rootCommand = new System.CommandLine.RootCommand
            {
                Description = "Apify - A robust and powerful CLI tool for testing APIs and a mock server."
            };
            rootCommand.AddGlobalOption(RootOption.DebugOption);

            // Add the list-env command directly to the root command
            var listEnvCommand = new Command("list-env", "List available environments");
            rootCommand.AddCommand(listEnvCommand);

            // Add subcommands
            rootCommand.AddCommand(new CallCommand());
            rootCommand.AddCommand(new AboutCommand());
            rootCommand.AddCommand(new InitCommand());
            rootCommand.AddCommand(new CreateRequestCommand());
            rootCommand.AddCommand(new CreateMockCommand());
            rootCommand.AddCommand(new TestsCommand());
            rootCommand.AddCommand(new MockServerCommand());

            if (args.Length == 0)
            {
                // Show help if no arguments are provided
                return await rootCommand.InvokeAsync(["--help"]);
            }
            // Parse and execute
            return await rootCommand.InvokeAsync(args);
        }
    }
}
