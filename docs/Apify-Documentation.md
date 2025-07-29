# Apify

A powerful C# CLI application for comprehensive API testing and mocking, enabling developers to streamline API validation and development workflows with rich configuration and execution capabilities.

## Features

- **Comprehensive API Testing**: Define and run detailed API tests.
    - **Multiple Request Methods**: Support for GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS.
    - **Rich Payload Types**: JSON, Text, Form Data.
    - **File Upload Support**: Test multipart/form-data requests with file uploads.
    - **Detailed Assertions**: Validate response status, headers, body content (JSON properties, arrays, values), and response time.
- **Integrated Mock Server**: Simulate API endpoints for development and testing.
    - **Dynamic & Conditional Responses**: Define mock responses based on request parameters, headers, or body content.
    - **Template Variables**: Use built-in (random data, timestamps) and custom (request-derived) variables in mock responses.
    - **File-based Configuration**: Manage mock definitions in simple `.mock.json` files.
- **Environment Management**: Use different configurations for development, staging, production, etc.
    - **Variable Overriding**: Project, Environment, and Request-level variable support with clear precedence.
- **User-Friendly CLI**:
    - **Interactive Creation**: Commands to interactively create test and mock definitions.
    - **Detailed Reports**: Comprehensive output with request, response, and assertion details.
    - **Visual Progress Indicators**: Animated progress display for running multiple tests.
- **Deployment & Extensibility**:
    - **Single File Deployment**: Simplified deployment as a single executable file.
    - **.NET 8.0 & .NET 9 Ready**: Built with .NET 8.0, with automatic multi-targeting for .NET 9.0 when available.

## Getting Started

### Prerequisites

- .NET 8.0 SDK (required)
- .NET 9.0 SDK (optional, for building with .NET 9.0 when available)

### Installation

