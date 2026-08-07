# Local Reproduction

To reproduce the GitHub CI failure locally, the exact sequence of CI steps was executed:

1. `dotnet restore Emcore.Platform.slnx`
2. `dotnet format Emcore.Platform.slnx --verify-no-changes`

The format check correctly failed locally with the identical error output indicating trailing whitespace on several blank lines in `gateways/Emcore.ApiGateway.Tests/GatewayTests.cs`, which were introduced during the recent Swagger CSP hotfix.
