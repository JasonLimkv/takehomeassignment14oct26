A .NET 10 web portal for Singapore UEN format validation and two-hour weather forecasts.

## Included
- UEN format validation with the last 10 checks
- Singapore weather forecasts with an interactive map and searchable locations
- Responsive web interface
- Unit tests and HTTP integration tests
UEN validation checks format only; registration and the correctness of the last letter are not verified

## Projects
Project | Contents 
- `OneST` | UEN validator and weather service
- `OneST.Web` | Web pages and UI assets
- `OneST.test` | Unit tests
- `OneST.IntegrationTests` | HTTP integration tests

## Requirements
- .NET 10 SDK
- Visual Studio Code with Microsoft's C# Dev Kit
- A modern browser with JavaScript enabled
- NuGet package access or a populated package cache for restore
- Weather API access from the running application for live forecasts

## Run in VS Code
1. Open the repository root folder
2. Load `OneST.slnx` when prompted and allow dependencies to restore
3. Open the Command Palette with **Ctrl+Shift+P**, select **Tasks: Run Build Task**, and choose **dotnet: build** for the solution
4. Press **Ctrl+F5** and select **OneST.Web** if prompted
5. Open the URL shown by the application

Depending on the selected launch profile, the app runs at:
- **HTTP:** [localhost:5128](http://localhost:5128)
- **HTTPS:** [localhost:7138](https://localhost:7138)

Use the address shown when the app starts. HTTPS requires a trusted development certificate
Use VS Code's **Stop** control to stop the application before rebuilding

## Run Tests

Only trust a project after reviewing its source and deciding you trust the code.
When opening the folder for the first time, select **Yes, I trust the authors** in the trust prompt. Leave any option to trust the parent folder unchecked.
If VS Code is already in **Restricted Mode**:
1. Click **Manage** in the top Restricted Mode banner, or click **Restricted Mode** at the bottom-left of VS Code
2. Select **Trust** in the Workspace Trust screen and confirm if prompted. Trust only this project folder, not its parent folder
3. Reload the window if prompted, then wait for C# Dev Kit to load the projects and restore dependencies

Open the **Testing** sidebar using the flask-shaped icon on the left and select **Run All Tests** at the top of the panel.

Alternatively:
1. Press **Ctrl+Shift+P**
2. Type `Test: Run All Tests`
3. Select the command and press **Enter**

### Test Summary
67 automated test cases: 42 unit tests and 25 HTTP integration tests
- UEN: valid and invalid formats, input length, whitespace, letter case and unsupported characters
- Weather: forecast data, location selection, expired or future forecasts, API failures, timeouts and cancellation
- HTTP: page routes, UEN form submissions and weather JSON responses
- Security: missing or invalid antiforgery tokens, HTML encoding and safe production error messages
- Map: embedded forecasts and coordinates, local asset references and handling missing data

Weather responses are simulated, so the tests do not depend on the live weather API. These tests do not run browser JavaScript.

## Data Sources
- **Weather:** [NEA via data.gov.sg](https://data.gov.sg/datasets/d_3f9e064e25005b0e42969944ccaf2e7a/view)
- **Map:** [URA planning-area boundaries](https://data.gov.sg/datasets/d_2cc750190544007400b2cfd5d7f53209/view)
- **Data licence:** [Singapore Open Data Licence](https://data.gov.sg/open-data-licence)
Licence notices for bundled UI assets are included with their files.
