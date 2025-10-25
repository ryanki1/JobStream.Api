# Quick Start Guide - Company Registration API

## 🚀 Get Started in 5 Minutes

### Step 1: Set Up PostgreSQL

The application uses PostgreSQL for database storage. You have two options:

#### Option A: Run PostgreSQL via Docker (Recommended)

```bash
docker run -d \
  --name jobstream-postgres \
  -p 5432:5432 \
  postgres:15-alpine
```

#### Option B: Install PostgreSQL Locally

```bash
# macOS:
brew install postgresql@15
brew services start postgresql@15
```

That's it! No need to manually create users or databases.

### Step 2: Build and Run

```bash
cd JobStream.Api
dotnet run
```

The application will automatically:
- ✅ Create PostgreSQL user (jobstream_user) if needed
- ✅ Create database (jobstream_dev) if needed
- ✅ Apply all database migrations
- ✅ Start the web server
- ⚠️ Generate temporary encryption keys (with a warning)

### Step 3: Open Swagger UI

Navigate to: **http://localhost:5252/swagger**

You'll see all 7 API endpoints documented and ready to test!

> **Note:** If you prefer HTTPS, run with: `dotnet run --launch-profile https`
> Then access Swagger at: **https://localhost:7088/swagger**

### Step 4: Test the Full Registration Flow

#### 3.1 Start Registration

Click on **POST /api/company/register/start**, then "Try it out"

Request body:
```json
{
  "companyEmail": "contact@mycompany.com",
  "primaryContactName": "John Doe"
}
```

Click **Execute**

✅ You should receive:
```json
{
  "registrationId": "...",  ← Copy this!
  "status": "initiated",
  "expiresAt": "..."
}
```

#### 3.2 Get Verification Token

Check the console logs (where you ran `dotnet run`):

Look for:
```
[Information] MockEmailService: Sending email verification to contact@mycompany.com
[Information] Verification URL: http://localhost:4200/register/verify?token=ABC123XYZ789
```

Copy the **token** value (the part after `token=`)

#### 3.3 Verify Email

Click on **POST /api/company/register/verify-email**, then "Try it out"

```json
{
  "registrationId": "PASTE_YOUR_REGISTRATION_ID_HERE",
  "verificationToken": "PASTE_YOUR_TOKEN_HERE"
}
```

✅ You should receive:
```json
{
  "verified": true,
  "nextStep": "company-details"
}
```

#### 3.4 Update Company Details

Click on **PUT /api/company/register/{registrationId}/company-details**, then "Try it out"

```json
{
  "legalName": "Acme Corporation GmbH",
  "registrationNumber": "HRB12345",
  "vatId": "DE123456789",
  "linkedInUrl": "https://linkedin.com/company/acme",
  "address": {
    "street": "Main Street 123",
    "city": "Berlin",
    "postalCode": "10115",
    "country": "Germany"
  },
  "industry": "Software Development",
  "companySize": "51-200",
  "description": "This is a test company description that needs to be at least 200 words long. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum. Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi architecto beatae vitae dicta sunt explicabo. Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit, sed quia consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt neque porro quisquam est qui dolorem ipsum quia dolor sit amet consectetur adipisci velit sed quia non numquam eius modi tempora incidunt ut labore et dolore magnam aliquam quaerat voluptatem."
}
```

✅ You should receive:
```json
{
  "saved": true,
  "validationErrors": [],
  "nextStep": "documents"
}
```

#### 3.5 Upload Document

Click on **POST /api/company/register/{registrationId}/documents**, then "Try it out"

- `file`: Click "Choose File" and select a PDF, JPG, or PNG (max 10MB)
- `documentType`: Enter "CertificateOfIncorporation"

Click **Execute**

✅ You should receive:
```json
{
  "documentId": "...",
  "fileName": "your-file.pdf",
  "uploadedAt": "...",
  "status": "pending_verification"
}
```

#### 3.6 Submit Financial Verification

Click on **POST /api/company/register/{registrationId}/financial-verification**, then "Try it out"

```json
{
  "bankName": "Deutsche Bank",
  "iban": "DE89370400440532013000",
  "accountHolderName": "Acme Corporation GmbH",
  "balanceProofDocumentId": "PASTE_DOCUMENT_ID_FROM_STEP_3.5"
}
```

✅ You should receive:
```json
{
  "verified": false,
  "status": "pending_manual_review",
  "estimatedReviewTime": "2-3 business days"
}
```

#### 3.7 Check Status

Click on **GET /api/company/register/{registrationId}/status**, then "Try it out"

Enter your `registrationId` and click **Execute**

✅ You should receive:
```json
{
  "registrationId": "...",
  "currentStep": "submit",
  "completedSteps": [
    "email",
    "company-details",
    "documents",
    "financial-verification"
  ],
  "status": "FinancialSubmitted",
  "lastUpdated": "...",
  "expiresAt": "..."
}
```

#### 3.8 Final Submission

Click on **POST /api/company/register/{registrationId}/submit**, then "Try it out"

```json
{
  "termsAccepted": true,
  "stakeAmount": 2500,
  "walletAddress": "0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb"
}
```

