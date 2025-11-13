# Blockchain Setup Guide - Polygon Amoy Testnet

This guide will help you configure JobStream API to use your Coinbase Wallet with Polygon Amoy Testnet for blockchain job postings.

## Prerequisites

- [x] Coinbase Wallet app installed with 0.1 POL on Amoy Testnet
- [ ] Wallet address and private key
- [ ] Smart contract deployed to Amoy Testnet

## Step 1: Get Your Wallet Address

You already have this from connecting to the faucet. It starts with `0x` and is 42 characters long.

**From Coinbase Wallet app:**
1. Open Coinbase Wallet app
2. Your wallet address is displayed at the top of the main screen
3. Tap to copy it

## Step 2: Export Your Private Key from Coinbase Wallet

**IMPORTANT: Your private key gives full access to your wallet. Never share it or commit it to git!**

### On Mobile (Coinbase Wallet App):

1. Open **Coinbase Wallet** app
2. Tap **Settings** (gear icon) ⚙️
3. Tap **Security**
4. Tap **Show Recovery Phrase** or **Show Private Key**
   - If you see "Recovery Phrase" (12-24 words), that's your seed phrase
   - If you see "Private Key", that's what you need
5. Authenticate with Face ID/Touch ID/PIN
6. **Copy the private key** (it starts with `0x`)

### Converting Seed Phrase to Private Key (if needed):

If Coinbase Wallet only shows you a seed phrase (12-24 words), you'll need to derive the private key:

**Option 1: Using MetaMask (Recommended for beginners)**
1. Install MetaMask browser extension
2. Click "Import wallet"
3. Enter your 12-word seed phrase from Coinbase Wallet
4. Click on the three dots → Account details → Export Private Key
5. Enter your password and copy the private key

**Option 2: Using JavaScript/Node.js**
```javascript
// Install: npm install ethers
const { ethers } = require('ethers');

const mnemonic = "your twelve word seed phrase goes here";
const wallet = ethers.Wallet.fromMnemonic(mnemonic);
console.log('Private Key:', wallet.privateKey);
```

## Step 3: Configure Environment Variables

Edit the `.env` file in the project root:

```bash
# Polygon Blockchain Configuration
BLOCKCHAIN_WALLET_ADDRESS=0xYOUR_WALLET_ADDRESS_HERE
BLOCKCHAIN_PRIVATE_KEY=0xYOUR_PRIVATE_KEY_HERE
BLOCKCHAIN_RPC_URL=https://rpc-amoy.polygon.technology
BLOCKCHAIN_CONTRACT_ADDRESS=0xCONTRACT_ADDRESS_AFTER_DEPLOYMENT
BLOCKCHAIN_CHAIN_ID=80002
BLOCKCHAIN_GAS_LIMIT=500000
USE_MOCK_BLOCKCHAIN=true
```

### Configuration Details:

- **BLOCKCHAIN_WALLET_ADDRESS**: Your wallet address from Step 1
- **BLOCKCHAIN_PRIVATE_KEY**: Your private key from Step 2 (must start with `0x`)
- **BLOCKCHAIN_RPC_URL**: Polygon Amoy RPC endpoint (you can also use Alchemy or Infura for better reliability)
- **BLOCKCHAIN_CONTRACT_ADDRESS**: Smart contract address (add after deployment in Step 4)
- **BLOCKCHAIN_CHAIN_ID**: 80002 (Amoy Testnet) or 137 (Polygon Mainnet)
- **USE_MOCK_BLOCKCHAIN**: Set to `true` for testing without blockchain, `false` for real blockchain

## Step 4: Deploy Smart Contract to Amoy Testnet

The smart contract is located at: `blockchain/contracts/JobPosting.sol`

### Deploy using Hardhat:

```bash
cd blockchain
npm install

# Set environment variables for deployment
export PRIVATE_KEY="your_private_key_here"
export AMOY_RPC_URL="https://rpc-amoy.polygon.technology"

# Deploy to Amoy Testnet
npx hardhat run scripts/deploy.js --network amoy
```

After deployment, you'll see:
```
JobPosting deployed to: 0xABCDEF123456...
```

Copy this contract address and add it to your `.env` file as `BLOCKCHAIN_CONTRACT_ADDRESS`.

### Verify Contract on PolygonScan (Optional):

```bash
npx hardhat verify --network amoy <CONTRACT_ADDRESS>
```

## Step 5: Install Nethereum Package

The `PolygonBlockchainService` requires the Nethereum library:

```bash
dotnet add package Nethereum.Web3
```

Then uncomment the implementation in `Services/PolygonBlockchainService.cs`.

## Step 6: Test the Configuration

1. Keep `USE_MOCK_BLOCKCHAIN=true` initially
2. Run the API: `dotnet run`
3. Create a test job posting via Swagger at `http://localhost:5000/swagger`
4. Verify it works with the mock service
5. Set `USE_MOCK_BLOCKCHAIN=false` to use real blockchain
6. Restart the API and test again

## Step 7: Verify Blockchain Transactions

After creating a job posting with real blockchain:

1. Go to [Amoy PolygonScan](https://amoy.polygonscan.com/)
2. Search for your wallet address
3. You should see transactions for:
   - `createDraftPosting`
   - `publishPosting`
   - `updateDraftPosting`

## Security Best Practices

### DO:
✅ Store private keys in environment variables (`.env` file)
✅ Add `.env` to `.gitignore` (already done)
✅ Use a testnet wallet with minimal funds for development
✅ Use different wallets for development and production
✅ Consider using a secrets manager (AWS Secrets Manager, Azure Key Vault) in production

### DON'T:
❌ Never commit private keys to git
❌ Never share your private key with anyone
❌ Never use your main wallet for development
❌ Never hardcode secrets in `appsettings.json`

## Troubleshooting

### "Insufficient funds for gas"
- Check your wallet balance on [Amoy PolygonScan](https://amoy.polygonscan.com/)
- Get more test POL from the faucet

### "Invalid private key format"
- Private key must start with `0x`
- Private key must be 66 characters long (including `0x`)
- No spaces or special characters

### "Contract not deployed at address"
- Verify the contract address on [Amoy PolygonScan](https://amoy.polygonscan.com/)
- Make sure you deployed to the correct network (Amoy, not Mumbai or Mainnet)

### "Transaction timeout"
- Amoy testnet can be slow
- Increase the gas limit in configuration
- Try a different RPC URL (Alchemy, Infura)

## Getting Better RPC URLs

Public RPCs can be slow. Get free RPC URLs from:

### Alchemy (Recommended)
1. Go to [alchemy.com](https://www.alchemy.com/)
2. Sign up for free account
3. Create a new app → Select "Polygon" → "Amoy"
4. Copy the HTTPS URL
5. Update `BLOCKCHAIN_RPC_URL` in `.env`

### Infura
1. Go to [infura.io](https://infura.io/)
2. Sign up for free account
3. Create new project → Select "Polygon Amoy"
4. Copy the HTTPS endpoint
5. Update `BLOCKCHAIN_RPC_URL` in `.env`

## Next Steps

Once configured:
1. Test creating draft job postings
2. Test publishing job postings
3. Test updating draft job postings
4. View transactions on PolygonScan
5. Monitor gas usage and costs

## References

- [Polygon Amoy Testnet](https://polygon.technology/)
- [Polygon Faucet](https://faucet.polygon.technology/)
- [Amoy PolygonScan](https://amoy.polygonscan.com/)
- [Nethereum Documentation](https://docs.nethereum.com/)
- [Coinbase Wallet Guide](https://www.coinbase.com/wallet)
