import torch
from transformers import AutoTokenizer, AutoModelForSequenceClassification
from typing import Optional, Dict, List
import asyncio

from models.responses import SentimentAnalysis, SentimentType, SentimentSource, WebIntelligence


class SentimentAnalyzer:
    """
    PyTorch-based sentiment analyzer using multilingual BERT

    This service uses a pre-trained transformer model to analyze sentiment
    of text related to companies (reviews, news, descriptions, etc.)

    Supports multiple languages including:
    - German (primary target market)
    - English (international companies)
    - French, Spanish, Italian, Dutch
    """

    def __init__(self, model_name: str = "nlptown/bert-base-multilingual-uncased-sentiment"):
        """
        Initialize sentiment analyzer

        Args:
            model_name: Hugging Face model identifier
                Default: nlptown/bert-base-multilingual-uncased-sentiment
                - Trained on product reviews in 6 languages (de, en, es, fr, it, nl)
                - Outputs 5-star rating (1-5 stars)
                - Perfect for German/English business descriptions
        """
        self.model_name = model_name
        self.tokenizer = None
        self.model = None
        self.device = "cuda" if torch.cuda.is_available() else "cpu"
        print(f"🖥️  Using device: {self.device}")
        print(f"🌍 Multilingual model: Supports German, English, and 4 other languages")

    async def load_model(self):
        """Load the PyTorch model and tokenizer (async to not block startup)"""
        try:
            # Run model loading in thread pool to avoid blocking
            loop = asyncio.get_event_loop()
            await loop.run_in_executor(None, self._load_model_sync)
            print(f"✅ Loaded model: {self.model_name}")
        except Exception as e:
            print(f"❌ Failed to load model: {e}")
            raise

    def _load_model_sync(self):
        """Synchronous model loading"""
        self.tokenizer = AutoTokenizer.from_pretrained(self.model_name)
        self.model = AutoModelForSequenceClassification.from_pretrained(self.model_name)
        self.model.to(self.device)
        self.model.eval()  # Set to evaluation mode

    def analyze_text(self, text: str) -> Dict[str, float]:
        """
        Analyze sentiment of a single text using PyTorch

        Args:
            text: Text to analyze

        Returns:
            Dictionary with sentiment classification and scores
        """
        if not text or len(text.strip()) == 0:
            return {
                "sentiment": "NEUTRAL",
                "score": 0.5,
                "positive_score": 0.5,
                "negative_score": 0.5
            }

        # Tokenize input
        inputs = self.tokenizer(
            text,
            return_tensors="pt",
            truncation=True,
            max_length=512,
            padding=True
        )

        # Move to device
        inputs = {k: v.to(self.device) for k, v in inputs.items()}

        # Run inference
        with torch.no_grad():
            outputs = self.model(**inputs)
            predictions = torch.nn.functional.softmax(outputs.logits, dim=-1)

        # Extract scores
        negative_score = predictions[0][0].item()
        positive_score = predictions[0][1].item()

        # Determine sentiment
        if positive_score > 0.6:
            sentiment = "POSITIVE"
        elif negative_score > 0.6:
            sentiment = "NEGATIVE"
        else:
            sentiment = "NEUTRAL"

        return {
            "sentiment": sentiment,
            "score": max(positive_score, negative_score),
            "positive_score": positive_score,
            "negative_score": negative_score
        }

    async def analyze_company_sentiment(
        self,
        company_name: str,
        business_description: Optional[str],
        web_intel: WebIntelligence
    ) -> SentimentAnalysis:
        """
        Analyze overall company sentiment from multiple sources

        Args:
            company_name: Company name
            business_description: Business description text
            web_intel: Web intelligence data (may contain reviews, news, etc.)

        Returns:
            SentimentAnalysis object with aggregated results
        """
        sources: List[SentimentSource] = []
        sentiment_scores: List[float] = []

        # Analyze business description if provided
        if business_description:
            desc_sentiment = self.analyze_text(business_description)
            sources.append(SentimentSource(
                source="Business Description",
                sentiment=SentimentType(desc_sentiment["sentiment"]),
                rating=None,
                review_count=None
            ))
            sentiment_scores.append(desc_sentiment["positive_score"])

        # Simulate sentiment from web intelligence
        # In a real implementation, you would:
        # 1. Scrape actual reviews from Trustpilot/Glassdoor
        # 2. Fetch news articles and analyze them
        # 3. Check social media mentions

        # For Sprint 3, we'll create mock sentiment based on web presence
        if web_intel.companies_house_match and web_intel.website_active:
            # Company has legitimate presence - simulate positive sentiment
            mock_review_text = f"{company_name} is a legitimate company with an active web presence"
            review_sentiment = self.analyze_text(mock_review_text)

            sources.append(SentimentSource(
                source="Web Presence Analysis",
                sentiment=SentimentType(review_sentiment["sentiment"]),
                rating=4.0 if review_sentiment["sentiment"] == "POSITIVE" else 3.0,
                review_count=web_intel.news_articles_found if web_intel.news_articles_found > 0 else 1
            ))
            sentiment_scores.append(review_sentiment["positive_score"])

        # Handle case where no sentiment data is available
        if len(sentiment_scores) == 0:
            return SentimentAnalysis(
                overall_sentiment=SentimentType.NEUTRAL,
                sentiment_score=0.5,
                sources=[]
            )

        # Calculate average sentiment
        avg_score = sum(sentiment_scores) / len(sentiment_scores)

        # Determine overall sentiment
        if avg_score > 0.6:
            overall_sentiment = SentimentType.POSITIVE
        elif avg_score < 0.4:
            overall_sentiment = SentimentType.NEGATIVE
        else:
            overall_sentiment = SentimentType.NEUTRAL

        return SentimentAnalysis(
            overall_sentiment=overall_sentiment,
            sentiment_score=avg_score,
            sources=sources
        )

    async def batch_analyze(self, texts: List[str]) -> List[Dict[str, float]]:
        """
        Analyze multiple texts in batch for efficiency

        Args:
            texts: List of texts to analyze

        Returns:
            List of sentiment results
        """
        results = []
        for text in texts:
            result = self.analyze_text(text)
            results.append(result)
        return results
