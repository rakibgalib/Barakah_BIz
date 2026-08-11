"use client";

import { useCallback, useEffect, useState } from "react";
import { useAuth } from "@/context/AuthContext";
import { DataTable, type DataTableColumn } from "@/components/DataTable";
import { inventoryApi, ApiError } from "@/lib/api-client";
import { runDeferred } from "@/lib/effect-utils";
import type { InventoryItem } from "@/lib/types";

function AdjustControl({
  item,
  tenant,
  onAdjusted,
}: {
  item: InventoryItem;
  tenant: string;
  onAdjusted: () => void;
}) {
  const [delta, setDelta] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function apply() {
    const parsed = Number(delta);
    if (!delta || Number.isNaN(parsed) || !Number.isInteger(parsed)) {
      setError("Enter a whole number");
      return;
    }
    setError(null);
    setBusy(true);
    try {
      await inventoryApi.adjust(item.id, { delta: parsed, updatedBy: null }, tenant);
      setDelta("");
      onAdjusted();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Adjust failed");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="flex items-center gap-1">
      <input
        type="number"
        value={delta}
        onChange={(e) => setDelta(e.target.value)}
        placeholder="+/-5"
        className="input-field w-20 px-2 py-1"
      />
      <button
        type="button"
        disabled={busy}
        onClick={apply}
        className="btn-primary px-2 py-1 text-xs"
      >
        Adjust
      </button>
      {error && <span className="text-xs text-red-600">{error}</span>}
    </div>
  );
}

export default function InventoryPage() {
  const { selectedTenant } = useAuth();

  const [lowStock, setLowStock] = useState<InventoryItem[]>([]);
  const [lowStockLoading, setLowStockLoading] = useState(false);
  const [lowStockError, setLowStockError] = useState<string | null>(null);

  const [branchId, setBranchId] = useState("");
  const [branchItems, setBranchItems] = useState<InventoryItem[]>([]);
  const [branchLoading, setBranchLoading] = useState(false);
  const [branchError, setBranchError] = useState<string | null>(null);
  const [searchedBranchId, setSearchedBranchId] = useState<string | null>(null);

  const loadLowStock = useCallback(() => {
    if (!selectedTenant) return;
    runDeferred(() => {
      setLowStockLoading(true);
      setLowStockError(null);
      inventoryApi
        .listLowStock(selectedTenant)
        .then((items) => setLowStock(items as InventoryItem[]))
        .catch((err) =>
          setLowStockError(err instanceof ApiError ? err.message : "Failed to load — is the Inventory service running?")
        )
        .finally(() => setLowStockLoading(false));
    });
  }, [selectedTenant]);

  useEffect(() => {
    loadLowStock();
  }, [loadLowStock]);

  const loadBranch = useCallback(() => {
    if (!selectedTenant || !branchId.trim()) return;
    const id = branchId.trim();
    setSearchedBranchId(id);
    setBranchLoading(true);
    setBranchError(null);
    inventoryApi
      .listByBranch(id, selectedTenant)
      .then((items) => setBranchItems(items as InventoryItem[]))
      .catch((err) =>
        setBranchError(err instanceof ApiError ? err.message : "Failed to load — is the Inventory service running?")
      )
      .finally(() => setBranchLoading(false));
  }, [selectedTenant, branchId]);

  if (!selectedTenant) {
    return (
      <div>
        <h1 className="page-title mb-2">Inventory</h1>
        <p className="text-ink-600">Pick an active tenant in the sidebar to manage its inventory.</p>
      </div>
    );
  }

  const columns = (onAdjusted: () => void): DataTableColumn<InventoryItem>[] => [
    { key: "productId", label: "Product Id" },
    { key: "branchId", label: "Branch Id" },
    { key: "quantity", label: "Qty" },
    { key: "minStockLevel", label: "Min" },
    { key: "maxStockLevel", label: "Max" },
    {
      key: "adjust",
      label: "Adjust stock",
      render: (item: InventoryItem) => <AdjustControl item={item} tenant={selectedTenant} onAdjusted={onAdjusted} />,
    },
  ];

  return (
    <div className="space-y-8">
      <div>
        <h1 className="page-title mb-2">Inventory</h1>
        <p className="mb-4 text-sm text-ink-500">Low stock items across all branches for this tenant.</p>
        <DataTable<InventoryItem>
          columns={columns(loadLowStock)}
          rows={lowStock}
          loading={lowStockLoading}
          error={lowStockError}
          emptyMessage="No low-stock items."
        />
      </div>

      <div>
        <h2 className="mb-2 text-base font-semibold text-ink-900">Browse by branch</h2>
        <p className="mb-4 text-sm text-ink-500">
          Inventory Service only lists items by branch (no tenant-wide list exists), so enter a
          Branch Id to see its full stock.
        </p>
        <div className="mb-4 flex max-w-md gap-2">
          <input
            type="text"
            value={branchId}
            onChange={(e) => setBranchId(e.target.value)}
            placeholder="Branch Id (guid)"
            className="input-field"
          />
          <button
            type="button"
            onClick={loadBranch}
            className="btn-primary shrink-0"
          >
            Load
          </button>
        </div>
        {searchedBranchId && (
          <DataTable<InventoryItem>
            columns={columns(loadBranch)}
            rows={branchItems}
            loading={branchLoading}
            error={branchError}
            emptyMessage={`No inventory records for branch ${searchedBranchId}.`}
          />
        )}
      </div>
    </div>
  );
}
