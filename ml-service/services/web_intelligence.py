import aiohttp
import asyncio
from typing import Optional
from datetime import datetime
import re

from models.requests import CompanyVerificationRequest
from models.responses import WebIntelligence


class WebIntelligenceService:
    """
    Service for gathering web intelligence about companies

    This service checks:
    - Handelsregister (German Commercial Register) for company verification
    - VIES (EU VAT validation system)
    - Website accessibility
    - LinkedIn presence
    - News mentions

    Primary target: Germany and Euro-Zone companies
    """

    def __init__(self):
        self.timeout = aiohttp.ClientTimeout(total=10)

    async def gather_intelligence(self, request: CompanyVerificationRequest) -> WebIntelligence:
        """
        Gather intelligence from multiple web sources

        Args:
            request: Company verification request

        Returns:
            WebIntelligence object with gathered data
        """
        # Run all checks concurrently for speed
        handelsregister_task = self.check_handelsregister(request.company_number)
        website_task = self.check_website(request.website_url)
        linkedin_task = self.check_linkedin(request.linkedin_url)
        news_task = self.search_news(request.company_name)

        # Await all tasks
        handelsregister_match, website_data, linkedin_data, news_data = await asyncio.gather(
            handelsregister_task,
            website_task,
            linkedin_task,
            news_task,
            return_exceptions=True
        )

        # Handle any exceptions
        if isinstance(handelsregister_match, Exception):
            handelsregister_match = False
        if isinstance(website_data, Exception):
            website_data = (False, None)
        if isinstance(linkedin_data, Exception):
            linkedin_data = (False, None)
        if isinstance(news_data, Exception):
            news_data = (0, None)

        return WebIntelligence(
            companies_house_match=handelsregister_match,
            website_active=website_data[0] if isinstance(website_data, tuple) else False,
            website_age=website_data[1] if isinstance(website_data, tuple) else None,
            linkedin_verified=linkedin_data[0] if isinstance(linkedin_data, tuple) else False,
            linkedin_followers=linkedin_data[1] if isinstance(linkedin_data, tuple) else None,
            news_articles_found=news_data[0] if isinstance(news_data, tuple) else 0,
            recent_news_date=news_data[1] if isinstance(news_data, tuple) else None
        )

    async def check_handelsregister(self, company_number: Optional[str]) -> bool:
        """
        Check if company exists in German Handelsregister (Commercial Register)

        This can verify:
        - German companies via Handelsregister/Unternehmensregister
        - EU companies via VIES VAT validation

        Args:
            company_number: German HRB/HRA number or EU company registration number

        Returns:
            True if company found, False otherwise
        """
        if not company_number:
            return False

        try:
            # German Handelsregister format examples:
            # HRB 12345 (GmbH)
            # HRA 12345 (e.K., OHG, KG)
            # Local court: "HRB 12345 München"

            # Clean company number
            clean_number = company_number.strip()

            # TODO: Implement actual Handelsregister API call
            # Options:
            # 1. Unternehmensregister API (https://www.unternehmensregister.de)
            # 2. Handelsregister API (requires paid access or scraping)
            # 3. North Data API (commercial, has free tier)
            # 4. OpenCorporates API (international registry aggregator)

            # For Sprint 3, we'll validate format and return mock response
            # Real implementation would look like:
            # url = f"https://www.unternehmensregister.de/ureg/search1.9.html?submitaction=search&suchart=unternehmen"
            # Or use VIES for VAT validation

            print(f"🏢 Checking Handelsregister for: {clean_number}")

            # Basic validation: German HRB/HRA numbers typically have this format
            is_valid_format = bool(re.search(r'(HRB|HRA|GmbH|\d{6,})', clean_number, re.IGNORECASE))

            return is_valid_format  # Mock response for Sprint 3

        except Exception as e:
            print(f"❌ Handelsregister check failed: {e}")
            return False

    async def check_website(self, website_url: Optional[str]) -> tuple[bool, Optional[str]]:
        """
        Check if website is accessible

        Args:
            website_url: Company website URL

        Returns:
            Tuple of (is_active, estimated_age)
        """
        if not website_url:
            return (False, None)

        try:
            async with aiohttp.ClientSession(timeout=self.timeout) as session:
                async with session.get(str(website_url), allow_redirects=True) as response:
                    if response.status == 200:
                        print(f"✅ Website accessible: {website_url}")

                        # Try to estimate website age from headers or content
                        # This is a simplified version
                        website_age = "Unknown"

                        return (True, website_age)
                    else:
                        print(f"⚠️  Website returned status {response.status}: {website_url}")
                        return (False, None)

        except asyncio.TimeoutError:
            print(f"⏱️  Website timeout: {website_url}")
            return (False, None)
        except Exception as e:
            print(f"❌ Website check failed: {e}")
            return (False, None)

    async def check_linkedin(self, linkedin_url: Optional[str]) -> tuple[bool, Optional[int]]:
        """
        Check if LinkedIn company page exists

        Args:
            linkedin_url: LinkedIn company URL

        Returns:
            Tuple of (exists, follower_count)
        """
        if not linkedin_url:
            return (False, None)

        try:
            # LinkedIn scraping is complex and requires authentication
            # For Sprint 3, we'll do a basic URL check

            async with aiohttp.ClientSession(timeout=self.timeout) as session:
                async with session.head(str(linkedin_url), allow_redirects=True) as response:
                    if response.status == 200:
                        print(f"✅ LinkedIn page found: {linkedin_url}")
                        # Mock follower count for Sprint 3
                        follower_count = 500  # Would need scraping or API for real count
                        return (True, follower_count)
                    else:
                        print(f"⚠️  LinkedIn page not found: {linkedin_url}")
                        return (False, None)

        except Exception as e:
            print(f"❌ LinkedIn check failed: {e}")
            return (False, None)

    async def search_news(self, company_name: str) -> tuple[int, Optional[datetime]]:
        """
        Search for news articles about the company

        Args:
            company_name: Company name to search

        Returns:
            Tuple of (article_count, most_recent_date)
        """
        try:
            # In a real implementation, you would use:
            # - Google News API
            # - NewsAPI.org
            # - Web scraping of news sites

            # For Sprint 3, we'll simulate this
            print(f"📰 Searching news for: {company_name}")

            # Mock response - would need real news API
            article_count = 0  # Would be populated by real API
            most_recent_date = None

            return (article_count, most_recent_date)

        except Exception as e:
            print(f"❌ News search failed: {e}")
            return (0, None)

    async def scrape_reviews(self, company_name: str) -> dict:
        """
        Scrape reviews from review sites (Trustpilot, Glassdoor, etc.)

        This is a placeholder for Sprint 4 - requires more complex scraping

        Args:
            company_name: Company name

        Returns:
            Dictionary with review data
        """
        # TODO: Implement in Sprint 4
        return {
            "trustpilot": None,
            "glassdoor": None,
            "google": None
        }
