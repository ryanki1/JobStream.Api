# Company Registration API - Implementation Summary

## Overview

This document provides a comprehensive summary of the backend API implementation for the company registration workflow as defined in the user story. This is a complete RESTful API built with ASP.NET Core 7.0 that handles multi-step company registration with email verification, document uploads, financial verification, and admin review queue.

## What Has Been Implemented

### ✅ All 7 RESTful API Endpoints

1. **POST /api/company/register/start** - Initialize registration session
2. **POST /api/company/register/verify-email** - Verify company email domain
3. **PUT /api/company/register/{id}/company-details** - Save company information
4. **POST /api/company/register/{id}/documents** - Upload verification documents
5. **POST /api/company/register/{id}/financial-verification** - Submit financial verification
6. **GET /api/company/register/{id}/status** - Get current registration progress
7. **POST /api/company/register/{id}/submit** - Final submission for review

### ✅ Data Models

- **CompanyRegistration** entity with all required fields:
  - Email verification fields and tokens
  - Company details (legal name, VAT ID, registration number, etc.)
  - Address as nested JSON object
  - Financial information (encrypted IBAN, bank details)
  - Blockchain fields (wallet address, stake amount, smart contract address)
  - Status tracking and timestamps
  - Review queue management

- **RegistrationDocument** entity for file uploads:
  - Document metadata and type
  - Storage path and secure URLs
  - File size and encryption support
  - Upload timestamps

- **RegistrationStatus** enum with correct workflow states:
  - Initiated
  - EmailVerified
  - DetailsSubmitted
  - DocumentsUploaded
  - FinancialSubmitted
  - PendingReview
  - Approved
  - Rejected

### ✅ DTOs (Data Transfer Objects)

Comprehensive request/response DTOs for all endpoints:
- `StartRegistrationRequest/Response`
- `VerifyEmailRequest/Response`
- `UpdateCompanyDetailsRequest/Response`
- `UploadDocumentRequest/Response`
- `FinancialVerificationRequest/Response`
- `RegistrationStatusResponse`
- `SubmitRegistrationRequest/Response`
- `ErrorResponse` with standardized error format

### ✅ Business Logic Service

`CompanyRegistrationService` implements:
- Registration workflow management
- Email verification token generation and validation
- Business email validation (blocks free email providers)
- Duplicate email checking
- Step-by-step validation (ensures proper order)
- Document upload handling
- IBAN encryption before storage
- Review queue position management
- Auto-cleanup of expired registrations

### ✅ Validation

Comprehensive validation at multiple levels:

**Data Annotations:**
- Required fields
- String length constraints
- Email format validation
- Regex patterns for specific formats

**Custom Validators (`ValidationHelpers`):**
- IBAN validation with checksum verification
- VAT ID validation for 25+ European countries
- Ethereum wallet address validation
- LinkedIn company URL validation
- Business email domain validation
- Description word count validation (200 words minimum)
- File type and size validation
- File signature validation (prevents spoofing)

### ✅ Security Implementation

**AES-256 Encryption Service:**
- Real cryptographic encryption (not just Base64)
- Encrypts sensitive data: IBAN, Tax IDs
- Configurable keys via appsettings.json or Azure Key Vault
- Falls back to temporary keys in development with warnings

**Rate Limiting Middleware:**
- 10 requests per minute per IP (configurable)
- In-memory tracking with automatic cleanup
- Returns HTTP 429 with retry-after header
- Supports X-Forwarded-For for load balancers

**Error Handling Middleware:**
- Catches all unhandled exceptions
- Returns standardized error responses
- Different messages for development vs production
- Proper HTTP status codes (400, 401, 404, 409, 413, 429, 500)

**CORS Configuration:**
- Configured for Angular frontend (localhost:4200)
- Allows credentials
- All headers and methods permitted

### ✅ Database Configuration

**Entity Framework Core with SQLite:**
- Updated `JobStreamDbContext` for new model structure
- Proper indexes for performance:
  - Unique index on CompanyEmail
  - Indexes on Status, CreatedAt, EmailVerificationToken, ExpiresAt
  - Foreign key relationships with cascade delete
- Decimal precision for StakeAmount
- Enum to string conversion for Status
- Auto-generated default values

### ✅ Dependency Injection

All services properly registered in `Program.cs`:
- Database context (SQLite)
- Storage service (file system for now)
- Email service (mock with logging)
- Encryption service (AES-256)
- Business logic service (CompanyRegistrationService)
- Middleware (error handling, rate limiting)

