"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/context/AuthContext";
import { analyticsApi, inventoryApi, orderApi, ApiError } from "@/lib/api-client";
import { runDeferred } from "@/lib/effect-utils";
import type { InventoryItem, Order, SalesSummary } from "@/lib/types";

interface CardState<T> {
  value: T | null;
  loading: boolean;
  error: string | null;
}

function OrdersIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" strokeWidth="1.75" stroke="currentColor" className="h-5 w-5">
      <path strokeLinecap="round" strokeLinejoin="round" d="M3 7h18M3 7l1.5 12a2 2 0 002 1.8h11a2 2 0 002-1.8L21 7M3 7l1.2-3.2A2 2 0 016.1 2.5h11.8a2 2 0 011.9 1.3L21 7M9 11v4M15 11v4" />
    </svg>
  );
}

function StockIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" strokeWidth="1.75" stroke="currentColor" className="h-5 w-5">
      <path strokeLinecap="round" strokeLinejoin="round" d="M12 3l8 4.5v9L12 21l-8-4.5v-9L12 3zM4.5 7.5L12 12l7.5-4.5M12 12v9" />
    </svg>
  );
}

function RevenueIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" strokeWidth="1.75" stroke="currentColor" className="h-5 w-5">
      <path strokeLinecap="round" strokeLinejoin="round" d="M12 3v18M17 7.5c0-1.9-2.2-3-5-3s-5 1.1-5 3 2.2 2.6 5 3 5 1.1 5 3-2.2 3-5 3-5-1.1-5-3" />
    </svg>
  );
}

