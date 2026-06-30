export interface AdminAuthConfig {
  secretKeyAuthEnabled: boolean;
  oidcEnabled: boolean;
  issuer: string | null;
  clientId: string | null;
  scope: string;
  requiredRole: string | null;
}