### ✅ Swagger/OpenAPI Documentation

- Auto-generated API documentation
- Available at `/swagger` endpoint
- Detailed endpoint descriptions
- Request/response examples
- HTTP status code documentation

### ✅ Configuration

`appsettings.json` includes:
- Database connection string
- Storage configuration
- Rate limiting settings
- Encryption keys (placeholder - see setup instructions)
- Email settings
- Registration workflow parameters

## Project Structure

```
JobStream.Api/
├── Controllers/
│   ├── CompanyRegistrationController.cs  ✅ All 7 endpoints
│   └── WeatherForecastController.cs      (existing sample)
├── Data/
│   └── JobStreamDbContext.cs             ✅ Updated for new models
├── DTOs/
│   └── RegistrationDTOs.cs               ✅ All request/response DTOs
├── Middleware/
│   ├── ErrorHandlingMiddleware.cs        ✅ Global error handling
│   └── RateLimitingMiddleware.cs         ✅ Rate limiting
├── Models/
│   └── CompanyRegistration.cs            ✅ Updated entities
├── Services/
│   ├── AesEncryptionService.cs           ✅ Real AES-256 encryption
│   ├── CompanyRegistrationService.cs     ✅ Business logic
│   └── MockServices.cs                   ✅ Updated email service
├── Validators/
│   └── ValidationHelpers.cs              ✅ Custom validation logic
├── Program.cs                            ✅ Updated with all registrations
├── appsettings.json                      ✅ Configuration
├── ENCRYPTION_KEY_SETUP.md               ✅ Security setup guide
└── README_COMPANY_REGISTRATION.md        ✅ This file
```

## API Endpoints Documentation

### 1. Start Registration

**Endpoint:** `POST /api/company/register/start`

**Request:**
```json
{
  "companyEmail": "contact@acme.com",
  "primaryContactName": "John Doe"
}
```

**Response (201 Created):**
```json
{
  "registrationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "initiated",
  "expiresAt": "2024-01-30T12:00:00Z"
}
```

**Validation:**
- Business email only (no gmail/yahoo/etc.)
- Email not already registered
- Sends verification email automatically

---

### 2. Verify Email

**Endpoint:** `POST /api/company/register/verify-email`

**Request:**
```json
{
  "registrationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "verificationToken": "abc123xyz789..."
}
```

**Response (200 OK):**
```json
{
  "verified": true,
  "nextStep": "company-details"
}
```

**Validation:**
- Token must match
- Token not expired (24 hours)

---

### 3. Update Company Details

**Endpoint:** `PUT /api/company/register/{registrationId}/company-details`

**Request:**
```json
{
  "legalName": "Acme Corporation GmbH",
  "registrationNumber": "HRB 12345",
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
  "description": "Long description with minimum 200 words..."
}
```

**Response (200 OK):**
```json
{
  "saved": true,
  "validationErrors": [],
  "nextStep": "documents"
}
```

**Validation:**
- Email must be verified first
- All required fields
- VAT ID format validated by country
- LinkedIn URL must be company page
- Description must have 200+ words

---

### 4. Upload Document

**Endpoint:** `POST /api/company/register/{registrationId}/documents`

**Request:** Multipart form-data
- `file`: The document file
- `documentType`: Type of document (e.g., "CertificateOfIncorporation")

**Response (201 Created):**
```json
{
  "documentId": "7fa85f64-5717-4562-b3fc-2c963f66afa7",
  "fileName": "business_license.pdf",
  "uploadedAt": "2024-01-23T14:30:00Z",
  "status": "pending_verification"
}
```

**Validation:**
- Company details must be submitted first
- PDF, JPG, or PNG only
- Max 10MB per file
- File signature validation (prevents spoofing)

---

### 5. Financial Verification

**Endpoint:** `POST /api/company/register/{registrationId}/financial-verification`

**Request:**
```json
{
  "bankName": "Deutsche Bank",
  "iban": "DE89370400440532013000",
  "accountHolderName": "Acme Corporation GmbH",
  "balanceProofDocumentId": "7fa85f64-5717-4562-b3fc-2c963f66afa7"
}
```

**Response (200 OK):**
```json
{
  "verified": false,
  "status": "pending_manual_review",
  "estimatedReviewTime": "2-3 business days"
}
```

**Validation:**
- Documents must be uploaded first
- IBAN format and checksum validated
- IBAN encrypted before storage (AES-256)

---

### 6. Get Registration Status

**Endpoint:** `GET /api/company/register/{registrationId}/status`

