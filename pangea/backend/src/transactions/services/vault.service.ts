import { Injectable, Logger, NotFoundException } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { TokenizedAsset } from '../../assets/entities/tokenized-asset.entity';
import { SmartContractService } from '../../smart-contracts/services/smart-contract.service';

/**
 * Vault Service
 * Manages vault addresses for deposits/withdrawals
 * Each asset can have a vault contract deployed for handling deposits
 */
@Injectable()
export class VaultService {
  private readonly logger = new Logger(VaultService.name);
  private readonly vaultAddressCache = new Map<string, string>(); // Cache: assetId-blockchain -> vaultAddress

  constructor(
    @InjectRepository(TokenizedAsset)
    private assetRepository: Repository<TokenizedAsset>,
    private smartContractService: SmartContractService,
  ) {}

  /**
   * Get vault address for an asset and blockchain
   * If vault doesn't exist, deploy one
   */
  async getVaultAddress(
    assetId: string,
    blockchain: string,
  ): Promise<string> {
    const cacheKey = `${assetId}-${blockchain}`;

    // Check cache first
    if (this.vaultAddressCache.has(cacheKey)) {
      return this.vaultAddressCache.get(cacheKey)!;
    }

    // Check if asset exists
    const asset = await this.assetRepository.findOne({
      where: { id: assetId },
    });

    if (!asset) {
      throw new NotFoundException(`Asset ${assetId} not found`);
    }

    // Check if vault address is stored in asset metadata
    if (asset.metadata?.vaultAddresses?.[blockchain]) {
      const vaultAddress = asset.metadata.vaultAddresses[blockchain];
      this.vaultAddressCache.set(cacheKey, vaultAddress);
      return vaultAddress;
    }

    // If no vault exists, deploy one
    this.logger.log(
      `No vault found for asset ${assetId} on ${blockchain}. Deploying new vault...`,
    );

    try {
      const deployResult = await this.smartContractService.deployVault();
      const vaultAddress = deployResult.contractAddress;

      // Store vault address in asset metadata
      if (!asset.metadata) {
        asset.metadata = {};
      }
      if (!asset.metadata.vaultAddresses) {
        asset.metadata.vaultAddresses = {};
      }
      asset.metadata.vaultAddresses[blockchain] = vaultAddress;
      await this.assetRepository.save(asset);

      // Cache the address
      this.vaultAddressCache.set(cacheKey, vaultAddress);

      this.logger.log(
        `✅ Vault deployed and stored for asset ${assetId} on ${blockchain}: ${vaultAddress}`,
      );

      return vaultAddress;
    } catch (error) {
      this.logger.error(
        `Failed to deploy vault for asset ${assetId}: ${error.message}`,
        error.stack,
      );
      throw error;
    }
  }

  /**
   * Clear vault address cache (useful for testing or when vaults are redeployed)
   */
  clearCache(): void {
    this.vaultAddressCache.clear();
  }
}











