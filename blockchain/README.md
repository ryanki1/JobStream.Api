# JobStream Blockchain Smart Contract

Smart Contract implementation for blockchain-based job posting system on Polygon Mumbai Testnet.

## 📋 Overview

This smart contract implements SCRUM-4, enabling verified companies to post sprint-based job opportunities on the blockchain. The contract uses a two-status system:
- **DRAFT**: Editable postings stored off-chain (not on blockchain)
- **PUBLISHED**: Immutable postings stored on Polygon blockchain

## 🏗️ Architecture

### Smart Contract: `JobPosting.sol`

**Features:**
- Create draft job postings
- Update draft postings (before publishing)
- Publish postings to blockchain (immutable)
- Verify posting existence
- Track company postings

**Key Functions:**
- `createDraftPosting(companyId, title, description)` - Create a new draft
- `publishPosting(postingId)` - Publish draft to blockchain
- `updateDraftPosting(postingId, title, description)` - Update draft
- `verifyPostingExists(postingId)` - Check if posting exists
- `getPosting(postingId)` - Get posting details

## 🚀 Quick Start

### Prerequisites

- Node.js v22.10.0 or higher
- npm v10.9.0 or higher
- Polygon Mumbai testnet MATIC (free from faucet)

### Installation

```bash
cd blockchain
npm install
```

### Configuration

1. Copy `.env.example` to `.env`:
```bash
cp .env.example .env
```

2. Add your configuration to `.env`:
```env
# Get free RPC from https://alchemy.com or use public endpoint
POLYGON_MUMBAI_RPC_URL=https://rpc-mumbai.maticvigil.com

# Your wallet private key (NEVER commit this!)
PRIVATE_KEY=your_private_key_here

# Optional: For contract verification on PolygonScan
POLYGONSCAN_API_KEY=your_api_key_here
```

3. Get free testnet MATIC:
   - Visit: https://faucet.polygon.technology/
   - Connect your wallet
   - Request test MATIC (free)

## 🧪 Testing

Run the comprehensive test suite (24 tests):

```bash
npm test
```

Test coverage includes:
- ✅ Contract deployment
- ✅ Creating draft postings
- ✅ Publishing postings
- ✅ Updating drafts
- ✅ Access control
- ✅ Input validation
- ✅ Multi-company scenarios
- ✅ Complete workflow lifecycle

## 📦 Compilation

Compile the Solidity smart contract:

```bash
npm run compile
```

This generates:
- ABI files in `artifacts/`
- Contract bytecode
- TypeChain type definitions

## 🚢 Deployment

### Deploy to Local Network (for testing)

```bash
npm run deploy:local
```

### Deploy to Polygon Mumbai Testnet

```bash
npm run deploy:mumbai
```

**Deployment Output:**
```
🚀 Deploying JobPosting Smart Contract...
📍 Network: mumbai
👤 Deployer address: 0x...
💰 Deployer balance: 1.5 MATIC
✅ JobPosting contract deployed to: 0x...
📊 Initial posting ID counter: 1000
👑 Contract owner: 0x...

📋 Deployment Summary:
=======================
Contract Address: 0x...
Network: mumbai
Chain ID: 80001
Deployer: 0x...
=======================

💾 Deployment info saved to deployment-info.json
🌐 View on PolygonScan Mumbai:
https://mumbai.polygonscan.com/address/0x...
```

## 🔍 Verification

### Online Verification

After deployment, verify the contract on PolygonScan:

```bash
npx hardhat verify --network mumbai <CONTRACT_ADDRESS>
```

### Manual Verification Steps

1. Visit PolygonScan Mumbai: https://mumbai.polygonscan.com/address/YOUR_CONTRACT_ADDRESS
2. Go to "Contract" tab
3. Click "Verify and Publish"
4. Or use the verification command above

### Testing the Deployment

Check if a posting exists on the blockchain:

```javascript
const jobPosting = await ethers.getContractAt("JobPosting", contractAddress);
const exists = await jobPosting.verifyPostingExists(1000);
console.log("Posting exists:", exists);
```

## 📁 Project Structure

```
blockchain/
├── contracts/
│   └── JobPosting.sol          # Main smart contract
├── scripts/
│   └── deploy.js               # Deployment script
├── test/
│   └── JobPosting.test.js      # Test suite (24 tests)
├── hardhat.config.js           # Hardhat configuration
├── package.json                # Dependencies
├── .env.example                # Environment template
├── .gitignore                  # Git ignore rules
└── README.md                   # This file
```

## 🔐 Security Features

- **Access Control**: Only posting owner can publish/update
- **Immutability**: Published postings cannot be modified
- **Input Validation**: All inputs are validated
- **Owner Verification**: Built-in ownership checks
- **Transparent**: All transactions visible on blockchain

## 💡 Usage Example

### Creating and Publishing a Job Posting

```javascript
// 1. Create draft
const tx1 = await jobPosting.createDraftPosting(
  "company123",
  "Senior Blockchain Developer",
  "Looking for experienced Solidity developer..."
);
const receipt1 = await tx1.wait();
const postingId = 1000; // First posting ID

// 2. Update draft (optional)
await jobPosting.updateDraftPosting(
  postingId,
  "Senior Blockchain Developer (Updated)",
  "Updated description..."
);

// 3. Publish to blockchain
const tx2 = await jobPosting.publishPosting(postingId);
await tx2.wait();

// 4. Verify it's published
const isPublished = await jobPosting.isPostingPublished(postingId);
console.log("Published:", isPublished); // true

// 5. Get posting details
const posting = await jobPosting.getPosting(postingId);
console.log("Title:", posting.title);
console.log("Status:", posting.status); // 1 = Published
```

## 📊 Contract Specifications

- **Solidity Version**: 0.8.20
- **Network**: Polygon Mumbai Testnet
- **Chain ID**: 80001
- **Initial Posting ID**: 1000
- **Gas Optimization**: Enabled (200 runs)

## 🔗 Useful Links

- **Polygon Mumbai Faucet**: https://faucet.polygon.technology/
- **Mumbai Explorer**: https://mumbai.polygonscan.com/
- **Polygon Docs**: https://docs.polygon.technology/
- **Hardhat Docs**: https://hardhat.org/docs

## 🐛 Troubleshooting

### Issue: "insufficient funds for gas"
**Solution**: Get more testnet MATIC from the faucet

### Issue: "nonce too low"
**Solution**: Reset your wallet's transaction history or wait a few minutes

### Issue: Compilation fails
**Solution**: Ensure you're using Node.js v22.10.0 or higher:
```bash
nvm use 22.10.0
npm install
npm run compile
```

### Issue: Tests failing
**Solution**: Clean and recompile:
```bash
npx hardhat clean
npm run compile
npm test
```

## 📝 Notes

- This is a **TESTNET** deployment for SCRUM-4
- Production deployment to mainnet will be in a future sprint
- Keep your private key secure and NEVER commit `.env` file
- Testnet MATIC has no real value - it's free for testing

## 🎯 Success Criteria (SCRUM-4)

✅ Job postings stored on Polygon testnet (not mainnet)
✅ Only published postings written to blockchain
✅ Entries verifiable online via PolygonScan
✅ Cost: FREE (using testnet)
✅ All tests passing (24/24)

## 👥 Team

Built for JobStream.Api MVP - Blockchain Job Posting System

---

**Ready for Sprint Demo!** 🚀
