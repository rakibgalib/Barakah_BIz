"use client";

import { useState } from "react";
import type { FormEvent } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthContext";
import { AuthApiError } from "@/lib/auth";

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export default function LoginPage() {
  const router = useRouter();
  const { login } = useAuth();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);
  const [apiError, setApiError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setApiError(null);

    if (!email.trim() || !password) {
      setValidationError("Email and password are required.");
      return;
    }
    if (!EMAIL_PATTERN.test(email.trim())) {
      setValidationError("Enter a valid email address.");
      return;
    }
    setValidationError(null);

    setSubmitting(true);
    try {
      await login(email.trim(), password);
      router.push("/");
    } catch (err) {
      if (err instanceof AuthApiError) {
        setApiError(err.message);
      } else {
        setApiError("Failed to reach the Identity service — is it running on localhost:5001?");
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="relative flex min-h-screen w-full items-center justify-center overflow-hidden bg-brand-950 px-4">
      <div
        className="pointer-events-none absolute inset-0 opacity-70"
        style={{
          background:
            "radial-gradient(600px circle at 15% 20%, rgba(38,133,79,0.35), transparent 60%), radial-gradient(500px circle at 85% 80%, rgba(196,154,62,0.18), transparent 55%)",
        }}
      />

      <div className="relative w-full max-w-sm rounded-xl border border-brand-800 bg-brand-900/60 p-8 shadow-2xl backdrop-blur-sm">
        <div className="mb-6 flex items-center gap-3">
          <span className="flex h-10 w-10 items-center justify-center rounded-lg bg-gold-400 text-base font-bold text-brand-950">
            B
          </span>
          <div>
            <h1 className="text-lg font-semibold text-white">Barakah Admin</h1>
            <p className="text-xs text-brand-300">Sign in to manage the platform</p>
          </div>
        </div>

        <form onSubmit={handleSubmit} noValidate className="space-y-4">
          <div>
            <label htmlFor="email" className="mb-1.5 block text-sm font-medium text-brand-100">
              Email
            </label>
            <input
              id="email"
              type="email"
              autoComplete="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full rounded-md border border-brand-700 bg-brand-950/60 px-3 py-2 text-sm text-white placeholder:text-brand-400 focus:border-gold-400 focus:outline-none focus:ring-2 focus:ring-gold-400/20"
            />
          </div>

          <div>
            <label htmlFor="password" className="mb-1.5 block text-sm font-medium text-brand-100">
              Password
            </label>
            <input
              id="password"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full rounded-md border border-brand-700 bg-brand-950/60 px-3 py-2 text-sm text-white placeholder:text-brand-400 focus:border-gold-400 focus:outline-none focus:ring-2 focus:ring-gold-400/20"
            />
          </div>

          {validationError && (
            <p className="rounded-md bg-red-950/40 px-3 py-2 text-sm text-red-300">{validationError}</p>
          )}
          {apiError && <p className="rounded-md bg-red-950/40 px-3 py-2 text-sm text-red-300">{apiError}</p>}

          <button
            type="submit"
            disabled={submitting}
            className="w-full rounded-md bg-gold-400 px-3 py-2.5 text-sm font-semibold text-brand-950 transition-colors hover:bg-gold-300 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {submitting ? "Signing in…" : "Sign in"}
          </button>
        </form>
      </div>
    </div>
  );
}
