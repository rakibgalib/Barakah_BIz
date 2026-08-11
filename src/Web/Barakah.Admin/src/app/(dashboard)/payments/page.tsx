"use client";

import { Suspense, useCallback, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { useAuth } from "@/context/AuthContext";
import { DataTable } from "@/components/DataTable";
import { StatusBadge } from "@/components/StatusBadge";
import { paymentApi, ApiError } from "@/lib/api-client";
import { runDeferred } from "@/lib/effect-utils";
import type { Payment } from "@/lib/types";

function PaymentsContent() {
  const { selectedTenant } = useAuth();
  const searchParams = useSearchParams();
  const [orderIdDraft, setOrderIdDraft] = useState(searchParams.get("orderId") ?? "");
  const [orderId, setOrderId] = useState(searchParams.get("orderId") ?? "");

  const [payments, setPayments] = useState<Payment[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadPayments = useCallback(() => {
    if (!selectedTenant || !orderId) return;
    runDeferred(() => {
      setLoading(true);
      setError(null);
      paymentApi
        .listByOrder(orderId, selectedTenant)
        .then((items) => setPayments(items as Payment[]))
        .catch((err) => setError(err instanceof ApiError ? err.message : "Failed to load — is the Payment service running?"))
        .finally(() => setLoading(false));
    });
  }, [selectedTenant, orderId]);

  useEffect(() => {
    loadPayments();
  }, [loadPayments]);

  if (!selectedTenant) {
    return (
      <div>
        <h1 className="page-title mb-2">Payments</h1>
        <p className="text-ink-600">Pick an active tenant in the sidebar to view payments.</p>
      </div>
    );
  }

  return (
    <div>
      <h1 className="page-title mb-2">Payments</h1>
      <p className="mb-4 text-sm text-ink-500">
        Payment Service only lists payments by order (no tenant-wide list exists). Enter an Order
        Id, or open a payment link from an order&apos;s detail page.
      </p>
      <div className="mb-4 flex max-w-md gap-2">
        <input
          type="text"
          value={orderIdDraft}
          onChange={(e) => setOrderIdDraft(e.target.value)}
          placeholder="Order Id (guid)"
          className="input-field"
        />
        <button
          type="button"
          onClick={() => setOrderId(orderIdDraft.trim())}
          className="btn-primary shrink-0"
        >
          Load
        </button>
      </div>

      {orderId ? (
        <DataTable<Payment>
          columns={[
            { key: "id", label: "Payment Id" },
            { key: "amount", label: "Amount", render: (p) => `$${p.amount.toFixed(2)}` },
            { key: "method", label: "Method" },
            { key: "status", label: "Status", render: (p) => <StatusBadge status={p.status} /> },
            { key: "transactionReference", label: "Reference", render: (p) => p.transactionReference ?? "—" },
            { key: "createdAt", label: "Created", render: (p) => new Date(p.createdAt).toLocaleString() },
          ]}
          rows={payments}
          loading={loading}
          error={error}
          emptyMessage="No payments found for this order."
        />
      ) : (
        <p className="text-sm text-gold-600">Enter an Order Id above to load payments.</p>
      )}
    </div>
  );
}

export default function PaymentsPage() {
  return (
    <Suspense fallback={<p className="text-ink-500">Loading...</p>}>
      <PaymentsContent />
    </Suspense>
  );
}
