using System.CommandLine;
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
            Console.WriteLine("A command-line tool for API testing and creating mock server for API");
            Console.WriteLine();
            
      
            ConsoleHelper.WriteKeyValue("Version", GetType().Assembly.GetName().Version?.ToString() ?? string.Empty);
            ConsoleHelper.WriteKeyValue("Website", "https://apifyapp.com");
            ConsoleHelper.WriteKeyValue("Author", "Nahid Bin Azhar");
            ConsoleHelper.WriteKeyValue("Author URL", "https://nahid.im");
            
        
            return Task.CompletedTask;
        }

    }
}