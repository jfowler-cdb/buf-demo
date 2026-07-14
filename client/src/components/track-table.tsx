import type { Track } from "@/gen/cdbaby/demo/v1beta1/tracks_pb";

function formatDuration(track: Track): string {
  if (!track.duration) return "—";
  const totalSeconds = Number(track.duration.seconds);
  const m = Math.floor(totalSeconds / 60);
  const s = totalSeconds % 60;
  return `${m}:${String(s).padStart(2, "0")}`;
}

export function TrackTable({
  tracks,
  onEdit,
  onDelete,
}: {
  tracks: Track[];
  onEdit: (track: Track) => void;
  onDelete: (id: string) => void;
}) {
  return (
    <table className="w-full text-left text-sm">
      <thead>
        <tr className="border-b border-zinc-200 dark:border-zinc-800">
          <th className="pb-2 font-medium text-zinc-500 dark:text-zinc-400">#</th>
          <th className="pb-2 font-medium text-zinc-500 dark:text-zinc-400">Title</th>
          <th className="pb-2 font-medium text-zinc-500 dark:text-zinc-400">Artist</th>
          <th className="pb-2 font-medium text-zinc-500 dark:text-zinc-400">Duration</th>
          <th className="pb-2 font-medium text-zinc-500 dark:text-zinc-400">ISRC</th>
          <th className="pb-2 font-medium text-zinc-500 dark:text-zinc-400"></th>
        </tr>
      </thead>
      <tbody>
        {tracks.map((t) => (
          <tr key={t.id} className="border-b border-zinc-100 dark:border-zinc-800/50">
            <td className="py-3 text-zinc-500 dark:text-zinc-400">{t.trackNumber}</td>
            <td className="py-3 text-zinc-900 dark:text-zinc-100">{t.title}</td>
            <td className="py-3 text-zinc-700 dark:text-zinc-300">{t.artist}</td>
            <td className="py-3 text-zinc-700 dark:text-zinc-300">{formatDuration(t)}</td>
            <td className="py-3 text-zinc-500 dark:text-zinc-400 font-mono text-xs">{t.isrc || "—"}</td>
            <td className="py-3 text-right">
              <button onClick={() => onEdit(t)}
                className="mr-2 text-zinc-500 hover:text-zinc-900 dark:hover:text-zinc-100">
                Edit
              </button>
              <button onClick={() => onDelete(t.id)}
                className="text-red-500 hover:text-red-700 dark:hover:text-red-400">
                Delete
              </button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
