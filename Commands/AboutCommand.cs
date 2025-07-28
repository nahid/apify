using System.CommandLine;
using System.Reflection;
using Apify.Services;
using Apify.Utils;
using Jint;


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
           
            
            
            // Find your resource name here
            var expresso = new DynamicExpressionManager();
            
// Add a fake window object to the JS global scope
           
            
            
            var result = expresso.Compile<int>("faker.number.int({ min: 1, max: 100 })");
              
            Console.WriteLine(result.ToString());
            
     
            
            //Console.WriteLine(engine.Evaluate("Config.MockServer").ToString());

            return Task.CompletedTask;
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