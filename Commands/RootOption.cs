//TODO: Need to remove this file
using System.CommandLine;
using Apify.Services;
using Apify.Utils;

namespace Apify.Commands
{
    public sealed class RootOption
    {
        
        // Added global debug flag that can be accessed by all commands
        public static Option<bool> DebugOption { get; } = new Option<bool>(
            name: "--debug",
            description: "Show detailed debug output and logging information");
        
        
        public const string DefaultApiDirectory = ".apify";
        public const string DefaultConfigFileName = "apify-config.json";
        
    }
}