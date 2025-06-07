# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Building and Testing
- **Build entire solution**: `dotnet build TestApi.sln`
- **Run all tests**: `dotnet test TestApi.sln`
- **Run unit tests only**: `dotnet test TestApi.UnitTests/TestApi.UnitTests.csproj`
- **Run BDD tests only**: `dotnet test TestApi.BddTests/TestApi.BddTests.csproj`
- **Run single test**: `dotnet test --filter "TestName"`
- **Run API locally**: `dotnet run --project TestApi/TestApi.csproj`

### Project Structure
This is a .NET 9.0 solution with four projects:
- **TestApi**: ASP.NET Core Web API with controllers for Jobs and Math operations
- **TestSupport**: Shared library containing JSON comparison utilities and HTTP file parsing
- **TestApi.UnitTests**: xUnit unit tests using AwesomeAssertions
- **TestApi.BddTests**: Reqnroll (SpecFlow successor) BDD tests with feature files

## Architecture

### Test Support Library (TestSupport)
The TestSupport project provides reusable testing utilities:

#### JsonComparer
- **Purpose**: Compares JSON strings with support for tokenized placeholders
- **Token Format**: Use double square brackets like `[[TOKEN_NAME]]` for extraction
- **Usage**: `JsonComparer.ExactMatch(expected, actual, out tokens, out mismatches)` or `JsonComparer.SubsetMatch(...)`
- **Features**: Exact matching, subset matching, token extraction, detailed mismatch reporting

#### HttpFile Parser
- **Purpose**: Parses HTTP request files into structured data
- **Returns**: `ParsedHttpRequest` with Method, Url, Headers, and Body
- **Usage**: `await HttpFileParser.ParseAsync(stream)`

### BDD Testing Pattern
- **Framework**: Reqnroll (modern SpecFlow replacement)
- **Feature files**: Located in TestApi.BddTests/ with `.feature` extension
- **Token system**: Uses both `[[TOKEN]]` for JSON extraction and `{{TOKEN}}` for URL substitution
- **Test structure**: Given-When-Then scenarios testing HTTP API endpoints
- **Response validation**: Full HTTP response comparison including status codes, headers, and JSON bodies

### API Structure
- **Framework**: ASP.NET Core Web API (.NET 9.0)
- **Dependency Injection**: Uses built-in DI container
- **Controllers**: JobController (job management), MathController (calculations)
- **Error Handling**: Returns RFC 9110 compliant problem details for errors