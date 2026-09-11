export interface UserDto {
  id: string;
  email: string;
  displayName: string;
  pictureUrl: string | null;
}

export interface AuthResponse {
  accessToken: string;
  expiresAtUtc: string;
  user: UserDto;
}

/** Which sign-in methods the server offers, and the Google client id to render with. */
export interface AuthConfigDto {
  googleClientId: string | null;
  googleEnabled: boolean;
  devLoginEnabled: boolean;
}
