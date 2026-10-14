# OneST Take-Home Assignment

A C# implementation of the UEN format-validation service.

## Requirements

- .NET 10 SDK
- Visual Studio Code with C# Dev Kit for the instructions below

## Project Structure

- OneST: reusable UEN validation logic.
- OneST.test: automated xUnit tests.
- OneST.slnx: solution containing both projects.

## UEN Validation

Call UenValidator.IsValid(input) to receive a true or false result.

Supported formats:
- Business: eight digits followed by a letter.
- Local company: nine digits followed by a letter.
- Other entity: R, S or T; two year digits; a recognized
  two-character entity code; four sequence digits; a final letter.

Entity codes follow the table supplied in the assessment brief.

## Input Handling and Assumptions

- Null, empty and whitespace-only input is rejected.
- Surrounding whitespace is removed.
- ASCII lowercase letters are accepted and converted to uppercase.
- Remaining characters must be ASCII letters or digits.
- Internal whitespace and punctuation are rejected.
- Year fields are checked as digits, without chronological restrictions.
- The final character must be a letter, but its checksum is not verified.
- No registration lookup or issued-sequence verification is performed.

A successful result means the input matches a supported format,
not that the entity exists.

## Running Tests in VS Code

1. Open the assignment folder and load OneST.slnx.
2. Open the Command Palette and select Tasks: Run Build Task.
3. Choose the dotnet: build task for the solution.
4. Select Test: Run All Tests from the Command Palette.
5. Review the results in the Testing sidebar.

Tests cover the three formats, supported prefixes, selected entity
codes, invalid lengths, null input, whitespace and Unicode characters.

## Current Scope

UEN validation and automated tests are implemented.
The user interface and weather service are not implemented yet.

## Running the Web Portal in VS Code

1. Open the repository folder and load OneST.slnx.
2. Press Ctrl+F5 and select OneST.Web if prompted.
3. Open the local URL shown by the application.
4. Stop the application with Shift+F5 before rebuilding.

Completed UEN checks use Post/Redirect/Get to avoid repeating
submissions on refresh. Refresh clears the current result and input;
the browser-tab history remains available.