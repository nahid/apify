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
        private const string DefaultConfigFileName = "apify-config.json";
        private const string DefaultApiDirectoryName = ".apify";
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
                (force) => ExecuteAsync(force),
                forceOption
            );
        }

        private async Task ExecuteAsync(bool force)
        {
            ConsoleHelper.WriteHeader("About API Testing Project");


            var result = DynamicExpression.Execute("Faker.Email()");
            // var result = funcs["RandomString()"]() as string;
            ConsoleHelper.WriteSection($"Result: {result}: {result?.Length}");
            

            var config = _env.LoadConfiguration();
            var env = _env.LoadEnvironment("Development");
            /*var faker = new Faker();
            var name = faker.Internet.Email()*/
            Console.WriteLine("Configuration:");
        }

    }
}