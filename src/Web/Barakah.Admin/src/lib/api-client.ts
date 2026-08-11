import { ensureFreshAccessToken } from "./auth";

export class ApiError extends Error {
  status: number;
  constructor(status: number, message: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

export interface ApiFetchOptions {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  body?: unknown;
  tenantSubdomain?: string;
  /** Skip attaching the Authorization header (not needed by these services, but here for completeness). */
  skipAuth?: boolean;
}

function readErrorMessage(body: unknown, fallback: string): string {
  if (body && typeof body === "object" && "error" in body && typeof (body as { error?: unknown }).error === "string") {
    return (body as { error: string }).error;
  }
  return fallback;
}

/**
 * Generic fetch wrapper for all Barakah backend services.
 *
 * None of Catalog/Inventory/Order/Payment/Tenant validate the JWT — they gate tenant-scoped
 * requests on the `X-Tenant-Subdomain` header instead. We still attach `Authorization` when a
 * token is available since Identity's `/api/users/me` needs it, and it's harmless elsewhere.
 */
export async function apiFetch<T>(baseUrl: string, path: string, options: ApiFetchOptions = {}): Promise<T> {
  const { method = "GET", body, tenantSubdomain, skipAuth = false } = options;

  const headers: Record<string, string> = {};

  if (!skipAuth) {
    const token = await ensureFreshAccessToken();
    if (token) {
      headers["Authorization"] = `Bearer ${token}`;
    }
  }

  if (tenantSubdomain) {
    headers["X-Tenant-Subdomain"] = tenantSubdomain;
  }

  if (body !== undefined) {
    headers["Content-Type"] = "application/json";
  }

  let res: Response;
  try {
    res = await fetch(`${baseUrl}${path}`, {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  } catch {
    throw new ApiError(0, "Failed to load — is the service running?");
  }

  if (res.status === 204) {
    return undefined as T;
  }

  let parsed: unknown = null;
  const text = await res.text();
  if (text) {
    try {
      parsed = JSON.parse(text);
    } catch {
      parsed = null;
    }
  }

  if (!res.ok) {
    throw new ApiError(res.status, readErrorMessage(parsed, `Request failed with status ${res.status}.`));
  }

  return parsed as T;
}

const IDENTITY_URL = process.env.NEXT_PUBLIC_IDENTITY_URL ?? "http://localhost:5001";
const TENANT_URL = process.env.NEXT_PUBLIC_TENANT_URL ?? "http://localhost:5003";
const CATALOG_URL = process.env.NEXT_PUBLIC_CATALOG_URL ?? "http://localhost:5004";
const INVENTORY_URL = process.env.NEXT_PUBLIC_INVENTORY_URL ?? "http://localhost:5005";
const ORDER_URL = process.env.NEXT_PUBLIC_ORDER_URL ?? "http://localhost:5006";
const PAYMENT_URL = process.env.NEXT_PUBLIC_PAYMENT_URL ?? "http://localhost:5007";
const NOTIFICATION_URL = process.env.NEXT_PUBLIC_NOTIFICATION_URL ?? "http://localhost:5011";
const ANALYTICS_URL = process.env.NEXT_PUBLIC_ANALYTICS_URL ?? "http://localhost:5012";

export const identityApi = {
  me: () => apiFetch(IDENTITY_URL, "/api/users/me"),
};

export const tenantApi = {
  baseUrl: TENANT_URL,
  getById: (id: string) => apiFetch(TENANT_URL, `/api/tenants/${id}`, { skipAuth: true }),
  getBySubdomain: (subdomain: string) =>
    apiFetch(TENANT_URL, `/api/tenants/by-subdomain/${encodeURIComponent(subdomain)}`, { skipAuth: true }),
  create: (body: unknown) => apiFetch(TENANT_URL, "/api/tenants", { method: "POST", body, skipAuth: true }),
  updateStatus: (id: string, body: unknown) =>
    apiFetch(TENANT_URL, `/api/tenants/${id}/status`, { method: "PATCH", body, skipAuth: true }),
};

export const catalogApi = {
  baseUrl: CATALOG_URL,
  list: (tenantSubdomain: string) => apiFetch(CATALOG_URL, "/api/products", { tenantSubdomain }),
  getById: (id: string, tenantSubdomain: string) => apiFetch(CATALOG_URL, `/api/products/${id}`, { tenantSubdomain }),
  create: (body: unknown, tenantSubdomain: string) =>
    apiFetch(CATALOG_URL, "/api/products", { method: "POST", body, tenantSubdomain }),
  update: (id: string, body: unknown, tenantSubdomain: string) =>
    apiFetch(CATALOG_URL, `/api/products/${id}`, { method: "PUT", body, tenantSubdomain }),
  remove: (id: string, tenantSubdomain: string) =>
    apiFetch(CATALOG_URL, `/api/products/${id}`, { method: "DELETE", tenantSubdomain }),
};

export const inventoryApi = {
  baseUrl: INVENTORY_URL,
  listByBranch: (branchId: string, tenantSubdomain: string) =>
    apiFetch(INVENTORY_URL, `/api/inventory/branch/${branchId}`, { tenantSubdomain }),
  listLowStock: (tenantSubdomain: string) => apiFetch(INVENTORY_URL, "/api/inventory/low-stock", { tenantSubdomain }),
  create: (body: unknown, tenantSubdomain: string) =>
    apiFetch(INVENTORY_URL, "/api/inventory", { method: "POST", body, tenantSubdomain }),
  adjust: (id: string, body: unknown, tenantSubdomain: string) =>
    apiFetch(INVENTORY_URL, `/api/inventory/${id}/adjust`, { method: "PATCH", body, tenantSubdomain }),
};

export const orderApi = {
  baseUrl: ORDER_URL,
  getById: (id: string, tenantSubdomain: string) => apiFetch(ORDER_URL, `/api/orders/${id}`, { tenantSubdomain }),
  listByBranch: (branchId: string, tenantSubdomain: string) =>
    apiFetch(ORDER_URL, `/api/orders/branch/${branchId}`, { tenantSubdomain }),
  create: (body: unknown, tenantSubdomain: string) =>
    apiFetch(ORDER_URL, "/api/orders", { method: "POST", body, tenantSubdomain }),
  updateStatus: (id: string, body: unknown, tenantSubdomain: string) =>
    apiFetch(ORDER_URL, `/api/orders/${id}/status`, { method: "PATCH", body, tenantSubdomain }),
  updatePaymentStatus: (id: string, body: unknown, tenantSubdomain: string) =>
    apiFetch(ORDER_URL, `/api/orders/${id}/payment-status`, { method: "PATCH", body, tenantSubdomain }),
};

export const paymentApi = {
  baseUrl: PAYMENT_URL,
  getById: (id: string, tenantSubdomain: string) => apiFetch(PAYMENT_URL, `/api/payments/${id}`, { tenantSubdomain }),
  listByOrder: (orderId: string, tenantSubdomain: string) =>
    apiFetch(PAYMENT_URL, `/api/payments/order/${orderId}`, { tenantSubdomain }),
  create: (body: unknown, tenantSubdomain: string) =>
    apiFetch(PAYMENT_URL, "/api/payments", { method: "POST", body, tenantSubdomain }),
};

export const notificationApi = {
  baseUrl: NOTIFICATION_URL,
  list: (tenantSubdomain: string) => apiFetch(NOTIFICATION_URL, "/api/notifications", { tenantSubdomain }),
  getById: (id: string, tenantSubdomain: string) =>
    apiFetch(NOTIFICATION_URL, `/api/notifications/${id}`, { tenantSubdomain }),
  create: (body: unknown, tenantSubdomain: string) =>
    apiFetch(NOTIFICATION_URL, "/api/notifications", { method: "POST", body, tenantSubdomain }),
};

export const analyticsApi = {
  baseUrl: ANALYTICS_URL,
  salesSummary: (tenantSubdomain: string, from?: string, to?: string) => {
    const params = new URLSearchParams();
    if (from) params.set("from", from);
    if (to) params.set("to", to);
    const qs = params.toString();
    return apiFetch(ANALYTICS_URL, `/api/analytics/sales-summary${qs ? `?${qs}` : ""}`, { tenantSubdomain });
  },
  /**
   * The export endpoint is tenant-scoped via the `X-Tenant-Subdomain` header (like every other
   * service here), so it can't be a plain <a href> link — the browser can't attach custom headers
   * to a navigation. Fetch it and trigger a client-side blob download instead.
   */
  downloadOrdersCsv: async (tenantSubdomain: string): Promise<void> => {
    const token = await ensureFreshAccessToken();
    const res = await fetch(`${ANALYTICS_URL}/api/analytics/export/orders.csv`, {
      headers: {
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        "X-Tenant-Subdomain": tenantSubdomain,
      },
    });
    if (!res.ok) {
      throw new ApiError(res.status, `Export failed with status ${res.status}.`);
    }
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = "orders.csv";
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
  },
};
