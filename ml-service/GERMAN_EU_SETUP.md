# German & EU Company Verification Setup

## Overview

The ML verification service is configured for **German and Euro-Zone** companies, with support for German Handelsregister and EU-wide business registers.

## Business Register Integration

### Germany: Handelsregister

**What it is:**
- Official German commercial register
- Maintained by local courts (Amtsgerichte)
- Contains: GmbH, AG, KG, OHG, e.K., etc.

**Registration Number Formats:**
```
HRB 12345          # GmbH, AG (Handelsregister B)
HRA 12345          # e.K., OHG, KG (Handelsregister A)
HRB 12345 München  # With court location
```

**API Options:**

#### 1. Unternehmensregister.de (Official, FREE)
- **URL**: https://www.unternehmensregister.de
- **Cost**: FREE for manual searches
- **API**: No official API, requires web scraping
- **Data**: Complete company data, annual reports

#### 2. VIES (EU VAT Validation - FREE) ⭐ RECOMMENDED for MVP
- **URL**: https://ec.europa.eu/taxation_customs/vies/
- **Cost**: FREE
- **Coverage**: All EU companies with VAT numbers
- **API**: Yes (SOAP/REST)
- **Response**: Company name, address, VAT validity
- **Perfect for Sprint 3**: Simple, free, covers all EU

**Example VIES Request:**
```python
import requests

# Validate German VAT number
url = "https://ec.europa.eu/taxation_customs/vies/rest-api/ms/DE/vat/123456789"
response = requests.get(url)

# Returns:
{
  "isValid": true,
  "name": "Example GmbH",
  "address": "Musterstraße 1, 80331 München"
}
```

#### 3. North Data API (Commercial, has free tier)
- **URL**: https://www.northdata.de
- **Cost**: FREE tier: 100 requests/month, then €29/month
- **Coverage**: German companies, UBO data, network analysis
- **API**: REST API with good documentation
- **Best for**: Production after MVP

#### 4. OpenCorporates (International)
- **URL**: https://opencorporates.com/api
- **Cost**: FREE tier: 500 requests/month, then $99/month
- **Coverage**: 200+ million companies worldwide
- **API**: REST API
- **Best for**: International expansion

### Other EU Countries

| Country | Registry | API Available |
|---------|----------|---------------|
| Austria | Firmenbuch | Limited |
| Switzerland | Handelsregister (CH) | Yes (via Zefix) |
| France | INPI/Infogreffe | Yes (paid) |
| Netherlands | KvK (Kamer van Koophandel) | Yes (paid) |
| Italy | Registro Imprese | Limited |

**For MVP: Use VIES for all EU countries** ✅

## Current Implementation (Sprint 3)

### What's Implemented:

1. **Format Validation**
   - Validates German HRB/HRA format
   - Checks for typical company identifiers (GmbH, AG, etc.)
   - Returns mock response for valid formats

2. **Field Name Compatibility**
   - Field: `companies_house_match` (for backward compatibility)
   - Description: Now mentions "Handelsregister for DE, Companies House for UK"
   - Alias: `handelsregister_match` supported

3. **Risk Scoring**
   - Business register verification: 25 points (highest weight)
   - Message: "Company verified in official business register"

### What Needs Real API (Sprint 4+):

```python
# In web_intelligence.py - Replace mock with real VIES API:

async def check_handelsregister(self, company_number: Optional[str]) -> bool:
    """Check company via VIES (EU VAT validation)"""

    # Extract country code and VAT number
    # German VAT: DE123456789
    # Format: DE + 9 digits

    if not company_number:
        return False

    # Parse VAT number
    vat_match = re.match(r'([A-Z]{2})(\d+)', company_number)
    if not vat_match:
        return False

    country_code = vat_match.group(1)
    vat_number = vat_match.group(2)

    try:
        # VIES REST API
        url = f"https://ec.europa.eu/taxation_customs/vies/rest-api/ms/{country_code}/vat/{vat_number}"

        async with aiohttp.ClientSession() as session:
            async with session.get(url) as response:
                if response.status == 200:
                    data = await response.json()
                    return data.get("isValid", False)
                else:
                    return False

    except Exception as e:
        print(f"❌ VIES check failed: {e}")
        return False
```

