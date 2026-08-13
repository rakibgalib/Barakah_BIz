"use client";

import { useCallback, useEffect, useState } from "react";
import type { FormEvent } from "react";
import { useAuth } from "@/context/AuthContext";
import { DataTable } from "@/components/DataTable";
import { superShopApi, ApiError } from "@/lib/api-client";
import { runDeferred } from "@/lib/effect-utils";
import type { LoyaltyAccount, ProductBatch, Supplier } from "@/lib/types";

function SuppliersSection({ tenant }: { tenant: string }) {
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState({ name: "", contactEmail: "", contactPhone: "" });
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  const load = useCallback(() => {
    runDeferred(() => {
      setLoading(true);
      setError(null);
      superShopApi
        .listSuppliers(tenant)
        .then((data) => setSuppliers(data as Supplier[]))
        .catch((err) => setError(err instanceof ApiError ? err.message : "Failed to load — is the SuperShop service running?"))
        .finally(() => setLoading(false));
    });
  }, [tenant]);

  useEffect(() => {
    load();
  }, [load]);

  async function handleCreate(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!form.name.trim()) {
      setCreateError("Name is required.");
      return;
    }
    setCreateError(null);
    setCreating(true);
    try {
      await superShopApi.createSupplier(
        { name: form.name.trim(), contactEmail: form.contactEmail.trim() || null, contactPhone: form.contactPhone.trim() || null },
        tenant
      );
      setForm({ name: "", contactEmail: "", contactPhone: "" });
      load();
    } catch (err) {
      setCreateError(err instanceof ApiError ? err.message : "Failed to reach the SuperShop service — is it running?");
    } finally {
      setCreating(false);
    }
  }

  async function rate(id: string, rating: number) {
    try {
      await superShopApi.rateSupplier(id, { rating }, tenant);
      load();
    } catch {
      // surfaced via the table's own error state on next load if the service is down
    }
  }

  return (
    <div className="space-y-4">
      <DataTable<Supplier>
        columns={[
          { key: "name", label: "Name" },
          { key: "contactEmail", label: "Email", render: (s) => s.contactEmail ?? "—" },
          { key: "contactPhone", label: "Phone", render: (s) => s.contactPhone ?? "—" },
          { key: "rating", label: "Rating", render: (s) => (s.rating != null ? s.rating.toFixed(1) : "—") },
          {
            key: "actions",
            label: "",
            render: (s) => (
              <div className="flex gap-1">
                {[3, 4, 5].map((r) => (
                  <button key={r} type="button" onClick={() => rate(s.id, r)} className="btn-secondary px-2 py-1 text-xs">
                    {r}★
                  </button>
                ))}
              </div>
            ),
          },
        ]}
        rows={suppliers}
        loading={loading}
        error={error}
        emptyMessage="No suppliers yet for this tenant."
      />
      <form onSubmit={handleCreate} className="flex flex-wrap items-end gap-2">
        <input type="text" value={form.name} onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))} placeholder="Supplier name" className="input-field max-w-xs" />
        <input type="email" value={form.contactEmail} onChange={(e) => setForm((f) => ({ ...f, contactEmail: e.target.value }))} placeholder="Email" className="input-field max-w-xs" />
        <input type="text" value={form.contactPhone} onChange={(e) => setForm((f) => ({ ...f, contactPhone: e.target.value }))} placeholder="Phone" className="input-field max-w-xs" />
        <button type="submit" disabled={creating} className="btn-primary shrink-0">
          {creating ? "Adding..." : "Add supplier"}
        </button>
      </form>
      {createError && <p className="text-sm text-red-600">{createError}</p>}
    </div>
  );
}

function LoyaltySection({ tenant }: { tenant: string }) {
  const [customerIdDraft, setCustomerIdDraft] = useState("");
  const [account, setAccount] = useState<LoyaltyAccount | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [earnPoints, setEarnPoints] = useState("10");

  async function load(customerId: string) {
    if (!customerId) return;
    setError(null);
    try {
      const acc = (await superShopApi.getLoyaltyAccount(customerId, tenant)) as LoyaltyAccount;
      setAccount(acc);
    } catch (err) {
      setAccount(null);
      setError(err instanceof ApiError ? err.message : "No account found, or service unreachable.");
    }
  }

  async function earn() {
    const points = Number(earnPoints);
    if (!customerIdDraft.trim() || Number.isNaN(points) || points <= 0) return;
    try {
      const acc = (await superShopApi.earnPoints({ customerId: customerIdDraft.trim(), points }, tenant)) as LoyaltyAccount;
      setAccount(acc);
      setError(null);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to reach the SuperShop service — is it running?");
    }
  }

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap gap-2">
        <input type="text" value={customerIdDraft} onChange={(e) => setCustomerIdDraft(e.target.value)} placeholder="Customer ID" className="input-field max-w-xs" />
        <button type="button" onClick={() => load(customerIdDraft.trim())} className="btn-secondary shrink-0">
          Look up
        </button>
        <input type="number" value={earnPoints} onChange={(e) => setEarnPoints(e.target.value)} className="input-field w-24 px-2 py-2" />
        <button type="button" onClick={earn} className="btn-primary shrink-0">
          Earn points
        </button>
      </div>
      {error && <p className="text-sm text-red-600">{error}</p>}
      {account && (
        <div className="card max-w-sm">
          <p className="text-sm text-ink-500">Points balance</p>
          <p className="text-2xl font-semibold tabular-nums text-ink-900">{account.pointsBalance}</p>
          <p className="mt-2 text-xs text-ink-500">Lifetime earned: {account.totalPointsEarned}</p>
        </div>
      )}
    </div>
  );
}

function ExpiringBatchesSection({ tenant }: { tenant: string }) {
  const [batches, setBatches] = useState<ProductBatch[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(() => {
    runDeferred(() => {
      setLoading(true);
      setError(null);
      superShopApi
        .listExpiringBatches(tenant)
        .then((data) => setBatches(data as ProductBatch[]))
        .catch((err) => setError(err instanceof ApiError ? err.message : "Failed to load — is the SuperShop service running?"))
        .finally(() => setLoading(false));
    });
  }, [tenant]);

  useEffect(() => {
    load();
  }, [load]);

  return (
    <DataTable<ProductBatch>
      columns={[
        { key: "batchNumber", label: "Batch #" },
        { key: "quantity", label: "Qty", align: "right" },
        { key: "expiresAt", label: "Expires", render: (b) => new Date(b.expiresAt).toLocaleDateString() },
      ]}
      rows={batches}
      loading={loading}
      error={error}
      emptyMessage="No batches expiring soon."
    />
  );
}

export default function SuperShopPage() {
  const { selectedTenant } = useAuth();

  if (!selectedTenant) {
    return (
      <div>
        <h1 className="page-title">SuperShop</h1>
        <p className="page-subtitle">Pick an active tenant in the sidebar to manage suppliers, loyalty, and stock.</p>
      </div>
    );
  }

  return (
    <div className="space-y-10">
      <h1 className="page-title">SuperShop</h1>

      <section>
        <h2 className="section-label mb-2">Suppliers</h2>
        <SuppliersSection tenant={selectedTenant} />
      </section>

      <section>
        <h2 className="section-label mb-2">Customer Loyalty</h2>
        <LoyaltySection tenant={selectedTenant} />
      </section>

      <section>
        <h2 className="section-label mb-2">Expiring Batches</h2>
        <ExpiringBatchesSection tenant={selectedTenant} />
      </section>
    </div>
  );
}