**Response (200 OK):**
```json
{
  "registrationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "currentStep": "financial-verification",
  "completedSteps": ["email", "company-details", "documents"],
  "status": "FinancialSubmitted",
  "lastUpdated": "2024-01-23T14:30:00Z",
  "expiresAt": "2024-01-30T12:00:00Z"
}
```

---

### 7. Submit for Review

**Endpoint:** `POST /api/company/register/{registrationId}/submit`

**Request:**
```json
{
  "termsAccepted": true,
  "stakeAmount": 2500,
  "walletAddress": "0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb"
}
```

**Response (200 OK):**
```json
{
  "submitted": true,
  "reviewQueuePosition": 3,
  "estimatedReviewTime": "24-48 hours",
  "smartContractAddress": "0x...",
  "nextSteps": "Wait for admin approval and stake deposit confirmation"
}
```

**Validation:**
- All previous steps completed
- Terms must be accepted
- Stake amount >= 2500
- Ethereum wallet address format validated
- Sends confirmation email

---

## Error Response Format

All errors follow this standardized format:

```json
{
  "error": true,
  "code": "VALIDATION_ERROR",
  "message": "User-friendly error message",
  "details": {
    "field": "vatId",
    "reason": "Invalid VAT ID format for Germany"
  }
}
```

### Error Codes

- `VALIDATION_ERROR` - Input validation failed (400)
- `EMAIL_ALREADY_REGISTERED` - Email already in use (409)
- `REGISTRATION_NOT_FOUND` - Registration ID not found (404)
- `UNAUTHORIZED` - Authentication failed (401)
- `FILE_TOO_LARGE` - File exceeds 10MB (413)
- `RATE_LIMIT_EXCEEDED` - Too many requests (429)
- `INTERNAL_SERVER_ERROR` - Server error (500)

## Security Features

### 1. Data Encryption

- **AES-256-CBC** encryption for sensitive data
- **IBAN** and **Tax IDs** encrypted at rest
- Configurable keys (see `ENCRYPTION_KEY_SETUP.md`)

### 2. Rate Limiting

- 10 requests per minute per IP address
- Configurable in appsettings.json
- Automatic cleanup of old entries

### 3. Input Validation

- Data annotations on DTOs
- Custom validation logic
- SQL injection prevention (parameterized queries)
- File signature validation

### 4. HTTPS Enforcement

- HTTPS redirection enabled
- Secure headers (can be enhanced)

### 5. CORS

- Restricted to Angular frontend origin
- Credentials allowed

## Setup Instructions

### 1. Prerequisites

- .NET 7.0 SDK
- SQLite (included)

### 2. Installation

```bash
cd JobStream.Api
dotnet restore
```

### 3. Configure Encryption Keys

⚠️ **IMPORTANT:** Generate your own encryption keys before running!

See [ENCRYPTION_KEY_SETUP.md](./ENCRYPTION_KEY_SETUP.md) for detailed instructions.

Quick setup for development:

```bash
# Generate keys (see ENCRYPTION_KEY_SETUP.md for code)
# Then store in user secrets:
dotnet user-secrets init
dotnet user-secrets set "Encryption:Key" "YOUR_GENERATED_KEY"
dotnet user-secrets set "Encryption:IV" "YOUR_GENERATED_IV"
```

### 4. Run the Application

```bash
dotnet run
```

The API will be available at:
- HTTP: http://localhost:5252 (default with `dotnet run`)
- HTTPS: https://localhost:7088 (use `dotnet run --launch-profile https`)
- Swagger: http://localhost:5252/swagger (or https://localhost:7088/swagger with HTTPS)

### 5. Test the API

Use Swagger UI, Postman, or curl:

```bash
# Start registration (using HTTP - default)
curl -X POST http://localhost:5252/api/company/register/start \
  -H "Content-Type: application/json" \
  -d '{
    "companyEmail": "contact@acme.com",
    "primaryContactName": "John Doe"
  }'
```

## Database

### Schema

The database is automatically created on first run using Entity Framework Core's `EnsureCreated()`.

**Tables:**
- `CompanyRegistrations` - Main registration data
- `RegistrationDocuments` - Uploaded documents

**Indexes:**
- Unique index on `CompanyEmail`
- Performance indexes on `Status`, `CreatedAt`, `ExpiresAt`, `EmailVerificationToken`

### Location

SQLite database file: `jobstream.db` (in the application root)

To reset the database:
```bash
rm jobstream.db
dotnet run
```

## Testing

### Manual Testing with Swagger

