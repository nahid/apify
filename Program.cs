using Apify.Commands;
using Apify.Services;
using NuGet.Versioning;
using System.CommandLine;
using System.Reflection;
using System.Text.Json;

namespace Apify
{
    public class Program
    {
        public async static Task<int> Main(string[] args)
        {
            var version = Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "1.0.0";
            version = version.TrimStart('v');
            var currentVersion = NuGetVersion.Parse(version);
            await CheckForUpdate(currentVersion);
            
            
            
            var rootCommand = new System.CommandLine.RootCommand
            {
                Description = "Apify - A robust and powerful CLI tool for testing APIs and a mock server."
            };
            rootCommand.AddGlobalOption(RootOption.DebugOption);

            // Add subcommands
            rootCommand.AddCommand(new CallCommand());
            rootCommand.AddCommand(new AboutCommand());
            rootCommand.AddCommand(new InitCommand());
            rootCommand.AddCommand(new CreateRequestCommand());
            rootCommand.AddCommand(new CreateMockCommand());
            rootCommand.AddCommand(new TestsCommand());
            rootCommand.AddCommand(new MockServerCommand());
            rootCommand.AddCommand(new ListEnvsCommand());

            if (args.Length == 0)
            {
                // Show help if no arguments are provided
                return await rootCommand.InvokeAsync(["--help"]);
            }
            // Parse and execute
            return await rootCommand.InvokeAsync(args);
        }
        
        public async static Task<NuGetVersion?> GetLatestGitHubVersion()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Apify CLI/1.0.0");

            var url = $"https://api.github.com/repos/nahid/apify/releases/latest";
            var json = await client.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            var tag = doc.RootElement.GetProperty("tag_name").GetString();
            tag = tag?.TrimStart('v');

            if (tag != null)
                return NuGetVersion.Parse(tag);
            
            return null;
        }
        
        
        public async static Task CheckForUpdate(NuGetVersion currentVersion)
        {
            var latestVersion = await GetLatestGitHubVersion();

            if (latestVersion != null && latestVersion > currentVersion)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("============================================================");
                Console.WriteLine($"🚀 New version available: {latestVersion}");
                Console.WriteLine("👉 Visit: https://github.com/nahid/apify/releases/latest");
                Console.WriteLine("============================================================\n");
                Console.ResetColor();
            }
        }

    }
    

}
