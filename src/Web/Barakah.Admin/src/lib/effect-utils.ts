/**
 * Runs `fn` on the next microtask instead of synchronously.
 *
 * List pages in this app fetch data in a `useEffect` that resets loading/error state and then
 * calls a service. Calling setState synchronously as the first thing an effect does can trigger
 * an extra synchronous render, so we push the whole load routine (including its `setLoading(true)`
 * kick-off) to a microtask — same-tick from the user's perspective, but no longer synchronous
 * within the effect's own call frame.
 */
export function runDeferred(fn: () => void): void {
  queueMicrotask(fn);
}
