
using System.CommandLine;
using System.Text.RegularExpressions;
using Apify.Models;
using Apify.Services;
using Apify.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apify.Commands
{
    public class ImportPostmanCommand : Command
    {
        public ImportPostmanCommand() : base("import:postman", "Import a Postman collection and convert it to Apify's file-based format.")
        {
            var fileArgument = new Argument<string>(
                name: "file",
                description: "The path to the Postman collection JSON file.")
            {
                Arity = ArgumentArity.ExactlyOne
            };
            AddArgument(fileArgument);

            var outputDirOption = new Option<string>(
                "--output-dir",
                () => RootOption.DefaultApiDirectory,
                "The directory where the Apify collection will be saved."
            );
            AddOption(outputDirOption);

            var forceOption = new Option<bool>(
                "--force",
                () => false,
                "Force overwrite if the output directory already exists."
            );
            AddOption(forceOption);

            var envOption = new Option<string>(
                "--env-file",
                () => string.Empty,
                "The path to the Postman environment JSON file."
            );
            AddOption(envOption);

            this.SetHandler(
                ExecuteAsync,
                fileArgument, outputDirOption, forceOption, envOption, RootOption.DebugOption
            );
        }

        private async Task ExecuteAsync(string filePath, string outputDir, bool force, string? envFile, bool debug)
        {
            ConsoleHelper.WriteHeader("Importing Postman Collection");

            if (!File.Exists(filePath))
            {
                ConsoleHelper.WriteError($"File not found: {filePath}");
                return;
            }

            if (Directory.Exists(outputDir) && !force)
            {
                ConsoleHelper.WriteError($"Output directory already exists: {outputDir}");
                ConsoleHelper.WriteInfo("Use --force to overwrite the existing directory.");
                return;
            }

            if (Directory.Exists(outputDir) && force)
            {
                Directory.Delete(outputDir, true);
            }

            Directory.CreateDirectory(outputDir);

            try
            {
                if (!string.IsNullOrEmpty(envFile))
                {
                    await UpdateApifyConfigWithPostmanEnvironment(envFile);
                }

                string postmanJson = await File.ReadAllTextAsync(filePath);
                var postmanCollection = JsonConvert.DeserializeObject<PostmanCollection>(postmanJson);

                if (postmanCollection == null || postmanCollection.Items == null)
                {
                    ConsoleHelper.WriteError("Failed to parse Postman collection or the collection is empty.");
                    return;
                }

                if (postmanCollection.Auth != null)
                {
                    await UpdateApifyConfigWithPostmanAuth(postmanCollection.Auth);
                }

                ProcessItems(postmanCollection.Items, outputDir);

                ConsoleHelper.WriteSuccess($"Successfully imported Postman collection to: {outputDir}");
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to import Postman collection: {ex.Message}");
            }
        }
        
        private AuthorizationSchema? HandleAuthorization(PostmanAuth? postmanAuth)
        {
            if (postmanAuth == null)
            {
                return null;
            }

            var newAuth = new AuthorizationSchema();
            string? token = null;

            switch (postmanAuth.Type)
            {
                case "bearer":
                    newAuth.Type = AuthorizationType.Bearer;
                    token = postmanAuth.Bearer?.FirstOrDefault(d => d.Key == "token")?.Value;
                    break;
                case "basic":
                    newAuth.Type = AuthorizationType.Basic;
                    var username = postmanAuth.Basic?.FirstOrDefault(d => d.Key == "username")?.Value;
                    var password = postmanAuth.Basic?.FirstOrDefault(d => d.Key == "password")?.Value;
                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                    {
                        token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{username}:{password}"));
                    }
                    break;
                case "noauth":
                    newAuth.Type = AuthorizationType.None;
                    break;
                default:
                    ConsoleHelper.WriteInfo($"Postman auth type '{postmanAuth.Type}' is not supported for import.");
                    return null;
            }

            newAuth.Token = string.IsNullOrEmpty(token)? null : ReplaceVariables(token);
            return newAuth;
        }

        private async Task UpdateApifyConfigWithPostmanAuth(PostmanAuth postmanAuth)
        {
            var configService = new ConfigService();
            var apifyConfig = configService.LoadConfiguration();

            var newAuth = HandleAuthorization(postmanAuth);

            if (apifyConfig.Authorization != null && apifyConfig.Authorization.Token != newAuth.Token)
            {
                ConsoleHelper.WriteWarning("An existing authorization configuration was found and will be overwritten.");
            }

            apifyConfig.Authorization = newAuth;

            string configPath = Path.Combine(Directory.GetCurrentDirectory(), "apify-config.json");
            string updatedConfigJson = JsonHelper.SerializeObject(apifyConfig);
            await File.WriteAllTextAsync(configPath, updatedConfigJson);

            ConsoleHelper.WriteSuccess("Successfully updated apify-config.json with Postman authorization.");
        }

        private async Task UpdateApifyConfigWithPostmanEnvironment(string envFilePath)
        {
            if (!File.Exists(envFilePath))
            {
                ConsoleHelper.WriteError($"Environment file not found: {envFilePath}");
                return;
            }

            string envJson = await File.ReadAllTextAsync(envFilePath);
            var postmanEnv = JsonConvert.DeserializeObject<PostmanEnvironment>(envJson);

            if (postmanEnv == null || postmanEnv.Values == null)
            {
                ConsoleHelper.WriteError("Failed to parse Postman environment file.");
                return;
            }

            var configService = new ConfigService();
            var apifyConfig = configService.LoadConfiguration();

            var newVariables = postmanEnv.Values
                .Where(v => v.Enabled && v.Key != null && v.Value != null)
                .ToDictionary(v => v.Key!, v => v.Value!);

            var defaultEnv = apifyConfig.Environments.FirstOrDefault(e => e.Name == apifyConfig.DefaultEnvironment);
            if (defaultEnv == null)
            {
                defaultEnv = apifyConfig.Environments.FirstOrDefault();
            }

            if (defaultEnv != null)
            {
                foreach (var variable in newVariables)
                {
                    defaultEnv.Variables[variable.Key] = variable.Value;
                }
            }

            string configPath = Path.Combine(Directory.GetCurrentDirectory(), "apify-config.json");
            string updatedConfigJson = JsonHelper.SerializeObject(apifyConfig);
            await File.WriteAllTextAsync(configPath, updatedConfigJson);

            ConsoleHelper.WriteSuccess($"Successfully updated apify-config.json with variables from: {envFilePath}");
        }

        private void ProcessItems(PostmanItem[] items, string currentDir)
        {
            foreach (var item in items)
            {
                if (item.Items != null && item.Items.Length > 0)
                {
                    var newDir = Path.Combine(currentDir, item.Name!);
                    Directory.CreateDirectory(newDir);
                    ProcessItems(item.Items, newDir);
                }
                else if (item.Request != null)
                {
                    if (item.Request.Method == null || item.Request.Url == null)
                    {
                        continue;
                    }
                    
                    var requestSchema = new RequestDefinitionSchema
                    {
                        Name = item.Name!,
                        Method = item.Request.Method!,
                        Url = ReplaceVariables(item.Request.Url!.Raw!),
                        Headers = item.Request.Headers?.ToDictionary(h => h.Key!, h => ReplaceVariables(h.Value!)),
                        Body = ConvertPostBody(item.Request.Body),
                        PayloadType = GetPayloadType(item.Request.Body)
                    };
                    
                    if (item.Request.Auth != null)
                    {
                        requestSchema.Authorization = HandleAuthorization(item.Request.Auth);
                    }

                    var fileName = $"{item.Name!.Replace(" ", "_").ToLower()}.json";
                    var filePath = Path.Combine(currentDir, fileName);

                    string jsonContent = JsonHelper.SerializeObject(requestSchema);
                    File.WriteAllText(filePath, jsonContent);
                }
            }
        }

        private string ReplaceVariables(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            return Regex.Replace(value, @"\{\{([^{}]+)\}\}", "{{env.$1}}");
        }

        private Body? ConvertPostBody(PostmanBody? postmanBody)
        {
            if (postmanBody == null)
            {
                return null;
            }

            var body = new Body();
            switch (postmanBody.Mode)
            {
                case "raw":
                    if (postmanBody.Raw != null)
                    {
                        try
                        {
                            body.Json = JToken.Parse(ReplaceVariables(postmanBody.Raw));
                        }
                        catch (JsonReaderException)
                        {
                            body.Text = ReplaceVariables(postmanBody.Raw);
                        }
                    }
                    break;
                case "formdata":
                    body.FormData = postmanBody.FormData?.ToDictionary(f => f.Key!, f => ReplaceVariables(f.Value!));
                    break;
            }
            return body;
        }

        private PayloadContentType GetPayloadType(PostmanBody? postmanBody)
        {
            if (postmanBody == null)
            {
                return PayloadContentType.None;
            }

            switch (postmanBody.Mode)
            {
                case "raw":
                    if (postmanBody.Raw != null)
                    {
                        try
                        {
                            JToken.Parse(postmanBody.Raw);
                            return PayloadContentType.Json;
                        }
                        catch (JsonReaderException)
                        {
                            return PayloadContentType.Text;
                        }
                    }
                    return PayloadContentType.None;
                case "formdata":
                    return PayloadContentType.FormData;
                default:
                    return PayloadContentType.None;
            }
        }
    }
}
