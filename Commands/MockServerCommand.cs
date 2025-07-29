using System.CommandLine;
using Apify.Services;

namespace Apify.Commands
{
    public class MockServerCommand: Command
    {
        public MockServerCommand(): base("server:mock", "Start a mock API server based on mock files in the .apify directory")
        {
            var portOption = new Option<int>(
                name: "--port",
                description: "The port on which to run the mock server (on Windows, ports above 1024 may not require admin rights)",
                getDefaultValue: () => 0);
                
            var projectDirectoryOption = new Option<string>(
                name: "--project",
                description: "The project directory containing mock definition files",
                getDefaultValue: () => "");
                
            var verboseOption = new Option<bool>(
                name: "--verbose",
                description: "Show detailed output",
                getDefaultValue: () => false);

            var watchOption = new Option<bool>(
                name: "--watch",
                description: "Watch for file changes and reload the server automatically");
            watchOption.AddAlias("-w");

            AddOption(portOption);
            AddOption(projectDirectoryOption);
            AddOption(verboseOption);
            AddOption(watchOption);

            this.SetHandler(async (port, projectDirectory, verbose, watch, debug) =>
            {
                await RunMockServerAsync(port, projectDirectory, verbose, watch, debug);
            }, portOption, projectDirectoryOption, verboseOption, watchOption, RootOption.DebugOption);

        }

        private async Task RunMockServerAsync(int port, string projectDirectory, bool verbose, bool watch, bool debug)
        {
            var mockServer = new MockServerService(projectDirectory, debug);
            await mockServer.StartAsync(port, verbose, watch);
        }
    }
}