1. Navigate to http://localhost:5252/swagger
2. Click on an endpoint
3. Click "Try it out"
4. Fill in the request body
5. Click "Execute"

### Testing the Full Workflow

1. **Start Registration:**
   ```json
   POST /api/company/register/start
   {
     "companyEmail": "test@mycompany.com",
     "primaryContactName": "Jane Smith"
   }
   ```
   → Note the `registrationId`

2. **Check Logs for Verification Token:**
   ```
   MockEmailService: Sending email verification...
   Verification URL: http://localhost:4200/register/verify?token=abc123...
   ```

3. **Verify Email:**
   ```json
   POST /api/company/register/verify-email
   {
     "registrationId": "...",
     "verificationToken": "abc123..."
   }
   ```

4. **Update Company Details:**
   ```json
   PUT /api/company/register/{registrationId}/company-details
   {
     "legalName": "Test Company GmbH",
     "registrationNumber": "HRB12345",
     "vatId": "DE123456789",
     ...
   }
   ```

5. **Upload Documents** (use form-data in Postman)

6. **Submit Financial Verification**

7. **Check Status:**
   ```
   GET /api/company/register/{registrationId}/status
   ```

8. **Final Submission**

## Known Limitations / Out of Scope

The following were explicitly out of scope for this backend story:

- ❌ Frontend Angular components (separate story)
- ❌ Smart contract actual deployment (mock address generated)
- ❌ External business registry API integration
- ❌ Payment gateway integration for stake deposit
- ❌ Admin review dashboard/interface
- ❌ Real email sending (currently logs to console)
- ❌ Cloud blob storage (currently local file system)
- ❌ Authentication/Authorization (JWT, OAuth, etc.)

## Next Steps / Future Enhancements

### High Priority

1. **Authentication & Authorization**
   - Add JWT bearer authentication
   - Role-based access control (Admin, Company, etc.)

2. **Real Email Service**
   - Integrate SendGrid, AWS SES, or Mailgun
   - HTML email templates

3. **Cloud Storage**
   - Migrate to Azure Blob Storage or AWS S3
   - Generate signed URLs for secure download

4. **Unit Tests**
   - Service layer tests
   - Validation logic tests
   - Controller tests with mocked dependencies

### Medium Priority

5. **Integration Tests**
   - Full workflow end-to-end tests
   - Database integration tests

6. **Admin API Endpoints**
   - GET /api/admin/registrations (list pending)
   - PUT /api/admin/registrations/{id}/review (approve/reject)
   - GET /api/admin/registrations/{id}/documents

7. **Enhanced Security**
   - Add authentication tokens
   - Implement CSRF protection
   - Add security headers (HSTS, CSP, etc.)

### Low Priority

8. **Performance Optimization**
   - Redis for rate limiting (distributed)
   - Redis for caching
   - Background jobs for cleanup

9. **Monitoring & Logging**
   - Application Insights integration
   - Structured logging (Serilog)
   - Health check endpoints

10. **Advanced Features**
    - Webhook notifications
    - Real-time status updates (SignalR)
    - Document OCR for automatic data extraction

## Compliance & Production Readiness

### Before Production Deployment

- [ ] Generate and securely store real encryption keys
- [ ] Move to cloud blob storage (Azure/AWS)
- [ ] Implement real email service
- [ ] Add authentication & authorization
- [ ] Set up HTTPS certificates
- [ ] Configure production database (PostgreSQL/SQL Server)
- [ ] Set up monitoring and logging
- [ ] Perform security audit (OWASP top 10)
- [ ] Load testing
- [ ] Penetration testing
- [ ] GDPR compliance review (data retention, right to deletion)
- [ ] Backup and disaster recovery plan

## Support & Documentation

- **API Documentation:** http://localhost:5252/swagger
- **Encryption Setup:** [ENCRYPTION_KEY_SETUP.md](./ENCRYPTION_KEY_SETUP.md)
- **User Story:** See original SCRUM story document

## Changelog

### v1.0.0 (2024-01-23)

✅ Initial implementation complete:
- All 7 RESTful endpoints
- Data models and DTOs
- Business logic service
- Validation (data annotations + custom)
- AES-256 encryption
- Rate limiting
- Error handling
- Swagger documentation
- SQLite database with EF Core

## License

Proprietary - JobStream Platform

---

**Implementation Status:** ✅ COMPLETE

All requirements from the user story have been implemented. The API is ready for integration with the Angular frontend and further development of dependent features (admin dashboard, smart contracts, external integrations).
