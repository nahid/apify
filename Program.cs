using Apify.Commands;
using Apify.Models;
using Apify.Services;
using NuGet.Versioning;
using System.CommandLine;
using System.Net;
using System.Reflection;

namespace Apify
{
    //TODO: if variable has no dot it consider as environment variable. {{baseUrl}} same as {{env.baseUrl}}
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
            
            
            
            var rootCommand = new RootCommand
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
            rootCommand.AddCommand(new ImportPostmanCommand());

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
            CacheService cacheService = new CacheService();
            VersionCache? cachedVersion = cacheService.GetVersionCache();
            
            if (cachedVersion != null && cachedVersion.LastUpdated > DateTime.UtcNow.AddHours(-2))  
            {
                return NuGetVersion.Parse(cachedVersion.LatestVersion);
            }
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false // or false to disable
            };

            try
            {
                using var client = new HttpClient(handler);
                client.Timeout = TimeSpan.FromSeconds(5); // or whatever you want
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Apify CLI/1.0.0");
                
                var response = await client.GetAsync($"https://github.com/nahid/apify/releases/latest");

                if (response.StatusCode != HttpStatusCode.Found && response.StatusCode != HttpStatusCode.MovedPermanently)
                {
                    throw new Exception($"Unexpected status code: {response.StatusCode}");
                }
            
                var location = response.Headers.Location?.ToString();
            
                if (!string.IsNullOrEmpty(location))
                {
                    var lastSegment = location.Split('/').Last(); // e.g. v1.0.0-rc3
                    var tag = lastSegment.TrimStart('v'); // return 1.0.0-rc3
                    
                    cacheService.UpdateVersion(tag);
                    return NuGetVersion.Parse(tag);
                }
            }
            catch (Exception)
            {
                return null;
            }
           

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