## Test Data

### Valid German Companies for Testing:

```json
{
  "company_name": "SAP SE",
  "company_number": "HRB 719915",
  "vat_number": "DE143593636",
  "website_url": "https://www.sap.com",
  "description": "Enterprise software company"
}
```

```json
{
  "company_name": "Siemens AG",
  "company_number": "HRB 6684",
  "vat_number": "DE129273398",
  "website_url": "https://www.siemens.com",
  "description": "Industrial manufacturing and technology"
}
```

```json
{
  "company_name": "Zalando SE",
  "company_number": "HRB 158855 B",
  "vat_number": "DE260543043",
  "website_url": "https://www.zalando.com",
  "description": "E-commerce fashion platform"
}
```

### Invalid/Risky Company for Testing:

```json
{
  "company_name": "FakeGmbH",
  "company_number": "INVALID123",
  "vat_number": "",
  "website_url": "https://nonexistent-website-12345.com",
  "description": "Generic description"
}
```

## Integration Steps for Real API

### Step 1: Enable VIES (Recommended for MVP)

1. No API key required - VIES is public
2. Update `web_intelligence.py` with real VIES implementation
3. Change company_number field to accept VAT format: `DE123456789`
4. Test with real German VAT numbers

### Step 2: (Optional) Add North Data for Enhanced Data

1. Sign up at https://www.northdata.de
2. Get API key (free tier: 100 req/month)
3. Add to `.env`:
   ```
   NORTHDATA_API_KEY=your_key_here
   ```
4. Implement additional checks:
   - Company ownership structure
   - Financial health indicators
   - Related companies/networks

## Costs for Production

### FREE Option (Recommended for MVP):
```
VIES VAT Validation: $0 ✅
- Covers all EU companies
- No rate limits for reasonable use
- Official EU service
```

### Enhanced Option (Better Data):
```
VIES (FREE) + North Data Basic: €29/month
- 2,500 requests/month
- Complete German company data
- UBO (Ultimate Beneficial Owner) data
- Company networks
```

### Enterprise Option:
```
North Data Professional: €249/month
- 25,000 requests/month
- Real-time monitoring
- Advanced analytics
```

## German-Specific Features to Add (Sprint 4+)

1. **UBO Verification** (Ultimate Beneficial Owner)
   - Required by German anti-money laundering law
   - Available via North Data or Transparenzregister API

2. **Impressum Verification**
   - German law requires Impressum on websites
   - Parse website Impressum and cross-check with registration

3. **Geschäftsführer Validation** (Managing Directors)
   - Check if listed Geschäftsführer match Handelsregister
   - Detect frequent Geschäftsführer changes (red flag)

4. **Gewerbeamt Check** (Trade Office)
   - For businesses not in Handelsregister (freelancers, small traders)

5. **Bundeszentralregister** (Criminal Record Check)
   - For high-risk industries
   - Requires special authorization

## Localization

### Current Status:
- **Code**: English (for developer collaboration)
- **Comments**: English
- **API responses**: English
- **UI messages**: English

### Sprint 4 Localization:
- Add German language pack
- Translate risk flags and recommendations
- Support German address formats
- German number formatting (1.234,56)

## Compliance

### GDPR Considerations:
- ✅ Only public business data processed
- ✅ No personal data of employees
- ✅ Transparent processing (admin sees AI report)
- ⚠️ Log data retention (delete after 90 days)
- ⚠️ Cross-border data transfer (if using non-EU ML service)

### German Legal Requirements:
- Company verification required by law (GwG - Geldwäschegesetz)
- JobStream acts as "gatekeeper" platform
- Must verify business legitimacy before publishing jobs
- AI can assist but human (admin) makes final decision ✅

---

**Next Steps:**
1. Complete Sprint 3 with mock Handelsregister
2. Test with German company data
3. Sprint 4: Integrate VIES API (FREE)
4. Sprint 5+: Add North Data for enhanced verification

**Questions?** Check:
- VIES Documentation: https://ec.europa.eu/taxation_customs/vies/technicalInformation.html
- North Data API Docs: https://www.northdata.de/api/doc
- German company law: https://www.gesetze-im-internet.de/hgb/
