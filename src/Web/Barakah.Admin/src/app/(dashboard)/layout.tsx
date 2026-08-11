"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthContext";
import { Sidebar } from "@/components/Sidebar";

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  const { user, loading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!loading && !user) {
      router.replace("/login");
    }
  }, [loading, user, router]);

  if (loading) {
    return (
      <div className="flex min-h-screen w-full items-center justify-center bg-ink-50 text-sm text-ink-400">
        Loading…
      </div>
    );
  }

  if (!user) {
    // Redirect is in-flight; render nothing to avoid a flash of dashboard content.
    return null;
  }

  return (
    <div className="flex min-h-screen w-full">
      <Sidebar />
      <main className="flex-1 overflow-y-auto bg-ink-50 p-8">{children}</main>
    </div>
  );
}
