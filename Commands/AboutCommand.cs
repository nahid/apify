using System.CommandLine;
using System.Reflection;
using Apify.Services;
using Apify.Utils;


namespace Apify.Commands
{
    public class AboutCommand : Command
    {
        private ConfigService _config;

        public AboutCommand() : base("about", "Getting application information")
        
        {
            var forceOption = new Option<bool>(
                "--force",
                () => false,
                "Force overwrite of existing files"
            );

            AddOption(forceOption);
            
            _config = new ConfigService();

            this.SetHandler(
                (_, _) => ExecuteAsync(),
                forceOption, RootOption.DebugOption
            );
        }

        private Task ExecuteAsync()
        {
            ConsoleHelper.WriteHeader("About Apify");
            Console.WriteLine("A robust and powerful CLI tool for testing APIs and a mock server.");
            Console.WriteLine();

            var version = Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "N/A";

            ConsoleHelper.WriteKeyValue("Version", version);
            ConsoleHelper.WriteKeyValue("Website", "https://apifyapp.com");
            ConsoleHelper.WriteKeyValue("Author", "Nahid Bin Azhar");
            ConsoleHelper.WriteKeyValue("Author URL", "https://nahid.im");
            
        
            return Task.CompletedTask;
        }

    }
}