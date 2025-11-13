from typing import Dict, List
from models.requests import CompanyVerificationRequest
from models.responses import WebIntelligence, SentimentAnalysis, RiskLevel, SentimentType


class RiskScorer:
    """
    Risk scoring algorithm for company verification

    Calculates risk score based on:
    - Web presence indicators
    - Sentiment analysis results
    - Company information completeness
    """

    # Risk weights (sum to 100)
    WEIGHTS = {
        "business_register": 25,     # Very important (Handelsregister/Companies House)
        "website": 15,               # Important
        "linkedin": 10,              # Moderately important
        "news_presence": 10,         # Moderately important
        "sentiment": 25,             # Very important
        "information_completeness": 15  # Important
    }

    def calculate_risk(
        self,
        web_intel: WebIntelligence,
        sentiment: SentimentAnalysis,
        request: CompanyVerificationRequest
    ) -> Dict:
        """
        Calculate overall risk score and generate recommendations

        Args:
            web_intel: Web intelligence data
            sentiment: Sentiment analysis results
            request: Original verification request

        Returns:
            Dictionary with risk score, level, flags, recommendations, and confidence
        """
        risk_score = 0.0
        flags: List[str] = []
        recommendations: List[str] = []

        # 1. Business Register verification (0-25 points risk)
        # Handelsregister (DE), Companies House (UK), or other EU registers
        if not web_intel.companies_house_match:
            risk_score += self.WEIGHTS["business_register"]
            flags.append("BUSINESS_REGISTER_NOT_FOUND")
            recommendations.append("⚠️ Company not found in business register (Handelsregister/Companies House)")
        else:
            recommendations.append("✅ Company verified in official business register")

        # 2. Website presence (0-15 points risk)
        if not web_intel.website_active:
            risk_score += self.WEIGHTS["website"]
            flags.append("WEBSITE_UNREACHABLE")
            recommendations.append("⚠️ Website URL is not accessible")
        else:
            recommendations.append("✅ Active website found")

        # 3. LinkedIn presence (0-10 points risk)
        if not web_intel.linkedin_verified:
            risk_score += self.WEIGHTS["linkedin"] * 0.5  # Partial risk - LinkedIn is optional
            flags.append("NO_LINKEDIN_PRESENCE")
            recommendations.append("⚠️ No LinkedIn company page found")
        else:
            if web_intel.linkedin_followers and web_intel.linkedin_followers > 100:
                recommendations.append(f"✅ LinkedIn page with {web_intel.linkedin_followers} followers")
            else:
                recommendations.append("ℹ️ LinkedIn page found (limited followers)")

        # 4. News/Web mentions (0-10 points risk)
        if web_intel.news_articles_found == 0:
            risk_score += self.WEIGHTS["news_presence"] * 0.7  # Partial risk - new companies may have no news
            flags.append("NO_WEB_MENTIONS")
            recommendations.append("ℹ️ No news articles found (may be new company)")
        else:
            recommendations.append(f"✅ Found {web_intel.news_articles_found} news articles")

        # 5. Sentiment analysis (0-25 points risk)
        if sentiment.overall_sentiment == SentimentType.NEGATIVE:
            risk_score += self.WEIGHTS["sentiment"]
            flags.append("NEGATIVE_SENTIMENT")
            recommendations.append(f"⚠️ Negative sentiment detected (score: {sentiment.sentiment_score:.2f})")
        elif sentiment.overall_sentiment == SentimentType.NEUTRAL:
            risk_score += self.WEIGHTS["sentiment"] * 0.3  # Partial risk
            recommendations.append(f"ℹ️ Neutral sentiment (score: {sentiment.sentiment_score:.2f})")
        else:
            recommendations.append(f"✅ Positive sentiment detected (score: {sentiment.sentiment_score:.2f})")

        # 6. Information completeness (0-15 points risk)
        provided_fields = 0
        total_fields = 5  # company_number, vat, website, linkedin, description

        if request.company_number:
            provided_fields += 1
        if request.vat_number:
            provided_fields += 1
        if request.website_url:
            provided_fields += 1
        if request.linkedin_url:
            provided_fields += 1
        if request.business_description:
            provided_fields += 1

        completeness_ratio = provided_fields / total_fields
        if completeness_ratio < 0.6:  # Less than 60% complete
            info_risk = self.WEIGHTS["information_completeness"] * (1 - completeness_ratio)
            risk_score += info_risk
            flags.append("INCOMPLETE_INFORMATION")
            recommendations.append(f"⚠️ Limited information provided ({provided_fields}/{total_fields} fields)")
        else:
            recommendations.append(f"✅ Complete information provided ({provided_fields}/{total_fields} fields)")

        # Determine risk level
        if risk_score <= 30:
            risk_level = RiskLevel.LOW
        elif risk_score <= 60:
            risk_level = RiskLevel.MEDIUM
        else:
            risk_level = RiskLevel.HIGH

        # Calculate confidence based on data availability
        confidence = self._calculate_confidence(web_intel, sentiment, completeness_ratio)

        # Add final recommendation
        if risk_level == RiskLevel.HIGH:
            recommendations.append("⛔ RECOMMEND MANUAL VERIFICATION")
        elif risk_level == RiskLevel.MEDIUM:
            recommendations.append("⚠️ Review carefully before approval")
        else:
            recommendations.append("✅ Low risk - likely legitimate company")

        return {
            "score": round(risk_score, 1),
            "level": risk_level,
            "flags": flags,
            "recommendations": recommendations,
            "confidence": round(confidence, 2)
        }

    def _calculate_confidence(
        self,
        web_intel: WebIntelligence,
        sentiment: SentimentAnalysis,
        completeness_ratio: float
    ) -> float:
        """
        Calculate confidence in the risk assessment

        Higher confidence = more data available to make assessment

        Returns:
            Confidence score between 0 and 1
        """
        confidence_factors = []

        # Companies House data available
        if web_intel.companies_house_match:
            confidence_factors.append(0.9)
        else:
            confidence_factors.append(0.5)

        # Website data available
        if web_intel.website_active:
            confidence_factors.append(0.8)
        else:
            confidence_factors.append(0.4)

        # Sentiment data available
        if len(sentiment.sources) > 0:
            confidence_factors.append(0.85)
        else:
            confidence_factors.append(0.5)

        # Information completeness
        confidence_factors.append(completeness_ratio)

        # Average of all factors
        return sum(confidence_factors) / len(confidence_factors)
