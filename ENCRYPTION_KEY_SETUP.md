# Encryption Key Setup Instructions

## Important: Generate Your Own Encryption Keys

The `appsettings.json` file contains placeholder values for the AES-256 encryption keys. **You MUST generate your own keys before running in production.**

## Quick Setup for Development

### Option 1: Using dotnet user-secrets (Recommended for Development)

```bash
# Generate keys using the helper method
dotnet run --no-launch-profile --generate-keys

# Or run this C# code snippet:
var keys = JobStream.Api.Services.AesEncryptionService.GenerateKeyAndIV();
Console.WriteLine($"Key: {keys.Key}");
Console.WriteLine($"IV: {keys.IV}");

# Then store in user secrets (NOT in appsettings.json for security):
dotnet user-secrets init
dotnet user-secrets set "Encryption:Key" "YOUR_GENERATED_KEY_HERE"
dotnet user-secrets set "Encryption:IV" "YOUR_GENERATED_IV_HERE"
```

### Option 2: Update appsettings.Development.json

Create or update `appsettings.Development.json`:

```json
{
  "Encryption": {
    "Key": "YOUR_GENERATED_KEY_HERE",
    "IV": "YOUR_GENERATED_IV_HERE"
  }
}
```

**IMPORTANT**: Add `appsettings.Development.json` to `.gitignore` to prevent committing secrets!

## Production Setup

For production, **NEVER** store encryption keys in configuration files. Use one of these secure methods:

### Option 1: Azure Key Vault (Recommended)

```csharp
// In Program.cs
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-keyvault.vault.azure.net/"),
    new DefaultAzureCredential());
```

Then store keys in Azure Key Vault:
- `Encryption--Key` → Your generated key
- `Encryption--IV` → Your generated IV

### Option 2: Environment Variables

```bash
export Encryption__Key="YOUR_GENERATED_KEY_HERE"
export Encryption__IV="YOUR_GENERATED_IV_HERE"
```

### Option 3: AWS Secrets Manager

```csharp
// In Program.cs
builder.Configuration.AddSecretsManager();
```

## Generate Keys Using C# Code

You can generate keys using this code:

```csharp
using JobStream.Api.Services;

var (key, iv) = AesEncryptionService.GenerateKeyAndIV();
Console.WriteLine($"Key: {key}");
Console.WriteLine($"IV: {iv}");
```

Or use this standalone script:

```csharp
using System.Security.Cryptography;

using var aes = Aes.Create();
aes.KeySize = 256;
aes.GenerateKey();
aes.GenerateIV();

Console.WriteLine($"Key: {Convert.ToBase64String(aes.Key)}");
Console.WriteLine($"IV: {Convert.ToBase64String(aes.IV)}");
```

## Security Best Practices

1. **NEVER commit encryption keys to source control**
2. **Use different keys for each environment** (Dev, Staging, Production)
3. **Rotate keys periodically** (e.g., every 90 days)
4. **Keep a secure backup** of your production keys in a password manager
5. **Restrict access** to production keys to authorized personnel only
6. **Use Azure Key Vault or AWS Secrets Manager** for production

## What Gets Encrypted

The following sensitive data is encrypted using AES-256:
- IBAN (bank account numbers)
- Tax IDs
- VAT IDs (if configured)

## Troubleshooting

### Error: "Encryption key or IV not found in configuration"

The application will generate temporary keys and log a warning. This is OK for development but **NOT for production**.

### Error: "Encryption key must be 256 bits (32 bytes)"

Your key is not the correct length. Generate a new key using the methods above.

### Error: "Encryption IV must be 128 bits (16 bytes)"

Your IV is not the correct length. Generate a new IV using the methods above.

## Example: Complete Development Setup

```bash
# 1. Generate keys
dotnet run --project JobStream.Api -- generate-keys

# Output:
# Key: abc123...xyz789==
# IV: def456...uvw012==

# 2. Store in user secrets
cd JobStream.Api
dotnet user-secrets init
dotnet user-secrets set "Encryption:Key" "abc123...xyz789=="
dotnet user-secrets set "Encryption:IV" "def456...uvw012=="

# 3. Verify
dotnet user-secrets list

# 4. Run the application
dotnet run
```

## Verifying Encryption is Working

Check the application logs on startup:

```
[Information] AES-256 encryption service initialized successfully
```

If you see this warning, encryption keys are not configured:
```
[Warning] Encryption key or IV not found in configuration. Generating temporary keys.
[Warning] WARNING: For production, set Encryption:Key and Encryption:IV in appsettings.json or Azure Key Vault
```
