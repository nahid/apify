using System.CommandLine;
using APITester.Services;
using APITester.Utils;

namespace APITester.Commands
{
    public class RootCommand
    {
        public System.CommandLine.RootCommand Command { get; }

        public RootCommand()
        {
            Command = new System.CommandLine.RootCommand
            {
                Description = "API Tester - A CLI tool for testing APIs similar to Postman"
            };
            
            // Add the list-env command
            var listEnvCommand = new Command("list-env", "List available environments");
            
            listEnvCommand.SetHandler(() => 
            {
                ListEnvironments();
            });
            
            // Add the init command
            var initCommand = new InitCommand();
            
            // Add commands to root command
            Command.AddCommand(listEnvCommand);
            Command.AddCommand(initCommand);
        }
        
        private void ListEnvironments()
        {
            ConsoleHelper.DisplayTitle("API Tester - Available Environments");
            
            var environmentService = new EnvironmentService();
            var profiles = environmentService.LoadConfigurationProfiles();
            
            if (profiles.Count == 0)
            {
                ConsoleHelper.WriteInfo("No environment profiles found. Creating default profile...");
                environmentService.CreateDefaultEnvironmentFile();
                profiles = environmentService.LoadConfigurationProfiles();
            }
            
            if (profiles.Count == 0)
            {
                ConsoleHelper.WriteError("No environment profiles available.");
                return;
            }
            
            ConsoleHelper.WriteSection("Available Configuration Profiles:");
            
            foreach (var profile in profiles)
            {
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
}