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
            string[] resources = [
                "Apify.includes.faker.min.js",
                "Apify.includes.assert.js",
                "Apify.includes.app.js"
            ];
            
            
            // Find your resource name here
            var engine = new Engine();
// Add a fake window object to the JS global scope
            engine.Execute("var window = this;");
            engine.SetValue("Config", _config.LoadConfiguration());
            engine.Execute(@"function getName(name) {
                return 'Your Name: ' + name;
            }");


            // Load the JS code from embedded resource
            var assembly = Assembly.GetExecutingAssembly();
     

            foreach (var resource in resources)
            {
                using (Stream stream = assembly.GetManifestResourceStream(resource))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string js = reader.ReadToEnd();
                
                    engine.Execute(js);
                }
            }
            
            
            var result = engine.Evaluate("apify.assert.equals(3, 3)").ToObject();
              
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