"use client";

import { useCallback, useState } from "react";
import type { FormEvent } from "react";
import { useAuth } from "@/context/AuthContext";
import { DataTable } from "@/components/DataTable";
import { StatusBadge } from "@/components/StatusBadge";
import { pharmacyApi, ApiError } from "@/lib/api-client";
import { runDeferred } from "@/lib/effect-utils";
import type { Prescription } from "@/lib/types";

const EMPTY_FORM = {
  patientId: "",
  patientName: "",
  productId: "",
  medicationName: "",
  quantity: "1",
  prescribedBy: "",
  refillsAllowed: "0",
  isControlledSubstance: false,
  expiresAt: "",
};

export default function PrescriptionsPage() {
  const { selectedTenant } = useAuth();

  const [patientIdDraft, setPatientIdDraft] = useState("");
  const [patientId, setPatientId] = useState("");
  const [prescriptions, setPrescriptions] = useState<Prescription[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [form, setForm] = useState(EMPTY_FORM);
  const [createError, setCreateError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  const loadPatient = useCallback(
    (id: string) => {
      if (!selectedTenant || !id) return;
      runDeferred(() => {
        setLoading(true);
        setError(null);
        pharmacyApi
          .listByPatient(id, selectedTenant)
          .then((items) => setPrescriptions(items as Prescription[]))
          .catch((err) => setError(err instanceof ApiError ? err.message : "Failed to load — is the Pharmacy service running?"))
          .finally(() => setLoading(false));
      });
    },
    [selectedTenant]
  );

  async function handleCreate(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!selectedTenant) return;

    const quantity = Number(form.quantity);
    const refillsAllowed = Number(form.refillsAllowed);
    if (!form.patientId.trim() || !form.patientName.trim() || !form.medicationName.trim() || !form.prescribedBy.trim() || !form.expiresAt) {
      setCreateError("All fields except product ID are required.");
      return;
    }
    setCreateError(null);
    setCreating(true);
    try {
      await pharmacyApi.create(
        {
          patientId: form.patientId.trim(),
          patientName: form.patientName.trim(),
          productId: form.productId.trim() || "00000000-0000-0000-0000-000000000000",
          medicationName: form.medicationName.trim(),
          quantity,
          prescribedBy: form.prescribedBy.trim(),
          refillsAllowed,
          isControlledSubstance: form.isControlledSubstance,
          expiresAt: new Date(form.expiresAt).toISOString(),
        },
        selectedTenant
      );
      setForm(EMPTY_FORM);
      setPatientId(form.patientId.trim());
      setPatientIdDraft(form.patientId.trim());
      loadPatient(form.patientId.trim());
    } catch (err) {
      setCreateError(err instanceof ApiError ? err.message : "Failed to reach the Pharmacy service — is it running?");
    } finally {
      setCreating(false);
    }
  }

  async function handleRefill(id: string) {
    if (!selectedTenant) return;
    setActionError(null);
    try {
      await pharmacyApi.refill(id, selectedTenant);
      loadPatient(patientId);
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : "Failed to reach the Pharmacy service — is it running?");
    }
  }

  if (!selectedTenant) {
    return (
      <div>
        <h1 className="page-title">Prescriptions</h1>
        <p className="page-subtitle">Pick an active tenant in the sidebar to manage prescriptions.</p>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div>
        <h1 className="page-title mb-2">Prescriptions</h1>
        <p className="page-subtitle mb-4">
          Pharmacy Service has no tenant-wide list — look up a patient&apos;s history by ID below.
        </p>

        <div className="mb-4 flex max-w-md gap-2">
          <input
            type="text"
            value={patientIdDraft}
            onChange={(e) => setPatientIdDraft(e.target.value)}
            placeholder="Patient ID"
            className="input-field"
          />
          <button
            type="button"
            onClick={() => {
              setPatientId(patientIdDraft.trim());
              loadPatient(patientIdDraft.trim());
            }}
            className="btn-primary shrink-0"
          >
            Load
          </button>
        </div>
        {actionError && <p className="mb-4 text-sm text-red-600">{actionError}</p>}

        {patientId ? (
          <DataTable<Prescription>
            columns={[
              { key: "medicationName", label: "Medication" },
              { key: "quantity", label: "Qty", align: "right" },
              { key: "status", label: "Status", render: (p) => <StatusBadge status={p.status} /> },
              { key: "refills", label: "Refills", render: (p) => `${p.refillsUsed}/${p.refillsAllowed}` },
              { key: "prescribedBy", label: "Prescribed by" },
              { key: "expiresAt", label: "Expires", render: (p) => new Date(p.expiresAt).toLocaleDateString() },
              {
                key: "actions",
                label: "",
                render: (p) =>
                  p.status === "Active" && p.refillsUsed < p.refillsAllowed ? (
                    <button type="button" onClick={() => handleRefill(p.id)} className="btn-secondary text-xs">
                      Refill
                    </button>
                  ) : null,
              },
            ]}
            rows={prescriptions}
            loading={loading}
            error={error}
            emptyMessage="No prescriptions found for this patient."
          />
        ) : (
          <p className="text-sm text-gold-600">Enter a patient ID above to load their prescription history.</p>
        )}
      </div>

      <div className="card max-w-md">
        <h2 className="mb-4 text-base font-semibold text-ink-900">Create prescription</h2>
        <form onSubmit={handleCreate} className="space-y-3">
          <div>
            <label className="field-label">Patient ID</label>
            <input type="text" value={form.patientId} onChange={(e) => setForm((f) => ({ ...f, patientId: e.target.value }))} className="input-field" />
          </div>
          <div>
            <label className="field-label">Patient name</label>
            <input type="text" value={form.patientName} onChange={(e) => setForm((f) => ({ ...f, patientName: e.target.value }))} className="input-field" />
          </div>
          <div>
            <label className="field-label">Medication name</label>
            <input type="text" value={form.medicationName} onChange={(e) => setForm((f) => ({ ...f, medicationName: e.target.value }))} className="input-field" />
          </div>
          <div>
            <label className="field-label">Product ID (Catalog, optional)</label>
            <input type="text" value={form.productId} onChange={(e) => setForm((f) => ({ ...f, productId: e.target.value }))} className="input-field" />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="field-label">Quantity</label>
              <input type="number" min="1" value={form.quantity} onChange={(e) => setForm((f) => ({ ...f, quantity: e.target.value }))} className="input-field" />
            </div>
            <div>
              <label className="field-label">Refills allowed</label>
              <input type="number" min="0" value={form.refillsAllowed} onChange={(e) => setForm((f) => ({ ...f, refillsAllowed: e.target.value }))} className="input-field" />
            </div>
          </div>
          <div>
            <label className="field-label">Prescribed by</label>
            <input type="text" value={form.prescribedBy} onChange={(e) => setForm((f) => ({ ...f, prescribedBy: e.target.value }))} className="input-field" />
          </div>
          <div>
            <label className="field-label">Expires</label>
            <input type="date" value={form.expiresAt} onChange={(e) => setForm((f) => ({ ...f, expiresAt: e.target.value }))} className="input-field" />
          </div>
          <label className="flex items-center gap-2 text-sm text-ink-700">
            <input
              type="checkbox"
              checked={form.isControlledSubstance}
              onChange={(e) => setForm((f) => ({ ...f, isControlledSubstance: e.target.checked }))}
            />
            Controlled substance
          </label>
          {createError && <p className="text-sm text-red-600">{createError}</p>}
          <button type="submit" disabled={creating} className="btn-primary w-full">
            {creating ? "Creating..." : "Create prescription"}
          </button>
        </form>
      </div>
    </div>
  );
}
