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
            listEnvCommand.SetHandler(() => ListEnvironments());
            rootCommand.AddCommand(listEnvCommand);

            // Add subcommands
            rootCommand.AddCommand(new RunCommand().Command);
            rootCommand.AddCommand(new InitCommand());
            rootCommand.AddCommand(new CreateRequestCommand());

            // Parse and execute
            return await rootCommand.InvokeAsync(args);
        }

        private static void ListEnvironments()
        {
            ConsoleHelper.DisplayTitle("Apify - Available Environments");
            
            var environmentService = new EnvironmentService();
            
            // Load the single profile from the current directory
            var profile = environmentService.LoadConfigurationProfile();
            
            if (profile == null)
            {
                ConsoleHelper.WriteInfo("No environment profile found. Creating default profile...");
                environmentService.CreateDefaultEnvironmentFile();
                profile = environmentService.LoadConfigurationProfile();
                
                if (profile == null)
                {
                    ConsoleHelper.WriteError("Failed to create a default profile.");
                    return;
                }
            }
            
            ConsoleHelper.WriteSection("Available Configuration Profile:");
            
            ConsoleHelper.WriteLineColored($"Profile: {profile.Name}", ConsoleColor.Cyan);
            
            if (!string.IsNullOrEmpty(profile.Description))
            {
                ConsoleHelper.WriteLineColored($"  Description: {profile.Description}", ConsoleColor.DarkGray);
            }
            
            if (!string.IsNullOrEmpty(profile.DefaultEnvironment))
            {
                ConsoleHelper.WriteLineColored($"  Default Environment: {profile.DefaultEnvironment}", ConsoleColor.DarkCyan);
            }
            
            ConsoleHelper.WriteLineColored("  Environments:", ConsoleColor.White);
            
            foreach (var env in profile.Environments)
            {
                ConsoleHelper.WriteLineColored($"    - {env.Name}", ConsoleColor.Green);
                
                if (!string.IsNullOrEmpty(env.Description))
                {
                    ConsoleHelper.WriteLineColored($"      Description: {env.Description}", ConsoleColor.DarkGray);
                }
                
                ConsoleHelper.WriteLineColored($"      Variables: {env.Variables.Count}", ConsoleColor.DarkYellow);
                
                // Display variable names (not values to protect sensitive information)
                if (env.Variables.Count > 0)
                {
                    var variableNames = string.Join(", ", env.Variables.Keys);
                    ConsoleHelper.WriteLineColored($"      Names: {variableNames}", ConsoleColor.DarkGray);
                }
            }
            
            Console.WriteLine(); // Add blank line between profiles
        }
    }
}
