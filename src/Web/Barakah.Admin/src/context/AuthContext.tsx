"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import type { ReactNode } from "react";
import {
  getStoredAuth,
  isTokenExpiringSoon,
  login as authLogin,
  logout as authLogout,
  refreshAccessToken,
} from "@/lib/auth";
import type { UserResponse } from "@/lib/types";

const TENANT_STORAGE_KEY = "barakah-admin-tenant";
const BRANCH_STORAGE_KEY = "barakah-admin-branch";

interface AuthContextValue {
  user: UserResponse | null;
  loading: boolean;
  selectedTenant: string | null;
  setSelectedTenant: (subdomain: string | null) => void;
  /**
   * Order/Inventory Service have no tenant-wide "list all" endpoint (only by-branch and
   * low-stock lookups exist — see src/Modules/OrderService and InventoryService Controllers).
   * We track a selected Branch Id client-side so pages can filter against those endpoints.
   */
  selectedBranchId: string | null;
  setSelectedBranchId: (branchId: string | null) => void;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [selectedTenant, setSelectedTenantState] = useState<string | null>(null);
  const [selectedBranchId, setSelectedBranchIdState] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function init() {
      const stored = getStoredAuth();
      if (!stored) {
        setLoading(false);
        return;
      }

      if (isTokenExpiringSoon()) {
        const refreshed = await refreshAccessToken();
        if (!cancelled) {
          setUser(refreshed?.user ?? null);
        }
      } else if (!cancelled) {
        setUser(stored.user);
      }

      if (!cancelled) {
        setSelectedTenantState(window.localStorage.getItem(TENANT_STORAGE_KEY));
        setSelectedBranchIdState(window.localStorage.getItem(BRANCH_STORAGE_KEY));
        setLoading(false);
      }
    }

    void init();
    return () => {
      cancelled = true;
    };
  }, []);

  const setSelectedTenant = useCallback((subdomain: string | null) => {
    setSelectedTenantState(subdomain);
    if (subdomain) {
      window.localStorage.setItem(TENANT_STORAGE_KEY, subdomain);
    } else {
      window.localStorage.removeItem(TENANT_STORAGE_KEY);
    }
  }, []);

  const setSelectedBranchId = useCallback((branchId: string | null) => {
    setSelectedBranchIdState(branchId);
    if (branchId) {
      window.localStorage.setItem(BRANCH_STORAGE_KEY, branchId);
    } else {
      window.localStorage.removeItem(BRANCH_STORAGE_KEY);
    }
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const auth = await authLogin(email, password);
    setUser(auth.user);
  }, []);

  const logout = useCallback(async () => {
    await authLogout();
    setUser(null);
    setSelectedTenant(null);
    setSelectedBranchId(null);
  }, [setSelectedTenant, setSelectedBranchId]);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      loading,
      selectedTenant,
      setSelectedTenant,
      selectedBranchId,
      setSelectedBranchId,
      login,
      logout,
    }),
    [user, loading, selectedTenant, setSelectedTenant, selectedBranchId, setSelectedBranchId, login, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return ctx;
}
