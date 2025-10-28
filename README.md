# Auth0 FGA – Temporary Access Example

This example demonstrates how to handle temporary access using Auth0 Fine-Grained Authorization (FGA), including detection of duplicate tuple errors when re-applying access after expiration.

## Purpose
- Grant a temporary role to a user.
- Let the access expire.
- Attempt to re-grant the same role.
- Capture the FGA validation error.
- Parse the error message `"tuple to be written already existed"`.
- Resolve it by deleting the existing tuple before retrying.

## Flow Summary
1. Add temporary access using `WriteTuple` with a condition.
2. Wait until duration expires.
3. FGA denies access but tuple still exists.
4. Write attempt throws validation error.
5. Error message is parsed to detect duplicate.
6. Old tuple is deleted, and access can be re-applied.

## Configuration
All required FGA values must be provided using *appsettings.json* or *User Secrets* under the section:

```json
"ClientConfiguration": {
  "ApiUrl": "",
  "StoreId": "",
  "AuthorizationModelId": "",
  "Credentials": {
    "Config": {
      "ApiTokenIssuer": "",
      "ApiAudience": "",
      "ClientId": "",
      "ClientSecret": ""
    }
  }
}

Files Included

    Program.cs

    Auth0FgaService.cs

    TestScenario.cs

    Models.cs

    ObjectFormatter.cs

    Config/ClientConfiguration.cs

Key Error Handling Snippet

if (errorMessage.IndexOf("tuple to be written already existed", StringComparison.OrdinalIgnoreCase) >= 0)
{
    // Handle duplicate tuple
    await RemoveTemporaryUserFrom(...);
}

Requirements

    .NET 8+

    OpenFGA SDK

    Auth0 FGA Store and Authorization Model configured

    This example is intentionally minimal to focus on handling the duplicate tuple scenario caused by temporary access re-application.


---

### Næste trin:
Vil du have, at jeg samler alle filerne i én struktur med filnavne præcis som de skal indtastes i din gist editor?  
Så du blot kan copy/paste én efter én?
