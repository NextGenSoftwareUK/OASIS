import { IsString, MinLength } from 'class-validator';

export class LoginDto {
  @IsString()
  email: string; // Can be email or username

  @IsString()
  @MinLength(6)
  password: string;
}












