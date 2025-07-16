using System.CommandLine;
using System.Text.Json;
using Apify.Models;
using Apify.Services;
using Apify.Utils;
using Bogus;
using Newtonsoft.Json;

namespace Apify.Commands
{
    public class AboutCommand : Command
    {
        private ConfigService _env;

        public AboutCommand() : base("about", "Getting application information")
        
        {
            var forceOption = new Option<bool>(
                "--force",
                () => false,
                "Force overwrite of existing files"
            );

            AddOption(forceOption);
            
            _env = new ConfigService();

            this.SetHandler(
                (force, debug) => ExecuteAsync(force, debug),
                forceOption, RootOption.DebugOption
            );
        }

        private async Task ExecuteAsync(bool force, bool debug)
        {
            ConsoleHelper.WriteHeader("About API Testing Project");
            

            var config = _env.LoadConfiguration();
            var env = _env.LoadEnvironment("Development");
            /*var faker = new Faker();
            var name = faker.Internet.Email()*/
            Console.WriteLine("Configuration:");
        }

    }
}