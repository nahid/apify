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


The easiest way to get Apify is to download the pre-built executable from the [GitHub Releases](https://github.com/nahid/apify/releases) page.

1.  Go to the [latest release](https://github.com/nahid/apify/releases/latest).
2.  Download the appropriate `.zip` file for your operating system and architecture (e.g., `apify-win-x64.zip` for Windows 64-bit, `apify-linux-x64.zip` for Linux 64-bit, `apify-osx-arm64.zip` for macOS ARM64).
3.  Extract the contents of the `.zip` file to a directory of your choice (e.g., `C:\Program Files\Apify` on Windows, `/opt/apify` on Linux/macOS).
4.  Add the directory where you extracted Apify to your system's PATH environment variable. This allows you to run `apify` from any terminal.

### CLI Installation

For a quick installation via your command line, use the following platform-specific instructions. Remember to replace `[DOWNLOAD_URL]` with the actual download link for your OS and architecture from the [latest GitHub release](https://github.com/nahid/apify/releases/latest).

#### Linux & macOS

```bash
# Download the appropriate zip file and extract it
curl -L [DOWNLOAD_URL] -o apify.zip
unzip apify.zip
```

This will extract the `apify` binary (and possibly other files) to your current directory.

```bash
# Make the binary executable and move it to /usr/local/bin
chmod a+x apify
sudo mv apify /usr/local/bin/
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


#### Windows (PowerShell)

1. Download the appropriate `.zip` file for Windows from the [latest release](https://github.com/nahid/apify/releases/latest) and extract it using File Explorer or a tool like WinRAR. Inside the extracted folder, you'll find `apify.exe`.

2. Create a new folder for Apify in `Program Files` (run PowerShell as Administrator):

    ```powershell
    New-Item -ItemType Directory -Force -Path "$env:ProgramFiles\Apify"
    ```

3. Move `apify.exe` to the new folder:

    ```powershell
    Move-Item -Path ".\apify\apify.exe" -Destination "$env:ProgramFiles\Apify"
    ```

4. Add Apify to your user PATH environment variable:

    ```powershell
    [Environment]::SetEnvironmentVariable("PATH", $env:PATH + ";C:\Program Files\Apify", "User")
    ```

5. Restart your terminal, then verify the installation:

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

> For documentation, please visit [Apify Documentation](https://apify.dev/docs).

## Development
To contribute to Apify or build it from source, follow these steps:

### Prerequisites

- .NET 8.0 SDK (required)
- .NET 9.0 SDK (optional, for building with .NET 9.0 when available)

### Cloning the Repository

```bash
git clone git@github.com:nahid/apify.git
```

### Building the Project
Navigate to the project directory and run:

```bash
cd apify
dotnet build
```

### Running Tests
To run the tests, use:

```bash
dotnet test
```


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


## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details. (Ensure you have a LICENSE file).
