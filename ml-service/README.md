# JobStream ML Verification Service

AI-powered company verification microservice using PyTorch and FastAPI.

## Features

- **PyTorch Sentiment Analysis**: DistilBERT-based sentiment classification
- **Web Intelligence Gathering**: Companies House, LinkedIn, news mentions
- **Risk Scoring**: Automated risk assessment with recommendations
- **Fast API**: Async endpoints with automatic OpenAPI docs

## Architecture

```
┌─────────────────────────────────────────────┐
│          FastAPI Application                │
│                                             │
│  ┌─────────────────────────────────────┐  │
│  │  Sentiment Analyzer (PyTorch)       │  │
│  │  - DistilBERT model                 │  │
│  │  - Transformers library             │  │
│  └─────────────────────────────────────┘  │
│                                             │
│  ┌─────────────────────────────────────┐  │
│  │  Web Intelligence Service           │  │
│  │  - Companies House API              │  │
│  │  - Website checking                 │  │
│  │  - LinkedIn verification            │  │
│  │  - News search                      │  │
│  └─────────────────────────────────────┘  │
│                                             │
│  ┌─────────────────────────────────────┐  │
│  │  Risk Scorer                        │  │
│  │  - Weighted scoring algorithm       │  │
│  │  - Risk classification              │  │
│  └─────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
```

## Quick Start

### Local Development (without Docker)

```bash
cd ml-service

# Create virtual environment
python3 -m venv venv
source venv/bin/activate  # On Windows: venv\Scripts\activate

# Install dependencies
pip install -r requirements.txt

# Run the service
uvicorn main:app --reload --port 8000
```

The service will be available at:
- API: http://localhost:8000
- Interactive docs: http://localhost:8000/docs
- Health check: http://localhost:8000/health

### Docker Development

```bash
# From the JobStream.Api root directory
docker-compose -f docker-compose.ml.yml up --build

# Or run in background
docker-compose -f docker-compose.ml.yml up -d

# View logs
docker-compose -f docker-compose.ml.yml logs -f ml-service

# Stop service
docker-compose -f docker-compose.ml.yml down
```

## API Usage

### Verify Company

```bash
POST http://localhost:8000/api/v1/verify-company
Content-Type: application/json

{
  "registration_id": "197c8ae3-aa7b-41f0-be6e-e60e13f63232",
  "company_name": "TechCorp Solutions Ltd",
  "company_number": "12345678",
  "vat_number": "GB123456789",
  "website_url": "https://techcorp.example.com",
  "linkedin_url": "https://linkedin.com/company/techcorp",
  "business_description": "We provide innovative software solutions"
}
```

### Example Response

```json
{
  "registration_id": "197c8ae3-aa7b-41f0-be6e-e60e13f63232",
  "overall_risk_score": 23.5,
  "risk_level": "LOW",
  "verified_at": "2025-11-12T14:30:00Z",
  "processing_time_ms": 4523,
  "web_intelligence": {
    "companies_house_match": true,
    "website_active": true,
    "linkedin_verified": true,
    "news_articles_found": 8
  },
  "sentiment_analysis": {
    "overall_sentiment": "POSITIVE",
    "sentiment_score": 0.78
  },
  "risk_flags": [],
  "recommendations": [
    "✅ Company verified on Companies House",
    "✅ Active website found",
    "✅ Positive sentiment detected"
  ],
  "confidence": 0.85
}
```

## PyTorch Model

### DistilBERT Sentiment Analysis

- **Model**: `distilbert-base-uncased-finetuned-sst-2-english`
- **Parameters**: 88 million
- **Size**: ~250MB
- **Inference Time**: 500ms-1s on CPU
- **Accuracy**: 95%+ on sentiment tasks

The model is downloaded automatically on first startup and cached locally.

### Why DistilBERT?

- 60% faster than BERT
- 40% smaller than BERT
- Retains 95% of BERT's performance
- Works well on CPU (no GPU required for inference)

## Cost

### Software: $0
- PyTorch: FREE (Apache 2.0 license)
- Transformers: FREE (Apache 2.0 license)
- FastAPI: FREE (MIT license)
- Pre-trained models: FREE (Hugging Face)

### Infrastructure:
- Local development: $0
- AWS EC2 t3.small: ~$15/month
- No per-request costs

## Testing

```bash
# Install test dependencies
pip install pytest pytest-asyncio httpx

# Run tests
pytest tests/

# Run with coverage
pytest --cov=services tests/
```

## Configuration

Edit `.env` file:

```env
# Service Settings
PORT=8000
LOG_LEVEL=INFO

# Model Configuration
MODEL_NAME=distilbert-base-uncased-finetuned-sst-2-english
MODEL_CACHE_DIR=/models

# Companies House API (optional for Sprint 3)
COMPANIES_HOUSE_API_KEY=your_key_here
```

## Performance

### Expected Performance (CPU-based):

| Hardware | Inference Time | Concurrent Requests |
|----------|---------------|---------------------|
| MacBook Pro M1 | ~300ms | 10+ |
| AWS t3.small | ~800ms | 5-10 |
| AWS t3.medium | ~500ms | 10-20 |

### Optimization Tips:

1. **Batch Processing**: Process multiple companies at once
2. **Model Caching**: Model loads once on startup
3. **Async IO**: All web requests run concurrently
4. **Connection Pooling**: Reuse HTTP connections

## Integration with .NET API

The .NET API calls this service via HTTP:

```csharp
// In JobStream.Api
var mlClient = new HttpClient();
var response = await mlClient.PostAsJsonAsync(
    "http://localhost:8000/api/v1/verify-company",
    verificationRequest
);
```

## Troubleshooting

### Model Download Issues

```bash
# Manually download model
python -c "from transformers import AutoTokenizer, AutoModelForSequenceClassification; \
    AutoTokenizer.from_pretrained('distilbert-base-uncased-finetuned-sst-2-english'); \
    AutoModelForSequenceClassification.from_pretrained('distilbert-base-uncased-finetuned-sst-2-english')"
```

### Port Already in Use

```bash
# Kill process on port 8000
lsof -ti:8000 | xargs kill -9

# Or use different port
uvicorn main:app --port 8001
```

### Slow Inference

- First request is slower (model loading)
- Subsequent requests are faster (model cached in memory)
- Consider upgrading to t3.medium for better performance

## Future Enhancements (Sprint 4+)

- [ ] Real Companies House API integration
- [ ] Review site scraping (Trustpilot, Glassdoor)
- [ ] News API integration
- [ ] Document OCR and analysis
- [ ] Logo verification via image similarity
- [ ] GPU support for faster inference
- [ ] Redis caching layer
- [ ] Rate limiting
- [ ] Prometheus metrics

## License

Part of JobStream project.
