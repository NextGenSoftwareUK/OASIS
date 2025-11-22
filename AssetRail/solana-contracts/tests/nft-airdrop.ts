import * as anchor from "@coral-xyz/anchor";
import { Program } from "@coral-xyz/anchor";
import { NftAirdrop } from "../target/types/nft_airdrop";
import { expect } from "chai";

describe("NFT Airdrop", () => {
  const provider = anchor.AnchorProvider.env();
  anchor.setProvider(provider);

  const program = anchor.workspace.NftAirdrop as Program<NftAirdrop>;
  
  let campaignPda: anchor.web3.PublicKey;
  let whitelistPda: anchor.web3.PublicKey;
  
  const campaignName = "AssetRail Genesis NFT Drop";
  const user = provider.wallet as anchor.Wallet;
  
  // Generate test recipient wallets
  const recipients: anchor.web3.Keypair[] = [];
  const numRecipients = 5;

  before(async () => {
    // Generate test recipients
    for (let i = 0; i < numRecipients; i++) {
      const recipient = anchor.web3.Keypair.generate();
      
      // Fund recipients for transaction fees
      const airdropTx = await provider.connection.requestAirdrop(
        recipient.publicKey,
        2 * anchor.web3.LAMPORTS_PER_SOL
      );
      await provider.connection.confirmTransaction(airdropTx);
      
      recipients.push(recipient);
    }

    // Derive campaign PDA
    [campaignPda] = anchor.web3.PublicKey.findProgramAddressSync(
      [
        Buffer.from("campaign"),
        user.publicKey.toBuffer(),
        Buffer.from(campaignName),
      ],
      program.programId
    );

    // Derive whitelist PDA
    [whitelistPda] = anchor.web3.PublicKey.findProgramAddressSync(
      [
        Buffer.from("whitelist"),
        campaignPda.toBuffer(),
      ],
      program.programId
    );
  });

  it("Initializes an airdrop campaign", async () => {
    const collectionUri = "ipfs://QmAssetRailGenesisCollection";
    const maxRecipients = 1000;

    const tx = await program.methods
      .initializeCampaign(campaignName, collectionUri, maxRecipients)
      .accounts({
        campaign: campaignPda,
        authority: user.publicKey,
        systemProgram: anchor.web3.SystemProgram.programId,
      })
      .rpc();

    console.log("Campaign initialized:", tx);

    const campaignAccount = await program.account.campaign.fetch(campaignPda);
    
    expect(campaignAccount.name).to.equal(campaignName);
    expect(campaignAccount.maxRecipients).to.equal(maxRecipients);
    expect(campaignAccount.totalMinted).to.equal(0);
    expect(campaignAccount.isActive).to.be.true;
  });

  it("Adds recipients to whitelist", async () => {
    const whitelistAddresses = recipients.map(r => r.publicKey);

    const tx = await program.methods
      .addToWhitelist(whitelistAddresses)
      .accounts({
        campaign: campaignPda,
        whitelist: whitelistPda,
        authority: user.publicKey,
        systemProgram: anchor.web3.SystemProgram.programId,
      })
      .rpc();

    console.log("Whitelist updated:", tx);

    const whitelistAccount = await program.account.whitelist.fetch(whitelistPda);
    expect(whitelistAccount.recipients.length).to.equal(numRecipients);
  });

  it("Checks if wallet is whitelisted", async () => {
    const isWhitelisted = await program.methods
      .isWhitelisted(recipients[0].publicKey)
      .accounts({
        campaign: campaignPda,
        whitelist: whitelistPda,
      })
      .view();

    expect(isWhitelisted).to.be.true;

    // Check non-whitelisted address
    const randomWallet = anchor.web3.Keypair.generate();
    const notWhitelisted = await program.methods
      .isWhitelisted(randomWallet.publicKey)
      .accounts({
        campaign: campaignPda,
        whitelist: whitelistPda,
      })
      .view();

    expect(notWhitelisted).to.be.false;
  });

  it("Airdrops NFTs in batch", async () => {
    const batchRecipients = recipients.slice(0, 3).map(r => r.publicKey);
    const metadataUris = [
      "ipfs://QmNFT1",
      "ipfs://QmNFT2",
      "ipfs://QmNFT3",
    ];
    const names = [
      "AssetRail Genesis #1",
      "AssetRail Genesis #2",
      "AssetRail Genesis #3",
    ];

    const tx = await program.methods
      .airdropBatch(batchRecipients, metadataUris, names)
      .accounts({
        campaign: campaignPda,
        authority: user.publicKey,
      })
      .rpc();

    console.log("Batch airdrop completed:", tx);

    const campaignAccount = await program.account.campaign.fetch(campaignPda);
    expect(campaignAccount.totalMinted).to.equal(3);
  });

  it("Mints single NFT to recipient", async () => {
    const recipient = recipients[3];
    const mint = anchor.web3.Keypair.generate();
    
    const [tokenAccount] = anchor.web3.PublicKey.findProgramAddressSync(
      [
        recipient.publicKey.toBuffer(),
        anchor.utils.token.TOKEN_PROGRAM_ID.toBuffer(),
        mint.publicKey.toBuffer(),
      ],
      anchor.utils.token.ASSOCIATED_PROGRAM_ID
    );

    const name = "AssetRail Genesis #4";
    const symbol = "ARGEN";
    const uri = "ipfs://QmNFT4";

    const tx = await program.methods
      .mintNft(name, symbol, uri)
      .accounts({
        campaign: campaignPda,
        mint: mint.publicKey,
        tokenAccount: tokenAccount,
        recipient: recipient.publicKey,
        authority: user.publicKey,
        tokenProgram: anchor.utils.token.TOKEN_PROGRAM_ID,
        associatedTokenProgram: anchor.utils.token.ASSOCIATED_PROGRAM_ID,
        systemProgram: anchor.web3.SystemProgram.programId,
        rent: anchor.web3.SYSVAR_RENT_PUBKEY,
      })
      .signers([mint])
      .rpc();

    console.log("Single NFT minted:", tx);

    const campaignAccount = await program.account.campaign.fetch(campaignPda);
    expect(campaignAccount.totalMinted).to.equal(4);
  });

  it("Recipient claims NFT from whitelist", async () => {
    const recipient = recipients[4];
    const mint = anchor.web3.Keypair.generate();
    
    const [tokenAccount] = anchor.web3.PublicKey.findProgramAddressSync(
      [
        recipient.publicKey.toBuffer(),
        anchor.utils.token.TOKEN_PROGRAM_ID.toBuffer(),
        mint.publicKey.toBuffer(),
      ],
      anchor.utils.token.ASSOCIATED_PROGRAM_ID
    );

    const name = "AssetRail Genesis #5";
    const symbol = "ARGEN";
    const uri = "ipfs://QmNFT5";

    const tx = await program.methods
      .claimNft(name, symbol, uri)
      .accounts({
        campaign: campaignPda,
        whitelist: whitelistPda,
        mint: mint.publicKey,
        tokenAccount: tokenAccount,
        recipient: recipient.publicKey,
        authority: user.publicKey,
        tokenProgram: anchor.utils.token.TOKEN_PROGRAM_ID,
        associatedTokenProgram: anchor.utils.token.ASSOCIATED_PROGRAM_ID,
        systemProgram: anchor.web3.SystemProgram.programId,
        rent: anchor.web3.SYSVAR_RENT_PUBKEY,
      })
      .signers([recipient, mint])
      .rpc();

    console.log("NFT claimed:", tx);

    const campaignAccount = await program.account.campaign.fetch(campaignPda);
    expect(campaignAccount.totalMinted).to.equal(5);
  });

  it("Gets campaign statistics", async () => {
    const stats = await program.methods
      .getCampaignStats()
      .accounts({
        campaign: campaignPda,
      })
      .view();

    console.log("\n=== Campaign Statistics ===");
    console.log("Total Minted:", stats.totalMinted);
    console.log("Max Recipients:", stats.maxRecipients);
    console.log("Remaining:", stats.remaining);
    console.log("Status:", stats.isActive ? "Active" : "Inactive");
    console.log("===========================\n");

    expect(stats.totalMinted).to.equal(5);
    expect(stats.remaining).to.equal(995);
  });

  it("Pauses campaign", async () => {
    const tx = await program.methods
      .pauseCampaign()
      .accounts({
        campaign: campaignPda,
        authority: user.publicKey,
      })
      .rpc();

    console.log("Campaign paused:", tx);

    const campaignAccount = await program.account.campaign.fetch(campaignPda);
    expect(campaignAccount.isActive).to.be.false;
  });

  it("Resumes campaign", async () => {
    const tx = await program.methods
      .resumeCampaign()
      .accounts({
        campaign: campaignPda,
        authority: user.publicKey,
      })
      .rpc();

    console.log("Campaign resumed:", tx);

    const campaignAccount = await program.account.campaign.fetch(campaignPda);
    expect(campaignAccount.isActive).to.be.true;
  });

  it("Prevents non-whitelisted users from claiming", async () => {
    const nonWhitelisted = anchor.web3.Keypair.generate();
    
    // Fund the wallet
    const airdropTx = await provider.connection.requestAirdrop(
      nonWhitelisted.publicKey,
      2 * anchor.web3.LAMPORTS_PER_SOL
    );
    await provider.connection.confirmTransaction(airdropTx);

    const mint = anchor.web3.Keypair.generate();
    const [tokenAccount] = anchor.web3.PublicKey.findProgramAddressSync(
      [
        nonWhitelisted.publicKey.toBuffer(),
        anchor.utils.token.TOKEN_PROGRAM_ID.toBuffer(),
        mint.publicKey.toBuffer(),
      ],
      anchor.utils.token.ASSOCIATED_PROGRAM_ID
    );

    try {
      await program.methods
        .claimNft("Test NFT", "TEST", "ipfs://test")
        .accounts({
          campaign: campaignPda,
          whitelist: whitelistPda,
          mint: mint.publicKey,
          tokenAccount: tokenAccount,
          recipient: nonWhitelisted.publicKey,
          authority: user.publicKey,
          tokenProgram: anchor.utils.token.TOKEN_PROGRAM_ID,
          associatedTokenProgram: anchor.utils.token.ASSOCIATED_PROGRAM_ID,
          systemProgram: anchor.web3.SystemProgram.programId,
          rent: anchor.web3.SYSVAR_RENT_PUBKEY,
        })
        .signers([nonWhitelisted, mint])
        .rpc();

      throw new Error("Should have failed - not whitelisted");
    } catch (err) {
      expect(err.toString()).to.include("NotWhitelisted");
      console.log("Correctly prevented non-whitelisted claim");
    }
  });
});






