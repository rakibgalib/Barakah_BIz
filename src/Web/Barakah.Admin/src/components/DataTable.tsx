import type { ReactNode } from "react";

export interface DataTableColumn<T> {
  key: string;
  label: string;
  render?: (row: T) => ReactNode;
  align?: "left" | "right";
}

export interface DataTableProps<T> {
  columns: DataTableColumn<T>[];
  rows: T[];
  loading: boolean;
  error: string | null;
  emptyMessage: string;
  /** Extracts a stable React key for a row. Defaults to the row's `id` field if present, else index. */
  rowKey?: (row: T, index: number) => string;
}

function defaultRowKey<T>(row: T, index: number): string {
  const maybeId = (row as { id?: unknown }).id;
  return typeof maybeId === "string" || typeof maybeId === "number" ? String(maybeId) : String(index);
}

export function DataTable<T>({ columns, rows, loading, error, emptyMessage, rowKey = defaultRowKey }: DataTableProps<T>) {
  return (
    <div className="overflow-hidden rounded-lg border border-ink-200 bg-white">
      <div className="overflow-x-auto">
        <table className="min-w-full text-sm">
          <thead className="border-b border-ink-200 bg-ink-50/60">
            <tr>
              {columns.map((col) => (
                <th
                  key={col.key}
                  className={`whitespace-nowrap px-4 py-2.5 text-[11px] font-semibold uppercase tracking-wider text-ink-500 ${
                    col.align === "right" ? "text-right" : "text-left"
                  }`}
                >
                  {col.label}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-ink-100">
            {loading && (
              <tr>
                <td colSpan={columns.length} className="px-4 py-10 text-center text-sm text-ink-400">
                  Loading…
                </td>
              </tr>
            )}

            {!loading && error && (
              <tr>
                <td colSpan={columns.length} className="px-4 py-10 text-center text-sm text-red-600">
                  {error}
                </td>
              </tr>
            )}

            {!loading && !error && rows.length === 0 && (
              <tr>
                <td colSpan={columns.length} className="px-4 py-10 text-center text-sm text-ink-400">
                  {emptyMessage}
                </td>
              </tr>
            )}

            {!loading &&
              !error &&
              rows.map((row, index) => (
                <tr key={rowKey(row, index)} className="transition-colors hover:bg-brand-50/60">
                  {columns.map((col) => (
                    <td
                      key={col.key}
                      className={`whitespace-nowrap px-4 py-2.5 text-ink-800 tabular-nums ${
                        col.align === "right" ? "text-right" : "text-left"
                      }`}
                    >
                      {col.render ? col.render(row) : String((row as Record<string, unknown>)[col.key] ?? "")}
                    </td>
                  ))}
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
