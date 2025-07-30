
using System.CommandLine;
using Apify.Services;
using Apify.Utils;

namespace Apify.Commands
{
    public class ListEnvsCommand : Command
    {
        private readonly ConfigService _configService;

        public ListEnvsCommand() : base("list:env", "Show a list of available environments")
        {
            _configService = new ConfigService();

            var envNameOption = new Option<string?>(
                name: "--env",
                description: "The name of the environment to show details for.");
            AddOption(envNameOption);

            this.SetHandler(async (envName) =>
            {
                await Handle(envName);
            }, envNameOption);
        }

        private Task Handle(string? envName)
        {
            try
            {
                var config = _configService.LoadConfiguration();

                ConsoleHelper.WriteColored($"Environment Variables({envName ?? config.DefaultEnvironment ?? "Development"})",
                    ConsoleColor.DarkYellow);
                Console.WriteLine();

                foreach (var variable in _configService.GetEnvironmentVariables(envName))
                {
                    ConsoleHelper.WriteKeyValue($"  {variable.Key}", variable.Value);
                }
                   
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return Task.CompletedTask;
        }
    }
}
