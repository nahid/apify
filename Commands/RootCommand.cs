using System.CommandLine;

namespace APITester.Commands
{
    public class RootCommand
    {
        public System.CommandLine.RootCommand Command { get; }

        public RootCommand()
        {
            Command = new System.CommandLine.RootCommand
            {
                Description = "API Tester - A CLI tool for testing APIs similar to Postman"
            };
        }
    }
}
