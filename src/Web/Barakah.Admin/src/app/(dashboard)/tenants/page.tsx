"use client";

import { useState } from "react";
import type { FormEvent } from "react";
import { DataTable } from "@/components/DataTable";
import { StatusBadge } from "@/components/StatusBadge";
import { tenantApi, ApiError } from "@/lib/api-client";
import type { Tenant } from "@/lib/types";

// Tenant Service has no "list all tenants" endpoint (only create / get-by-id / get-by-subdomain /
// update-status — see src/Modules/TenantService/Controllers/TenantsController.cs). We keep a
// client-side registry of tenants this admin has created or looked up, refreshable one at a time
// via GetBySubdomain, instead of a live server-backed list.
const KNOWN_TENANTS_KEY = "barakah-admin-known-tenants";

function loadKnownTenants(): Tenant[] {
  if (typeof window === "undefined") return [];
  try {
    const raw = window.localStorage.getItem(KNOWN_TENANTS_KEY);
    return raw ? (JSON.parse(raw) as Tenant[]) : [];
  } catch {
    return [];
  }
}

function saveKnownTenants(tenants: Tenant[]) {
  window.localStorage.setItem(KNOWN_TENANTS_KEY, JSON.stringify(tenants));
}

function upsertTenant(tenants: Tenant[], tenant: Tenant): Tenant[] {
  const withoutExisting = tenants.filter((t) => t.id !== tenant.id);
  return [tenant, ...withoutExisting];
}

export default function TenantsPage() {
  const [tenants, setTenants] = useState<Tenant[]>(() => loadKnownTenants());
  const [lookupSubdomain, setLookupSubdomain] = useState("");
  const [lookupError, setLookupError] = useState<string | null>(null);
  const [lookupLoading, setLookupLoading] = useState(false);

  const [form, setForm] = useState({ name: "", subdomain: "", email: "", tier: "starter" });
  const [createError, setCreateError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);

  async function handleLookup(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!lookupSubdomain.trim()) return;
    setLookupError(null);
    setLookupLoading(true);
    try {
      const tenant = (await tenantApi.getBySubdomain(lookupSubdomain.trim())) as Tenant;
      const next = upsertTenant(tenants, tenant);
      setTenants(next);
      saveKnownTenants(next);
      setLookupSubdomain("");
    } catch (err) {
      setLookupError(err instanceof ApiError ? err.message : "Failed to load — is the Tenant service running?");
    } finally {
      setLookupLoading(false);
    }
  }

  async function handleCreate(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!form.name.trim() || !form.subdomain.trim() || !form.email.trim() || !form.tier.trim()) {
      setCreateError("All fields are required.");
      return;
    }
    setCreateError(null);
    setCreating(true);
    try {
      const tenant = (await tenantApi.create({
        name: form.name.trim(),
        subdomain: form.subdomain.trim(),
        email: form.email.trim(),
        tier: form.tier.trim(),
      })) as Tenant;
      const next = upsertTenant(tenants, tenant);
      setTenants(next);
      saveKnownTenants(next);
      setForm({ name: "", subdomain: "", email: "", tier: "starter" });
    } catch (err) {
      setCreateError(err instanceof ApiError ? err.message : "Failed to reach the Tenant service — is it running?");
    } finally {
      setCreating(false);
    }
  }

  return (
    <div className="space-y-8">
      <div>
        <h1 className="page-title">Tenants</h1>
        <p className="page-subtitle mb-4">
          The Tenant service does not expose a bulk list endpoint, so this list is built from
          tenants you create or look up below.
        </p>

        <form onSubmit={handleLookup} className="mb-4 flex max-w-md gap-2">
          <input
            type="text"
            value={lookupSubdomain}
            onChange={(e) => setLookupSubdomain(e.target.value)}
            placeholder="Look up by subdomain"
            className="input-field"
          />
          <button type="submit" disabled={lookupLoading} className="btn-primary shrink-0">
            {lookupLoading ? "Looking up..." : "Look up"}
          </button>
        </form>
        {lookupError && <p className="mb-4 text-sm text-red-600">{lookupError}</p>}

        <DataTable<Tenant>
          columns={[
            { key: "name", label: "Name" },
            { key: "subdomain", label: "Subdomain" },
            { key: "email", label: "Email" },
            { key: "tier", label: "Tier" },
            { key: "status", label: "Status", render: (t) => <StatusBadge status={t.status} /> },
            { key: "createdAt", label: "Created", render: (t) => new Date(t.createdAt).toLocaleString() },
          ]}
          rows={tenants}
          loading={false}
          error={null}
          emptyMessage="No known tenants yet — create one below or look one up by subdomain."
        />
      </div>

      <div className="card max-w-md">
        <h2 className="mb-4 text-base font-semibold text-ink-900">Create tenant</h2>
        <form onSubmit={handleCreate} className="space-y-3">
          <div>
            <label className="field-label">Name</label>
            <input
              type="text"
              value={form.name}
              onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
              className="input-field"
            />
          </div>
          <div>
            <label className="field-label">Subdomain</label>
            <input
              type="text"
              value={form.subdomain}
              onChange={(e) => setForm((f) => ({ ...f, subdomain: e.target.value }))}
              className="input-field"
            />
          </div>
          <div>
            <label className="field-label">Email</label>
            <input
              type="email"
              value={form.email}
              onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))}
              className="input-field"
            />
          </div>
          <div>
            <label className="field-label">Tier</label>
            <input
              type="text"
              value={form.tier}
              onChange={(e) => setForm((f) => ({ ...f, tier: e.target.value }))}
              className="input-field"
            />
          </div>
          {createError && <p className="text-sm text-red-600">{createError}</p>}
          <button type="submit" disabled={creating} className="btn-primary w-full">
            {creating ? "Creating..." : "Create tenant"}
          </button>
        </form>
      </div>
    </div>
  );
}
