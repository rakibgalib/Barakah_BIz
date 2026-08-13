"use client";

import { useCallback, useEffect, useState } from "react";
import type { FormEvent } from "react";
import { useAuth } from "@/context/AuthContext";
import { DataTable } from "@/components/DataTable";
import { restaurantApi, ApiError } from "@/lib/api-client";
import { runDeferred } from "@/lib/effect-utils";
import type { MenuItem } from "@/lib/types";

const EMPTY_FORM = { name: "", description: "", basePrice: "", category: "", allergenTags: "" };

export default function MenuItemsPage() {
  const { selectedTenant } = useAuth();

  const [items, setItems] = useState<MenuItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [prices, setPrices] = useState<Record<string, string>>({});

  const [form, setForm] = useState(EMPTY_FORM);
  const [createError, setCreateError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);

  const loadItems = useCallback(() => {
    if (!selectedTenant) return;
    runDeferred(() => {
      setLoading(true);
      setError(null);
      restaurantApi
        .list(selectedTenant)
        .then((data) => setItems(data as MenuItem[]))
        .catch((err) => setError(err instanceof ApiError ? err.message : "Failed to load — is the Restaurant service running?"))
        .finally(() => setLoading(false));
    });
  }, [selectedTenant]);

  useEffect(() => {
    loadItems();
  }, [loadItems]);

  async function handleCreate(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!selectedTenant) return;

    const basePrice = Number(form.basePrice);
    if (!form.name.trim() || !form.category.trim() || Number.isNaN(basePrice)) {
      setCreateError("Name, category, and a valid price are required.");
      return;
    }
    setCreateError(null);
    setCreating(true);
    try {
      await restaurantApi.create(
        {
          name: form.name.trim(),
          description: form.description.trim() || null,
          basePrice,
          category: form.category.trim(),
          allergenTags: form.allergenTags.trim() || null,
        },
        selectedTenant
      );
      setForm(EMPTY_FORM);
      loadItems();
    } catch (err) {
      setCreateError(err instanceof ApiError ? err.message : "Failed to reach the Restaurant service — is it running?");
    } finally {
      setCreating(false);
    }
  }

  async function checkPrice(id: string) {
    if (!selectedTenant) return;
    try {
      const priced = (await restaurantApi.currentPrice(id, selectedTenant)) as { currentPrice: number; pricingTier: string };
      setPrices((p) => ({ ...p, [id]: `$${priced.currentPrice.toFixed(2)} (${priced.pricingTier})` }));
    } catch {
      setPrices((p) => ({ ...p, [id]: "—" }));
    }
  }

  if (!selectedTenant) {
    return (
      <div>
        <h1 className="page-title">Menu Items</h1>
        <p className="page-subtitle">Pick an active tenant in the sidebar to manage the menu.</p>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div>
        <h1 className="page-title mb-4">Menu Items</h1>
        <DataTable<MenuItem>
          columns={[
            { key: "name", label: "Name" },
            { key: "category", label: "Category" },
            { key: "basePrice", label: "Base price", align: "right", render: (m) => `$${m.basePrice.toFixed(2)}` },
            { key: "allergenTags", label: "Allergens", render: (m) => m.allergenTags ?? "—" },
            {
              key: "currentPrice",
              label: "Current price",
              render: (m) =>
                prices[m.id] ?? (
                  <button type="button" onClick={() => checkPrice(m.id)} className="btn-secondary text-xs">
                    Check
                  </button>
                ),
            },
          ]}
          rows={items}
          loading={loading}
          error={error}
          emptyMessage="No menu items yet for this tenant."
        />
      </div>

      <div className="card max-w-md">
        <h2 className="mb-4 text-base font-semibold text-ink-900">Add menu item</h2>
        <form onSubmit={handleCreate} className="space-y-3">
          <div>
            <label className="field-label">Name</label>
            <input type="text" value={form.name} onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))} className="input-field" />
          </div>
          <div>
            <label className="field-label">Description</label>
            <input type="text" value={form.description} onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))} className="input-field" />
          </div>
          <div>
            <label className="field-label">Category</label>
            <input type="text" value={form.category} onChange={(e) => setForm((f) => ({ ...f, category: e.target.value }))} className="input-field" />
          </div>
          <div>
            <label className="field-label">Base price</label>
            <input type="number" step="0.01" value={form.basePrice} onChange={(e) => setForm((f) => ({ ...f, basePrice: e.target.value }))} className="input-field" />
          </div>
          <div>
            <label className="field-label">Allergen tags (comma-separated)</label>
            <input type="text" value={form.allergenTags} onChange={(e) => setForm((f) => ({ ...f, allergenTags: e.target.value }))} placeholder="nuts,dairy" className="input-field" />
          </div>
          {createError && <p className="text-sm text-red-600">{createError}</p>}
          <button type="submit" disabled={creating} className="btn-primary w-full">
            {creating ? "Adding..." : "Add menu item"}
          </button>
        </form>
      </div>
    </div>
  );
}
