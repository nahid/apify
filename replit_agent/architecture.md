# Architecture Overview

## 1. Overview

Apify is a command-line interface (CLI) tool built in C# with .NET 8.0 that enables developers to test, validate, and mock APIs. It follows a modular architecture with clear separation of concerns between commands, services, models, and utilities. The application is designed to be run as a standalone executable and provides functionalities similar to Postman but in a CLI environment.

The primary purpose of Apify is to allow developers to define API tests in JSON format, execute them against endpoints, and validate responses through assertions. It also includes a mock server capability to simulate API responses for testing purposes.

## 2. System Architecture

Apify follows a clean architecture pattern with the following key layers:

1. **Command Layer**: Provides the CLI interface and handles user interactions
2. **Service Layer**: Contains the core business logic for executing API requests, evaluating assertions, and running the mock server
3. **Model Layer**: Defines data structures for API definitions, test assertions, and configuration
4. **Utility Layer**: Provides helper functions and extensions for common operations

The application uses a command-based architecture powered by the System.CommandLine library, allowing for a structured CLI experience with commands, arguments, and options.

## 3. Key Components

### 3.1. Command Components

The command structure forms the user interface of the application:

- **RootCommand**: Entry point that hosts all subcommands
- **RunCommand**: Executes API tests from definition files
- **InitCommand**: Initializes a new API testing project with configuration files
- **TestsCommand**: Runs all tests in the configured directory
- **CreateRequestCommand**: Creates new API request definition files
- **MockServerCommand**: Starts a mock API server based on definition files

### 3.2. Service Components

Services encapsulate the core business logic:

- **ApiExecutor**: Handles the execution of HTTP requests based on API definitions
- **AssertionEvaluator**: Evaluates test assertions against API responses
- **EnvironmentService**: Manages environment variables and configurations
- **TestRunner**: Coordinates the execution of tests against API endpoints
- **MockServerService**: Implements a lightweight HTTP server for mocking API responses
- **ConditionEvaluator**: Evaluates conditional expressions for dynamic mock responses

### 3.3. Model Components

Models define the core data structures:

- **ApiDefinition**: Represents an API request with headers, payload, and tests
- **TestAssertion**: Defines assertions for validating API responses
- **TestResult**: Contains the result of a test execution
- **ConfigurationProfile**: Stores environment configurations
- **TestEnvironment**: Defines environment-specific variables
- **MockApiDefinition**: Describes a mock API endpoint

### 3.4. Utility Components

Utilities provide common functionality:

- **ConsoleHelper**: Formats and displays console output with colors
- **JsonHelper**: Provides JSON parsing and manipulation functions
- **Custom JSON Converters**: Handle special cases in JSON deserialization

## 4. Data Flow

The application follows a consistent flow for executing API tests:

1. **Command Parsing**: The CLI args are parsed by System.CommandLine to determine the action
2. **Environment Loading**: Configuration and environment variables are loaded
3. **Test Definition Loading**: API test definitions are loaded from JSON files
4. **Request Execution**: HTTP requests are sent to target APIs
5. **Response Processing**: API responses are parsed and prepared for assertion
6. **Assertion Evaluation**: Test assertions are evaluated against responses
7. **Result Reporting**: Test results are formatted and displayed to the user

For the mock server functionality:

1. **Definition Loading**: Mock API definitions are loaded from JSON files
2. **HTTP Server Initialization**: An HttpListener is started to handle incoming requests
3. **Request Handling**: Incoming requests are matched against mock definitions
4. **Response Generation**: Appropriate responses are generated based on conditions
5. **Response Delivery**: Mock responses are sent back to the client

## 5. External Dependencies

Apify relies on a small set of external dependencies:

1. **System.CommandLine**: Provides the command-line parsing and execution framework
2. **Newtonsoft.Json**: Used for JSON serialization/deserialization
3. **ConsoleTableExt**: Formats tabular data for console output
4. **DynamicExpresso.Core**: Enables runtime evaluation of expressions for assertions

The application intentionally limits external dependencies to maintain simplicity and reduce the attack surface.

## 6. Configuration Management

The application uses a JSON-based configuration system:

- **apify-config.json**: Stores global configuration and environment variables
- **.apify/**: Directory for storing API test and mock definitions
- **Environment Variables**: Supports variable substitution in test definitions

This configuration approach allows for:

1. **Environment Separation**: Testing against dev, staging, production environments
2. **Reusable Variables**: Defining common values once and reusing them
3. **Template Capabilities**: Using variables in requests, headers, and assertions

## 7. Testing Approach

Apify supports a comprehensive approach to API testing:

1. **Request Definition**: Define API endpoints, methods, headers, and payloads
2. **Assertion Definition**: Specify expected responses and validation rules
3. **Test Execution**: Run tests against real endpoints or mock servers
4. **Result Reporting**: Display detailed results of test execution

Assertion types include:

- Status code validation
- Response time checks
- Header validation
- JSON property existence and value checks
- Array validation
- Custom expression evaluation

## 8. Mock Server Capabilities

The built-in mock server provides capabilities for simulating API responses:

1. **Static Responses**: Pre-defined responses for specific endpoints
2. **Dynamic Templates**: Response templates with variable substitution
3. **Conditional Responses**: Different responses based on request conditions
4. **Delay Simulation**: Adding latency to simulate real-world conditions
5. **File Upload Handling**: Supporting multipart/form-data requests

## 9. Deployment Strategy

Apify is designed for flexible deployment:

1. **Single-File Executables**: Published as self-contained executables for each platform
2. **Cross-Platform Support**: Works on Windows, macOS, and Linux
3. **Native AOT Compilation**: Optional ahead-of-time compilation for improved startup time
4. **Continuous Integration**: GitHub Actions workflows for building and testing
5. **Multi-Targeting**: Supports .NET 8.0 with forward compatibility

The deployment approach prioritizes portability and ease of use, allowing the tool to be used in various environments without complex setup requirements.

## 10. Future Extensibility

The architecture supports future extensions in several areas:

1. **Additional Assertion Types**: New validation mechanisms can be added
2. **Enhanced Mock Capabilities**: More sophisticated response generation
3. **Reporting Formats**: Additional output formats beyond console display
4. **Integration with CI/CD**: Better support for automated testing pipelines
5. **Plugin System**: Potential for custom extensions and integrations

The modular design with clean separation of concerns makes these extensions straightforward to implement without major architectural changes.