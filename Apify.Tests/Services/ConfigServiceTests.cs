
using Xunit;
using Apify.Services;
using System.IO;
using Apify.Models;
using System.Collections.Generic;
using System;

namespace Apify.Tests.Services
{
    public class ConfigServiceTests
    {
        [Fact]
        public void LoadConfiguration_WhenFileDoesNotExist_ThrowsFileNotFoundException()
        {
            // Arrange
            var configService = new ConfigService();
            configService.SetConfigFilePath("non-existent-file.json");

            // Act & Assert
            Xunit.Assert.Throws<FileNotFoundException>(() => configService.LoadConfiguration());
        }

        [Fact]
        public void LoadConfiguration_WhenFileIsValid_ReturnsConfiguration()
        {
            // Arrange
            var configService = new ConfigService();
            var config = new ApifyConfigSchema
            {
                DefaultEnvironment = "Development",
                Environments = new List<EnvironmentSchema>
                {
                    new EnvironmentSchema
                    {
                        Name = "Development",
                        Variables = new Dictionary<string, string>
                        {
                            { "baseUrl", "http://localhost:5000" }
                        }
                    }
                }
            };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(config);
            var path = Path.GetTempFileName();
            File.WriteAllText(path, json);
            configService.SetConfigFilePath(path);

            // Act
            var result = configService.LoadConfiguration();

            // Assert
            Xunit.Assert.NotNull(result);
            Xunit.Assert.Equal("Development", result.DefaultEnvironment);

            // Clean up
            File.Delete(path);
        }

        [Fact]
        public void LoadConfiguration_WhenFileIsEmpty_ThrowsFormatException()
        {
            // Arrange
            var configService = new ConfigService();
            var path = Path.GetTempFileName();
            File.WriteAllText(path, string.Empty);
            configService.SetConfigFilePath(path);

            // Act & Assert
            Xunit.Assert.Throws<FormatException>(() => configService.LoadConfiguration());

            // Clean up
            File.Delete(path);
        }

        [Fact]
        public void LoadConfiguration_WhenFileIsInvalid_ThrowsFormatException()
        {
            // Arrange
            var configService = new ConfigService();
            var path = Path.GetTempFileName();
            File.WriteAllText(path, "invalid json");
            configService.SetConfigFilePath(path);

            // Act & Assert
            Xunit.Assert.Throws<FormatException>(() => configService.LoadConfiguration());

            // Clean up
            File.Delete(path);
        }

        [Fact]
        public void GetDefaultEnvironment_WhenDefaultIsSpecified_ReturnsDefaultEnvironment()
        {
            // Arrange
            var configService = new ConfigService();
            var config = new ApifyConfigSchema
            {
                DefaultEnvironment = "Production",
                Environments = new List<EnvironmentSchema>
                {
                    new EnvironmentSchema { Name = "Development" },
                    new EnvironmentSchema { Name = "Production" }
                }
            };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(config);
            var path = Path.GetTempFileName();
            File.WriteAllText(path, json);
            configService.SetConfigFilePath(path);

            // Act
            var result = configService.GetDefaultEnvironment();

            // Assert
            Xunit.Assert.NotNull(result);
            Xunit.Assert.Equal("Production", result.Name);

            // Clean up
            File.Delete(path);
        }

        [Fact]
        public void GetDefaultEnvironment_WhenNoDefaultIsSpecified_ReturnsFirstEnvironment()
        {
            // Arrange
            var configService = new ConfigService();
            var config = new ApifyConfigSchema
            {
                Environments = new List<EnvironmentSchema>
                {
                    new EnvironmentSchema { Name = "Development" },
                    new EnvironmentSchema { Name = "Production" }
                }
            };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(config);
            var path = Path.GetTempFileName();
            File.WriteAllText(path, json);
            configService.SetConfigFilePath(path);

            // Act
            var result = configService.GetDefaultEnvironment();

            // Assert
            Xunit.Assert.NotNull(result);
            Xunit.Assert.Equal("Development", result.Name);

            // Clean up
            File.Delete(path);
        }

        [Fact]
        public void ApplyVariablesToString_WithValidInput_ReturnsSubstitutedString()
        {
            // Arrange
            var configService = new ConfigService();
            var config = new ApifyConfigSchema
            {
                DefaultEnvironment = "Development",
                Environments = new List<EnvironmentSchema>
                {
                    new EnvironmentSchema
                    {
                        Name = "Development",
                        Variables = new Dictionary<string, string>
                        {
                            { "baseUrl", "http://localhost:5000" }
                        }
                    }
                }
            };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(config);
            var path = Path.GetTempFileName();
            File.WriteAllText(path, json);
            configService.SetConfigFilePath(path);
            configService.LoadConfiguration();
            configService.GetDefaultEnvironment();

            // Act
            var result = configService.ApplyVariablesToString("{{baseUrl}}/api/users");

            // Assert
            Xunit.Assert.Equal("http://localhost:5000/api/users", result);

            // Clean up
            File.Delete(path);
        }
    }
}
