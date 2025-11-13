from fastapi import FastAPI, HTTPException, status
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
import time
from contextlib import asynccontextmanager

from models import CompanyVerificationRequest, CompanyVerificationResponse
from services.sentiment_analyzer import SentimentAnalyzer
from services.web_intelligence import WebIntelligenceService
from services.risk_scorer import RiskScorer


# Global service instances
sentiment_analyzer: SentimentAnalyzer = None
web_intelligence: WebIntelligenceService = None
risk_scorer: RiskScorer = None


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Initialize services on startup"""
    global sentiment_analyzer, web_intelligence, risk_scorer

    print("🚀 Starting ML Verification Service...")

    # Load PyTorch model (happens once on startup)
    print("📦 Loading PyTorch sentiment analysis model...")
    sentiment_analyzer = SentimentAnalyzer()
    await sentiment_analyzer.load_model()
    print("✅ Sentiment analyzer ready")

    # Initialize other services
    web_intelligence = WebIntelligenceService()
    risk_scorer = RiskScorer()
    print("✅ All services initialized")

    yield

    # Cleanup on shutdown
    print("👋 Shutting down ML Verification Service...")


# Create FastAPI app
app = FastAPI(
    title="JobStream ML Verification Service",
    description="AI-powered company verification using PyTorch sentiment analysis",
    version="1.0.0",
    lifespan=lifespan
)

# CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # TODO: Restrict to JobStream.Api domain in production
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.get("/health", status_code=status.HTTP_200_OK)
async def health_check():
    """Health check endpoint"""
    return {
        "status": "healthy",
        "service": "ml-verification",
        "version": "1.0.0",
        "model_loaded": sentiment_analyzer is not None
    }


@app.get("/", status_code=status.HTTP_200_OK)
async def root():
    """Root endpoint"""
    return {
        "service": "JobStream ML Verification Service",
        "version": "1.0.0",
        "docs": "/docs",
        "health": "/health"
    }


@app.post(
    "/api/v1/verify-company",
    response_model=CompanyVerificationResponse,
    status_code=status.HTTP_200_OK,
    summary="Verify company registration",
    description="Performs AI-powered verification of company using web intelligence and sentiment analysis"
)
async def verify_company(request: CompanyVerificationRequest):
    """
    Verify a company registration using:
    - Web intelligence gathering (Companies House, LinkedIn, news)
    - PyTorch sentiment analysis
    - Risk scoring algorithm

    Returns detailed verification report with risk assessment.
    """
    start_time = time.time()

    try:
        # Step 1: Gather web intelligence
        print(f"🔍 Gathering web intelligence for {request.company_name}...")
        web_intel = await web_intelligence.gather_intelligence(request)

        # Step 2: Perform sentiment analysis (PyTorch)
        print(f"🧠 Analyzing sentiment with PyTorch for {request.company_name}...")
        sentiment = await sentiment_analyzer.analyze_company_sentiment(
            company_name=request.company_name,
            business_description=request.business_description,
            web_intel=web_intel
        )

        # Step 3: Calculate risk score
        print(f"📊 Calculating risk score for {request.company_name}...")
        risk_result = risk_scorer.calculate_risk(
            web_intel=web_intel,
            sentiment=sentiment,
            request=request
        )

        # Calculate processing time
        processing_time_ms = int((time.time() - start_time) * 1000)

        # Build response
        response = CompanyVerificationResponse(
            registration_id=request.registration_id,
            overall_risk_score=risk_result["score"],
            risk_level=risk_result["level"],
            processing_time_ms=processing_time_ms,
            web_intelligence=web_intel,
            sentiment_analysis=sentiment,
            risk_flags=risk_result["flags"],
            recommendations=risk_result["recommendations"],
            confidence=risk_result["confidence"]
        )

        print(f"✅ Verification complete for {request.company_name} (Risk: {risk_result['level']})")
        return response

    except Exception as e:
        print(f"❌ Error verifying {request.company_name}: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Verification failed: {str(e)}"
        )


@app.exception_handler(Exception)
async def global_exception_handler(request, exc):
    """Global exception handler"""
    return JSONResponse(
        status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
        content={
            "error": "Internal server error",
            "message": str(exc),
            "type": type(exc).__name__
        }
    )


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
