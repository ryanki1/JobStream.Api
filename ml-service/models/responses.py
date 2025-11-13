from pydantic import BaseModel, Field
from typing import List, Optional
from datetime import datetime
from enum import Enum
from uuid import UUID


class RiskLevel(str, Enum):
    """Risk level classification"""
    LOW = "LOW"
    MEDIUM = "MEDIUM"
    HIGH = "HIGH"


class SentimentType(str, Enum):
    """Sentiment classification"""
    POSITIVE = "POSITIVE"
    NEUTRAL = "NEUTRAL"
    NEGATIVE = "NEGATIVE"


class SentimentSource(BaseModel):
    """Individual sentiment source details"""
    source: str = Field(..., description="Source name (e.g., Trustpilot, Google News)")
    rating: Optional[float] = Field(None, ge=0, le=5, description="Rating out of 5 if applicable")
    review_count: Optional[int] = Field(None, ge=0, description="Number of reviews")
    article_count: Optional[int] = Field(None, ge=0, description="Number of articles found")
    sentiment: SentimentType = Field(..., description="Sentiment classification")


class SentimentAnalysis(BaseModel):
    """Sentiment analysis results"""
    overall_sentiment: SentimentType = Field(..., description="Overall sentiment classification")
    sentiment_score: float = Field(..., ge=0, le=1, description="Sentiment confidence score (0-1)")
    sources: List[SentimentSource] = Field(default_factory=list, description="Individual source sentiments")


class WebIntelligence(BaseModel):
    """Web intelligence gathering results"""
    companies_house_match: bool = Field(
        ...,
        description="Whether company found in business register (Handelsregister for DE, Companies House for UK, etc.)",
        alias="handelsregister_match"
    )
    website_active: bool = Field(..., description="Whether website is accessible")
    website_age: Optional[str] = Field(None, description="Estimated website age")
    linkedin_verified: bool = Field(..., description="Whether LinkedIn page exists")
    linkedin_followers: Optional[int] = Field(None, ge=0, description="LinkedIn follower count")
    news_articles_found: int = Field(0, ge=0, description="Number of news articles found")
    recent_news_date: Optional[datetime] = Field(None, description="Date of most recent news article")

    class Config:
        populate_by_name = True  # Allow both field name and alias


class CompanyVerificationResponse(BaseModel):
    """Complete verification response"""
    registration_id: UUID = Field(..., description="Company registration ID")
    overall_risk_score: float = Field(..., ge=0, le=100, description="Risk score (0-100, lower is better)")
    risk_level: RiskLevel = Field(..., description="Risk classification")
    verified_at: datetime = Field(default_factory=datetime.utcnow, description="Verification timestamp")
    processing_time_ms: int = Field(..., ge=0, description="Processing time in milliseconds")

    web_intelligence: WebIntelligence = Field(..., description="Web intelligence results")
    sentiment_analysis: SentimentAnalysis = Field(..., description="Sentiment analysis results")

    risk_flags: List[str] = Field(default_factory=list, description="List of risk flags identified")
    recommendations: List[str] = Field(default_factory=list, description="Actionable recommendations")

    confidence: float = Field(..., ge=0, le=1, description="Overall confidence in the assessment")

    class Config:
        json_schema_extra = {
            "example": {
                "registration_id": "197c8ae3-aa7b-41f0-be6e-e60e13f63232",
                "overall_risk_score": 23.5,
                "risk_level": "LOW",
                "verified_at": "2025-11-12T14:30:00Z",
                "processing_time_ms": 4523,
                "web_intelligence": {
                    "companies_house_match": True,
                    "website_active": True,
                    "website_age": "3 years",
                    "linkedin_verified": True,
                    "linkedin_followers": 1250,
                    "news_articles_found": 8,
                    "recent_news_date": "2025-10-15T00:00:00Z"
                },
                "sentiment_analysis": {
                    "overall_sentiment": "POSITIVE",
                    "sentiment_score": 0.78,
                    "sources": [
                        {
                            "source": "Trustpilot",
                            "rating": 4.2,
                            "review_count": 45,
                            "sentiment": "POSITIVE"
                        }
                    ]
                },
                "risk_flags": [],
                "recommendations": [
                    "Company verified on Companies House",
                    "Active web presence with positive sentiment"
                ],
                "confidence": 0.85
            }
        }
