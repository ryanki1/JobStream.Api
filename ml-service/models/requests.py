from pydantic import BaseModel, Field, HttpUrl
from typing import Optional
from uuid import UUID


class CompanyVerificationRequest(BaseModel):
    """Request model for company verification"""

    registration_id: UUID = Field(..., description="Company registration ID from database")
    company_name: str = Field(..., min_length=1, max_length=255, description="Company legal name")
    company_number: Optional[str] = Field(None, description="Companies House registration number")
    vat_number: Optional[str] = Field(None, description="VAT/Tax number")
    website_url: Optional[HttpUrl] = Field(None, description="Company website URL")
    linkedin_url: Optional[HttpUrl] = Field(None, description="LinkedIn company page URL")
    business_description: Optional[str] = Field(None, description="Business description from registration")

    class Config:
        json_schema_extra = {
            "example": {
                "registration_id": "197c8ae3-aa7b-41f0-be6e-e60e13f63232",
                "company_name": "TechCorp Solutions Ltd",
                "company_number": "12345678",
                "vat_number": "GB123456789",
                "website_url": "https://techcorp.example.com",
                "linkedin_url": "https://linkedin.com/company/techcorp",
                "business_description": "We provide innovative software solutions for enterprise clients"
            }
        }