The easiest way to get Apify is to download the pre-built executable from the [GitHub Releases](https://github.com/nahid/apify/releases) page.

1.  Go to the [latest release](https://github.com/nahid/apify/releases/latest).
2.  Download the appropriate `.zip` file for your operating system and architecture (e.g., `apify-win-x64.zip` for Windows 64-bit, `apify-linux-x64.zip` for Linux 64-bit, `apify-osx-arm64.zip` for macOS ARM64).
3.  Extract the contents of the `.zip` file to a directory of your choice (e.g., `C:\Program Files\Apify` on Windows, `/opt/apify` on Linux/macOS).
4.  Add the directory where you extracted Apify to your system's PATH environment variable. This allows you to run `apify` from any terminal.

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

### Project Initialization (`apify init`)

To start using Apify in your project, navigate to your desired project directory and run:

```bash
apify init
```

This command interactively guides you through the initial setup, prompting for:
- **Project Name**: A descriptive name for your API testing project.
- **Default Environment Name**: The name for your primary testing environment (e.g., "Development", "Local").
- **Additional Environment Variables**: You'll be asked if you want to configure global variables that apply across all environments (e.g., `baseUrl`, `apiToken`).
- **Additional Environments**: You can define more environments (e.g., "Staging", "Production") and their specific variables.
- **Sample Mock API Definitions**: Option to generate example mock files.

Upon successful initialization, Apify creates the following:
- `apify-config.json`: The central configuration file for your project. This file stores project metadata, environment definitions, and mock server settings.
- `.apify/`: A dedicated directory where your API test definitions (`.json` files) and mock API definitions (`.mock.json` files) will reside.
- Sample API test files (e.g., `users/get.json`, `users/create.json`) within the `.apify/` directory, demonstrating basic GET and POST requests with assertions.
- If you opt to create sample mocks, a `user.mock.json` file will be generated in `.apify/users/`, showcasing conditional mock responses.

**Example Interactive Session:**

```
$ apify init
Initializing API Testing Project
? Enter project name: MyAwesomeApiProject
? Enter default environment name [Development]:
? Configure additional environment variables? (y/N): y
Enter environment variables (empty name to finish):
? Variable name: baseUrl
? Value for baseUrl: https://api.example.com/v1
? Variable name: apiToken
? Value for apiToken: your-secret-token
? Variable name:
? Add additional environments? (y/N): y
? Environment name (empty to finish): Staging
? Description for Staging [Staging environment]:
? baseUrl for Staging [https://api.example.com/v1]: https://staging.example.com/v1
? apiToken for Staging [your-secret-token]: staging-secret-token
? Environment name (empty to finish):
? Create sample mock API definitions? (y/N): y
✓ Created sample API test: .apify/users/get.json
✓ Created sample POST API test: .apify/users/create.json
✓ Created sample mock API definitions in .apify/users
✓ Created configuration file: apify-config.json

Project initialized successfully!

🚀 Quick Start Guide
... (rest of the quick start guide)
```

You can also initialize a project non-interactively by providing options:

```bash
apify init --name "My Headless Project" --mock --force
```
- `--name <project_name>`: Specifies the project name directly.
- `--mock`: Automatically creates sample mock API definitions.
- `--force`: Overwrites existing `apify-config.json` and `.apify/` directory if they exist, without prompting.

This command is crucial for setting up your testing workspace and provides a foundational `apify-config.json` with sensible defaults and example API definitions.

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

- **`Name`, `Description`**: Project metadata.
- **`DefaultEnvironment`**: The environment used if `--env` is not specified.
- **`Variables` (Project-Level)**: Key-value pairs available across all tests and environments unless overridden.
- **`Environments`**: An array of environment objects.
    - **`Name`**: Unique name for the environment (e.g., "Development", "Staging").
    - **`Variables`**: Key-value pairs specific to this environment. These override project-level variables.
- **`MockServer`**: Configuration for the mock server.
    - **`Port`**: Port for the mock server.
    - **`Verbose`**: Enable verbose logging for the mock server.
    - **`EnableCors`**: Enable CORS headers (defaults to allow all).
    - **`DefaultHeaders`**: Headers to be added to all mock responses.

### API Test Definitions (`.json`)

API tests are defined in JSON files (e.g., `.apify/users/get-users.json`).

Structure:
```json
{
  "Name": "Get All Users",
  "Description": "Fetches the list of all users",
  "Url": "{{baseUrl}}/users?page={{defaultPage}}",
  "Method": "GET",
  "Headers": {
    "Accept": "application/json",
    "X-Api-Key": "{{apiKey}}"
  },
  "Body": null,
  "PayloadType": "none", // "json", "text", "formData"
  "Timeout": 30000, // Optional, in milliseconds
  "Tags": ["users", "smoke"], // Optional, for filtering tests
  "Variables": { // Request-specific variables (highest precedence)
    "defaultPage": "1"
  },
  "Tests": [
    {
      "Title": "Status code is 200 OK",
      "Case": "{# $.assert($.response.statusCode === 200) #}"
    },
    {
      "Title": "Response body is an array",
      "Case": "{# $.assert(Array.isArray($.response.json().data)) #}"
    }
  ]
}
```

- **`Variables` (Request-Level)**: Override environment and project variables.
- **`Tags`**: Used for filtering tests with `apify tests --tag <tagname>`.
- **`Tests` (Assertions)**: A list of assertion objects.
    - **`Title`**: A descriptive name for the assertion.
    - **`Case`**: A Javascript (ES6) expression to be evaluated. The expression should return a boolean value. You can use the `# Apify

A powerful C# CLI application for comprehensive API testing and mocking, enabling developers to streamline API validation and development workflows with rich configuration and execution capabilities.

## Features

- **Comprehensive API Testing**: Define and run detailed API tests.
    - **Multiple Request Methods**: Support for GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS.
    - **Rich Payload Types**: JSON, Text, Form Data.
    - **File Upload Support**: Test multipart/form-data requests with file uploads.
    - **Detailed Assertions**: Validate response status, headers, body content (JSON properties, arrays, values), and response time.
- **Integrated Mock Server**: Simulate API endpoints for development and testing.
    - **Dynamic & Conditional Responses**: Define mock responses based on request parameters, headers, or body content.
    - **Template Variables**: Use built-in (random data, timestamps) and custom (request-derived) variables in mock responses.
    - **File-based Configuration**: Manage mock definitions in simple `.mock.json` files.
- **Environment Management**: Use different configurations for development, staging, production, etc.
    - **Variable Overriding**: Project, Environment, and Request-level variable support with clear precedence.
- **User-Friendly CLI**:
    - **Interactive Creation**: Commands to interactively create test and mock definitions.
    - **Detailed Reports**: Comprehensive output with request, response, and assertion details.
    - **Visual Progress Indicators**: Animated progress display for running multiple tests.
- **Deployment & Extensibility**:
    - **Single File Deployment**: Simplified deployment as a single executable file.
    - **.NET 8.0 & .NET 9 Ready**: Built with .NET 8.0, with automatic multi-targeting for .NET 9.0 when available.

## Getting Started

### Prerequisites

- .NET 8.0 SDK (required)
- .NET 9.0 SDK (optional, for building with .NET 9.0 when available)

### Installation

The easiest way to get Apify is to download the pre-built executable from the [GitHub Releases](https://github.com/nahid/apify/releases) page.

1.  Go to the [latest release](https://github.com/nahid/apify/releases/latest).
2.  Download the appropriate `.zip` file for your operating system and architecture (e.g., `apify-win-x64.zip` for Windows 64-bit, `apify-linux-x64.zip` for Linux 64-bit, `apify-osx-arm64.zip` for macOS ARM64).
3.  Extract the contents of the `.zip` file to a directory of your choice (e.g., `C:\Program Files\Apify` on Windows, `/opt/apify` on Linux/macOS).
4.  Add the directory where you extracted Apify to your system's PATH environment variable. This allows you to run `apify` from any terminal.

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

### Project Initialization (`apify init`)

To start using Apify in your project, navigate to your desired project directory and run:

```bash
apify init
```

This command interactively guides you through the initial setup, prompting for:
- **Project Name**: A descriptive name for your API testing project.
- **Default Environment Name**: The name for your primary testing environment (e.g., "Development", "Local").
- **Additional Environment Variables**: You'll be asked if you want to configure global variables that apply across all environments (e.g., `baseUrl`, `apiToken`).
- **Additional Environments**: You can define more environments (e.g., "Staging", "Production") and their specific variables.
- **Sample Mock API Definitions**: Option to generate example mock files.

Upon successful initialization, Apify creates the following:
- `apify-config.json`: The central configuration file for your project. This file stores project metadata, environment definitions, and mock server settings.
- `.apify/`: A dedicated directory where your API test definitions (`.json` files) and mock API definitions (`.mock.json` files) will reside.
- Sample API test files (e.g., `users/get.json`, `users/create.json`) within the `.apify/` directory, demonstrating basic GET and POST requests with assertions.
- If you opt to create sample mocks, a `user.mock.json` file will be generated in `.apify/users/`, showcasing conditional mock responses.

**Example Interactive Session:**

```
$ apify init
Initializing API Testing Project
? Enter project name: MyAwesomeApiProject
? Enter default environment name [Development]:
? Configure additional environment variables? (y/N): y
Enter environment variables (empty name to finish):
? Variable name: baseUrl
? Value for baseUrl: https://api.example.com/v1
? Variable name: apiToken
? Value for apiToken: your-secret-token
? Variable name:
? Add additional environments? (y/N): y
? Environment name (empty to finish): Staging
? Description for Staging [Staging environment]:
? baseUrl for Staging [https://api.example.com/v1]: https://staging.example.com/v1
? apiToken for Staging [your-secret-token]: staging-secret-token
? Environment name (empty to finish):
? Create sample mock API definitions? (y/N): y
✓ Created sample API test: .apify/users/get.json
✓ Created sample POST API test: .apify/users/create.json
✓ Created sample mock API definitions in .apify/users
✓ Created configuration file: apify-config.json

Project initialized successfully!

🚀 Quick Start Guide
... (rest of the quick start guide)
```

You can also initialize a project non-interactively by providing options:

```bash
apify init --name "My Headless Project" --mock --force
```
- `--name <project_name>`: Specifies the project name directly.
- `--mock`: Automatically creates sample mock API definitions.
- `--force`: Overwrites existing `apify-config.json` and `.apify/` directory if they exist, without prompting.

This command is crucial for setting up your testing workspace and provides a foundational `apify-config.json` with sensible defaults and example API definitions.

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

- **`Name`, `Description`**: Project metadata.
- **`DefaultEnvironment`**: The environment used if `--env` is not specified.
- **`Variables` (Project-Level)**: Key-value pairs available across all tests and environments unless overridden.
- **`Environments`**: An array of environment objects.
    - **`Name`**: Unique name for the environment (e.g., "Development", "Staging").
    - **`Variables`**: Key-value pairs specific to this environment. These override project-level variables.
- **`MockServer`**: Configuration for the mock server.
    - **`Port`**: Port for the mock server.
    - **`Verbose`**: Enable verbose logging for the mock server.
    - **`EnableCors`**: Enable CORS headers (defaults to allow all).
    - **`DefaultHeaders`**: Headers to be added to all mock responses.

### API Test Definitions (`.json`)

API tests are defined in JSON files (e.g., `.apify/users/get-users.json`).

Structure:
```json
{
  "Name": "Get All Users",
  "Description": "Fetches the list of all users",
  "Url": "{{baseUrl}}/users?page={{defaultPage}}",
  "Method": "GET",
  "Headers": {
    "Accept": "application/json",
    "X-Api-Key": "{{apiKey}}"
  },
  "Body": null,
  "PayloadType": "none", // "json", "text", "formData"
  "Timeout": 30000, // Optional, in milliseconds
  "Tags": ["users", "smoke"], // Optional, for filtering tests
  "Variables": { // Request-specific variables (highest precedence)
    "defaultPage": "1"
  },
   object and its methods to perform assertions.

### Mock API Definitions (`.mock.json`)

Mock APIs are defined in `.mock.json` files (e.g., `.apify/mocks/users/get-user-by-id.mock.json`).

Structure:
```json
{
  "Name": "Mock User by ID",
  "Method": "GET",
  "Endpoint": "/api/users/:id", // Path parameters with :param or {param}
  "Responses": [
    {
      "Condition": "path.id == \"1\"", // C#-like condition
      "StatusCode": 200,
      "Headers": {
        "X-Source": "Mock-Conditional-User1"
      },
            "ResponseTemplate": {
        "id": 1,
        "name": "John Doe (Mocked)",
        "email": "john.mock@example.com",
        "requested_id": "{{path.id}}",
        "random_code": "{# $.faker.random.numeric(4) #}"
      }
    },
    {
      "Condition": "query.type == \"admin\" && header.X-Admin-Token == \"SUPER_SECRET\"",
      "StatusCode": 200,
      "ResponseTemplate": {
        "id": "{{path.id}}",
        "name": "Admin User (Mocked)",
        "email": "admin.mock@example.com",
        "role": "admin",
        "token_used": "{{header.X-Admin-Token}}",
        "uuid": "{# $.faker.datatype.uuid() #}"
      }
    },
    {
      "Condition": "body.status == \"pending\"", // Example for POST/PUT
      "StatusCode": 202,
      "ResponseTemplate": {
        "message": "Request for user {{path.id}} with status 'pending' accepted.",
        "received_payload": "{{body}}" // Full request body
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
    - **`Condition`**: A C#-like expression to determine if this response should be used.
        - Access request data:
            - `path.paramName` (e.g., `path.id`)
            - `query.paramName` (e.g., `query.page`)
            - `headers.HeaderName` (e.g., `headers.Authorization`, case-insensitive)
            - `body.fieldName` (e.g., `body.username`, for JSON bodies)
        - `default` can be used for a default fallback response.
    - **`StatusCode`**: The HTTP status code to return.
    - **`Headers`**: An object of response headers.
    - **`ResponseTemplate`**: The body of the response. Can be a JSON object or a string.
        - **Template Variables**:
            - `{{path.paramName}}`: Value of a path parameter.
            - `{{query.paramName}}`: Value of a query parameter.
            - `{{headers.HeaderName}}`: Value of a request header (case-insensitive).
            - `{{body.fieldName}}`: Value of a field from the JSON request body.
            - `{{body}}`: The full raw request body (string).
            - `{# $.faker.random.numeric(4) #}`: A random 4-digit number.
            - `{# $.faker.datatype.uuid() #}`: A random UUID.
            - `{# $.faker.date.recent() #}`: A recent date.
            - Any environment or project variable (e.g., `{{baseUrl}}`).

### Tags(Variable & Expressions)

Apify provides a powerful templating engine that allows you to use variables and expressions to make your API tests and mocks dynamic. There are two types of tags you can use:

- `{{ variable }}`: For simple variable substitution.
- `{# expression #}`: For executing JavaScript (ES6) code.

#### `{{ variable }}`

This tag is used to substitute a variable with its value. The variable can be defined in your project's `apify-config.json` file, in an environment, or in the request itself.

**Example:**

```json
{
  "Name": "Get User",
  "Url": "{{baseUrl}}/users/{{userId}}",
  "Method": "GET"
}
```

In this example, `{{baseUrl}}` and `{{userId}}` will be replaced with their respective values before the request is sent.

#### `{# expression #}`

This tag is used to execute JavaScript (ES6) code. This allows you to perform complex operations, such as generating random data, performing calculations, or even making assertions.

The expression engine supports all ES6 features and provides a set of reserved objects that you can use to interact with Apify's core functionalities.

##### Reserved Objects

The following objects are available within the expression context:

- **`$/apify`**: This object provides access to Apify's core functionalities.
- **`$.request`**: This object contains all the details of the current request, including the URL, method, headers, and body.
- **`$.response`**: This object contains all the details of the response, including the status code, headers, and body.
- **`$.assert`**: This object provides a set of assertion methods that you can use to validate the response.
- **`$.faker`**: This object provides access to the [Faker.js](https://fakerjs.dev/) library, which you can use to generate fake data.

**Example:**

```json
{
  "Name": "Create User",
  "Url": "{{baseUrl}}/users",
  "Method": "POST",
  "Body": {
    "name": "{# $.faker.name.firstName() #}",
    "email": "{# $.faker.internet.email() #}",
    "password": "{# $.faker.internet.password() #}"
  }
}
```

In this example, the `{# $.faker.name.firstName() #}`, `{# $.faker.internet.email() #}`, and `{# $.faker.internet.password() #}` expressions will be executed to generate a random name, email, and password before the request is sent.

## Commands

Apify commands are run as `apify <command> [subcommand] [options]` if installed globally, or `dotnet run -- <command> [subcommand] [options]` if run from the project directory.

### `apify init`
Initializes a new API testing project in the current directory.

```bash
apify init [--force]
```
- `--force`: Overwrite existing `apify-config.json` and `.apify` directory if they exist.
- Prompts for project name, default environment name, and other environments to create.
- Creates `apify-config.json`, `.apify/` directory with sample test and mock files.

### `apify create:request`

This command interactively guides you through the creation of a new API request definition file (`.json`). These files define the HTTP request to be made, including its URL, method, headers, body, and associated tests.

```bash
apify create:request <file_path> [--force] [--prompt]
```

- `<file_path>`: (Required) The desired path and name for your new API request file. Apify automatically resolves this path relative to your `.apify/` directory and appends the `.json` extension. For example, `users.get` will create `.apify/users/get.json`.
- `--force`: (Optional) If a file already exists at the specified `<file_path>`, this flag will force an overwrite without prompting for confirmation.
- `--prompt`: (Optional) Forces interactive prompting for all details, even if some are provided via other command-line options.

**Interactive Prompts:**
When you run `apify create:request` (especially with `--prompt` or without sufficient arguments), you'll be guided through the following:
- **API request name**: A human-readable name for your request (e.g., "Get User by ID").
- **HTTP Method**: Choose from common HTTP verbs (GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS).
- **URL**: The endpoint for your request. You can use environment variables (e.g., `{{baseUrl}}/users/{{userId}}`) and runtime variables here.
- **Add request headers?**: Option to define custom HTTP headers.
- **Add request payload?**: If the method typically includes a body (POST, PUT, PATCH), you'll be prompted to define the payload type (JSON, Text, FormData, Binary) and its content.

**Example Usage:**

To create a new GET request for fetching user details:

```bash
apify create:request users.getById --prompt
```

**Example Interactive Session:**

```
$ apify create:request users.getById --prompt
Creating New API Request
? API request name (e.g., Get User): Get User by ID
? Choose HTTP Method? [GET]: GET
? URL (e.g., {{baseUrl}}/users/{{userId}} or https://api.example.com/users): {{baseUrl}}/users/1
? Add request headers? (y/N): y
Enter headers (empty name to finish):
? Header name (e.g., Content-Type): Accept
? Value for Accept: application/json
? Header name (e.g., Content-Type):
✓ API request is successfully created to: .apify/users/getById.json
You can run it with: apify run users.getById
```

This will create a file named `.apify/users/getById.json` with content similar to this:

```json
{
  "Name": "Get User by ID",
  "Url": "{{baseUrl}}/users/1",
  "Method": "GET",
  "Headers": {
    "Accept": "application/json"
  },
  "PayloadType": "none",
  "Body": null,
  "Tests": []
}
```

**Example with POST Request and JSON Payload:**

```bash
apify create:request users.create --prompt
```

**Example Interactive Session:**

```
$ apify create:request users.create --prompt
Creating New API Request
? API request name (e.g., Get User): Create New User
? Choose HTTP Method? [GET]: POST
? URL (e.g., {{baseUrl}}/users/{{userId}} or https://api.example.com/users): {{baseUrl}}/users
? Add request headers? (y/N): y
Enter headers (empty name to finish):
? Header name (e.g., Content-Type): Content-Type
? Value for Content-Type: application/json
? Header name (e.g., Content-Type):
? Add request payload? (y/N): y
? Payload type: JSON
? Enter JSON payload: {"name": "John Doe", "job": "Software Engineer"}
✓ API request is successfully created to: .apify/users/create.json
You can run it with: apify run users.create
```

This will create a file named `.apify/users/create.json` with content similar to this:

```json
{
  "Name": "Create New User",
  "Url": "{{baseUrl}}/users",
  "Method": "POST",
  "Headers": {
    "Content-Type": "application/json"
  },
  "PayloadType": "json",
  "Body": {
    "json": {
      "name": "John Doe",
      "job": "Software Engineer"
    }
  },
  "Tests": []
}
```

### `apify create:mock`

This command interactively assists you in creating a new mock API definition file (`.mock.json`). These files define how your mock server should respond to specific requests, including conditional responses based on request parameters, headers, or body content.

```bash
apify create:mock <file_path> [--force] [--prompt] [--name <name>] [--method <method>] [--endpoint <endpoint>] [--status-code <status>] [--content-type <type>] [--response-body <body>]
```

- `<file_path>`: (Required) The desired path and name for your new mock API file. Apify automatically resolves this path relative to your `.apify/` directory and appends the `.mock.json` extension. For example, `users.getById` will create `.apify/users/getById.mock.json`.
- `--force`: (Optional) If a file already exists at the specified `<file_path>`, this flag will force an overwrite without prompting for confirmation.
- `--prompt`: (Optional) Forces interactive prompting for all details, even if some are provided via other command-line options.
- `--name <name>`: (Optional) Specifies the name of the mock API.
- `--method <method>`: (Optional) Specifies the HTTP method (e.g., GET, POST).
- `--endpoint <endpoint>`: (Optional) Specifies the endpoint path (e.g., `/api/users/{id}`).
- `--status-code <status>`: (Optional) Sets the HTTP status code for the default response.
- `--content-type <type>`: (Optional) Sets the Content-Type header for the default response.
- `--response-body <body>`: (Optional) Provides the response body for the default response.

**Interactive Prompts:**
When you run `apify create:mock` (especially with `--prompt` or without sufficient arguments), you'll be guided through the following:
- **Mock API name**: A descriptive name for your mock (e.g., "Get User by ID Mock").
- **Endpoint path**: The URL path for the mock. Supports path parameters like `/users/{id}`.
- **HTTP method**: Choose from common HTTP verbs.
- **Status Code**: The HTTP status code for the response.
- **Content Type**: The `Content-Type` header for the response.
- **Response body**: The content of the response body. For JSON, you can enter a plain JSON string.
- **Add custom response headers?**: Option to define custom HTTP headers for the mock response.
- **Add response delay?**: Option to simulate network latency.

**Example Usage:**

To create a new mock for a GET request to `/api/users/{id}`:

```bash
apify create:mock users.getById --prompt
```

**Example Interactive Session:**

```
$ apify create:mock users.getById --prompt
Creating New Mock API Response
? Mock API name (e.g., Get User): Get User by ID Mock
? Endpoint path (e.g., /api/users/1 or /users): /api/users/{id}
? HTTP method: GET
? Status Code: 200 - OK
? Content Type: application/json
? Enter JSON Body(Plain Text):
{
  "id": "{{path.id}}",
  "name": "{# $.faker.name.firstName() #} {# $.faker.name.lastName() #}",
  "email": "{# $.faker.internet.email() #}"
}
(Press Enter on an empty line to finish input)

? Add custom response headers? (y/N): n
? Add response delay (simulates latency)? (y/N): n
✓ Mock API response saved to: .apify/users/getById.mock.json
You can test it with: apify server:mock --port=1988
Then access: http://localhost:1988/api/users/1
```

This will create a file named `.apify/users/getById.mock.json` with content similar to this:

```json
{
  "Name": "Get User by ID Mock",
  "Description": "",
  "Method": "GET",
  "Endpoint": "/api/users/{id}",
  "Responses": [
    {
      "Condition": "default",
      "StatusCode": 200,
      "Headers": {},
      "ResponseTemplate": {
        "id": "{{path.id}}",
        "name": "{# $.faker.name.firstName() #} {# $.faker.name.lastName() #}",
        "email": "{# $.faker.internet.email() #}"
      }
    }
  ]
}
```

**Example with Conditional Response:**

To create a mock that responds differently based on the `id` path parameter:

```json
{
  "Name": "Mock User by ID",
  "Method": "GET",
  "Endpoint": "/api/users/{id}",
  "Responses": [
    {
      "Condition": "path.id == \"1\"",
      "StatusCode": 200,
      "ResponseTemplate": { "id": 1, "name": "Mocked Alice", "email": "alice.mock@example.com" }
    },
    {
      "Condition": "default",
      "StatusCode": 404,
      "ResponseTemplate": { "error": "User not found", "id_searched": "{{path.id}}" }
    }
  ]
}
```

In this example:
- If the `id` in the path is `1`, it returns a 200 OK with Alice's details.
- Otherwise (the `default` condition), it returns a 404 Not Found with an error message.

**Using Request Body in Conditions (for POST/PUT/PATCH mocks):**

```json
{
  "Name": "Create User Mock",
  "Method": "POST",
  "Endpoint": "/api/users",
  "Responses": [
    {
      "Condition": "body.name == \"John Doe\"",
      "StatusCode": 201,
      "ResponseTemplate": {
        "message": "User John Doe created successfully",
        "id": "{# $.faker.random.numeric(4) #}",
        "name": "{{body.name}}",
        "job": "{{body.job}}"
      }
    },
    {
      "Condition": "default",
      "StatusCode": 400,
      "ResponseTemplate": {
        "error": "Invalid request body",
        "received_body": "{{body}}"
      }
    }
  ]
}
```

This mock demonstrates:
- Accessing `body.name` from the request JSON body in the `Condition`.
- Using `{{body.name}}` and `{{body.job}}` to echo values from the request body into the response template.
- Using `{{body}}` to include the entire request body in the response for debugging or logging.


### `apify call`

This command executes an API test defined in a `.json` file. It sends the HTTP request, receives the response, and runs any defined assertions, providing detailed output based on the specified options.

```bash
apify call <file_path> [--env <environment_name>] [--vars <key=value;...>] [--tests] [--show-request] [--show-response] [--show-only-response] [--verbose]
```

- `<file_path>`: (Required) The path to your API definition file. This can be a direct path (e.g., `.apify/users/get.json`) or use dot notation (e.g., `users.get`). Apify automatically looks for files within the `.apify/` directory.
- `--env <environment_name>` or `-e`: (Optional) Specifies the environment to use for this call (e.g., "Production", "Staging"). If not provided, Apify uses the `DefaultEnvironment` specified in your `apify-config.json`.
- `--vars <key=value;...>`: (Optional) Provides runtime variables that are specific to this command execution. These variables will override any project-level or environment-level variables with the same name. Multiple variables can be separated by semicolons (e.g., `--vars "userId=123;token=abc"`).
- `--tests` or `-t`: (Optional) Forces the execution and display of tests defined within the API definition file, even if `RequestOptions.Tests` is set to `false` in `apify-config.json`.
- `--show-request` or `-sr`: (Optional) Displays the full details of the outgoing HTTP request (URL, method, headers, body) before sending it.
- `--show-response` or `-srp`: (Optional) Displays the full details of the received HTTP response (status, headers, body) after the request completes.
- `--show-only-response` or `-r`: (Optional) Displays only the response details, suppressing the request details. This option takes precedence over `--show-request`.
- `--verbose` or `-v`: (Optional) Enables verbose output, which includes detailed request and response information, as well as full test results. This option overrides `--show-request`, `--show-response`, and `--show-only-response`.

**Example Usage:**

Let's assume you have an `apify-config.json` with a `Development` environment and a `baseUrl` variable set to `https://reqres.in/api`.

And you have an API definition file `.apify/users/get.json`:

```json
{
  "Name": "Get Single User",
  "Url": "{{baseUrl}}/users/{{vars.userId}}",
  "Method": "GET",
  "Headers": {
    "Accept": "application/json"
  },
  "Tests": [
    {
      "Title": "Status code is 200 OK",
      "Case": "{# $.assert($.response.statusCode === 200) #}"
    },
    {
      "Title": "User ID matches requested ID",
      "Case": "{# $.assert($.response.json().data.id == $.request.variables.userId) #}"
    },
    {
      "Title": "User email is present",
      "Case": "{# $.assert($.response.json().data.email !== null) #}"
    }
  ]
}
```

Now, you can call this API definition with different options:

1.  **Basic Call (using default environment and no extra variables):**
    ```bash
    apify call users.get
    ```
    This will execute the request for `https://reqres.in/api/users/` (since `userId` is not provided, it might result in an error or a list of users depending on the API).

2.  **Call with Runtime Variables:**
    ```bash
    apify call users.get --vars "userId=2"
    ```
    This will execute the request for `https://reqres.in/api/users/2`.

3.  **Call with Specific Environment and Verbose Output:**
    ```bash
    apify call users.get --env Production --vars "userId=3" --verbose
    ```
    If your `Production` environment has a different `baseUrl`, it will use that. The `--verbose` flag will show the full request and response details, along with detailed test results.

4.  **Call to Show Only Response:**
    ```bash
    apify call users.get --vars "userId=1" --show-only-response
    ```
    This will only display the HTTP response body and status, without showing the request details.

5.  **Call to Force Test Execution and Show Request:**
    ```bash
    apify call users.get --vars "userId=4" --tests --show-request
    ```
    This ensures that the tests defined in `users.get.json` are run and their results displayed, and also shows the details of the outgoing request.

### `apify tests`

This command is designed to run all API tests defined in your `.apify/` directory and its subdirectories. It provides a comprehensive overview of your API health, including individual test results and a summary.

```bash
apify tests [--env <environment_name>] [--vars <key=value;...>] [--tag <tag_name>] [--dir <directory_path>] [--verbose]
```

- `--env <environment_name>` or `-e`: (Optional) Specifies the environment to use for all tests (e.g., "Production", "Staging"). If not provided, Apify uses the `DefaultEnvironment` from `apify-config.json`.
- `--vars <key=value;...>`: (Optional) Provides runtime variables that are applied to all tests during this execution. These variables override project-level and environment-level variables. Multiple variables can be separated by semicolons.
- `--tag <tag_name>`: (Optional) Filters the tests to run. Only tests that have the specified tag in their `Tags` array within their `.json` definition will be executed.
- `--dir <directory_path>`: (Optional) Specifies the root directory to search for API test files. Defaults to `.apify/`.
- `--verbose` or `-v`: (Optional) Enables verbose output, showing detailed request/response information and individual assertion results for each test.

**How it Works:**
1.  Apify scans the specified directory (defaulting to `.apify/`) for all `.json` files that are not mock definitions (`.mock.json`).
2.  For each found API test file, it loads the definition, applies environment and runtime variables, and executes the HTTP request.
3.  All assertions defined within each API test file are run against the received response.
4.  During execution, a visual spinner indicates progress, and a summary is displayed upon completion.

**Example Usage:**

Let's assume you have the following API test files:

- `.apify/users/get.json` (with `Tags: ["smoke", "users"]`)
- `.apify/users/create.json` (with `Tags: ["users"]`)
- `.apify/products/list.json` (with `Tags: ["smoke", "products"]`)

1.  **Run all tests in the default directory:**
    ```bash
    apify tests
    ```
    This will execute `users/get.json`, `users/create.json`, and `products/list.json` using your default environment.

2.  **Run tests for a specific environment:**
    ```bash
    apify tests --env Staging
    ```
    All tests will be executed against the `Staging` environment's configurations (e.g., `baseUrl`).

3.  **Run only tests with the `smoke` tag:**
    ```bash
    apify tests --tag smoke
    ```
    This will execute `users/get.json` and `products/list.json`.

4.  **Run tests with runtime variables and verbose output:**
    ```bash
    apify tests --vars "testUser=api_tester" --verbose
    ```
    The `testUser` variable will be available in all tests, and detailed output for each request, response, and assertion will be shown.

5.  **Run tests from a custom directory:**
    ```bash
    apify tests --dir ./my-custom-tests
    ```
    Apify will look for `.json` test files within the `./my-custom-tests` directory instead of `.apify/`.

### `apify server:mock`

This command starts a local HTTP server that serves mock API responses based on your `.mock.json` definition files. It's invaluable for frontend development, testing, and scenarios where you need to simulate API behavior without a live backend.

```bash
apify server:mock [--port <port_number>] [--project <path_to_mocks>] [--verbose] [--watch] [--debug]
```

- `--port <port_number>`: (Optional) Specifies the port on which the mock server will listen. If not provided, it defaults to the `Port` setting in the `MockServer` block of your `apify-config.json` (typically 1988).
- `--project <path_to_mocks>`: (Optional) Specifies the root directory where Apify should look for `.mock.json` files. Defaults to the current directory.
- `--verbose` or `-v`: (Optional) Enables verbose logging for the mock server, showing details about incoming requests and matched mock responses.
- `--watch` or `-w`: (Optional) Watch for file changes and reload the server automatically.
- `--debug`: (Optional) Provides even more detailed debug output, useful for troubleshooting mock server behavior and condition evaluation.

**How it Works:**
1.  Apify scans the specified directory (defaulting to `.apify/`) for all `.mock.json` files.
2.  It loads and parses these files, creating a registry of mock API definitions, including their endpoints, methods, and conditional responses.
3.  When a request comes into the mock server, it attempts to match the incoming request (method, URL, headers, body) against the defined mocks.
4.  If a match is found, it evaluates the conditions within the matched mock and returns the appropriate response template, applying any dynamic variables or expressions.

**Configuration (`apify-config.json`):**

The `MockServer` section in your `apify-config.json` provides global settings for the mock server:

```json
{
  "MockServer": {
    "Port": 1988,
    "Verbose": true,
    "EnableCors": true,
    "DefaultHeaders": {
      "X-Powered-By": "Apify Mock Server",
      "Cache-Control": "no-cache"
    }
  }
}
```

- **`Port`**: The default port for the mock server.
- **`Verbose`**: Global setting to enable verbose logging for the mock server.
- **`EnableCors`**: If `true`, the mock server will add CORS headers to all responses, allowing cross-origin requests from web browsers.
- **`DefaultHeaders`**: A dictionary of headers that will be added to all mock responses by default. These can be overridden by headers defined in individual `.mock.json` files.

**Example Usage:**

1.  **Start the mock server on the default port (1988) with verbose logging:**
    ```bash
    apify server:mock --verbose
    ```
    You will see output indicating the server has started and listing the loaded mock endpoints.

2.  **Start the mock server on a custom port:**
    ```bash
    apify server:mock --port 8080
    ```
    Access your mocks at `http://localhost:8080/`.

3.  **Start the mock server from a different directory:**
    ```bash
    apify server:mock --project ./my-custom-mocks
    ```
    Apify will load `.mock.json` files from `./my-custom-mocks` instead of `.apify/`.

**Accessing Mocked Endpoints:**

Once the mock server is running, you can access your defined endpoints using tools like `curl`, Postman, Insomnia, or directly from your web browser or frontend application.

For example, if you have a mock defined with `"Endpoint": "/api/users/{id}"`:

```bash
curl http://localhost:1988/api/users/1
```

This will return the response defined in your `users.getById.mock.json` (or similar) for `id=1`.

**Troubleshooting Port Binding Issues (Windows):**

On Windows, if you encounter "Access is denied" errors when trying to bind to a port (especially ports below 1024), you might need to:
- Run your command prompt or PowerShell as an **Administrator**.
- Or, add a URL reservation (one-time setup) using an Administrator PowerShell:
  ```powershell
  netsh http add urlacl url=http://+:YOUR_PORT/ user=Everyone
  ```
  Replace `YOUR_PORT` with the port you intend to use.
- Or, use a port number above 1024 (e.g., 8080, 3000).

### `apify list-env`

This command displays a summary of all environments defined in your `apify-config.json` file, along with their associated variables. It's useful for quickly inspecting your environment configurations and ensuring variables are set as expected.

```bash
apify list-env
```

**Example Output:**

Assuming your `apify-config.json` has `Development` and `Production` environments defined:

```
$ apify list-env

Environment: Development
  Description: Development environment specific variables
  Variables:
    baseUrl: https://dev-api.myproject.com
    apiKey: dev-secret-key

Environment: Production
  Description: Production environment specific variables
  Variables:
    baseUrl: https://api.myproject.com
    apiKey: prod-secret-key

Global Project Variables:
  globalProjectVar: This variable is available in all environments and tests
```

- **Environment Variables**: Variables specific to each environment are listed under their respective environment names.
- **Global Project Variables**: Variables defined at the top-level `Variables` block in `apify-config.json` are listed separately, as they apply across all environments.

### Global Options
These options can be used with most commands.

- `--debug`: Show detailed debug output, including stack traces and internal logging. Useful for troubleshooting Apify itself.

## CI/CD with GitHub Actions

This project includes GitHub Actions workflows for continuous integration and releases.

### Continuous Integration

The CI workflow (if included in your project, typically in `.github/workflows/ci.yml`) runs on every push to the main branch and pull requests. It usually:
1. Builds the project.
2. Runs API tests (e.g., `apify tests --env ci`) to verify functionality against a deployed or mocked environment.

### Release Process

To create a release (if configured, typically in `.github/workflows/release.yml`):
1. **Tag-based release**: Create and push a new tag: `git tag -a v1.0.0 -m "Version 1.0.0" && git push origin v1.0.0`
2. **Manual release**: Via GitHub Actions UI.

This process typically builds single file executables for various platforms and attaches them to a GitHub release.

## Examples

### Example 1: Testing and Mocking a User API

1.  **Initialize Project:**
    ```bash
    apify init
    ```

2.  **Create a Mock for `GET /api/users/:id`:**
    Use `apify create:mock users.getById` and define it like this in `.apify/users/getById.mock.json`:
    ```json
    {
      "Name": "Mock User by ID",
      "Method": "GET",
      "Endpoint": "/api/users/:id",
      "Responses": [
        {
          "Condition": "path.id == \"1\"",
          "StatusCode": 200,
          "ResponseTemplate": { "id": 1, "name": "Mocked Alice", "email": "alice.mock@example.com" }
        },
        {
          "Condition": "default",
          "StatusCode": 404,
          "ResponseTemplate": { "error": "MockUser not found" }
        }
      ]
    }
    ```

3.  **Start the Mock Server:**
    ```bash
    apify server:mock --port 8080
    ```
    (In a separate terminal)

4.  **Create an API Test for `GET /api/users/1`:**
    Use `apify create:request users.getUser1` and define it in `.apify/users/getUser1.json`:
    ```json
    {
      "Name": "Get User 1",
      "Url": "{{baseUrl}}/users/1", 
      "Method": "GET",
      "Tests": [
        { "Title": "Status 200", "Case": "{# $.assert($.response.statusCode === 200) #}" },
        { "Title": "ID is 1", "Case": "{# $.assert($.response.json().id === 1) #}" },
        { "Title": "Name is Mocked Alice", "Case": "{# $.assert($.response.json().name === 'Mocked Alice') #}" }
      ]
    }
    ```

5.  **Run the Test:**
    ```bash
    apify call users.getUser1 --verbose
    # This will hit your running mock server.
    ```

## License

This project is licensed under the MIT License.