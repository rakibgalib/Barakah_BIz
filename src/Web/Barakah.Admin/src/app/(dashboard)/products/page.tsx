"use client";

import { useCallback, useEffect, useState } from "react";
import type { FormEvent } from "react";
import { useAuth } from "@/context/AuthContext";
import { DataTable } from "@/components/DataTable";
import { catalogApi, ApiError } from "@/lib/api-client";
import { runDeferred } from "@/lib/effect-utils";
import type { Product } from "@/lib/types";

const EMPTY_FORM = { sku: "", name: "", description: "", basePrice: "", businessType: "retail", category: "" };

export default function ProductsPage() {
  const { selectedTenant } = useAuth();

  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [form, setForm] = useState(EMPTY_FORM);
  const [createError, setCreateError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);

  const loadProducts = useCallback(() => {
    if (!selectedTenant) return;
    runDeferred(() => {
      setLoading(true);
      setError(null);
      catalogApi
        .list(selectedTenant)
        .then((items) => setProducts(items as Product[]))
        .catch((err) => setError(err instanceof ApiError ? err.message : "Failed to load — is the Catalog service running?"))
        .finally(() => setLoading(false));
    });
  }, [selectedTenant]);

  useEffect(() => {
    loadProducts();
  }, [loadProducts]);

  async function handleCreate(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!selectedTenant) return;

    const basePrice = Number(form.basePrice);
    if (!form.sku.trim() || !form.name.trim() || !form.businessType.trim() || Number.isNaN(basePrice)) {
      setCreateError("SKU, name, business type, and a valid base price are required.");
      return;
    }
    setCreateError(null);
    setCreating(true);
    try {
      await catalogApi.create(
        {
          sku: form.sku.trim(),
          name: form.name.trim(),
          description: form.description.trim() || null,
          basePrice,
          businessType: form.businessType.trim(),
          category: form.category.trim() || null,
        },
        selectedTenant
      );
      setForm(EMPTY_FORM);
      loadProducts();
    } catch (err) {
      setCreateError(err instanceof ApiError ? err.message : "Failed to reach the Catalog service — is it running?");
    } finally {
      setCreating(false);
    }
  }

  if (!selectedTenant) {
    return (
      <div>
        <h1 className="page-title mb-2">Products</h1>
        <p className="text-ink-600">Pick an active tenant in the sidebar to manage its products.</p>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div>
        <h1 className="page-title mb-4">Products</h1>
        <DataTable<Product>
          columns={[
            { key: "sku", label: "SKU" },
            { key: "name", label: "Name" },
            { key: "basePrice", label: "Price", render: (p) => `$${p.basePrice.toFixed(2)}` },
            { key: "businessType", label: "Type" },
            { key: "category", label: "Category", render: (p) => p.category ?? "—" },
            { key: "updatedAt", label: "Updated", render: (p) => new Date(p.updatedAt).toLocaleString() },
          ]}
          rows={products}
          loading={loading}
          error={error}
          emptyMessage="No products found for this tenant."
        />
      </div>

      <div className="card max-w-md">
        <h2 className="mb-4 text-base font-semibold text-ink-900">Create product</h2>
        <form onSubmit={handleCreate} className="space-y-3">
          <div>
            <label className="field-label">SKU</label>
            <input
              type="text"
              value={form.sku}
              onChange={(e) => setForm((f) => ({ ...f, sku: e.target.value }))}
              className="input-field"
            />
          </div>
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
            <label className="field-label">Description</label>
            <input
              type="text"
              value={form.description}
              onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
              className="input-field"
            />
          </div>
          <div>
            <label className="field-label">Base price</label>
            <input
              type="number"
              step="0.01"
              value={form.basePrice}
              onChange={(e) => setForm((f) => ({ ...f, basePrice: e.target.value }))}
              className="input-field"
            />
          </div>
          <div>
            <label className="field-label">Business type</label>
            <input
              type="text"
              value={form.businessType}
              onChange={(e) => setForm((f) => ({ ...f, businessType: e.target.value }))}
              className="input-field"
            />
          </div>
          <div>
            <label className="field-label">Category</label>
            <input
              type="text"
              value={form.category}
              onChange={(e) => setForm((f) => ({ ...f, category: e.target.value }))}
              className="input-field"
            />
          </div>
          {createError && <p className="text-sm text-red-600">{createError}</p>}
          <button
            type="submit"
            disabled={creating}
            className="btn-primary w-full"
          >
            {creating ? "Creating..." : "Create product"}
          </button>
        </form>
      </div>
    </div>
  );
}