export default function DashboardOverviewPage() {
  const { selectedTenant, selectedBranchId } = useAuth();

  const [lowStock, setLowStock] = useState<CardState<InventoryItem[]>>({ value: null, loading: false, error: null });
  const [orders, setOrders] = useState<CardState<Order[]>>({ value: null, loading: false, error: null });
  const [salesSummary, setSalesSummary] = useState<CardState<SalesSummary>>({ value: null, loading: false, error: null });

  useEffect(() => {
    if (!selectedTenant) return;

    runDeferred(() => {
      setLowStock({ value: null, loading: true, error: null });
      inventoryApi
        .listLowStock(selectedTenant)
        .then((items) => setLowStock({ value: items as InventoryItem[], loading: false, error: null }))
        .catch((err) =>
          setLowStock({
            value: null,
            loading: false,
            error: err instanceof ApiError ? err.message : "Failed to load — is the Inventory service running?",
          })
        );
    });
  }, [selectedTenant]);

  useEffect(() => {
    if (!selectedTenant || !selectedBranchId) return;

    runDeferred(() => {
      setOrders({ value: null, loading: true, error: null });
      orderApi
        .listByBranch(selectedBranchId, selectedTenant)
        .then((items) => setOrders({ value: items as Order[], loading: false, error: null }))
        .catch((err) =>
          setOrders({
            value: null,
            loading: false,
            error: err instanceof ApiError ? err.message : "Failed to load — is the Order service running?",
          })
        );
    });
  }, [selectedTenant, selectedBranchId]);

  useEffect(() => {
    if (!selectedTenant) return;

    runDeferred(() => {
      setSalesSummary({ value: null, loading: true, error: null });
      analyticsApi
        .salesSummary(selectedTenant)
        .then((summary) => setSalesSummary({ value: summary as SalesSummary, loading: false, error: null }))
        .catch((err) =>
          setSalesSummary({
            value: null,
            loading: false,
            error: err instanceof ApiError ? err.message : "Failed to load — is the Analytics service running?",
          })
        );
    });
  }, [selectedTenant]);

  if (!selectedTenant) {
    return (
      <div>
        <h1 className="page-title">Dashboard</h1>
        <p className="page-subtitle">Pick an active tenant in the sidebar to see order and inventory data for it.</p>
      </div>
    );
  }

  return (
    <div>
      <h1 className="page-title">Dashboard</h1>
      <p className="page-subtitle mb-6">Snapshot for the active tenant.</p>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <Link href="/orders" className="card transition-colors hover:border-brand-300">
          <div className="mb-3 flex items-center gap-2">
            <span className="flex h-8 w-8 items-center justify-center rounded-md bg-brand-50 text-brand-700">
              <OrdersIcon />
            </span>
            <p className="section-label">Recent orders</p>
          </div>
          {!selectedBranchId ? (
            <p className="text-sm text-gold-600">Set a Branch Id on the Orders page to see this count.</p>
          ) : orders.loading ? (
            <p className="text-3xl font-semibold tabular-nums text-ink-900">…</p>
          ) : orders.error ? (
            <p className="text-sm text-red-600">{orders.error}</p>
          ) : (
            <p className="text-3xl font-semibold tabular-nums text-ink-900">{orders.value?.length ?? 0}</p>
          )}
        </Link>

        <Link href="/inventory" className="card transition-colors hover:border-brand-300">
          <div className="mb-3 flex items-center gap-2">
            <span className="flex h-8 w-8 items-center justify-center rounded-md bg-brand-50 text-brand-700">
              <StockIcon />
            </span>
            <p className="section-label">Low stock items</p>
          </div>
          {lowStock.loading ? (
            <p className="text-3xl font-semibold tabular-nums text-ink-900">…</p>
          ) : lowStock.error ? (
            <p className="text-sm text-red-600">{lowStock.error}</p>
          ) : (
            <p className="text-3xl font-semibold tabular-nums text-ink-900">{lowStock.value?.length ?? 0}</p>
          )}
        </Link>

        <div className="card">
          <div className="mb-3 flex items-center gap-2">
            <span className="flex h-8 w-8 items-center justify-center rounded-md bg-gold-100 text-gold-700">
              <RevenueIcon />
            </span>
            <p className="section-label">Revenue (30d)</p>
          </div>
          {salesSummary.loading ? (
            <p className="text-3xl font-semibold tabular-nums text-ink-900">…</p>
          ) : salesSummary.error ? (
            <p className="text-sm text-red-600">{salesSummary.error}</p>
          ) : (
            <p className="text-3xl font-semibold tabular-nums text-ink-900">
              ${(salesSummary.value?.totalRevenue ?? 0).toFixed(2)}
            </p>
          )}
        </div>
      </div>

      <div className="card mt-6">
        <div className="mb-4 flex items-center justify-between">
          <p className="section-label">Sales summary — last 30 days</p>
          <button
            type="button"
            onClick={() => analyticsApi.downloadOrdersCsv(selectedTenant).catch(() => {})}
            className="btn-secondary text-xs"
          >
            Export orders CSV
          </button>
        </div>
        {salesSummary.loading ? (
          <p className="text-2xl font-semibold text-ink-900">…</p>
        ) : salesSummary.error ? (
          <p className="text-sm text-red-600">{salesSummary.error}</p>
        ) : (
          <div className="grid grid-cols-2 gap-6 sm:grid-cols-4">
            <div>
              <p className="text-xs text-ink-500">Total orders</p>
              <p className="mt-1 text-xl font-semibold tabular-nums text-ink-900">{salesSummary.value?.totalOrders ?? 0}</p>
            </div>
            <div>
              <p className="text-xs text-ink-500">Total revenue</p>
              <p className="mt-1 text-xl font-semibold tabular-nums text-ink-900">
                ${(salesSummary.value?.totalRevenue ?? 0).toFixed(2)}
              </p>
            </div>
            <div>
              <p className="text-xs text-ink-500">Avg order value</p>
              <p className="mt-1 text-xl font-semibold tabular-nums text-ink-900">
                ${(salesSummary.value?.avgOrderValue ?? 0).toFixed(2)}
              </p>
            </div>
            <div>
              <p className="text-xs text-ink-500">Payments received</p>
              <p className="mt-1 text-xl font-semibold tabular-nums text-ink-900">
                ${(salesSummary.value?.totalPaymentsReceived ?? 0).toFixed(2)}
              </p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
