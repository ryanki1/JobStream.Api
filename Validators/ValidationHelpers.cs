using System.Text.RegularExpressions;

namespace JobStream.Api.Validators;

/// <summary>
/// Helper class for custom validation logic
/// </summary>
public static class ValidationHelpers
{
    private static readonly Regex IbanRegex = new Regex(@"^[A-Z]{2}[0-9]{2}[A-Z0-9]{1,30}$", RegexOptions.Compiled);
    private static readonly Regex EthereumAddressRegex = new Regex(@"^0x[a-fA-F0-9]{40}$", RegexOptions.Compiled);
    private static readonly Regex LinkedInCompanyUrlRegex = new Regex(@"^https:\/\/(www\.)?linkedin\.com\/company\/[a-zA-Z0-9\-]+\/?.*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex RegistrationNumberRegex = new Regex(@"^[a-zA-Z0-9\s\-]{5,50}$", RegexOptions.Compiled);

    // VAT ID patterns by country
    private static readonly Dictionary<string, Regex> VatIdPatterns = new()
    {
        { "DE", new Regex(@"^DE\d{9}$", RegexOptions.Compiled) }, // Germany
        { "AT", new Regex(@"^ATU\d{8}$", RegexOptions.Compiled) }, // Austria
        { "BE", new Regex(@"^BE0\d{9}$", RegexOptions.Compiled) }, // Belgium
        { "BG", new Regex(@"^BG\d{9,10}$", RegexOptions.Compiled) }, // Bulgaria
        { "CY", new Regex(@"^CY\d{8}[A-Z]$", RegexOptions.Compiled) }, // Cyprus
        { "CZ", new Regex(@"^CZ\d{8,10}$", RegexOptions.Compiled) }, // Czech Republic
        { "DK", new Regex(@"^DK\d{8}$", RegexOptions.Compiled) }, // Denmark
        { "EE", new Regex(@"^EE\d{9}$", RegexOptions.Compiled) }, // Estonia
        { "FI", new Regex(@"^FI\d{8}$", RegexOptions.Compiled) }, // Finland
        { "FR", new Regex(@"^FR[A-Z0-9]{2}\d{9}$", RegexOptions.Compiled) }, // France
        { "GB", new Regex(@"^GB(\d{9}|\d{12}|GD\d{3}|HA\d{3})$", RegexOptions.Compiled) }, // United Kingdom
        { "GR", new Regex(@"^GR\d{9}$", RegexOptions.Compiled) }, // Greece
        { "HR", new Regex(@"^HR\d{11}$", RegexOptions.Compiled) }, // Croatia
        { "HU", new Regex(@"^HU\d{8}$", RegexOptions.Compiled) }, // Hungary
        { "IE", new Regex(@"^IE\d{7}[A-Z]{1,2}$", RegexOptions.Compiled) }, // Ireland
        { "IT", new Regex(@"^IT\d{11}$", RegexOptions.Compiled) }, // Italy
        { "LT", new Regex(@"^LT(\d{9}|\d{12})$", RegexOptions.Compiled) }, // Lithuania
        { "LU", new Regex(@"^LU\d{8}$", RegexOptions.Compiled) }, // Luxembourg
        { "LV", new Regex(@"^LV\d{11}$", RegexOptions.Compiled) }, // Latvia
        { "MT", new Regex(@"^MT\d{8}$", RegexOptions.Compiled) }, // Malta
        { "NL", new Regex(@"^NL\d{9}B\d{2}$", RegexOptions.Compiled) }, // Netherlands
        { "PL", new Regex(@"^PL\d{10}$", RegexOptions.Compiled) }, // Poland
        { "PT", new Regex(@"^PT\d{9}$", RegexOptions.Compiled) }, // Portugal
        { "RO", new Regex(@"^RO\d{2,10}$", RegexOptions.Compiled) }, // Romania
        { "SE", new Regex(@"^SE\d{12}$", RegexOptions.Compiled) }, // Sweden
        { "SI", new Regex(@"^SI\d{8}$", RegexOptions.Compiled) }, // Slovenia
        { "SK", new Regex(@"^SK\d{10}$", RegexOptions.Compiled) }, // Slovakia
        { "ES", new Regex(@"^ES[A-Z0-9]\d{7}[A-Z0-9]$", RegexOptions.Compiled) }, // Spain
        { "CH", new Regex(@"^CHE\d{9}$", RegexOptions.Compiled) }, // Switzerland
    };

    /// <summary>
    /// Validates IBAN format
    /// </summary>
    public static bool IsValidIban(string iban)
    {
        if (string.IsNullOrWhiteSpace(iban))
            return false;

        // Remove spaces and convert to uppercase
        iban = iban.Replace(" ", "").ToUpperInvariant();

        // Check basic format
        if (!IbanRegex.IsMatch(iban))
            return false;

        // Validate checksum using mod-97 algorithm
        try
        {
            // Move first 4 characters to the end
            var rearranged = iban.Substring(4) + iban.Substring(0, 4);

            // Replace letters with numbers (A=10, B=11, ..., Z=35)
            var numericString = string.Concat(rearranged.Select(c =>
                char.IsLetter(c) ? ((int)c - 55).ToString() : c.ToString()
            ));

            // Calculate mod 97
            var remainder = 0;
            foreach (var digit in numericString)
            {
                remainder = (remainder * 10 + (digit - '0')) % 97;
            }

            return remainder == 1;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates VAT ID format based on country code
    /// </summary>
    public static bool IsValidVatId(string vatId, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(vatId))
        {
            errorMessage = "VAT ID is required";
            return false;
        }

        vatId = vatId.Replace(" ", "").ToUpperInvariant();

        if (vatId.Length < 4)
        {
            errorMessage = "VAT ID is too short";
            return false;
        }

        var countryCode = vatId.Substring(0, 2);

        if (!VatIdPatterns.ContainsKey(countryCode))
        {
            errorMessage = $"VAT ID validation not supported for country: {countryCode}";
            return false;
        }

        if (!VatIdPatterns[countryCode].IsMatch(vatId))
        {
            errorMessage = $"Invalid VAT ID format for {countryCode}";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates Ethereum wallet address
    /// </summary>
    public static bool IsValidEthereumAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return false;

        return EthereumAddressRegex.IsMatch(address);
    }

    /// <summary>
    /// Validates LinkedIn company URL
    /// </summary>
    public static bool IsValidLinkedInCompanyUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return LinkedInCompanyUrlRegex.IsMatch(url);
    }

    /// <summary>
    /// Validates company registration number format
    /// </summary>
    public static bool IsValidRegistrationNumber(string registrationNumber)
    {
        if (string.IsNullOrWhiteSpace(registrationNumber))
            return false;

        return RegistrationNumberRegex.IsMatch(registrationNumber);
    }

    /// <summary>
    /// Validates that description meets minimum word count
    /// </summary>
    public static bool HasMinimumWordCount(string text, int minimumWords, out int actualWordCount)
    {
        actualWordCount = 0;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        actualWordCount = words.Length;

        return actualWordCount >= minimumWords;
    }

    /// <summary>
    /// Validates that email is from a business domain (not free email provider)
    /// </summary>
    public static bool IsBusinessEmail(string email, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(email))
        {
            errorMessage = "Email is required";
            return false;
        }

        var freeEmailDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "gmail.com", "yahoo.com", "hotmail.com", "outlook.com", "live.com",
            "aol.com", "icloud.com", "mail.com", "protonmail.com", "gmx.com",
            "yandex.com", "zoho.com", "tutanota.com", "fastmail.com"
        };

        var domain = email.Split('@').LastOrDefault()?.ToLower();

        if (string.IsNullOrEmpty(domain))
        {
            errorMessage = "Invalid email format";
            return false;
        }

        if (freeEmailDomains.Contains(domain))
        {
            errorMessage = "Please use a business email address, not a free email provider";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates file size
    /// </summary>
    public static bool IsValidFileSize(long fileSizeBytes, long maxSizeBytes, out string? errorMessage)
    {
        errorMessage = null;

        if (fileSizeBytes <= 0)
        {
            errorMessage = "File is empty";
            return false;
        }

        if (fileSizeBytes > maxSizeBytes)
        {
            var maxSizeMb = maxSizeBytes / (1024 * 1024);
            errorMessage = $"File size exceeds maximum allowed size of {maxSizeMb}MB";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates file type based on content type
    /// </summary>
    public static bool IsValidFileType(string contentType, out string? errorMessage)
    {
        errorMessage = null;

        var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "image/jpeg",
            "image/jpg",
            "image/png"
        };

        if (string.IsNullOrWhiteSpace(contentType))
        {
            errorMessage = "Content type is required";
            return false;
        }

        if (!allowedTypes.Contains(contentType.ToLower()))
        {
            errorMessage = "Only PDF, JPG, and PNG files are allowed";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates that file signature matches declared content type
    /// This helps prevent file extension spoofing
    /// </summary>
    public static bool ValidateFileSignature(byte[] fileHeader, string contentType)
    {
        if (fileHeader == null || fileHeader.Length < 4)
            return false;

        // PDF signature: %PDF
        var pdfSignature = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        // JPEG signatures: FF D8 FF
        var jpegSignature = new byte[] { 0xFF, 0xD8, 0xFF };

        // PNG signature: 89 50 4E 47
        var pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        return contentType.ToLower() switch
        {
            "application/pdf" => fileHeader.Take(4).SequenceEqual(pdfSignature),
            "image/jpeg" or "image/jpg" => fileHeader.Take(3).SequenceEqual(jpegSignature),
            "image/png" => fileHeader.Take(4).SequenceEqual(pngSignature),
            _ => false
        };
    }
}
