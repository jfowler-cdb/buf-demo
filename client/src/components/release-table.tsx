import type { Release } from "@/gen/cdbaby/demo/v1beta1/releases_pb";

function formatDate(release: Release): string {
  if (!release.releaseDate) return "—";
  return new Date(Number(release.releaseDate.seconds) * 1000).toLocaleDateString();
}

export function ReleaseTable({
  releases,
  onEdit,
  onDelete,
}: {
  releases: Release[];
  onEdit: (release: Release) => void;
  onDelete: (id: string) => void;
}) {
  return (
    <table className="w-full text-left text-sm">
      <thead>
        <tr className="border-b border-zinc-200 dark:border-zinc-800">
          <th className="pb-2 font-medium text-zinc-500 dark:text-zinc-400">Title</th>
          <th className="pb-2 font-medium text-zinc-500 dark:text-zinc-400">Artist</th>
          <th className="pb-2 font-medium text-zinc-500 dark:text-zinc-400">Label</th>
          <th className="pb-2 font-medium text-zinc-500 dark:text-zinc-400">Release Date</th>
          <th className="pb-2 font-medium text-zinc-500 dark:text-zinc-400"></th>
        </tr>
      </thead>
      <tbody>
        {releases.map((r) => (
          <tr key={r.id} className="border-b border-zinc-100 dark:border-zinc-800/50">
            <td className="py-3 text-zinc-900 dark:text-zinc-100">{r.title}</td>
            <td className="py-3 text-zinc-700 dark:text-zinc-300">{r.artist}</td>
            <td className="py-3 text-zinc-700 dark:text-zinc-300">{r.label || "—"}</td>
            <td className="py-3 text-zinc-700 dark:text-zinc-300">{formatDate(r)}</td>
            <td className="py-3 text-right">
              <button
                onClick={() => onEdit(r)}
                className="mr-2 text-zinc-500 hover:text-zinc-900 dark:hover:text-zinc-100"
              >
                Edit
              </button>
              <button
                onClick={() => onDelete(r.id)}
                className="text-red-500 hover:text-red-700 dark:hover:text-red-400"
              >
                Delete
              </button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
