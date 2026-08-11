"use client";

import { useState } from "react";
import { useAuth } from "@/context/AuthContext";

export function TenantSwitcher() {
  const { selectedTenant, setSelectedTenant } = useAuth();
  const [draft, setDraft] = useState(selectedTenant ?? "");

  function apply() {
    const trimmed = draft.trim();
    setSelectedTenant(trimmed.length > 0 ? trimmed : null);
  }

  return (
    <div className="flex flex-col gap-1.5 border-t border-brand-800 px-4 py-3.5">
      <label htmlFor="tenant-subdomain" className="text-[10px] font-semibold uppercase tracking-wider text-brand-400/80">
        Active tenant
      </label>
      <div className="flex gap-1.5">
        <input
          id="tenant-subdomain"
          type="text"
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") apply();
          }}
          placeholder="subdomain"
          className="w-full min-w-0 rounded-md border border-brand-700 bg-brand-900 px-2.5 py-1.5 text-sm text-white placeholder:text-brand-400 focus:border-gold-400 focus:outline-none"
        />
        <button
          type="button"
          onClick={apply}
          className="shrink-0 rounded-md bg-brand-700 px-2.5 py-1.5 text-sm font-medium text-white transition-colors hover:bg-brand-600"
        >
          Set
        </button>
      </div>
      <p className="flex items-center gap-1.5 text-xs">
        <span className={`h-1.5 w-1.5 rounded-full ${selectedTenant ? "bg-gold-400" : "bg-brand-600"}`} />
        <span className={selectedTenant ? "text-brand-100" : "text-brand-400"}>
          {selectedTenant ? `Scoped to "${selectedTenant}"` : "No tenant selected"}
        </span>
      </p>
    </div>
  );
}
