"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthContext";
import { TenantSwitcher } from "@/components/TenantSwitcher";

const NAV_SECTIONS: { label: string; links: { href: string; label: string }[] }[] = [
  {
    label: "Overview",
    links: [{ href: "/", label: "Dashboard" }],
  },
  {
    label: "Platform",
    links: [{ href: "/tenants", label: "Tenants" }],
  },
  {
    label: "Commerce",
    links: [
      { href: "/products", label: "Products" },
      { href: "/inventory", label: "Inventory" },
      { href: "/orders", label: "Orders" },
      { href: "/payments", label: "Payments" },
    ],
  },
  {
    label: "Activity",
    links: [{ href: "/notifications", label: "Notifications" }],
  },
  {
    label: "Business Extensions",
    links: [
      { href: "/prescriptions", label: "Prescriptions" },
      { href: "/menu-items", label: "Menu Items" },
      { href: "/supershop", label: "SuperShop" },
      { href: "/sustainability", label: "Sustainability" },
    ],
  },
];

function initialsFor(email: string): string {
  const local = email.split("@")[0] ?? email;
  const parts = local.split(/[._-]/).filter(Boolean);
  const letters = parts.length >= 2 ? parts[0][0] + parts[1][0] : local.slice(0, 2);
  return letters.toUpperCase();
}

export function Sidebar() {
  const pathname = usePathname();
  const router = useRouter();
  const { user, logout } = useAuth();

  async function handleLogout() {
    await logout();
    router.push("/login");
  }

  return (
    <aside className="flex h-full w-64 shrink-0 flex-col bg-brand-950 text-ink-100">
      <div className="flex items-center gap-2.5 px-5 py-5">
        <span className="flex h-8 w-8 items-center justify-center rounded-md bg-gold-400 text-sm font-bold text-brand-950">
          B
        </span>
        <div className="leading-tight">
          <p className="text-[15px] font-semibold tracking-tight text-white">Barakah</p>
          <p className="text-[11px] font-medium uppercase tracking-wider text-brand-300">Admin</p>
        </div>
      </div>

      <nav className="flex-1 space-y-5 overflow-y-auto px-3 pb-4">
        {NAV_SECTIONS.map((section) => (
          <div key={section.label}>
            <p className="mb-1.5 px-3 text-[10px] font-semibold uppercase tracking-wider text-brand-400/80">
              {section.label}
            </p>
            <div className="space-y-0.5">
              {section.links.map((link) => {
                const isActive = link.href === "/" ? pathname === "/" : pathname.startsWith(link.href);
                return (
                  <Link
                    key={link.href}
                    href={link.href}
                    className={`relative block rounded-md px-3 py-1.5 text-sm font-medium transition-colors ${
                      isActive
                        ? "bg-brand-900 text-white before:absolute before:left-0 before:top-1/2 before:h-4 before:w-0.5 before:-translate-y-1/2 before:rounded-full before:bg-gold-400 before:content-['']"
                        : "text-brand-200 hover:bg-brand-900/60 hover:text-white"
                    }`}
                  >
                    {link.label}
                  </Link>
                );
              })}
            </div>
          </div>
        ))}
      </nav>

      <TenantSwitcher />

      <div className="flex items-center gap-2.5 border-t border-brand-800 px-4 py-3.5">
        <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-brand-800 text-[11px] font-semibold text-brand-100">
          {user ? initialsFor(user.email) : "?"}
        </span>
        <p className="min-w-0 flex-1 truncate text-xs text-brand-200" title={user?.email ?? ""}>
          {user?.email ?? "Not signed in"}
        </p>
        <button
          type="button"
          onClick={handleLogout}
          className="shrink-0 rounded-md px-2 py-1 text-xs font-medium text-brand-300 transition-colors hover:bg-brand-900 hover:text-white"
        >
          Log out
        </button>
      </div>
    </aside>
  );
}
