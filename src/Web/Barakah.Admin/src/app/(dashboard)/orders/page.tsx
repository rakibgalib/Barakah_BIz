"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/context/AuthContext";
import { DataTable } from "@/components/DataTable";
import { StatusBadge } from "@/components/StatusBadge";
import { orderApi, ApiError } from "@/lib/api-client";
import { runDeferred } from "@/lib/effect-utils";
import type { Order } from "@/lib/types";

export default function OrdersPage() {
  const { selectedTenant, selectedBranchId, setSelectedBranchId } = useAuth();
  const [branchDraft, setBranchDraft] = useState(selectedBranchId ?? "");

  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadOrders = useCallback(() => {
    if (!selectedTenant || !selectedBranchId) return;
    runDeferred(() => {
      setLoading(true);
      setError(null);
      orderApi
        .listByBranch(selectedBranchId, selectedTenant)
        .then((items) => setOrders(items as Order[]))
        .catch((err) => setError(err instanceof ApiError ? err.message : "Failed to load — is the Order service running?"))
        .finally(() => setLoading(false));
    });
  }, [selectedTenant, selectedBranchId]);

  useEffect(() => {
    loadOrders();
  }, [loadOrders]);

  if (!selectedTenant) {
    return (
      <div>
        <h1 className="page-title mb-2">Orders</h1>
        <p className="text-ink-600">Pick an active tenant in the sidebar to view its orders.</p>
      </div>
    );
  }

  return (
    <div>
      <h1 className="page-title mb-2">Orders</h1>
      <p className="mb-4 text-sm text-ink-500">
        Order Service only lists orders by branch (no tenant-wide list exists). Enter a Branch Id
        to load its orders.
      </p>
      <div className="mb-4 flex max-w-md gap-2">
        <input
          type="text"
          value={branchDraft}
          onChange={(e) => setBranchDraft(e.target.value)}
          placeholder="Branch Id (guid)"
          className="input-field"
        />
        <button
          type="button"
          onClick={() => setSelectedBranchId(branchDraft.trim() || null)}
          className="btn-primary shrink-0"
        >
          Load
        </button>
      </div>

      {selectedBranchId ? (
        <DataTable<Order>
          columns={[
            {
              key: "orderNumber",
              label: "Order #",
              render: (o) => (
                <Link href={`/orders/${o.id}`} className="text-blue-700 hover:underline">
                  {o.orderNumber}
                </Link>
              ),
            },
            { key: "status", label: "Status", render: (o) => <StatusBadge status={o.status} /> },
            { key: "paymentStatus", label: "Payment", render: (o) => <StatusBadge status={o.paymentStatus} /> },
            { key: "total", label: "Total", render: (o) => `$${o.total.toFixed(2)}` },
            { key: "createdAt", label: "Created", render: (o) => new Date(o.createdAt).toLocaleString() },
          ]}
          rows={orders}
          loading={loading}
          error={error}
          emptyMessage="No orders found for this branch."
        />
      ) : (
        <p className="text-sm text-gold-600">Enter a Branch Id above to load orders.</p>
      )}
    </div>
  );
}
