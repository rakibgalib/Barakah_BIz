"use client";

import { use, useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/context/AuthContext";
import { StatusBadge } from "@/components/StatusBadge";
import { orderApi, ApiError } from "@/lib/api-client";
import { runDeferred } from "@/lib/effect-utils";
import type { Order } from "@/lib/types";

const STATUS_OPTIONS = ["confirmed", "processing", "shipped", "delivered", "cancelled"];

export default function OrderDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const { selectedTenant } = useAuth();

  const [order, setOrder] = useState<Order | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [nextStatus, setNextStatus] = useState("");
  const [statusBusy, setStatusBusy] = useState(false);
  const [statusError, setStatusError] = useState<string | null>(null);

  const loadOrder = useCallback(() => {
    if (!selectedTenant) return;
    runDeferred(() => {
      setLoading(true);
      setError(null);
      orderApi
        .getById(id, selectedTenant)
        .then((o) => {
          const typed = o as Order;
          setOrder(typed);
          setNextStatus(typed.status);
        })
        .catch((err) => setError(err instanceof ApiError ? err.message : "Failed to load — is the Order service running?"))
        .finally(() => setLoading(false));
    });
  }, [selectedTenant, id]);

  useEffect(() => {
    loadOrder();
  }, [loadOrder]);

  async function updateStatus() {
    if (!selectedTenant || !order || nextStatus === order.status) return;
    setStatusBusy(true);
    setStatusError(null);
    try {
      const updated = (await orderApi.updateStatus(order.id, { status: nextStatus }, selectedTenant)) as Order;
      setOrder(updated);
    } catch (err) {
      setStatusError(err instanceof ApiError ? err.message : "Failed to update status.");
    } finally {
      setStatusBusy(false);
    }
  }

  if (!selectedTenant) {
    return <p className="text-ink-600">Pick an active tenant in the sidebar to view this order.</p>;
  }

  if (loading) {
    return <p className="text-ink-500">Loading...</p>;
  }

  if (error) {
    return <p className="text-red-600">{error}</p>;
  }

  if (!order) {
    return <p className="text-ink-500">Order not found.</p>;
  }

  return (
    <div className="space-y-6">
      <div>
        <Link href="/orders" className="text-sm text-blue-700 hover:underline">
          &larr; Back to orders
        </Link>
        <h1 className="page-title mt-2">{order.orderNumber}</h1>
        <div className="mt-1 flex items-center gap-2">
          <StatusBadge status={order.status} />
          <StatusBadge status={order.paymentStatus} />
        </div>
      </div>

      <div className="card grid grid-cols-2 gap-4 sm:grid-cols-4">
        <div>
          <p className="text-xs text-ink-500">Branch</p>
          <p className="text-sm text-ink-900">{order.branchId}</p>
        </div>
        <div>
          <p className="text-xs text-ink-500">Customer</p>
          <p className="text-sm text-ink-900">{order.customerId ?? "—"}</p>
        </div>
        <div>
          <p className="text-xs text-ink-500">Payment method</p>
          <p className="text-sm text-ink-900">{order.paymentMethod ?? "—"}</p>
        </div>
        <div>
          <p className="text-xs text-ink-500">Created</p>
          <p className="text-sm text-ink-900">{new Date(order.createdAt).toLocaleString()}</p>
        </div>
      </div>

      <div className="card">
        <h2 className="mb-3 text-base font-semibold text-ink-900">Line items</h2>
        <table className="min-w-full divide-y divide-ink-200 text-sm">
          <thead>
            <tr>
              <th className="px-3 py-2 text-left font-medium text-ink-600">Product</th>
              <th className="px-3 py-2 text-left font-medium text-ink-600">Qty</th>
              <th className="px-3 py-2 text-left font-medium text-ink-600">Unit price</th>
              <th className="px-3 py-2 text-left font-medium text-ink-600">Discount</th>
              <th className="px-3 py-2 text-left font-medium text-ink-600">Total</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-ink-100">
            {order.items.map((item) => (
              <tr key={item.productId}>
                <td className="px-3 py-2">{item.productId}</td>
                <td className="px-3 py-2">{item.quantity}</td>
                <td className="px-3 py-2">${item.unitPrice.toFixed(2)}</td>
                <td className="px-3 py-2">${item.discount.toFixed(2)}</td>
                <td className="px-3 py-2">${item.totalPrice.toFixed(2)}</td>
              </tr>
            ))}
          </tbody>
        </table>
        <div className="mt-4 flex justify-end">
          <div className="w-56 space-y-1 text-sm">
            <div className="flex justify-between">
              <span className="text-ink-500">Subtotal</span>
              <span>${order.subtotal.toFixed(2)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-ink-500">Discount</span>
              <span>${order.discount.toFixed(2)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-ink-500">Tax</span>
              <span>${order.tax.toFixed(2)}</span>
            </div>
            <div className="flex justify-between font-semibold">
              <span>Total</span>
              <span>${order.total.toFixed(2)}</span>
            </div>
          </div>
        </div>
      </div>

      <div className="card max-w-sm">
        <h2 className="mb-3 text-base font-semibold text-ink-900">Update status</h2>
        <div className="flex gap-2">
          <select
            value={nextStatus}
            onChange={(e) => setNextStatus(e.target.value)}
            className="input-field"
          >
            {STATUS_OPTIONS.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
          <button
            type="button"
            onClick={updateStatus}
            disabled={statusBusy || nextStatus === order.status}
            className="btn-primary shrink-0"
          >
            {statusBusy ? "Saving..." : "Update"}
          </button>
        </div>
        {statusError && <p className="mt-2 text-sm text-red-600">{statusError}</p>}
      </div>

      <div>
        <Link href={`/payments?orderId=${order.id}`} className="text-sm text-blue-700 hover:underline">
          View payments for this order &rarr;
        </Link>
      </div>
    </div>
  );
}
