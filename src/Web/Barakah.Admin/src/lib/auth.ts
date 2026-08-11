import type { AuthResponse, LoginRequest, RegisterRequest, UserResponse } from "./types";

const STORAGE_KEY = "barakah-admin-auth";
const IDENTITY_URL = process.env.NEXT_PUBLIC_IDENTITY_URL ?? "http://localhost:5001";

export interface StoredAuth {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  user: UserResponse;
}

export class AuthApiError extends Error {
  status: number;
  constructor(status: number, message: string) {
    super(message);
    this.name = "AuthApiError";
    this.status = status;
  }
}

function isBrowser(): boolean {
  return typeof window !== "undefined";
}

function readErrorMessage(body: unknown, fallback: string): string {
  if (body && typeof body === "object" && "error" in body && typeof (body as { error?: unknown }).error === "string") {
    return (body as { error: string }).error;
  }
  return fallback;
}

async function parseAuthResponse(res: Response, fallbackError: string): Promise<AuthResponse> {
  let body: unknown = null;
  try {
    body = await res.json();
  } catch {
    // ignore — some error responses may have no body
  }

  if (!res.ok) {
    throw new AuthApiError(res.status, readErrorMessage(body, fallbackError));
  }

  return body as AuthResponse;
}

function persistAuth(auth: AuthResponse): void {
  if (!isBrowser()) return;
  const stored: StoredAuth = {
    accessToken: auth.accessToken,
    refreshToken: auth.refreshToken,
    accessTokenExpiresAt: auth.accessTokenExpiresAt,
    user: auth.user,
  };
  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(stored));
}

export function getStoredAuth(): StoredAuth | null {
  if (!isBrowser()) return null;
  const raw = window.localStorage.getItem(STORAGE_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as StoredAuth;
  } catch {
    window.localStorage.removeItem(STORAGE_KEY);
    return null;
  }
}

export function getAccessToken(): string | null {
  return getStoredAuth()?.accessToken ?? null;
}

export function getCurrentUser(): UserResponse | null {
  return getStoredAuth()?.user ?? null;
}

/** True when the stored access token is missing, expired, or expires within 60 seconds. */
export function isTokenExpiringSoon(): boolean {
  const auth = getStoredAuth();
  if (!auth) return true;
  const expiresAt = new Date(auth.accessTokenExpiresAt).getTime();
  if (Number.isNaN(expiresAt)) return true;
  return expiresAt - Date.now() < 60_000;
}

export async function login(email: string, password: string): Promise<AuthResponse> {
  const body: LoginRequest = { email, password };
  const res = await fetch(`${IDENTITY_URL}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  const auth = await parseAuthResponse(res, "Invalid email or password.");
  persistAuth(auth);
  return auth;
}

export async function register(request: RegisterRequest): Promise<AuthResponse> {
  const res = await fetch(`${IDENTITY_URL}/api/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
  const auth = await parseAuthResponse(res, "A user with this email already exists.");
  persistAuth(auth);
  return auth;
}

/** Exchanges the stored refresh token for a new access/refresh token pair. */
export async function refreshAccessToken(): Promise<AuthResponse | null> {
  const stored = getStoredAuth();
  if (!stored) return null;

  const res = await fetch(`${IDENTITY_URL}/api/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken: stored.refreshToken }),
  });

  if (!res.ok) {
    // Refresh token is invalid/expired — the caller should treat this as logged out.
    clearAuth();
    return null;
  }

  const auth = (await res.json()) as AuthResponse;
  persistAuth(auth);
  return auth;
}

function clearAuth(): void {
  if (!isBrowser()) return;
  window.localStorage.removeItem(STORAGE_KEY);
}

export async function logout(): Promise<void> {
  const stored = getStoredAuth();
  if (stored) {
    try {
      await fetch(`${IDENTITY_URL}/api/auth/logout`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken: stored.refreshToken }),
      });
    } catch {
      // Best-effort — still clear local state even if the request fails (service down, etc).
    }
  }
  clearAuth();
}

/** Returns a valid access token, refreshing it first if it's expiring soon. Null if not logged in. */
export async function ensureFreshAccessToken(): Promise<string | null> {
  if (!getStoredAuth()) return null;
  if (isTokenExpiringSoon()) {
    const refreshed = await refreshAccessToken();
    return refreshed?.accessToken ?? null;
  }
  return getAccessToken();
}
