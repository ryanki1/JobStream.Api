# ML-Powered Company Verification Service

## Overview
A Python-based AI/ML microservice that assists admins in verifying company registrations by gathering intelligence from the web, analyzing sentiment, and providing risk assessments.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Admin Dashboard UI                       │
│  [Pending Registrations List] → [AI Verify Button] ────────┐│
└─────────────────────────────────────────────────────────────┘│
                                                                │
┌───────────────────────────────────────────────────────────┐  │
│              JobStream.Api (.NET Core)                     │  │
│  POST /api/v1/admin/registrations/{id}/ai-verify  ◄──────────┘
│         │                                                  │
│         │ Forward request                                 │
│         ▼                                                  │
│  MLVerificationService.cs ──────────────────┐             │
│         │                                    │             │
│         │ HTTP POST to ML Service            │             │
└─────────┼────────────────────────────────────┼─────────────┘
          │                                    │
          ▼                                    │
┌─────────────────────────────────────────────▼─────────────┐
│         Python ML Service (FastAPI)                        │
│  POST /api/v1/verify-company                               │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐  │
│  │  1. Web Intelligence Module                         │  │
│  │     - Companies House API lookup                    │  │
│  │     - LinkedIn company search                       │  │
│  │     - News/Press release scraping                   │  │
│  │     - Review sites (Glassdoor/Trustpilot)          │  │
│  └─────────────────────────────────────────────────────┘  │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐  │
│  │  2. Sentiment Analysis (PyTorch)                    │  │
│  │     - Pre-trained transformer model                 │  │
│  │     - Analyze reviews, news mentions                │  │
│  │     - Output: Positive/Neutral/Negative + scores    │  │
│  └─────────────────────────────────────────────────────┘  │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐  │
│  │  3. Document Analysis (PyTorch - Optional)          │  │
│  │     - Basic OCR text extraction                     │  │
│  │     - Logo verification via image similarity        │  │
│  └─────────────────────────────────────────────────────┘  │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐  │
│  │  4. Risk Scoring Engine                             │  │
│  │     - Aggregate signals from all modules            │  │
│  │     - Output: Risk score (0-100) + flags            │  │
│  └─────────────────────────────────────────────────────┘  │
│                                                             │
│  Returns: AIVerificationReport (JSON)                      │
└─────────────────────────────────────────────────────────────┘
```

## Sprint 3 Scope (Simplified Version)

### Phase 1: Core Infrastructure (3 points)
- [ ] FastAPI microservice setup with Docker
- [ ] Basic health check and API endpoints
- [ ] Integration with .NET API via HTTP client
- [ ] Request/response models

### Phase 2: Web Intelligence (2 points)
- [ ] Companies House API integration (UK companies)
- [ ] Basic web scraping for company website
- [ ] News search via Google News API (free tier)
- [ ] LinkedIn basic company info (if possible without auth)

### Phase 3: PyTorch Sentiment Analysis (2 points)
- [ ] Load pre-trained DistilBERT model
- [ ] Sentiment analysis on company reviews/news
- [ ] Batch inference optimization
- [ ] Cache frequently analyzed companies

### Phase 4: Risk Scoring (1 point)
- [ ] Weighted scoring algorithm
- [ ] Flag suspicious patterns (new company, no web presence, etc.)
- [ ] Return actionable insights for admin

### Phase 5: Admin Dashboard Integration (2 points)
- [ ] Add "AI Verify" button to pending registrations
- [ ] Display AI report in modal/side panel
- [ ] Show risk score with color coding
- [ ] Allow admin to override AI recommendation

**Total: 10 story points** (fits well with Admin Dashboard work)

## API Contract

### Request to ML Service
```json
POST http://localhost:8000/api/v1/verify-company
{
  "registrationId": "197c8ae3-aa7b-41f0-be6e-e60e13f63232",
  "companyName": "Example Ltd",
  "companyNumber": "12345678",
  "vatNumber": "GB123456789",
  "websiteUrl": "https://example.com",
  "linkedInUrl": "https://linkedin.com/company/example",
  "businessDescription": "We provide software solutions..."
}
```

### Response from ML Service
```json
{
  "registrationId": "197c8ae3-aa7b-41f0-be6e-e60e13f63232",
  "overallRiskScore": 23.5,
  "riskLevel": "LOW",
  "verifiedAt": "2025-11-12T14:30:00Z",
  "processingTimeMs": 4523,

  "webIntelligence": {
    "companiesHouseMatch": true,
    "websiteActive": true,
    "websiteAge": "3 years",
    "linkedInVerified": true,
    "linkedInFollowers": 1250,
    "newsArticlesFound": 8,
    "recentNewsDate": "2025-10-15"
  },

  "sentimentAnalysis": {
    "overallSentiment": "POSITIVE",
    "sentimentScore": 0.78,
    "sources": [
      {
        "source": "Trustpilot",
        "rating": 4.2,
        "reviewCount": 45,
        "sentiment": "POSITIVE"
      },
      {
        "source": "Google News",
        "articleCount": 8,
        "sentiment": "NEUTRAL"
      }
    ]
  },

  "riskFlags": [],

  "recommendations": [
    "Company verified on Companies House",
    "Active web presence with positive sentiment",
    "No suspicious patterns detected"
  ],

  "confidence": 0.85
}
```

### High Risk Example Response
```json
{
  "registrationId": "...",
  "overallRiskScore": 78.3,
  "riskLevel": "HIGH",

  "riskFlags": [
    "COMPANIES_HOUSE_NOT_FOUND",
    "WEBSITE_UNREACHABLE",
    "NO_LINKEDIN_PRESENCE",
    "NO_WEB_MENTIONS",
    "DESCRIPTION_GENERIC"
  ],

  "recommendations": [
    "⚠️ Company not found in Companies House registry",
    "⚠️ Website URL is not accessible",
    "⚠️ No LinkedIn company page found",
    "⛔ RECOMMEND MANUAL VERIFICATION"
  ],

  "confidence": 0.92
}
```

## Technology Stack

### Python ML Service
- **Framework**: FastAPI (async, fast, easy OpenAPI docs)
- **ML**: PyTorch + transformers (Hugging Face)
- **Model**: DistilBERT for sentiment analysis (88M params, fast inference)
- **Web Scraping**: BeautifulSoup4, requests, aiohttp
- **APIs**:
  - Companies House API (FREE)
  - Google News API (FREE tier)
  - Optional: RapidAPI for review aggregation
- **Containerization**: Docker
- **Caching**: Redis (optional, for repeated queries)

### .NET Integration
- **HTTP Client**: `System.Net.Http.HttpClient`
- **Service**: `MLVerificationService.cs`
- **Controller**: Add endpoint to `AdminController.cs`
- **Models**: `AIVerificationReport.cs`, `AIVerificationRequest.cs`

## PyTorch Model Details

### Pre-trained Model
```python
from transformers import AutoTokenizer, AutoModelForSequenceClassification
import torch

