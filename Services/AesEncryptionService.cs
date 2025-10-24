using System.Security.Cryptography;
using System.Text;

namespace JobStream.Api.Services;

/// <summary>
/// AES-256 encryption service for securing sensitive data like IBAN and Tax IDs
/// </summary>
public class AesEncryptionService : IEncryptionService
{
    private readonly ILogger<AesEncryptionService> _logger;
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesEncryptionService(ILogger<AesEncryptionService> logger, IConfiguration configuration)
    {
        _logger = logger;

        // Get encryption key and IV from configuration
        // In production, these should be stored in Azure Key Vault or similar secure storage
        var keyString = configuration.GetValue<string>("Encryption:Key");
        var ivString = configuration.GetValue<string>("Encryption:IV");

        // Check if keys are valid Base64 strings (not placeholders)
        bool hasValidKey = !string.IsNullOrEmpty(keyString) &&
                          !keyString.Contains("PLEASE_GENERATE") &&
                          IsValidBase64(keyString);
        bool hasValidIV = !string.IsNullOrEmpty(ivString) &&
                         !ivString.Contains("PLEASE_GENERATE") &&
                         IsValidBase64(ivString);

        if (!hasValidKey || !hasValidIV)
        {
            _logger.LogWarning("Encryption key or IV not found in configuration or contains placeholder values.");
            _logger.LogWarning("WARNING: Generating temporary keys for this session. For production, set proper Encryption:Key and Encryption:IV in appsettings.json or Azure Key Vault");

            // Generate temporary keys for development (NOT for production!)
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.GenerateKey();
            aes.GenerateIV();

            _key = aes.Key;
            _iv = aes.IV;

            _logger.LogWarning("=== COPY THESE VALUES TO YOUR appsettings.Development.json ===");
            _logger.LogWarning("Encryption:Key = {Key}", Convert.ToBase64String(_key));
            _logger.LogWarning("Encryption:IV = {IV}", Convert.ToBase64String(_iv));
            _logger.LogWarning("================================================================");
        }
        else
        {
            try
            {
                // At this point, we've already validated these are not null/empty via hasValidKey/hasValidIV
                _key = Convert.FromBase64String(keyString!);
                _iv = Convert.FromBase64String(ivString!);

                // Validate key size
                if (_key.Length != 32) // 256 bits = 32 bytes
                {
                    throw new InvalidOperationException("Encryption key must be 256 bits (32 bytes)");
                }

                if (_iv.Length != 16) // AES block size is always 128 bits = 16 bytes
                {
                    throw new InvalidOperationException("Encryption IV must be 128 bits (16 bytes)");
                }

                _logger.LogInformation("AES-256 encryption service initialized successfully with configured keys");
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Failed to parse encryption keys from configuration. They must be valid Base64 strings.");
                throw new InvalidOperationException("Invalid encryption key format. Please check your configuration.", ex);
            }
        }
    }

    private static bool IsValidBase64(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        try
        {
            Convert.FromBase64String(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> EncryptAsync(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            _logger.LogWarning("EncryptAsync called with null or empty plainText");
            return string.Empty;
        }

        try
        {
            _logger.LogDebug("Encrypting text (length: {Length})", plainText.Length);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                await swEncrypt.WriteAsync(plainText);
            }

            var encrypted = Convert.ToBase64String(msEncrypt.ToArray());
            _logger.LogDebug("Text encrypted successfully");

            return encrypted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting text");
            throw new InvalidOperationException("Encryption failed", ex);
        }
    }

    public async Task<string> DecryptAsync(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            _logger.LogWarning("DecryptAsync called with null or empty encryptedText");
            return string.Empty;
        }

        try
        {
            _logger.LogDebug("Decrypting text");

            var cipherBytes = Convert.FromBase64String(encryptedText);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipherBytes);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            var decrypted = await srDecrypt.ReadToEndAsync();
            _logger.LogDebug("Text decrypted successfully");

            return decrypted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting text");
            throw new InvalidOperationException("Decryption failed", ex);
        }
    }

    public async Task<byte[]> EncryptFileAsync(Stream fileStream)
    {
        if (fileStream == null)
        {
            throw new ArgumentNullException(nameof(fileStream));
        }

        try
        {
            _logger.LogDebug("Encrypting file stream");

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                await fileStream.CopyToAsync(csEncrypt);
            }

            var encryptedBytes = msEncrypt.ToArray();
            _logger.LogDebug("File encrypted successfully (size: {Size} bytes)", encryptedBytes.Length);

            return encryptedBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting file stream");
            throw new InvalidOperationException("File encryption failed", ex);
        }
    }

    public async Task<byte[]> DecryptFileAsync(byte[] encryptedData)
    {
        if (encryptedData == null || encryptedData.Length == 0)
        {
            throw new ArgumentException("Encrypted data cannot be null or empty", nameof(encryptedData));
        }

        try
        {
            _logger.LogDebug("Decrypting file data (size: {Size} bytes)", encryptedData.Length);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(encryptedData);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var msPlain = new MemoryStream();

            await csDecrypt.CopyToAsync(msPlain);

            var decryptedBytes = msPlain.ToArray();
            _logger.LogDebug("File decrypted successfully (size: {Size} bytes)", decryptedBytes.Length);

            return decryptedBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting file data");
            throw new InvalidOperationException("File decryption failed", ex);
        }
    }

    public string GenerateEncryptionKey()
    {
        // Generate a cryptographically secure random key
        using var rng = RandomNumberGenerator.Create();
        var keyBytes = new byte[32]; // 256 bits
        rng.GetBytes(keyBytes);

        var key = Convert.ToBase64String(keyBytes);
        _logger.LogDebug("Generated new encryption key");

        return key;
    }

    /// <summary>
    /// Generates a new AES-256 key and IV for configuration
    /// This is a utility method for initial setup
    /// </summary>
    public static (string Key, string IV) GenerateKeyAndIV()
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        aes.GenerateIV();

        return (Convert.ToBase64String(aes.Key), Convert.ToBase64String(aes.IV));
    }
}