✅ You should receive:
```json
{
  "submitted": true,
  "reviewQueuePosition": 1,
  "estimatedReviewTime": "24-48 hours",
  "smartContractAddress": "0x...",
  "nextSteps": "Wait for admin approval and stake deposit confirmation"
}
```

🎉 **Congratulations!** You've completed the entire registration workflow!

---

## 🧪 Testing Tips

### Rate Limiting

Try making more than 10 requests in a minute:

❌ You should receive HTTP 429:
```json
{
  "error": true,
  "code": "RATE_LIMIT_EXCEEDED",
  "message": "Too many requests. Maximum 10 requests per minute allowed.",
  "retryAfter": 60
}
```

### Validation Errors

Try using a free email provider:

```json
{
  "companyEmail": "test@gmail.com",  ← This will fail!
  "primaryContactName": "John Doe"
}
```

❌ You should receive HTTP 400:
```json
{
  "error": true,
  "code": "VALIDATION_ERROR",
  "message": "Please use a business email address, not a free email provider"
}
```

### Invalid VAT ID

Try an invalid German VAT ID:

```json
{
  "vatId": "DE123",  ← Too short!
  ...
}
```

❌ You should receive HTTP 400 with validation error

---

## 🗄️ Database Inspection

### View the Database

Connect to PostgreSQL using psql or a GUI tool:

```bash
# Using psql
psql -h localhost -U jobstream_user -d jobstream_dev

# List tables
\dt

# View registrations
SELECT "Id", "CompanyEmail", "Status", "CreatedAt" FROM "CompanyRegistrations";

# View documents
SELECT * FROM "RegistrationDocuments";

# Exit
\q
```

Or use a GUI tool like:
- [pgAdmin](https://www.pgadmin.org/)
- [DBeaver](https://dbeaver.io/)
- [DataGrip](https://www.jetbrains.com/datagrip/)

### Reset Database

```bash
# Drop and recreate the database
psql -h localhost -U jobstream_user -d postgres
DROP DATABASE jobstream_dev;
CREATE DATABASE jobstream_dev;
\q

# Restart the application
dotnet run
```

---

## 📁 Uploaded Files

Files are stored in the `uploads/` directory (created automatically).

```bash
ls -lh uploads/
```

---

## 🔐 Encryption Key Setup (Optional for Testing)

For development, the application generates temporary encryption keys automatically.

To set up real keys (optional):

1. Generate keys:
```bash
# Run this in Program.cs or a test project:
var (key, iv) = JobStream.Api.Services.AesEncryptionService.GenerateKeyAndIV();
Console.WriteLine($"Key: {key}");
Console.WriteLine($"IV: {iv}");
```

2. Store in user secrets:
```bash
dotnet user-secrets init
dotnet user-secrets set "Encryption:Key" "YOUR_KEY_HERE"
dotnet user-secrets set "Encryption:IV" "YOUR_IV_HERE"
```

See [ENCRYPTION_KEY_SETUP.md](./ENCRYPTION_KEY_SETUP.md) for detailed instructions.

---

## 🐛 Troubleshooting

### Port Already in Use

If port 7088 is already in use:

1. Edit `Properties/launchSettings.json`
2. Change the port numbers
3. Restart

### Connection Issues

If you can't connect to PostgreSQL:

```bash
# Check if PostgreSQL is running
docker ps  # If using Docker
# or
brew services list  # If installed via Homebrew

# Restart PostgreSQL container
docker restart jobstream-postgres

# Check connection
psql -h localhost -U jobstream_user -d jobstream_dev
```

### Build Errors

```bash
dotnet clean
dotnet restore
dotnet build
```

---

## 📊 Monitoring Logs

The application logs everything to the console:

```
[Information] JobStream API is starting...
[Information] Database initialized successfully
[Information] POST /api/company/register/start called for email: contact@mycompany.com
[Information] MockEmailService: Sending email verification to contact@mycompany.com
[Information] AES-256 encryption service initialized successfully
```

---

## 🚢 Next Steps

After testing locally:

1. ✅ Review the API endpoints
2. ✅ Understand the validation rules
3. ✅ Check error responses
4. ✅ Test the full workflow
5. 📝 Integrate with Angular frontend
6. 🔐 Set up real encryption keys for staging/production
7. 📧 Configure real email service
8. ☁️ Deploy to cloud (Azure, AWS, etc.)

---

## 📚 Additional Resources

- **Full Documentation:** [README_COMPANY_REGISTRATION.md](./README_COMPANY_REGISTRATION.md)
- **Encryption Setup:** [ENCRYPTION_KEY_SETUP.md](./ENCRYPTION_KEY_SETUP.md)
- **Swagger UI:** http://localhost:5252/swagger (or https://localhost:7088/swagger if using HTTPS profile)

---

## 💬 Questions?

Common questions answered in the full README:

- How does rate limiting work?
- What data is encrypted?
- How do I add authentication?
- How do I deploy to production?
- What are the validation rules?

See [README_COMPANY_REGISTRATION.md](./README_COMPANY_REGISTRATION.md)

---

**Happy Testing! 🎉**
