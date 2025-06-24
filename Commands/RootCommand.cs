using System.CommandLine;
using Apify.Services;
using Apify.Utils;

namespace Apify.Commands
{
    public class RootCommand
    {
        public System.CommandLine.RootCommand Command { get; }
        
        // Added global debug flag that can be accessed by all commands
        public static Option<bool> DebugOption { get; } = new Option<bool>(
            name: "--debug",
            description: "Show detailed debug output and logging information");

        public RootCommand()
        {
            Command = new System.CommandLine.RootCommand
            {
                Description = "Apify - A CLI tool for testing APIs similar to Postman"
            };
            
            // Add the global debug option
            Command.AddGlobalOption(DebugOption);
            
            // Add the list-env command
            var listEnvCommand = new Command("list-env", "List available environments");
            
            listEnvCommand.SetHandler((debug) => 
            {
                ListEnvironments(debug);
            }, DebugOption);
            
            // Add the init command
            var initCommand = new InitCommand();
            
            // Add the run command
            var runCommand = new CallCommand();
            
            // Add the create command
            var createCommand = new CreateRequestCommand();
            
            // Add the tests command
            var testsCommand = new TestsCommand();
            
            // Add the mock server command
            var mockServerCommand = MockServerCommand.CreateCommand();
            
            // Add commands to root command
            Command.AddCommand(listEnvCommand);
            Command.AddCommand(initCommand);
            Command.AddCommand(runCommand.Command);
            Command.AddCommand(createCommand);
            Command.AddCommand(testsCommand.Command);
            Command.AddCommand(mockServerCommand);
        }
        
        private void ListEnvironments(bool debug)
        {
            ConsoleHelper.DisplayTitle("Apify - Available Environments");
            
            var environmentService = new EnvironmentService(debug);
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
            
            ConsoleHelper.WriteSection("Available Configuration:");
            
            ConsoleHelper.WriteLineColored($"Name: {profile.Name}", ConsoleColor.Cyan);
            
            if (!string.IsNullOrEmpty(profile.Description))
            {
                ConsoleHelper.WriteLineColored($"  Description: {profile.Description}", ConsoleColor.DarkGray);
            }
            
            if (!string.IsNullOrEmpty(profile.DefaultEnvironment))
            {
                ConsoleHelper.WriteLineColored($"  Default EnvironmentSchema: {profile.DefaultEnvironment}", ConsoleColor.DarkCyan);
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
        }
    }
}