"use client";

import { useState, useCallback, useEffect } from "react";
import { trackClient, releaseClient } from "@/lib/client";
import type { Track } from "@/gen/cdbaby/demo/v1beta1/tracks_pb";
import { ConnectError } from "@connectrpc/connect";
import { TrackForm, trackToFormData, formDataToTrack, type TrackFormData } from "@/components/track-form";
import { TrackTable } from "@/components/track-table";
import { Pagination } from "@/components/pagination";
import { TrackSchema } from "@/gen/cdbaby/demo/v1beta1/tracks_pb";
import { create } from "@bufbuild/protobuf";

const PAGE_SIZE = 20;

export default function TracksPage() {
  const [tracks, setTracks] = useState<Track[]>([]);
  const [loaded, setLoaded] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [mode, setMode] = useState<"list" | "create" | "edit">("list");
  const [editingTrack, setEditingTrack] = useState<Track | undefined>();
  const [pageToken, setPageToken] = useState("");
  const [nextPageToken, setNextPageToken] = useState("");
  const [pageHistory, setPageHistory] = useState<string[]>([]);
  const [releases, setReleases] = useState<{ id: string; title: string }[]>([]);

  const loadTracks = useCallback(async (token: string = "") => {
    try {
      setError(null);
      const res = await trackClient.listTracks({ pageSize: PAGE_SIZE, pageToken: token });
      setTracks(res.tracks);
      setNextPageToken(res.nextPageToken);
      setPageToken(token);
      setLoaded(true);
    } catch (err) {
      setError(err instanceof ConnectError ? err.message : "Failed to load tracks");
      setLoaded(true);
    }
  }, []);

  const loadReleases = useCallback(async () => {
    try {
      const res = await releaseClient.listReleases({ pageSize: 100 });
      setReleases(res.releases.map((r) => ({ id: r.id, title: r.title })));
    } catch {
      // Non-critical — form will just not show release picker
    }
  }, []);

  useEffect(() => {
    loadTracks();
    loadReleases();
  }, [loadTracks, loadReleases]);

  const goNext = () => {
    setPageHistory((prev) => [...prev, pageToken]);
    loadTracks(nextPageToken);
  };

  const goPrev = () => {
    const prev = [...pageHistory];
    const prevToken = prev.pop() ?? "";
    setPageHistory(prev);
    loadTracks(prevToken);
  };

  const handleCreate = async (data: TrackFormData) => {
    const track = formDataToTrack(data);
    await trackClient.createTrack({ track });
    setMode("list");
    await loadTracks(pageToken);
  };

  const handleUpdate = async (data: TrackFormData) => {
    if (!editingTrack) return;
    const track = formDataToTrack(data, editingTrack.id);
    await trackClient.updateTrack({ track });
    setMode("list");
    setEditingTrack(undefined);
    await loadTracks(pageToken);
  };

  const handleDelete = async (id: string) => {
    try {
      await trackClient.deleteTrack({ id });
      await loadTracks(pageToken);
    } catch (err) {
      setError(err instanceof ConnectError ? err.message : "Failed to delete");
    }
  };

  return (
    <div className="mx-auto w-full max-w-3xl px-6 py-12">
      <div className="mb-8 flex items-center justify-between">
        <h1 className="text-2xl font-bold text-zinc-900 dark:text-zinc-50">Tracks</h1>
        {mode === "list" && (
          <button onClick={() => setMode("create")}
            className="rounded bg-zinc-900 px-4 py-2 text-sm font-medium text-white hover:bg-zinc-700 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-300">
            + New Track
          </button>
        )}
      </div>

      {error && (
        <div className="mb-4 rounded bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-400">
          {error}
        </div>
      )}

      {mode === "create" && (
        <div className="mb-8 rounded border border-zinc-200 p-6 dark:border-zinc-800">
          <h2 className="mb-4 text-lg font-semibold text-zinc-900 dark:text-zinc-50">New Track</h2>
          <TrackForm initial={trackToFormData()} onSubmit={handleCreate} onCancel={() => setMode("list")}
            submitLabel="Create" availableReleases={releases} />
        </div>
      )}

      {mode === "edit" && editingTrack && (
        <div className="mb-8 rounded border border-zinc-200 p-6 dark:border-zinc-800">
          <h2 className="mb-4 text-lg font-semibold text-zinc-900 dark:text-zinc-50">Edit Track</h2>
          <TrackForm initial={trackToFormData(editingTrack)} onSubmit={handleUpdate}
            onCancel={() => { setMode("list"); setEditingTrack(undefined); }}
            submitLabel="Update" availableReleases={releases} />
        </div>
      )}

      {loaded && tracks.length === 0 && mode === "list" && (
        <p className="text-zinc-500 dark:text-zinc-400">No tracks yet. Create one to get started.</p>
      )}

      {tracks.length > 0 && (
        <TrackTable tracks={tracks} onEdit={(t) => { setEditingTrack(t); setMode("edit"); }} onDelete={handleDelete} />
      )}

      {loaded && (pageHistory.length > 0 || nextPageToken) && (
        <Pagination page={pageHistory.length + 1} hasPrev={pageHistory.length > 0}
          hasNext={!!nextPageToken} onPrev={goPrev} onNext={goNext} />
      )}
    </div>
  );
}
