const { expect } = require("chai");
const { ethers } = require("hardhat");
const { loadFixture } = require("@nomicfoundation/hardhat-network-helpers");

describe("JobPosting Smart Contract", function () {
  // Fixture to deploy contract once and reuse for all tests
  async function deployJobPostingFixture() {
    const [owner, company1, company2, otherAccount] = await ethers.getSigners();

    const JobPosting = await ethers.getContractFactory("JobPosting");
    const jobPosting = await JobPosting.deploy();

    return { jobPosting, owner, company1, company2, otherAccount };
  }

  describe("Deployment", function () {
    it("Should set the correct owner", async function () {
      const { jobPosting, owner } = await loadFixture(deployJobPostingFixture);
      expect(await jobPosting.owner()).to.equal(owner.address);
    });

    it("Should initialize posting ID counter to 1000", async function () {
      const { jobPosting } = await loadFixture(deployJobPostingFixture);
      expect(await jobPosting.getCurrentPostingId()).to.equal(1000);
    });
  });

  describe("Creating Draft Postings", function () {
    it("Should create a draft posting successfully", async function () {
      const { jobPosting, company1 } = await loadFixture(deployJobPostingFixture);

      const companyId = "company123";
      const title = "Senior Blockchain Developer";
      const description = "Looking for experienced Solidity developer";

      const tx = await jobPosting.connect(company1).createDraftPosting(
        companyId,
        title,
        description
      );

      // Check event was emitted
      await expect(tx)
        .to.emit(jobPosting, "PostingCreated")
        .withArgs(1000, companyId, company1.address, title);

      // Verify posting was stored correctly
      const posting = await jobPosting.getPosting(1000);
      expect(posting.id).to.equal(1000);
      expect(posting.companyId).to.equal(companyId);
      expect(posting.title).to.equal(title);
      expect(posting.description).to.equal(description);
      expect(posting.status).to.equal(0); // Draft status
      expect(posting.walletAddress).to.equal(company1.address);
      expect(posting.publishedAt).to.equal(0);
      expect(posting.exists).to.be.true;
    });

    it("Should increment posting ID counter", async function () {
      const { jobPosting, company1 } = await loadFixture(deployJobPostingFixture);

      await jobPosting.connect(company1).createDraftPosting(
        "company1",
        "Job 1",
        "Description 1"
      );

      expect(await jobPosting.getCurrentPostingId()).to.equal(1001);

      await jobPosting.connect(company1).createDraftPosting(
        "company1",
        "Job 2",
        "Description 2"
      );

      expect(await jobPosting.getCurrentPostingId()).to.equal(1002);
    });

    it("Should track company postings", async function () {
      const { jobPosting, company1 } = await loadFixture(deployJobPostingFixture);

      await jobPosting.connect(company1).createDraftPosting(
        "company1",
        "Job 1",
        "Description 1"
      );
      await jobPosting.connect(company1).createDraftPosting(
        "company1",
        "Job 2",
        "Description 2"
      );

      const companyPostings = await jobPosting.getCompanyPostings(company1.address);
      expect(companyPostings.length).to.equal(2);
      expect(companyPostings[0]).to.equal(1000);
      expect(companyPostings[1]).to.equal(1001);
    });

    it("Should fail if company ID is empty", async function () {
      const { jobPosting, company1 } = await loadFixture(deployJobPostingFixture);

      await expect(
        jobPosting.connect(company1).createDraftPosting("", "Title", "Description")
      ).to.be.revertedWith("Company ID cannot be empty");
    });

    it("Should fail if title is empty", async function () {
      const { jobPosting, company1 } = await loadFixture(deployJobPostingFixture);

      await expect(
        jobPosting.connect(company1).createDraftPosting("company1", "", "Description")
      ).to.be.revertedWith("Title cannot be empty");
    });

    it("Should fail if description is empty", async function () {
      const { jobPosting, company1 } = await loadFixture(deployJobPostingFixture);

      await expect(
        jobPosting.connect(company1).createDraftPosting("company1", "Title", "")
      ).to.be.revertedWith("Description cannot be empty");
    });
  });

  describe("Publishing Postings", function () {
    it("Should publish a draft posting successfully", async function () {
      const { jobPosting, company1 } = await loadFixture(deployJobPostingFixture);

      // Create draft first
      await jobPosting.connect(company1).createDraftPosting(
        "company1",
        "Job Title",
        "Job Description"
      );

      // Publish it
      const tx = await jobPosting.connect(company1).publishPosting(1000);

      // Check event
      await expect(tx)
        .to.emit(jobPosting, "PostingPublished");

      // Verify status changed
      const posting = await jobPosting.getPosting(1000);
      expect(posting.status).to.equal(1); // Published status
      expect(posting.publishedAt).to.be.greaterThan(0);
    });

    it("Should verify posting is published", async function () {
      const { jobPosting, company1 } = await loadFixture(deployJobPostingFixture);

      await jobPosting.connect(company1).createDraftPosting(
        "company1",
        "Job Title",
        "Job Description"
      );

      // Should not be published yet
      expect(await jobPosting.isPostingPublished(1000)).to.be.false;

      await jobPosting.connect(company1).publishPosting(1000);

      // Now should be published
      expect(await jobPosting.isPostingPublished(1000)).to.be.true;
    });

    it("Should fail if posting does not exist", async function () {
      const { jobPosting, company1 } = await loadFixture(deployJobPostingFixture);

      await expect(
        jobPosting.connect(company1).publishPosting(9999)
      ).to.be.revertedWith("Posting does not exist");
    });

    it("Should fail if caller is not the posting owner", async function () {
      const { jobPosting, company1, company2 } = await loadFixture(deployJobPostingFixture);

      await jobPosting.connect(company1).createDraftPosting(
        "company1",
        "Job Title",
        "Job Description"
      );

      await expect(
        jobPosting.connect(company2).publishPosting(1000)
      ).to.be.revertedWith("Not the posting owner");
    });

    it("Should fail if posting is already published", async function () {
      const { jobPosting, company1 } = await loadFixture(deployJobPostingFixture);

      await jobPosting.connect(company1).createDraftPosting(
        "company1",
        "Job Title",
        "Job Description"
      );

      await jobPosting.connect(company1).publishPosting(1000);

      // Try to publish again
      await expect(
        jobPosting.connect(company1).publishPosting(1000)
      ).to.be.revertedWith("Posting must be in draft status");
    });
  });

  describe("Updating Draft Postings", function () {
    it("Should update a draft posting successfully", async function () {
      const { jobPosting, company1 } = await loadFixture(deployJobPostingFixture);

      await jobPosting.connect(company1).createDraftPosting(
        "company1",
        "Original Title",
        "Original Description"
      );

      const newTitle = "Updated Title";
      const newDescription = "Updated Description";

      const tx = await jobPosting.connect(company1).updateDraftPosting(
        1000,
        newTitle,
        newDescription
      );

      // Check event
      await expect(tx)
        .to.emit(jobPosting, "PostingUpdated")
        .withArgs(1000, newTitle);

      // Verify update
      const posting = await jobPosting.getPosting(1000);
      expect(posting.title).to.equal(newTitle);
      expect(posting.description).to.equal(newDescription);
    });

    it("Should fail to update if posting does not exist", async function () {
      const { jobPosting, company1 } = await loadFixture(deployJobPostingFixture);

      await expect(
        jobPosting.connect(company1).updateDraftPosting(9999, "Title", "Description")
      ).to.be.revertedWith("Posting does not exist");
    });

    it("Should fail to update if caller is not the posting owner", async function () {
      const { jobPosting, company1, company2 } = await loadFixture(deployJobPostingFixture);

      await jobPosting.connect(company1).createDraftPosting(
        "company1",
        "Title",
        "Description"
      );

      await expect(
        jobPosting.connect(company2).updateDraftPosting(1000, "New Title", "New Description")
      ).to.be.revertedWith("Not the posting owner");
    });

    it("Should fail to update if posting is published", async function () {
      const { jobPosting, company1 } = await loadFixture(deployJobPostingFixture);

      await jobPosting.connect(company1).createDraftPosting(
        "company1",
        "Title",
        "Description"
      );

      await jobPosting.connect(company1).publishPosting(1000);

      await expect(
        jobPosting.connect(company1).updateDraftPosting(1000, "New Title", "New Description")
      ).to.be.revertedWith("Posting must be in draft status");
    });

    it("Should fail if new title is empty", async function () {
      const { jobPosting, company1 } = await loadFixture(deployJobPostingFixture);

      await jobPosting.connect(company1).createDraftPosting(
        "company1",
        "Title",
        "Description"
      );

      await expect(
        jobPosting.connect(company1).updateDraftPosting(1000, "", "New Description")
      ).to.be.revertedWith("Title cannot be empty");
    });

    it("Should fail if new description is empty", async function () {
      const { jobPosting, company1 } = await loadFixture(deployJobPostingFixture);

      await jobPosting.connect(company1).createDraftPosting(
        "company1",
        "Title",
        "Description"
      );

      await expect(
        jobPosting.connect(company1).updateDraftPosting(1000, "New Title", "")
      ).to.be.revertedWith("Description cannot be empty");
    });
  });

  describe("View Functions", function () {
    it("Should verify posting exists", async function () {
      const { jobPosting, company1 } = await loadFixture(deployJobPostingFixture);

      expect(await jobPosting.verifyPostingExists(1000)).to.be.false;

      await jobPosting.connect(company1).createDraftPosting(
        "company1",
        "Title",
        "Description"
      );

      expect(await jobPosting.verifyPostingExists(1000)).to.be.true;
    });

    it("Should get posting details", async function () {
      const { jobPosting, company1 } = await loadFixture(deployJobPostingFixture);

      const companyId = "company123";
      const title = "Test Job";
      const description = "Test Description";

      await jobPosting.connect(company1).createDraftPosting(
        companyId,
        title,
        description
      );

      const posting = await jobPosting.getPosting(1000);
      expect(posting.companyId).to.equal(companyId);
      expect(posting.title).to.equal(title);
      expect(posting.description).to.equal(description);
    });

    it("Should fail to get non-existent posting", async function () {
      const { jobPosting } = await loadFixture(deployJobPostingFixture);

      await expect(
        jobPosting.getPosting(9999)
      ).to.be.revertedWith("Posting does not exist");
    });
  });

  describe("Multiple Companies Scenario", function () {
    it("Should handle postings from multiple companies", async function () {
      const { jobPosting, company1, company2 } = await loadFixture(deployJobPostingFixture);

      // Company 1 creates two postings
      await jobPosting.connect(company1).createDraftPosting(
        "company1",
        "Job 1 from Company 1",
        "Description"
      );
      await jobPosting.connect(company1).createDraftPosting(
        "company1",
        "Job 2 from Company 1",
        "Description"
      );

      // Company 2 creates one posting
      await jobPosting.connect(company2).createDraftPosting(
        "company2",
        "Job 1 from Company 2",
        "Description"
      );

      // Verify each company's postings
      const company1Postings = await jobPosting.getCompanyPostings(company1.address);
      const company2Postings = await jobPosting.getCompanyPostings(company2.address);

      expect(company1Postings.length).to.equal(2);
      expect(company2Postings.length).to.equal(1);

      // Verify ownership
      const posting1 = await jobPosting.getPosting(1000);
      const posting2 = await jobPosting.getPosting(1001);
      const posting3 = await jobPosting.getPosting(1002);

      expect(posting1.walletAddress).to.equal(company1.address);
      expect(posting2.walletAddress).to.equal(company1.address);
      expect(posting3.walletAddress).to.equal(company2.address);
    });
  });

  describe("Complete Workflow", function () {
    it("Should complete full lifecycle: create -> update -> publish", async function () {
      const { jobPosting, company1 } = await loadFixture(deployJobPostingFixture);

      // Step 1: Create draft
      await jobPosting.connect(company1).createDraftPosting(
        "company1",
        "Original Title",
        "Original Description"
      );

      let posting = await jobPosting.getPosting(1000);
      expect(posting.status).to.equal(0); // Draft
      expect(posting.title).to.equal("Original Title");

      // Step 2: Update draft
      await jobPosting.connect(company1).updateDraftPosting(
        1000,
        "Updated Title",
        "Updated Description"
      );

      posting = await jobPosting.getPosting(1000);
      expect(posting.status).to.equal(0); // Still draft
      expect(posting.title).to.equal("Updated Title");

      // Step 3: Publish
      await jobPosting.connect(company1).publishPosting(1000);

      posting = await jobPosting.getPosting(1000);
      expect(posting.status).to.equal(1); // Published
      expect(posting.publishedAt).to.be.greaterThan(0);

      // Step 4: Verify published and immutable
      expect(await jobPosting.isPostingPublished(1000)).to.be.true;

      await expect(
        jobPosting.connect(company1).updateDraftPosting(1000, "New Title", "New Description")
      ).to.be.revertedWith("Posting must be in draft status");
    });
  });
});
