export class AuthResponseDto {
  user: {
    id: string;
    email: string;
    username: string;
    firstName?: string;
    lastName?: string;
    avatarId: string;
    role: string;
  };
  token: string;
  expiresAt: Date;
}












