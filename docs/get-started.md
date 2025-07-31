# Apify Documentation

Apify is a robust command-line tool designed for comprehensive API testing and mocking. It allows developers to define API tests in JSON format and execute them against endpoints, providing detailed output of the request, response, and test results. The tool offers centralized environment management, variable substitution, and support for various payload types and file uploads. It also includes a powerful mock server for simulating API responses during development and testing.

## Table of Contents

*   [Getting Started](getting-started/installation.md)
*   [Core Concepts](core-concepts/)
*   [Command Reference](command-reference/)
*   [Environment Variables](variables/)
*   [Tags (Variables & Expressions)](tags.md)
*   [Initialization](initialization)
*   [API Testing](api-testing/)
*   [Test Runner](test-runner/)
*   [Mock Server](mock-server/)
*   [Troubleshooting](troubleshooting/)

## Getting Started

Apify is a powerful CLI application for comprehensive API testing and mocking, enabling developers to streamline API validation and development workflows with rich configuration and execution capabilities.

For installation instructions, including prerequisites for building from source, please refer to the [Installation Guide](installation.md).

Once installed, you can initialize your project and start defining API tests and mocks. Learn more about the core concepts and commands in the following sections.

## Installation

The easiest way to get Apify is to download the pre-built executable from the [GitHub Releases](https://github.com/nahid/apify/releases) page.

1.  Go to the [latest release](https://github.com/nahid/apify/releases/latest).
2.  Download the appropriate `.zip` file for your operating system and architecture (e.g., `apify-win-x64.zip` for Windows 64-bit, `apify-linux-x64.zip` for Linux 64-bit, `apify-osx-arm64.zip` for macOS ARM64).
3.  Extract the contents of the `.zip` file to a directory of your choice (e.g., `C:\Program Files\Apify` on Windows, `/opt/apify` on Linux/macOS).
4.  Add the directory where you extracted Apify to your system's PATH environment variable. This allows you to run `apify` from any terminal.

### CLI Installation

For a quick installation via your command line, use the following platform-specific instructions. See the [latest GitHub release](https://github.com/nahid/apify/releases/latest) for the most up-to-date download links.

#### Linux & macOS

**For Linux Operating Systems:**


```bash
curl -L https://github.com/nahid/apify/releases/latest/download/apify-linux-x64.zip -o apify.zip

```

**For macOS**

```bash
# macOS arm64 (Apple Silicon)
curl -L https://github.com/nahid/apify/releases/latest/download/apify-osx-arm64.zip -o apify.zip

# macOS x64 (Intel)
curl -L https://github.com/nahid/apify/releases/latest/download/apify-osx-x64.zip -o apify.zip
```

Unzip the downloaded file:

```bash
unzip apify.zip -d .
```

This will extract the `apify` binary (and possibly other files) to your current directory.

```bash
# Make the binary executable and move it to /usr/local/bin

sudo chmod a+x ./apify/apify
sudo mv ./apify/apify /usr/local/bin/
```

```bash
# Verify installation
apify --version
```

##### Resolving macOS Security Warnings

On macOS, you may see a security warning when running the binary for the first time (e.g., "cannot be opened because the developer cannot be verified"). To allow execution:

1. Attempt to run `apify` from the terminal. If blocked, note the warning.
2. Open **System Settings** > **Privacy & Security**.
3. Scroll down to the "Security" section. You should see a message about `apify` being blocked.
4. Click **Allow Anyway**.
5. Run `apify` again from the terminal. If prompted, click **Open** in the dialog.

Alternatively, you can remove the quarantine attribute via terminal:

```bash
sudo xattr -rd com.apple.quarantine /usr/local/bin/apify
```

This will allow the binary to run without further security prompts.

Now remove the downloaded zip file and the extracted directory if you no longer need them:

```bash
rm -rf apify.zip apify/
```

#### Windows (PowerShell)

1. Download the appropriate `.zip` file for Windows from the [latest release](https://github.com/nahid/apify/releases/latest) and extract it using File Explorer or a tool like WinRAR. Inside the extracted folder, you'll find `apify.exe`.

2. Create a new folder for Apify in `Program Files` (run PowerShell as Administrator):

   ```powershell
   New-Item -ItemType Directory -Force -Path "$env:ProgramFiles\Apify"
   ```

3. Download the latest release for Windows:

   ```powershell
   Invoke-WebRequest -Uri "https://github.com/nahid/apify/releases/latest/download/apify-win-x64.zip" -OutFile "apify.zip"
   ```

4. Unzip the downloaded file:

   ```powershell
   Expand-Archive -Path "apify.zip" -DestinationPath "."
   ```

5. Move `apify.exe` to the new folder:

   ```powershell
   Move-Item -Path ".\apify\apify.exe" -Destination "$env:ProgramFiles\Apify" -Force
   ```

6. Add Apify to your user PATH environment variable:

   ```powershell
   [Environment]::SetEnvironmentVariable("PATH", $env:PATH + ";C:\Program Files\Apify", "User")
   ```

7. Restart your terminal, then verify the installation:

   ```powershell
   apify --version
   ```

> Ensure you run PowerShell as Administrator for steps that modify `Program Files`.

Alternatively, you can build Apify from source:

### Build from Source

Apify supports Native AOT (Ahead-of-Time) compilation, which produces a self-contained executable with no dependency on the .NET runtime. This results in:

- Faster startup time
- Smaller deployment size
- No dependency on the .NET runtime
- Improved performance

To build the Native AOT version:

```bash
# Using the build script
./build-native.sh

# Or manually
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishAot=true
```

The resulting executable will be located at:
`bin/Release/net8.0/linux-x64/publish/apify`

You can run it directly without needing the .NET runtime:

```bash
./bin/Release/net8.0/linux-x64/publish/apify
```

#### Platform-Specific Builds

For other platforms, replace `linux-x64` with your target platform:

- Windows: `win-x64`
- macOS: `osx-x64`
- ARM64: `linux-arm64` or `osx-arm64`

## Core Concepts

Apify provides a robust framework for defining, running, and mocking API tests in your projects. This guide covers the foundational concepts, including project setup, configuration, test definitions, and mock API responses.

### 1. Project Initialization

Start by initializing your project with Apify:

```bash
apify init
```

This command will guide you through:

- Naming your project
- Setting a default environment (e.g., "Development")
- Adding additional environments (e.g., "Staging", "Production")

After initialization, your project will include:

- `apify-config.json`: Central configuration for environments, variables, and mock server.
- `.apify/`: Directory for API test definitions (`.json`) and mock definitions (`.mock.json`).
- Sample test and mock files to help you get started.

### 2. Configuration (`apify-config.json`)

This JSON file defines global project settings, environments, and mock server options.

**Example:**

```json
{
  "Name": "My Project API Tests",
  "Description": "API Tests for My Project",
  "DefaultEnvironment": "Development",
  "Variables": {
    "globalProjectVar": "This variable is available in all environments and tests"
  },
  "Environments": [
    {
      "Name": "Development",
      "Variables": {
        "baseUrl": "https://dev-api.myproject.com",
        "apiKey": "dev-secret-key"
      }
    },
    {
      "Name": "Production",
      "Variables": {
        "baseUrl": "https://api.myproject.com",
        "apiKey": "prod-secret-key"
      }
    }
  ],
  "MockServer": {
    "Port": 8080,
    "Directory": ".apify/mocks",
    "Verbose": false,
    "EnableCors": true,
    "DefaultHeaders": {
      "X-Mock-Server": "Apify"
    }
  }
}
```

**Key Sections:**

- **Name/Description**: Project metadata.
- **DefaultEnvironment**: Used if `--env` is not specified.
- **Variables**: Project-wide variables, overridable by environments or requests.
- **Environments**: Define environment-specific variables.
- **MockServer**: Configure the built-in mock server.

### 3. API Test Definitions

API tests are stored as JSON files in `.apify/` (e.g., `.apify/users/get-users.json`).

**Structure:**

```json
{
  "Name": "Get All Users",
  "Description": "Fetches the list of all users",
  "Url": "{{env.baseUrl}}/users?page={{env.defaultPage}}",
  "Method": "GET",
  "Headers": {
    "Accept": "application/json",
    "X-Api-Key": "{{env.apiKey}}"
  },
  "Variables": {
    "defaultPage": "1",
    "apiKey": "abcxyz123",
    "baseUrl": "https://api.example.com"
  },
  "Tests": [
    {
      "Title": "Status code is 200 OK",
      "Case": "$.response.getStatusCode() == 200"
    }
  ]
}
```

**Concepts:**

- **Variables**: Request-level variables override environment/project variables.
- **Tags**: (Optional) For filtering tests.
- **Tests**: Each assertion includes a title and a ES6(JavaScript) expression using the `Assert` object.

### 4. Mock API Definitions

Mocks are defined in `.mock.json` files under `.apify/mocks/` (e.g., `.apify/mocks/users/get-user-by-id.mock.json`).

**Structure:**

```json
{
  "Name": "Mock User by ID",
  "Method": "GET",
  "Endpoint": "/api/users/{id}",
  "Responses": [
    {
      "Condition": "$.path.id == 1",
      "StatusCode": 200,
      "Headers": {
        "X-Source": "Mock-Conditional-User1"
      },
      "ResponseTemplate": {
        "id": 1,
        "name": "John Doe (Mocked)",
        "email": "john.mock@example.com",
        "requested_id": "{{path.id}}",
        "random_code": "{# $.faker.datatype.number({min: 1000, max: 9999}) #}"
      }
    },
    {
      "Condition": "default",
      "StatusCode": 404,
      "ResponseTemplate": {
        "error": "User not found",
        "id_searched": "{{path.id}}"
      }
    }
  ]
}
```

**Key Points:**

- **Endpoint**: Supports path parameters (`{id}`).
- **Responses**: Evaluated in order; first matching condition is used.
  - **Condition**: ES6(JavaScript) expression using request data (`path`, `query`, `headers`, `body`).
  - **ResponseTemplate**: Supports template variables for dynamic responses, including random data via Faker.

### 5. Variable Resolution

Variables are resolved in the following order (highest to lowest precedence):

1. Request-level (`Variables` in test definition)
2. Environment-level (`Variables` in environment)
3. Project-level (`Variables` in config)

### 6. Mock Server

The built-in mock server can be started using your configuration. It serves mock responses based on your `.mock.json` files, supporting dynamic templating and conditional logic.

For more advanced usage, see the full documentation on test assertions, environment management, and mock server customization.

## Initialization

Initializes a new API testing project in the current directory. To start using Apify in your project, navigate to your project's root directory and run:

```bash
apify init [--force]
```

This command interactively prompts for:

- Project Name
- Default Environment Name (e.g., "Development")
- Additional environments (e.g., "Staging", "Production")

It creates:

- `apify-config.json`: The main configuration file.
- `.apify/`: A directory to store your API test definitions (`.json`) and mock definitions (`.mock.json`).
- Sample API test and mock definition files within `.apify/` and `.apify/mocks/` respectively.
- A `MockServer` configuration block in `apify-config.json`.

#### Command Options

- `--force`: Overwrite existing `apify-config.json` and `.apify` directory if they exist.
- `--name`: Specify a custom project name.
- `--force`: Overwrite existing files without prompting.
- `--debug`: Enable debug mode for more verbose output.
- Prompts for project name, default environment name, and other environments to create.
- Creates `apify-config.json`, `.apify/` directory with sample test and mock files.

### Configuration (`apify-config.json`)

This file stores project-level settings, environments, and mock server configuration.

```json
{
  "Name": "My Project API Tests",
  "Description": "API Tests for My Project",
  "DefaultEnvironment": "Development",
  "Variables": {
    "globalProjectVar": "This variable is available in all environments and tests"
  },
  "Environments": [
    {
      "Name": "Development",
      "Description": "Development environment specific variables",
      "Variables": {
        "baseUrl": "https://dev-api.myproject.com",
        "apiKey": "dev-secret-key"
      }
    },
    {
      "Name": "Production",
      "Description": "Production environment specific variables",
      "Variables": {
        "baseUrl": "https://api.myproject.com",
        "apiKey": "prod-secret-key"
      }
    }
  ],
  "MockServer": {
    "Port": 8080,
    "Directory": ".apify/mocks",
    "Verbose": false,
    "EnableCors": true,
    "DefaultHeaders": {
      "X-Mock-Server": "Apify"
    }
  }
}
```

#### Key Fields

_(\*) Required fields_

- **`Name`**: Name of the project.
- **`Description`**: Project metadata.
- **`DefaultEnvironment`**: The environment used if `--env` is not specified.
- **`Variables` (Project-Level)**: Key-value pairs available across all tests and environments unless overridden.
- **`Options`** - (optional): Additional global options for the API testing.
  - **`Verbose`**: Enable verbose logging for the CLI.
  - **`Tests`**: Enable test runner globally for all requests.
  - **`ShowRequest`**: Show request details in the console.
  - **`ShowResponse`**: Show response details in the console.
  - **`ShowOnlyResponse`**: Show only response details in the console.
- **`Authorization`** - (optional): Global authorization settings.
  - **`Type`**: Type of authorization (e.g., "bearer", "basic", "apiKey).
  - **`Token`**: Authorization token or credentials.
- **`Environments`**: An array of environment objects.
  - **`Name`**: Unique name for the environment (e.g., "Development", "Staging").
  - **`Variables`**: Key-value pairs specific to this environment. These override project-level variables.
- **`MockServer`**: Configuration for the mock server.
  - **`Port`**: Port for the mock server.
  - **`Verbose`**: Enable verbose logging for the mock server.
  - **`EnableCors`**: Enable CORS headers (defaults to allow all).
  - **`DefaultHeaders`**: Headers to be added to all mock responses.

## Command Reference

Apify CLI provides commands to help you create, manage, and test API projects. You can run commands globally as `apify <command> [options]`

### `apify init`

Set up a new API testing project in your current directory.

```bash
apify init [--force]
```

- `--force`: Overwrite existing `apify-config.json` and `.apify` directory if present.
- Prompts for project and environment details.
- Generates `apify-config.json` and a `.apify/` directory with sample files.

### `apify create:request`

Create a new API test definition interactively.

```bash
apify create:request <file> [--force]
```

- `<file>`: (Required) Path for the new API request definition (e.g., `users.all` → `.apify/users/all.json`).
- `--force`: Overwrite if the file exists.
- Guided prompts for request details and assertions.

### `apify create:mock`

Create a new mock API definition interactively.

```bash
apify create:mock <file> [--force]
```

- `<file>`: (Required) Path for the new mock definition (e.g., `users.get` → `.apify/users/get.mock.json`).
- `--force`: Overwrite if the file exists.
- Guided prompts for mock details, responses, and conditions.

### `apify call`

Run an API test from a definition file.

```bash
apify call <file> [--env <environment>] [--verbose]
```

- `<file>`: (Required) API definition file (e.g., `users/all.json` or `users.all`).
- `--env <environment>`: Specify environment (uses default if omitted).
- `--verbose`, `-v`: Show detailed request/response output.

### `apify tests`

Run all API tests in the `.apify` directory.

```bash
apify tests [--env <environment>] [--tag <tag>] [--verbose]
```

- `--env <environment>`: Specify environment.
- `--tag <tag>`: Run only tests with the given tag.
- `--verbose`, `-v`: Show detailed output.
- Displays progress and summary.

### `apify server:mock`

Start a local mock server using your mock definitions.

```bash
apify server:mock [--port <port>] [--directory <mocks_dir>] [--verbose]
```

- `--port <port>`: Port for the server (default: from config or 1988).
- `--project <mocks_dir>`: Project directory with mock files (default: `.apify`).
- `--verbose`, `-v`: Enable verbose logging.
- Reads settings from `apify-config.json` (`MockServer` block); CLI options override config.

### `apify list:env`

Show all environments and their variables from `apify-config.json`.

```bash
apify list-env
```

## Global Options

- `--debug`: Show debug output, including stack traces and internal logs. Useful for troubleshooting.

## Environment Variables

Apify's variable system enables dynamic configuration of API definitions and mock responses through template substitution. Variables can be set at different levels, each with a specific precedence:

### Variable Precedence

1. **Request-level variables** (highest priority):  
   Defined within an individual API test definition file, these override all other variables.
2. **Environment variables** (medium priority):  
   Specified in the `Environments` section of `apify-config.json`, these override project-level variables.
3. **Project-level variables** (lowest priority):  
   Set in the root `Variables` object of `apify-config.json`, these provide default values.

Reference variables using the `{{env.variableName}}` syntax in URLs, headers, and response bodies.

### Managing Environments

Configure multiple environments (such as development, staging, or production) in the `Environments` array of `apify-config.json`. Each environment can define its own variables, which take precedence over project-level variables.

### Example Environment Configuration

You can define multiple environments in the `Environments` array of `apify-config.json`. Each environment can have its own set of variables.

```json
{
  "Environments": [
    {
      "Name": "Development",
      "Variables": {
        "API_BASE_URL": "https://dev.api.example.com",
        "AUTH_TOKEN": "dev-token"
      }
    },
    {
      "Name": "Production",
      "Variables": {
        "API_BASE_URL": "https://api.example.com",
        "AUTH_TOKEN": "prod-token"
      }
    }
  ]
}
```

### Using Variables in API Definitions

When defining API requests, you can reference environment variables or custom variables directly in your API definition files. For example:

```json
{
  "Method": "GET",
  "Url": "{{ env.API_BASE_URL }}/users",
  "Headers": {
    "Authorization": "Bearer {{ env.AUTH_TOKEN }}"
  }
}
```

### Listing Environments and Variables

```bash
apify list-env
```

### Defining Global Variables

Global variables can be added directly to API definition files under the `Variables` object. These are scoped to all requests and override both environment and request-level variables with the same name.

```json
{
  "Variables": {
    "CUSTOM_VAR": "custom-value"
  }
}
```

To display all environments and their variables defined in `apify-config.json`, use:

```bash
apify list:env
```

## API Testing

Welcome to the API Testing section. Here you'll find everything you need to define, execute, and manage automated tests for your APIs using Apify.

### Overview

API testing is essential for ensuring your endpoints work as expected, handle edge cases, and remain reliable as your application evolves. Apify's API testing tools are designed to help you automate these checks, integrate them into your workflows, and gain confidence in your API's stability.

### Key Features

- **Comprehensive HTTP Method Support**  
   Test all standard HTTP methods, including GET, POST, PUT, DELETE, PATCH, HEAD, and OPTIONS.

- **Flexible Payload Handling**  
   Send requests with JSON, plain text, form data, or raw binary payloads. Easily test endpoints that require file uploads or complex data structures.

- **File Upload Testing**  
   Simulate multipart/form-data requests and verify file handling in your API.

- **Advanced Assertions**  
   Validate every aspect of the response:

  - Status codes
  - Headers
  - Body content (including nested JSON properties, arrays, and specific values)
  - Response time and performance

- **Chained Requests & Dynamic Data**  
   Chain multiple requests together, passing data from one response to the next. Test real-world API workflows and scenarios.

- **Environment & Variable Support**  
   Parameterize your tests with environment variables for flexible, reusable test cases.

- **Detailed Reporting**  
   Get clear, actionable feedback on test results, including logs, error messages, and performance metrics.

### Get Started

Browse the topics in this section:

- [Set Up Your First API Test](./create-request)
- [Execute API Tests](./call-command)
- [Define API Test Assertions](./assertions)
- [Manage Data and Expressions](./manage-data)
- [Generate Fake Data](./manage-data#faker-data)

### Create Request

Interactively guides you through the process of creating a new API test definition file. You will be prompted to provide details such as the request name, HTTP method, endpoint URL, headers, payload, and basic assertions. Once completed, the tool generates a structured JSON file in the `.apify` directory, ready to be used for automated API testing.

```bash
apify create:request <file> [--prompt] [--force]
```

- `<file>`: (Required) The file path for the new API request definition (e.g., `users.all` becomes `.apify/users/all.json`). The `.json` extension is added automatically.
- `--force`: Overwrite if the file already exists.
- Prompts for request name, HTTP method, URI, headers, payload, and basic assertions.

#### Example

```bash
apify create:request users.all --prompt
```

This command will create a new API test definition file at `.apify/users/all.json` with an interactive prompt to fill in the details.

#### Command Arguments

- **`<file>`**: The path where the API test definition will be created.

#### Command Options

- **`--name`**: Specify a custom name for the request (e.g., `--name "Get All Users"`).
- **`--method`**: Specify the HTTP method (e.g., `--method GET`).
- **`--url`**: Specify the request URL (e.g., `--url "{{env.baseUrl}}/users"`).
- **`--prompt`**: Use interactive prompts to fill in the request details.
- **`--force`**: Overwrite existing files without confirmation.
- **`--debug`**: Enable debug mode for more detailed output during creation.

### API Test Definition

API tests are defined in JSON files (e.g., `.apify/users/all.json`).

Structure:

```json
{
  "Name": "Get All Users",
  "Description": "Fetches the list of all users",
  "Url": "{{env.baseUrl}}/users?page={{env.defaultPage}}",
  "Method": "GET",
  "Headers": {
    "Accept": "application/json",
    "X-Api-Key": "{{env.apiKey}}"
  },
  "Body": null,
  "PayloadType": "none", // "json", "text", "formData"
  "Timeout": 30000, // Optional, in milliseconds
  "Tags": ["users", "smoke"], // Optional, for filtering tests
  "Variables": {
    // Request-specific variables (highest precedence)
    "defaultPage": "1"
  },
  "Tests": [
    {
      "Title": "Status code is 200 OK",
      "Case": "$.response.statusCode == 200"
    }
  ]
}
```

#### Fields Explained

- **`Name`**: Name of the API test (e.g., "Get All Users").
- **`Description`**: Optional description of the test.
- **`Url`**: The endpoint URL (supports variable substitution).
- **`Method`**: HTTP method (GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS).
- **`Headers`**: HTTP headers as key-value pairs.
- **`PayloadType`**: Type of payload (`none`, `json`, `text`, `formData`, `multipart`, `binary`).
- **`Body`**: The request body (for POST, PUT, PATCH). Can be JSON, text, etc.
  - **`Json`**: JSON object for `json` payload type.
  - **`Text`**: Plain text for `text` payload type.
  - **`FormData`**: URL-encoded form data for `formData`
  - **`Multipart`**: For `multipart/form-data`, specify files in the `Files` array.
  - **`name`**: The name of the file in the form.
  - **`content`**: The content of the file, which can be a string or binary data.
- **`Timeout`**: Request timeout in milliseconds (optional).
- **`Tags`**: Array of strings for categorizing and filtering tests (optional).
- **`Variables`**: Key-value pairs specific to this request (highest precedence).
- **`Tests`**: A list of assertion objects to validate the response.

#### Payload Types (`PayloadType`)

Apify supports multiple payload types for flexibility in testing different types of API endpoints.

| Payload Type | Description                         | Content-Type Header (auto-set)      |
| ------------ | ----------------------------------- | ----------------------------------- |
| `none`       | No request body                     | None                                |
| `json`       | JSON structured data                | `application/json`                  |
| `text`       | Plain text content                  | `text/plain`                        |
| `formData`   | URL-encoded form data               | `application/x-www-form-urlencoded` |
| `multipart`  | Multipart form data (file uploads)  | `multipart/form-data`               |
| `binary`     | Raw binary data (e.g., file upload) | `application/octet-stream`          |

#### Body Type (`Body`)

The `Body` field in the API test definition specifies the content of the request body based on the `PayloadType`, it's an optional field. Depending on the type, you can provide different formats:

- **`Json`**: For `json` payload type, specify a JSON object.
- **`Text`**: For `text` payload type, specify a string.
- **`FormData`**: For `formData` payload type, specify key-value pairs, e.g., `{"key": "value"}`.
- **`Multipart`**: For `multipart` payload type, specify files in the `Files` array.
  - **`name`**: The name of the file in the form.
  - **`content`**: The content of the file, which can be a string or binary data.
- **`Binary`**: For `binary` payload type, specify the file path.

#### Test Assertions

Apify provides comprehensive assertion capabilities using ES6(JavaScript) expressions to validate API responses. Assertions are defined in the `Tests` array of your API test definition. To know more about assertions, see the [Test Assertions](/docs/api-testing/assertions) section.

### Execute API Test

Executes an API test based on a specified definition file. The command reads the API request and expected response details from the provided file, sends the request to the target API endpoint, and evaluates the response using the assertions defined in the test file. This allows you to automate the validation of API endpoints, ensuring they behave as expected under different scenarios. You can specify the environment, enable verbose output for detailed logs, and use either JSON or dot notation for the file path. The results, including assertion outcomes and any errors, are displayed in the console.

```bash
apify call <file> [--env <environment_variables>] [--verbose]
```

- `<file>`: (Required) An API definition file path (e.g., `users/all.json`). Dot notation like `users.all` is also supported.
- `--env <environment_name>`: Specifies the environment to use (e.g., "Production"). Uses default from `apify-config.json` if not set.
- `--verbose` or `-v`: Displays detailed output, including request and response bodies.

#### Command Options

- **`--env`**: Specifies the environment name, e.g., "Production".
- **`--vars`**: Lets you define or override custom variables for your requests or tests, for example: `--vars "key1=value1;key2=value2"`. These variables are merged with the current environment and can be accessed using `{{ vars.key1 }}` or `{{ vars.key2 }}` in your placeholders.
- **`--tests`**: Runs all tests in the `.apify` directory.
- **`--show-request`**: Displays the request details before execution.
- **`--show-response`**: Displays the response details after execution.
- **`--show-only-response`**: Only shows the response details, skipping the request.
- **`--verbose`**: Displays detailed output.
- **`--debug`**: Enables debug mode for more detailed output.
- **`--tag`**: Filters tests by tag (e.g., "smoke", "regression").

#### Test Assertions

Apify provides comprehensive assertion capabilities using C# expressions to validate API responses. Assertions are defined in the `Tests` array of your API test definition.

- **`Title`**: A descriptive name for the assertion.
- **`Case`**: A C# expression to be evaluated. The expression should return a boolean value. You can use the `Assert` object and its methods to perform assertions.

To know more about assertions, see the [Test Assertions](/docs/api-testing/assertions) section.

### Test Assertions

Apify provides comprehensive assertion capabilities using ES6(JavaScript) expressions to validate API responses. Assertions are defined in the `Tests` array of your API test definition.

#### What are Assertions?

Assertions are logical statements that verify whether the actual API response matches the expected outcome. They help ensure your API behaves as intended by checking response status codes, headers, body content, and more.

#### Defining Assertions

You can define assertions in your test definition file under the `Tests` array. Each assertion uses a ES6(JavaScript) expression that evaluates to `true` or `false`. If an assertion fails, the test is marked as failed.

**Example:**

```json
{
  "Tests": [
    {
      "Description": "Status code is 200",
      "Assert": "$.response.getStatusCode() == 200"
    },
    {
      "Description": "Response contains expected property",
      "Assert": "$.response.getJson()?.name ? true : false"
    }
  ]
}
```

##### Fields

- **`Title`**: A descriptive name for the assertion.
- **`Case`**: Any ES6(JavaScript) to be evaluated. The expression should return a boolean value, or you can use the `$.assert` object and its methods to perform assertions.

#### Common Assertion Scenarios

- **Status Code Validation:**  
   Ensure the API returns the correct HTTP status code.

  ```js
  $.response.getStatusCode() == 200;
  ```

- **Header Validation:**  
   Check if a specific header exists or has the expected value.

  ```js
  $.response.getJson()?.name ? true : false;
  ```

- **Body Content Validation:**  
   Verify the response body contains expected data.

  ```js
  $.assert.isTrue($.response.getJson()?.active);
  ```

- **Array and Collection Checks:**  
   Assert that a collection in the response has the expected length.

  ```js
  $.response.getJson()?.items?.length == 10;
  ```

- **Handling Assertion Failures:**
  When an assertion fails, it throws an error with a message indicating the failure. You can catch these errors in your test runner to handle them gracefully.

  ```js
  $.assert.isTrue($.response.getJson()?.active);
  ```

When an assertion fails, Apify provides detailed error messages indicating which assertion failed and why. This helps you quickly identify and fix issues in your API.

#### Available Assertions

There are several built-in assertions you can use to validate API responses. Here are some common ones:

##### List of Assert Methods

- [$.assert.equals](#assertequals)
- [$.assert.notEquals](#assertnotequals)
- [$.assert.isTrue](#assertistrue)
- [$.assert.isFalse](#assertisfalse)
- [$.assert.isNull](#assertisnull)
- [$.assert.isNotNull](#assertisnotnull)
- [$.assert.isEmpty](#assertisempty)
- [$.assert.isNotEmpty](#assertisnotempty)
- [$.assert.isArray](#assertisarray)
- [$.assert.isObject](#assertisobject)
- [$.assert.isString](#assertisstring)
- [$.assert.isNumber](#assertisnumber)
- [$.assert.isBoolean](#assertisboolean)
- [$.assert.isGreaterThan](#assertisgreaterthan)
- [$.assert.isLessThan](#assertislessthan)
- [$.assert.isGreaterThanOrEqual](#assertisgreaterthanorequal)
- [$.assert.isLessThanOrEqual](#assertislessthanorequal)
- [$.assert.isBetween](#assertisbetween)
- [$.assert.isNotBetween](#assertisnotbetween)
- [$.assert.contains](#assertcontains)
- [$.assert.notContains](#assertnotcontains)
- [$.assert.matchesRegex](#assertmatchesregex)
- [$.assert.notMatchesRegex](#assertNotMatchesRegex)

There are another two objects available for the `Case` field, `$.request` and `$.response`, you can use them to access the request and response data directly. For example, you can access the request URL with `$.request.getBod().json.name` or the response body with `$.response.getJson().name`.

- [Request](/docs/api-testing/create-request/#fields-explained)
- [Response](#response)

##### Assert

Assertions are made using the `$.assert` object, which provides methods to validate various aspects of the response. All the `$.assert` methods support accepting messages that will be displayed if the assertion fails in last parameter and all methods return a boolean indicating success or failure. Here are some common assertions you can use:

##### Assert Method Reference

Below are the commonly used assertion methods available for validating API responses. Each method returns a boolean indicating success or failure and can accept an optional message as the last parameter.

###### $.assert.equals

Checks if two values are equal.

```js
$.assert.equals(actualValue, expectedValue);
```

**Example:**

```json
{
  "Title": "Check if name is John",
  "Case": "$.assert.equals(Response.Json['name'], 'John')"
}
```

###### $.assert.notEquals

Checks if two values are not equal.

```js
$.assert.notEquals(actualValue, expectedValue);
```

**Example:**

```json
{
  "Title": "Check if name is not John",
  "Case": "$.assert.notEquals(Response.Json['name'], 'John')"
}
```

###### $.assert.isTrue

Checks if a condition is true.

```js
$.assert.isTrue(condition);
```

**Example:**

```json
{
  "Title": "Check if user is active",
  "Case": "$.assert.isTrue(Response.Json['isActive'])"
}
```

###### $.assert.isFalse

Checks if a condition is false.

```js
$.assert.isFalse(condition);
```

**Example:**

```json
{
  "Title": "Check if user is not active",
  "Case": "$.assert.isFalse(Response.Json['isActive'])"
}
```

###### $.assert.isNull

Checks if a value is null.

```js
$.assert.isNull(value);
```

**Example:**

```json
{
  "Title": "Check if user data is null",
  "Case": "$.assert.isNull(Response.Json['userData'])"
}
```

###### $.assert.isNotNull

Checks if a value is not null.

```js
$.assert.isNotNull(value);
```

**Example:**

```json
{
  "Title": "Check if user data is not null",
  "Case": "$.assert.isNotNull(Response.Json['userData'])"
}
```

###### $.assert.isEmpty

Checks if a collection is empty.

```js
$.assert.isEmpty(collection);
```

**Example:**

```json
{
  "Title": "Check if items array is empty",
  "Case": "$.assert.isEmpty(Response.Json['items'])"
}
```

###### $.assert.isNotEmpty

Checks if a collection is not empty.

```js
$.assert.isNotEmpty(collection);
```

**Example:**

```json
{
  "Title": "Check if items array is not empty",
  "Case": "$.assert.isNotEmpty(Response.Json['items'])"
}
```

###### $.assert.isArray

Checks if a value is an array.

```js
$.assert.isArray(value);
```

**Example:**

```json
{
  "Title": "Check if items is an array",
  "Case": "$.assert.isArray(Response.Json['items'])"
}
```

###### $.assert.isObject

Checks if a value is an object.

```js
$.assert.isObject(value);
```

**Example:**

```json
{
  "Title": "Check if user data is an object",
  "Case": "$.assert.isObject(Response.Json['userData'])"
}
```

###### $.assert.isString

Checks if a value is a string.

```js
$.assert.isString(value);
```

**Example:**

```json
{
  "Title": "Check if name is a string",
  "Case": "$.assert.isString(Response.Json['name'])"
}
```

###### $.assert.isNumber

Checks if a value is a number.

```js
$.assert.isNumber(value);
```

**Example:**

```json
{
  "Title": "Check if age is a number",
  "Case": "$.assert.isNumber(Response.Json['age'])"
}
```

###### $.assert.isBoolean

Checks if a value is a boolean.

```js
$.assert.isBoolean(value);
```

**Example:**

```json
{
  "Title": "Check if isActive is a boolean",
  "Case": "$.assert.isBoolean(Response.Json['isActive'])"
}
```

###### $.assert.isGreaterThan

Checks if a value is greater than another.

```js
$.assert.isGreaterThan(actualValue, expectedValue);
```

**Example:**

```json
{
  "Title": "Check if user age is greater than 18",
  "Case": "$.assert.isGreaterThan(Response.Json['age'], 18)"
}
```

###### $.assert.isLessThan

Checks if a value is less than another.

```js
$.assert.isLessThan(actualValue, expectedValue);
```

**Example:**

```json
{
  "Title": "Check if user age is less than 65",
  "Case": "$.assert.isLessThan(Response.Json['age'], 65)"
}
```

###### $.assert.isGreaterThanOrEqual

Checks if a value is greater than or equal to another.

```js
$.assert.isGreaterThanOrEqual(actualValue, expectedValue);
```

**Example:**

```json
{
  "Title": "Check if user age is greater than or equal to 18",
  "Case": "$.assert.isGreaterThanOrEqual(Response.Json['age'], 18)"
}
```

###### $.assert.isLessThanOrEqual

Checks if a value is less than or equal to another.

```js
$.assert.isLessThanOrEqual(actualValue, expectedValue);
```

**Example:**

```json
{
  "Title": "Check if user age is less than or equal to 65",
  "Case": "$.assert.isLessThanOrEqual(Response.Json['age'], 65)"
}
```

###### $.assert.isBetween

Checks if a value is between two other values.

```js
$.assert.isBetween(actualValue, lowerBound, upperBound);
```

**Example:**

```json
{
  "Title": "Check if user age is between 18 and 65",
  "Case": "$.assert.isBetween(Response.Json['age'], 18, 65)"
}
```

###### $.assert.isNotBetween

Checks if a value is not between two other values.

```js
$.assert.isNotBetween(actualValue, lowerBound, upperBound);
```

**Example:**

```json
{
  "Title": "Check if user age is not between 18 and 65",
  "Case": "$.assert.isNotBetween(Response.Json['age'], 18, 65)"
}
```

###### $.assert.contains

Checks if a collection contains a specific value.

```js
$.assert.contains(collection, item);
```

**Example:**

```json
{
  "Title": "Check if items array contains 'item1'",
  "Case": "$.assert.contains(Response.Json['items'], 'item1')"
}
```

###### $.assert.notContains

Checks if a collection does not contain a specific value.

```js
$.assert.notContains(collection, item);
```

**Example:**

```json
{
  "Title": "Check if items array does not contain 'item2'",
  "Case": "$.assert.notContains(Response.Json['items'], 'item2')"
}
```

###### $.assert.matchesRegex

Checks if a string matches a regular expression.

```js
$.assert.matchesRegex(value, pattern);
```

**Example:**

```json
{
  "Title": "Check if email matches pattern",
  "Case": "$.assert.matchesRegex(Response.Json['email'], '^[\\w.-]+@[\\w.-]+\\.\\w+$')"
}
```

###### $.assert.notMatchesRegex

Checks if a string does not match a regular expression.

```js
$.assert.notMatchesRegex(value, pattern);
```

**Example:**

```json
{
  "Title": "Check if username does not match pattern",
  "Case": "$.assert.notMatchesRegex(Response.Json['username'], '^admin')"
}
```

### Manage Data and Expressions

When managing data in your API tests, you can use dynamic placeholders and expressions to make your tests more flexible and powerful. This allows you to reference environment variables, custom variables, and even generate fake data on the fly. You can use curly braces `{{ }}` to denote these placeholders.

#### Environment and Variable Placeholders

You can reference both environment variables and custom variables in your placeholders:

- `env`: Retrieves the value of an environment variable by its `KEY`.
- `vars`: Lets you define or override custom variables for your requests or tests, for example: `--vars "key1=value1;key2=value2"`. These variables are merged with the current environment and can be accessed using `{{ env.key1 }}` or `{{ env.key2 }}` in your placeholders.

For example:

```json
{
  "userId": "{{ env.USER_ID }}",
  "token": "{{ env.AUTH_TOKEN }}"
}
```

To know more about environment variables, see the [Environment Variables](/docs/variables#environment-variables) section.

#### Faker Data

You can generate fake data on the fly using [FakerJS](https://fakerjs.dev/). Use the following syntax:

- `{# $.faker.<method>() #}`: Calls a Faker method to generate data.

Examples:

```json
{
  "name": "{# $.faker.name.fullName() #}",
  "email": "{# $.faker.internet.email() #}"
}
```

#### Expression Evaluation

You can use expressions to transform or compute values dynamically with the pipe operator:

You can execute any ES6(JavaScript) expression on the fly using this syntax:

##### Examples

```json
{
  "timestamp": "{# $.faker.date.past().toISOString() #}",
  "upperCaseName": "{# $.faker.name.firstName().toUpperCase() #}"
}
```

In these examples:

- `$.faker.date.past().toISOString()` formats the current date.
- `$.faker.name.firstName().toUpperCase()` generates a random first name and converts it to uppercase.

This allows you to compose dynamic values and perform inline data manipulation within your API test definitions.

> **Note:**
> 
> - `{{ vars }}` and `{# expression #}` serve different purposes:
> - Use `{{ vars }}` to insert values from custom variables you supply. `vars` are not an expression but a direct reference to a variable's value.
> - Use `{# expression #}` to evaluate and insert the result of a runtime expression or code.

## Tests Runner

The Tests Runner is a command-line utility for executing API tests defined in Apify projects. It supports running tests against both live APIs and mock servers, and generates detailed result reports. By default, all tests are executed in the specified environment, or in the default environment if none is provided.

```bash
apify tests
```

### Command Options

- **`--env`**: Runs tests in the specified environment (e.g., `Development`, `Staging`, `Production`). Defaults to the environment set in `apify-config.json` if omitted.
- **`--vars`**: Lets you define or override custom variables for your requests or tests, for example: `--vars "key1=value1;key2=value2"`. These variables are merged with the current environment and can be accessed using `{{ vars.key1 }}` or `{{ vars.key2 }}` in your placeholders.
- **`--dir`**: Sets the directory containing the tests. Uses the default directory if not specified.
- **`--debug`**: Activates debug mode for more detailed logging during test execution.
- **`--verbose`**: Produces verbose output with comprehensive test execution logs.

## Mock Server

Apify includes an integrated mock server to simulate API endpoints for development and testing. This allows you to work on your application without needing a live backend, or to simulate specific API behaviors for testing edge cases.

### Features

- **Dynamic & Conditional Responses**: Define mock responses based on request parameters, headers, or body content.
- **Template Variables**: Use built-in (random data, timestamps) and custom (request-derived) variables in mock responses.
- **File-based Configuration**: Manage mock definitions in simple `.mock.json` files.

### Create Mock Definition

Creates a new mock API definition file interactively.

```bash
apify create:mock <file> [--force]
```

- `<file>`: (Required) The file path for the new mock API definition (e.g., `users.get` becomes `.apify/users/get.mock.json`). The `.mock.json` extension is added automatically.
- `--force`: Overwrite if the file already exists.
- Prompts for mock name, HTTP method, endpoint path, status code, content type, response body, headers, and conditional responses.

### Mock API Definitions (`.mock.json`)

Mock APIs are defined in `.mock.json` files (e.g., `.apify/mocks/users/get-user-by-id.mock.json`).

Structure:

```json
{
  "Name": "Mock User by ID",
  "Method": "GET",
  "Endpoint": "/api/users/{id}", // Path parameters with :param or {param}
  "Responses": [
    {
      "Condition": "$.path.id == 1", // ES6(JavaScript) condition
      "StatusCode": 200,
      "Headers": {
        "X-Source": "Mock-Conditional-User1"
      },
      "ResponseTemplate": {
        "id": 1,
        "name": "John Doe (Mocked)",
        "email": "john.mock@example.com",
        "requested_id": "{{path.id}}",
        "random_code": "{# $.faker.number.int({min: 1000, max: 9999}) #}" // Random number
      }
    },
    {
      "Condition": "$.query.type == \"admin\" && $.headers[\"X-Admin-Token\"] == \"SUPER_SECRET\"",
      "StatusCode": 200,
      "ResponseTemplate": {
        "id": "{{path.id}}",
        "name": "Admin User (Mocked)",
        "email": "admin.mock@example.com",
        "role": "admin",
        "token_used": "{{header.X-Admin-Token}}",
        "uuid": "{# $.faker.string.uuid() #}"
      }
    },
    {
      "Condition": "$.body.status == \"pending\"", // Example for POST/PUT
      "StatusCode": 202,
      "ResponseTemplate": {
        "message": "Request for user {{path.id}} with status 'pending' accepted.",
        "received_payload": "" // Full request body
      }
    },
    {
      "Condition": "default", // Default response if no other conditions match
      "StatusCode": 404,
      "ResponseTemplate": {
        "error": "User not found",
        "id_searched": "{{path.id}}"
      }
    }
  ]
}
```

- **`Endpoint`**: The URL path for the mock. Supports path parameters like `/users/:id` or `/users/{id}`.
- **`Responses`**: An array of conditional response objects. They are evaluated in order.
  - **`Condition`**: A ES6(JavaScript) expression to determine if this response should be used.
    - Access request data:
      - `path.paramName` (e.g., `path.id`)
      - `query.paramName` (e.g., `query.page`)
      - `headers.HeaderName` (e.g., `headers.Authorization`, case-insensitive)
      - `body.fieldName` (e.g., `body.username`, for JSON bodies)
    - `default` can be used for a default fallback response.
  - **`StatusCode`**: The HTTP status code to return.
  - **`Headers`**: An object of response headers.
  - **`ResponseTemplate`**: The body of the response. Can be a JSON object or a string.

### Mock Server Command

Starts a local API mock server using mock definition files.

```bash
apify server:mock [--port <port_number>] [--directory <path_to_mocks>] [--verbose]
```

- `--port <port_number>`: Port for the mock server (default: from `apify-config.json` or 1988).
- `--directory <path_to_mocks>`: Directory containing mock definition files (default: `.apify`).
- `--verbose` or `-v`: Enable verbose logging for the mock server.

> Reads configuration from the `MockServer` block in `apify-config.json` but command-line options take precedence.

### Create Mock Definition

Interactively creates a new mock API definition file. This command guides you through a step-by-step process where you specify details such as the API endpoint, HTTP methods, request parameters, and example responses. Once completed, a mock definition file is generated, which can be used to simulate API behavior for testing and development purposes.

```bash
apify create:mock <file> [--force]
```

- `<file>`: (Required) The file path for the new mock API definition (e.g., `users.get` becomes `.apify/users/get.mock.json`). The `.mock.json` extension is added automatically.
- `--force`: Overwrite if the file already exists.
- Prompts for mock name, HTTP method, endpoint path, status code, content type, response body, headers, and conditional responses.

#### Example

```bash
apify create:mock users.get --prompt --force
```

#### Command Arguments

This command requires a single argument:

- `<file>`: The path where the mock definition file will be created. It should follow the format of `<directory>.<schema>`, which translates to `.apify/<directory>/<schema>.mock.json`.

#### Command Options

Here are the options you can specify when running the command:

- **`--name`**: The name of the mock API. This is a required field and will be used to identify the mock in the system.
- **`--method`**: The HTTP method for the mock API (e.g., GET, POST, PUT, DELETE). This is required and must be a valid HTTP method.
- **`--endpoint`**: The endpoint path for the mock API (e.g., `/api/users/{id}`). This is required and can include path parameters.
- **`--content-type`**: The content type of the response (e.g., `application/json`). This is required and should match the expected response format.
- **`--status-code`**: The HTTP status code for the response (e.g., 200, 404). This is required and must be a valid HTTP status code.
- **`--response-body`**: The body of the response, which can be a JSON object or a string. This is required and should match the content type specified.
- **`--force`**: Overwrite if the file already exists.
- **`--prompts`**: If set, the command will prompt for additional details interactively, such as headers and conditional responses.
- **`--debug`**: Enable debug mode to log additional information during the command execution.

### Mock API Definitions (`.mock.json`)

Mock APIs are defined in `.mock.json` files, typically located within the `.apify` directory of your project. Each file represents a single mock endpoint and follows a naming convention based on the API resource and operation (for example, `.apify/users/get-user-by-id.mock.json`).

```json
{
  "name": "Get post by ID",
  "method": "GET",
  "endpoint": "/api/posts/{postId}",
  "responses": [
    {
      "condition": "$.path.postId == '1'",
      "statusCode": 200,
      "headers": {
        "X-Source": "Mock-Conditional-User1"
      },
      "responseTemplate": {
        "id": 1,
        "name": "{{ $.faker.person.fullName() }}",
        "email": "{{ $.faker.internet.email() }}",
        "requested_id": "{{ $.path.postId }}",
        "random_code": "{{ $.faker.number.int({ min: 1000, max: 9999 }) }}"
      }
    },
    {
      "condition": "$.query.type == 'admin' && $.headers['x-requested-with'] == 'XMLHttpRequest'",
      "statusCode": 200,
      "responseTemplate": {
        "id": "{{ $.path.postId }}",
        "name": "Admin User (Mocked)",
        "email": "admin.mock@example.com",
        "role": "admin",
        "requested_id": "{{ $.headers['x-requested-with'] }}",
        "uuid": "{{ $.faker.string.uuid() }}"
      }
    },
    {
      "condition": "default",
      "statusCode": 404,
      "responseTemplate": {
        "error": "User not found",
        "id_searched": "{{ $.path.postId }}"
      }
    }
  ]
}
```

#### Fields Explained

- **`Name`**: The name of the mock API (e.g., "Get User by ID").
- **`Description`**: Optional description of the mock API.
- **`Method`**: The HTTP method for the mock API (e.g., GET, POST).
- **`Endpoint`**: The endpoint path for the mock API, which can include path parameters (e.g., `/api/users/{id}`).
- **`Responses`**: An array of response definitions, each containing:
  - **`Condition`**: A condition that determines when this response should be used. It can reference path parameters, query parameters, headers, or body content.
  - **`StatusCode`**: The HTTP status code for the response (e.g., 200, 404).
  - **`Headers`**: Optional headers to include in the response.
  - **`ResponseTemplate`**: The body of the response, which can include dynamic values using expressions (e.g., `{{expr|> Faker.Name.FullName()}}`).

#### Path Parameters

Path parameters in the `Endpoint` field can be defined using either `{param}` syntax. For example:

- `GET /api/users/{userId}`
- `GET /api/posts/{postId}/comments/{commentId}`
  You can reference path parameters in both conditions and response templates:

- In **conditions** or expression blocks (e.g., `Condition` or `{{ expr|> ... }}`), access path parameters using `path["paramName"]` (for example, `path["userId"]`).
- In **replacement templates**, use the double curly braces syntax: `{{path.paramName}}` (for example, `{{path.userId}}`).

#### Available Variables

You can use the following variables in your mock definitions:

- **`path`**: Contains path parameters from the request URL.
- **`query`**: Contains query parameters from the request URL.
- **`headers`**: Contains HTTP headers from the request.
- **`body`**: Contains the request body (for POST, PUT, PATCH).

Access data in templates with `{{ ... }}` (dot notation)

#### Available Reserved Objects

- **`$.path`**: Represents the request path parameters.
- **`$.query`**: Represents the query string parameters.
- **`$.headers`**: Represents the request headers.
- **`$.body`**: Represents the request body.
- **`$.faker`**: Provides access to the [Faker.js](https://fakerjs.dev/) library for generating random data.

### Run Mock Server

Launches a local API mock server based on mock definition files, allowing you to simulate API endpoints for testing and development.

```bash
apify server:mock [--watch]
```

- `--watch`: Automatically reloads the server when mock definition files change.

This command reads configuration from the `MockServer` block in `apify-config.json` but command-line options take precedence.

#### Example

```bash
apify server:mock --port 3000 -w
```

#### Command Options

- **`--port <port_number>`**: Specify the port on which the mock server will run. If not provided, it defaults to the port specified in `apify-config.json` or 1988 if not set.
- **`--project <path>`**: Specify the directory where mock definition files are located. Defaults to `.apify`.
- **`--watch`**: Enable automatic reloading of the server when mock definition files change.

## Tags (Variables & Expressions)

Apify provides a powerful templating engine that allows you to use variables and expressions to make your API tests and mocks dynamic. There are two types of tags you can use:

- `{{ obj.var }}`: For referencing variables defined in your environment or project.
- `{# expression #}`: For evaluating ES6 expressions that can include variables, functions, and more.

### `{{ obj.var }}` - Variable Reference

This syntax is used to reference variables defined in your environment or project. It allows you to dynamically insert values into your API requests, headers, and response bodies.

**Example:**

```json
{
  "Name": "Get User",
  "Url": "{{env.baseUrl}}/users/{{env.userId}}",
  "Method": "GET"
}
```

In this example, `{{env.baseUrl}}` and `{{env.userId}}` will be replaced with their respective values before the request is sent.

### `{# expression #}` - Expression Evaluation

This tag is used to execute JavaScript (ES6) code. This allows you to perform complex operations, such as generating random data, performing calculations, or even making assertions.

The expression engine supports all ES6 features and provides a set of reserved objects that you can use to interact with Apify's core functionalities.

**Example:**

```json
{
  "Name": "Mock User by ID",
  "Method": "GET",
  "Endpoint": "/api/users/{id}",
  "Responses": [
    {
      "Condition": "true",
      "StatusCode": 200,
      "ResponseTemplate": {
        "id": "{# $.path.id #}",
        "name": "{# $.faker.person.fullName() #}",
        "email": "{# $.faker.internet.email() #}",
        "createdAt": "{# (new Date()).toISOString() #}"
      }
    }
  ]
}
```

In this example, the `ResponseTemplate` uses expressions to generate a random user's name and email, and to set the current date as the `createdAt` field.

#### Reserved Objects

The expression engine provides several reserved objects that you can use within your expressions:

- `$.path`: Represents the request path parameters.
- `$.query`: Represents the query string parameters.
- `$.body`: Represents the request body.
- `$.headers`: Represents the request headers.
- `$.faker`: Provides access to the [Faker.js](https://fakerjs.dev/) library for generating random data.
- `$.env`: Accesses environment variables defined in your Apify configuration.
- `$.vars`: Accesses variables defined in your Apify configuration.
- `$.request`: Represents the entire request object.
- `$.response`: Represents the response object.
- `$.assert`: Provides methods for making assertions in tests.

All reserved objects are available in the global scope of your expressions, but their availability depends on the context in which the expression is evaluated. You can leverage these objects, along with all ES6 features—such as variables, functions, and more—to create dynamic and powerful API tests and mocks in Apify.

> All reserved objects are accessible under the `apify` root object. The `$` symbol serves as a convenient alias for `apify`, so you can use either `apify` or `$` to reference these objects in your expressions.