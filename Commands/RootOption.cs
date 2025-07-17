//TODO: Need to remove this file
using System.CommandLine;

namespace Apify.Commands
{
    public static class RootOption
    {
        
        // Added a global debug flag that can be accessed by all commands
        public static Option<bool> DebugOption { get; } = new Option<bool>(
            name: "--debug",
            description: "Show detailed debug output and logging information");
        
        
        public const string DefaultApiDirectory = ".apify";
        public const string DefaultConfigFileName = "apify-config.json";
        
    }
}