const STATUS_STYLES: Record<string, { badge: string; dot: string }> = {
  // Order statuses
  confirmed: { badge: "bg-blue-50 text-blue-700", dot: "bg-blue-500" },
  pending: { badge: "bg-amber-50 text-amber-700", dot: "bg-amber-500" },
  processing: { badge: "bg-blue-50 text-blue-700", dot: "bg-blue-500" },
  shipped: { badge: "bg-indigo-50 text-indigo-700", dot: "bg-indigo-500" },
  delivered: { badge: "bg-brand-50 text-brand-700", dot: "bg-brand-500" },
  sent: { badge: "bg-brand-50 text-brand-700", dot: "bg-brand-500" },
  completed: { badge: "bg-brand-50 text-brand-700", dot: "bg-brand-500" },
  cancelled: { badge: "bg-red-50 text-red-700", dot: "bg-red-500" },
  canceled: { badge: "bg-red-50 text-red-700", dot: "bg-red-500" },
  failed: { badge: "bg-red-50 text-red-700", dot: "bg-red-500" },
  refunded: { badge: "bg-ink-100 text-ink-700", dot: "bg-ink-500" },
  paid: { badge: "bg-brand-50 text-brand-700", dot: "bg-brand-500" },
  // Tenant statuses
  active: { badge: "bg-brand-50 text-brand-700", dot: "bg-brand-500" },
  suspended: { badge: "bg-red-50 text-red-700", dot: "bg-red-500" },
};

const DEFAULT_STYLE = { badge: "bg-ink-100 text-ink-600", dot: "bg-ink-400" };

export function StatusBadge({ status }: { status: string }) {
  const style = STATUS_STYLES[status.toLowerCase()] ?? DEFAULT_STYLE;
  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium capitalize ${style.badge}`}>
      <span className={`h-1.5 w-1.5 rounded-full ${style.dot}`} />
      {status}
    </span>
  );
}