# Load DistilBERT fine-tuned on sentiment analysis
model_name = "distilbert-base-uncased-finetuned-sst-2-english"
tokenizer = AutoTokenizer.from_pretrained(model_name)
model = AutoModelForSequenceClassification.from_pretrained(model_name)

# Inference
def analyze_sentiment(text: str) -> dict:
    inputs = tokenizer(text, return_tensors="pt", truncation=True, max_length=512)

    with torch.no_grad():
        outputs = model(**inputs)
        predictions = torch.nn.functional.softmax(outputs.logits, dim=-1)

    negative_score = predictions[0][0].item()
    positive_score = predictions[0][1].item()

    return {
        "sentiment": "POSITIVE" if positive_score > negative_score else "NEGATIVE",
        "score": max(positive_score, negative_score),
        "positive_score": positive_score,
        "negative_score": negative_score
    }
```

### Why DistilBERT?
- **Fast**: 60% faster than BERT, suitable for real-time API
- **Accurate**: 95%+ accuracy on sentiment tasks
- **Lightweight**: 88M parameters vs BERT's 110M
- **Pre-trained**: No need to train from scratch
- **CPU-friendly**: Works well without GPU for inference

## Deployment

### Development
```bash
# In /ml-service directory
docker-compose up -d
```

### Docker Compose
```yaml
version: '3.8'

services:
  ml-service:
    build: ./ml-service
    ports:
      - "8000:8000"
    environment:
      - MODEL_CACHE_DIR=/models
      - COMPANIES_HOUSE_API_KEY=${COMPANIES_HOUSE_API_KEY}
      - LOG_LEVEL=INFO
    volumes:
      - ./ml-service:/app
      - model-cache:/models
    command: uvicorn main:app --host 0.0.0.0 --port 8000 --reload

volumes:
  model-cache:
