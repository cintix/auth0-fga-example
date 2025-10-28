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
```

### Key Error Handling Snippet
```c#
if (errorMessage.IndexOf("tuple to be written already existed", StringComparison.OrdinalIgnoreCase) >= 0)
{
    // Handle duplicate tuple
    await RemoveTemporaryUserFrom(...);
}
```
