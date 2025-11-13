const hre = require("hardhat");
const fs = require('fs');

async function main() {
  console.log("🚀 Deploying JobPosting Smart Contract...");
  console.log("📍 Network:", hre.network.name);

  // Get the deployer's account
  const [deployer] = await hre.ethers.getSigners();
  console.log("👤 Deployer address:", deployer.address);

  // Get deployer balance
  const balance = await hre.ethers.provider.getBalance(deployer.address);
  console.log("💰 Deployer balance:", hre.ethers.utils.formatEther(balance), "POL");

  // Deploy the contract
  const JobPosting = await hre.ethers.getContractFactory("JobPosting");
  const jobPosting = await JobPosting.deploy();

  await jobPosting.deployed();

  const contractAddress = jobPosting.address;
  console.log("✅ JobPosting contract deployed to:", contractAddress);

  // Verify initial state
  const currentId = await jobPosting.getCurrentPostingId();
  console.log("📊 Initial posting ID counter:", currentId.toString());

  const owner = await jobPosting.owner();
  console.log("👑 Contract owner:", owner);

  console.log("\n📋 Deployment Summary:");
  console.log("=======================");
  console.log("Contract Address:", contractAddress);
  console.log("Network:", hre.network.name);
  console.log("Chain ID:", hre.network.config.chainId);
  console.log("Deployer:", deployer.address);
  console.log("=======================\n");

  // Save deployment info to file
  const deploymentInfo = {
    network: hre.network.name,
    chainId: hre.network.config.chainId,
    contractAddress: contractAddress,
    deployer: deployer.address,
    deployedAt: new Date().toISOString(),
    blockNumber: await hre.ethers.provider.getBlockNumber()
  };

  fs.writeFileSync(
    './deployment-info.json',
    JSON.stringify(deploymentInfo, null, 2)
  );
  console.log("💾 Deployment info saved to deployment-info.json");

  if (hre.network.name === "amoy") {
    console.log("\n🔍 To verify the contract on Polygonscan, run:");
    console.log(`npx hardhat verify --network amoy ${contractAddress}`);
    console.log("\n🌐 View on PolygonScan Amoy:");
    console.log(`https://amoy.polygonscan.com/address/${contractAddress}`);
  }
}

main()
  .then(() => process.exit(0))
  .catch((error) => {
    console.error("❌ Deployment failed:");
    console.error(error);
    process.exit(1);
  });
