"use client";

import { useState } from "react";
import { useAuth } from "@/context/AuthContext";
import { clothingApi, ApiError } from "@/lib/api-client";
import type { SustainabilityRating } from "@/lib/types";

export default function SustainabilityPage() {
  const { selectedTenant } = useAuth();

  const [productIdDraft, setProductIdDraft] = useState("");
  const [rating, setRating] = useState<SustainabilityRating | null>(null);
  const [notFoundFor, setNotFoundFor] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [score, setScore] = useState("80");
  const [certifications, setCertifications] = useState("");
  const [saving, setSaving] = useState(false);

  async function lookup() {
    const productId = productIdDraft.trim();
    if (!productId || !selectedTenant) return;
    setError(null);
    setNotFoundFor(null);
    try {
      const r = (await clothingApi.getSustainabilityRating(productId, selectedTenant)) as SustainabilityRating;
      setRating(r);
      setScore(String(r.score));
      setCertifications(r.certifications ?? "");
    } catch (err) {
      setRating(null);
      if (err instanceof ApiError && err.status === 404) {
        setNotFoundFor(productId);
      } else {
        setError(err instanceof ApiError ? err.message : "Failed to reach the Clothing service — is it running?");
      }
    }
  }

  async function save() {
    const productId = (rating?.productId ?? notFoundFor ?? productIdDraft.trim()) || null;
    if (!productId || !selectedTenant) return;
    const scoreNum = Number(score);
    if (Number.isNaN(scoreNum) || scoreNum < 0 || scoreNum > 100) {
      setError("Score must be between 0 and 100.");
      return;
    }
    setSaving(true);
    setError(null);
    try {
      const body = { score: scoreNum, certifications: certifications.trim() || null };
      const saved = rating
        ? ((await clothingApi.updateSustainabilityRating(productId, body, selectedTenant)) as SustainabilityRating)
        : ((await clothingApi.createSustainabilityRating({ productId, ...body }, selectedTenant)) as SustainabilityRating);
      setRating(saved);
      setNotFoundFor(null);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to reach the Clothing service — is it running?");
    } finally {
      setSaving(false);
    }
  }

  if (!selectedTenant) {
    return (
      <div>
        <h1 className="page-title">Sustainability</h1>
        <p className="page-subtitle">Pick an active tenant in the sidebar to manage sustainability ratings.</p>
      </div>
    );
  }

  return (
    <div>
      <h1 className="page-title mb-2">Sustainability</h1>
      <p className="page-subtitle mb-4">
        Clothing Extension&apos;s one non-AI feature — a sustainability score (0-100) and
        certifications per product.
      </p>

      <div className="mb-4 flex max-w-md gap-2">
        <input
          type="text"
          value={productIdDraft}
          onChange={(e) => setProductIdDraft(e.target.value)}
          placeholder="Product ID (Catalog)"
          className="input-field"
        />
        <button type="button" onClick={lookup} className="btn-primary shrink-0">
          Look up
        </button>
      </div>
      {error && <p className="mb-4 text-sm text-red-600">{error}</p>}
      {notFoundFor && <p className="mb-4 text-sm text-gold-600">No rating yet for this product — create one below.</p>}

      {(rating || notFoundFor) && (
        <div className="card max-w-md">
          <div className="mb-3">
            <label className="field-label">Score (0-100)</label>
            <input type="number" min="0" max="100" value={score} onChange={(e) => setScore(e.target.value)} className="input-field" />
          </div>
          <div className="mb-4">
            <label className="field-label">Certifications (comma-separated)</label>
            <input
              type="text"
              value={certifications}
              onChange={(e) => setCertifications(e.target.value)}
              placeholder="organic,recycled,fair-trade"
              className="input-field"
            />
          </div>
          <button type="button" onClick={save} disabled={saving} className="btn-primary w-full">
            {saving ? "Saving..." : rating ? "Update rating" : "Create rating"}
          </button>
        </div>
      )}
    </div>
  );
}