```

## Cost Analysis

### FREE Components
- FastAPI framework: FREE
- PyTorch: FREE (open source)
- Hugging Face transformers: FREE
- DistilBERT model: FREE
- Companies House API: FREE (500 requests/day)
- Docker: FREE

### Paid Components (Optional)
- Google News API: FREE tier (100 requests/day)
- RapidAPI review aggregation: $0-15/month (basic tier)
- Hosting:
  - **AWS EC2 t2.micro (FREE TIER)**: $0 for 12 months ⭐ RECOMMENDED
  - AWS EC2 t3.small: ~$15/month (if better performance needed)

**Total MVP Cost: $0 for first year (using free tier)**

## Security Considerations

1. **API Keys**: Store in .env file, never commit
2. **Rate Limiting**: Implement on both .NET and Python sides
3. **Input Validation**: Sanitize all inputs before web scraping
4. **Timeout Handling**: Set reasonable timeouts (30s max)
5. **Error Handling**: Don't expose internal errors to admins
6. **Logging**: Log all verification requests for audit

## Testing Strategy

1. **Unit Tests**: Test each module independently
2. **Integration Tests**: Test .NET ↔ Python communication
3. **Mock Data**: Create fake companies for testing
4. **Performance**: Ensure < 5s response time
5. **Load Testing**: Handle 10 concurrent verification requests

## Future Enhancements (Sprint 4+)

- [ ] Document tampering detection (CNN-based)
- [ ] Logo verification via image similarity
- [ ] Social media presence check (Twitter, Facebook)
- [ ] Financial health indicators from public filings
- [ ] Anomaly detection across registration patterns
- [ ] Real-time monitoring of approved companies
- [ ] GPT-based analysis of business descriptions
- [ ] Blockchain verification of company claims

## Success Metrics

- **Accuracy**: 85%+ match with manual admin verification
- **Speed**: < 5 seconds per verification
- **Coverage**: Successfully verify 80%+ of UK companies
- **Admin Satisfaction**: Saves 5+ minutes per registration review
- **False Positives**: < 10% (avoid blocking legitimate companies)

## Sprint 3 Revised Breakdown

### Week 1
- **Day 1-2**: Authentication backend (5 points)
- **Day 3**: Python ML service setup (2 points)
- **Day 4**: Web intelligence module (2 points)
- **Day 5**: Sentiment analysis integration (2 points)

### Week 2
- **Day 1**: Risk scoring + .NET integration (2 points)
- **Day 2**: Admin dashboard frontend (3 points)
- **Day 3**: Authentication frontend (3 points)
- **Day 4**: Testing + bug fixes
- **Day 5**: Documentation + demo prep

**Total: 18 story points** (ambitious but achievable)

## Project Structure

```
JobStream.Api/
├── ml-service/                    # NEW Python microservice
│   ├── Dockerfile
│   ├── requirements.txt
│   ├── main.py                    # FastAPI app
│   ├── models/
│   │   ├── __init__.py
│   │   ├── requests.py            # Pydantic request models
│   │   └── responses.py           # Pydantic response models
│   ├── services/
│   │   ├── web_intelligence.py    # Web scraping & APIs
│   │   ├── sentiment_analyzer.py  # PyTorch sentiment
│   │   ├── risk_scorer.py         # Risk calculation
│   │   └── document_analyzer.py   # Optional: OCR/CV
│   ├── utils/
│   │   ├── cache.py
│   │   └── logging_config.py
│   └── tests/
│       └── test_verification.py
│
├── Services/
│   └── MLVerificationService.cs   # NEW .NET service
│
├── Models/
│   ├── AIVerificationReport.cs    # NEW
│   └── AIVerificationRequest.cs   # NEW
│
└── Controllers/
    └── AdminController.cs          # Add AI verification endpoint
```

## Implementation Priority

1. ✅ **Must Have (Sprint 3)**
   - FastAPI microservice
   - Companies House API integration
   - Basic PyTorch sentiment analysis
   - Risk scoring algorithm
   - .NET integration
   - Admin UI trigger button

2. 🔄 **Should Have (if time permits)**
   - Web scraping for news
   - Review site integration
   - Caching layer

3. 📋 **Could Have (Sprint 4)**
   - Document analysis
   - Image verification
   - Advanced anomaly detection

---

**Ready to implement?** This design gives you a working ML feature that showcases PyTorch while keeping Sprint 3 realistic at 18 points total.
