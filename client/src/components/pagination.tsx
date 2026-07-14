export function Pagination({
  page,
  hasPrev,
  hasNext,
  onPrev,
  onNext,
}: {
  page: number;
  hasPrev: boolean;
  hasNext: boolean;
  onPrev: () => void;
  onNext: () => void;
}) {
  return (
    <div className="mt-6 flex items-center justify-between">
      <button
        onClick={onPrev}
        disabled={!hasPrev}
        className="rounded border border-zinc-300 px-4 py-2 text-sm font-medium text-zinc-700 hover:bg-zinc-50 disabled:opacity-30 dark:border-zinc-700 dark:text-zinc-300 dark:hover:bg-zinc-800"
      >
        Previous
      </button>
      <span className="text-sm text-zinc-500 dark:text-zinc-400">
        Page {page}
      </span>
      <button
        onClick={onNext}
        disabled={!hasNext}
        className="rounded border border-zinc-300 px-4 py-2 text-sm font-medium text-zinc-700 hover:bg-zinc-50 disabled:opacity-30 dark:border-zinc-700 dark:text-zinc-300 dark:hover:bg-zinc-800"
      >
        Next
      </button>
    </div>
  );
}
