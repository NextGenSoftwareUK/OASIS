import { IsEnum, IsString, IsOptional } from 'class-validator';
import { KycStatus } from './admin-user-filters.dto';

export class UpdateKycStatusDto {
  @IsEnum(KycStatus)
  status: KycStatus;

  @IsOptional()
  @IsString()
  reason?: string; // Optional reason for rejection
}












