"use client";

import { useCallback, useEffect, useState } from "react";
import { useAuth } from "@/context/AuthContext";
import { DataTable } from "@/components/DataTable";
import { StatusBadge } from "@/components/StatusBadge";
import { notificationApi, ApiError } from "@/lib/api-client";
import { runDeferred } from "@/lib/effect-utils";
import type { Notification } from "@/lib/types";

export default function NotificationsPage() {
  const { selectedTenant } = useAuth();
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadNotifications = useCallback(() => {
    if (!selectedTenant) return;
    runDeferred(() => {
      setLoading(true);
      setError(null);
      notificationApi
        .list(selectedTenant)
        .then((items) => setNotifications(items as Notification[]))
        .catch((err) =>
          setError(err instanceof ApiError ? err.message : "Failed to load — is the Notification service running?"),
        )
        .finally(() => setLoading(false));
    });
  }, [selectedTenant]);

  useEffect(() => {
    loadNotifications();
  }, [loadNotifications]);

  if (!selectedTenant) {
    return (
      <div>
        <h1 className="page-title mb-2">Notifications</h1>
        <p className="text-ink-600">Pick an active tenant in the sidebar to view notification history.</p>
      </div>
    );
  }

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h1 className="page-title">Notifications</h1>
        <button
          type="button"
          onClick={loadNotifications}
          className="btn-primary"
        >
          Refresh
        </button>
      </div>
      <p className="mb-4 text-sm text-ink-500">
        History is populated automatically when Order Service publishes an order-created event and
        the Notification Service consumes it from Kafka (simulated send — no real email/SMS
        provider is called).
      </p>
      <DataTable<Notification>
        columns={[
          { key: "channel", label: "Channel" },
          { key: "recipient", label: "Recipient" },
          { key: "message", label: "Message" },
          { key: "status", label: "Status", render: (n) => <StatusBadge status={n.status} /> },
          { key: "orderId", label: "Order Id", render: (n) => n.orderId ?? "—" },
          { key: "createdAt", label: "Created", render: (n) => new Date(n.createdAt).toLocaleString() },
        ]}
        rows={notifications}
        loading={loading}
        error={error}
        emptyMessage="No notifications yet for this tenant."
      />
    </div>
  );
}
