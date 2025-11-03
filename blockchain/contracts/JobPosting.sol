// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

/**
 * @title JobPosting
 * @dev Smart Contract for managing blockchain-based job postings on Polygon
 * @notice Implements SCRUM-4: Blockchain job posting system for MVP/Deliverable-based projects
 */
contract JobPosting {
    // ============ State Variables ============

    /// @dev Counter for generating unique posting IDs
    uint256 private _postingIdCounter;

    /// @dev Owner of the contract (deployer)
    address public owner;

    // ============ Enums ============

    /// @dev Status of a job posting
    enum PostingStatus {
        Draft,      // 0: Editable, not visible publicly
        Published   // 1: Live on blockchain, immutable
    }

    // ============ Structs ============

    /// @dev Structure representing a job posting
    struct Posting {
        uint256 id;                 // Unique posting ID
        string companyId;           // Company identifier
        string title;               // Job title
        string description;         // Job description
        PostingStatus status;       // Current status
        address walletAddress;      // Company wallet address
        uint256 createdAt;          // Creation timestamp
        uint256 publishedAt;        // Publication timestamp (0 if not published)
        bool exists;                // Flag to check if posting exists
    }

    // ============ Storage Mappings ============

    /// @dev Mapping from posting ID to Posting struct
    mapping(uint256 => Posting) public postings;

    /// @dev Mapping from company wallet to their posting IDs
    mapping(address => uint256[]) public companyPostings;

    // ============ Events ============

    /// @dev Emitted when a new draft posting is created
    event PostingCreated(
        uint256 indexed postingId,
        string indexed companyId,
        address indexed walletAddress,
        string title
    );

    /// @dev Emitted when a posting is published
    event PostingPublished(
        uint256 indexed postingId,
        string indexed companyId,
        uint256 publishedAt
    );

    /// @dev Emitted when a draft posting is updated
    event PostingUpdated(
        uint256 indexed postingId,
        string title
    );

    // ============ Modifiers ============

    /// @dev Ensures only the contract owner can call the function
    modifier onlyOwner() {
        require(msg.sender == owner, "Only owner can call this function");
        _;
    }

    /// @dev Ensures the posting exists
    modifier postingExists(uint256 postingId) {
        require(postings[postingId].exists, "Posting does not exist");
        _;
    }

    /// @dev Ensures the caller is the owner of the posting
    modifier onlyPostingOwner(uint256 postingId) {
        require(
            postings[postingId].walletAddress == msg.sender,
            "Not the posting owner"
        );
        _;
    }

    /// @dev Ensures the posting is in draft status
    modifier onlyDraft(uint256 postingId) {
        require(
            postings[postingId].status == PostingStatus.Draft,
            "Posting must be in draft status"
        );
        _;
    }

    // ============ Constructor ============

    constructor() {
        owner = msg.sender;
        _postingIdCounter = 1000; // Start IDs from 1000
    }

    // ============ Core Functions ============

    /**
     * @dev Creates a new draft job posting
     * @param companyId The company identifier
     * @param title The job title
     * @param description The job description
     * @return postingId The ID of the created posting
     */
    function createDraftPosting(
        string memory companyId,
        string memory title,
        string memory description
    ) external returns (uint256 postingId) {
        require(bytes(companyId).length > 0, "Company ID cannot be empty");
        require(bytes(title).length > 0, "Title cannot be empty");
        require(bytes(description).length > 0, "Description cannot be empty");

        postingId = _postingIdCounter;
        _postingIdCounter++;

        Posting storage newPosting = postings[postingId];
        newPosting.id = postingId;
        newPosting.companyId = companyId;
        newPosting.title = title;
        newPosting.description = description;
        newPosting.status = PostingStatus.Draft;
        newPosting.walletAddress = msg.sender;
        newPosting.createdAt = block.timestamp;
        newPosting.publishedAt = 0;
        newPosting.exists = true;

        companyPostings[msg.sender].push(postingId);

        emit PostingCreated(postingId, companyId, msg.sender, title);

        return postingId;
    }

    /**
     * @dev Publishes a draft posting, making it live and immutable
     * @param postingId The ID of the posting to publish
     */
    function publishPosting(uint256 postingId)
        external
        postingExists(postingId)
        onlyPostingOwner(postingId)
        onlyDraft(postingId)
    {
        Posting storage posting = postings[postingId];
        posting.status = PostingStatus.Published;
        posting.publishedAt = block.timestamp;

        emit PostingPublished(postingId, posting.companyId, block.timestamp);
    }

    /**
     * @dev Updates a draft posting (only allowed before publishing)
     * @param postingId The ID of the posting to update
     * @param title New title
     * @param description New description
     */
    function updateDraftPosting(
        uint256 postingId,
        string memory title,
        string memory description
    )
        external
        postingExists(postingId)
        onlyPostingOwner(postingId)
        onlyDraft(postingId)
    {
        require(bytes(title).length > 0, "Title cannot be empty");
        require(bytes(description).length > 0, "Description cannot be empty");

        Posting storage posting = postings[postingId];
        posting.title = title;
        posting.description = description;

        emit PostingUpdated(postingId, title);
    }

    // ============ View Functions ============

    /**
     * @dev Retrieves a posting by ID
     * @param postingId The ID of the posting
     * @return The Posting struct
     */
    function getPosting(uint256 postingId)
        external
        view
        postingExists(postingId)
        returns (Posting memory)
    {
        return postings[postingId];
    }

    /**
     * @dev Checks if a posting exists and is published
     * @param postingId The ID of the posting
     * @return True if posting exists and is published
     */
    function isPostingPublished(uint256 postingId)
        external
        view
        returns (bool)
    {
        return postings[postingId].exists &&
               postings[postingId].status == PostingStatus.Published;
    }

    /**
     * @dev Verifies if a posting exists
     * @param postingId The ID of the posting
     * @return True if posting exists
     */
    function verifyPostingExists(uint256 postingId)
        external
        view
        returns (bool)
    {
        return postings[postingId].exists;
    }

    /**
     * @dev Gets all posting IDs for a company wallet
     * @param walletAddress The company's wallet address
     * @return Array of posting IDs
     */
    function getCompanyPostings(address walletAddress)
        external
        view
        returns (uint256[] memory)
    {
        return companyPostings[walletAddress];
    }

    /**
     * @dev Gets the current posting ID counter value
     * @return The next posting ID that will be assigned
     */
    function getCurrentPostingId() external view returns (uint256) {
        return _postingIdCounter;
    }
}
