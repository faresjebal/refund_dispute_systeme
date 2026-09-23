export interface TokenModel {
  accessToken: string;
  refreshToken: string;
  expiresAt: Date;
  tokenType: string;
}