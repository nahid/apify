using System.CommandLine;
using Apify.Services;

namespace Apify.Commands
{
    public class MockServerCommand
    {
        public static Command CreateCommand()
        {
            var command = new Command("mock-server", "Start a mock API server based on mock files in the .apify directory");
            
            var portOption = new Option<int>(
                name: "--port",
                description: "The port on which to run the mock server",
                getDefaultValue: () => 5000);
                
            var directoryOption = new Option<string>(
                name: "--directory",
                description: "The directory containing mock definition files",
                getDefaultValue: () => ".apify");
                
            var verboseOption = new Option<bool>(
                name: "--verbose",
                description: "Show detailed output",
                getDefaultValue: () => false);
            
            command.AddOption(portOption);
            command.AddOption(directoryOption);
            command.AddOption(verboseOption);
            
            command.SetHandler(async (port, directory, verbose) =>
            {
                await RunMockServerAsync(port, directory, verbose);
            }, portOption, directoryOption, verboseOption);
            
            return command;
        }
        
        private static async Task RunMockServerAsync(int port, string directory, bool verbose)
        {
            var mockServer = new MockServerService(directory);
            await mockServer.StartAsync(port, verbose);
        }
    }
}