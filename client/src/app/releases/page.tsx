"use client";

import { useState, useCallback, useEffect } from "react";
import { releaseClient } from "@/lib/client";
import { releaseToFormData, formDataToRelease, type ReleaseFormData } from "@/lib/validation";
import type { Release } from "@/gen/cdbaby/demo/v1beta1/releases_pb";
import { ConnectError } from "@connectrpc/connect";
import { ReleaseForm } from "@/components/release-form";
import { ReleaseTable } from "@/components/release-table";
import { Pagination } from "@/components/pagination";

const PAGE_SIZE = 10;

export default function ReleasesPage() {
  const [releases, setReleases] = useState<Release[]>([]);
  const [loaded, setLoaded] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [mode, setMode] = useState<"list" | "create" | "edit">("list");
  const [editingRelease, setEditingRelease] = useState<Release | undefined>();
  const [pageToken, setPageToken] = useState("");
  const [nextPageToken, setNextPageToken] = useState("");
  const [pageHistory, setPageHistory] = useState<string[]>([]);

  const loadReleases = useCallback(async (token: string = "") => {
    try {
      setError(null);
      const res = await releaseClient.listReleases({ pageSize: PAGE_SIZE, pageToken: token });
      setReleases(res.releases);
      setNextPageToken(res.nextPageToken);
      setPageToken(token);
      setLoaded(true);
    } catch (err) {
      setError(err instanceof ConnectError ? err.message : "Failed to load releases");
      setLoaded(true);
    }
  }, []);

  useEffect(() => {
    loadReleases();
  }, [loadReleases]);

  const goNext = () => {
    setPageHistory((prev) => [...prev, pageToken]);
    loadReleases(nextPageToken);
  };

  const goPrev = () => {
    const prev = [...pageHistory];
    const prevToken = prev.pop() ?? "";
    setPageHistory(prev);
    loadReleases(prevToken);
  };

  const handleCreate = async (data: ReleaseFormData) => {
    await releaseClient.createRelease({ release: formDataToRelease(data) });
    setMode("list");
    await loadReleases(pageToken);
  };

  const handleUpdate = async (data: ReleaseFormData) => {
    if (!editingRelease) return;
    await releaseClient.updateRelease({
      release: formDataToRelease(data, editingRelease.id),
    });
    setMode("list");
    setEditingRelease(undefined);
    await loadReleases(pageToken);
  };

  const handleDelete = async (id: string) => {
    try {
      await releaseClient.deleteRelease({ id });
      await loadReleases(pageToken);
    } catch (err) {
      setError(err instanceof ConnectError ? err.message : "Failed to delete");
    }
  };

  return (
    <div className="mx-auto w-full max-w-3xl px-6 py-12">
      <div className="mb-8 flex items-center justify-between">
        <h1 className="text-2xl font-bold text-zinc-900 dark:text-zinc-50">Releases</h1>
        {mode === "list" && (
          <button
            onClick={() => setMode("create")}
            className="rounded bg-zinc-900 px-4 py-2 text-sm font-medium text-white hover:bg-zinc-700 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-300"
          >
            + New Release
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
          <h2 className="mb-4 text-lg font-semibold text-zinc-900 dark:text-zinc-50">New Release</h2>
          <ReleaseForm
            initial={releaseToFormData()}
            onSubmit={handleCreate}
            onCancel={() => setMode("list")}
            submitLabel="Create"
          />
        </div>
      )}

      {mode === "edit" && editingRelease && (
        <div className="mb-8 rounded border border-zinc-200 p-6 dark:border-zinc-800">
          <h2 className="mb-4 text-lg font-semibold text-zinc-900 dark:text-zinc-50">Edit Release</h2>
          <ReleaseForm
            initial={releaseToFormData(editingRelease)}
            onSubmit={handleUpdate}
            onCancel={() => {
              setMode("list");
              setEditingRelease(undefined);
            }}
            submitLabel="Update"
          />
        </div>
      )}

      {loaded && releases.length === 0 && mode === "list" && (
        <p className="text-zinc-500 dark:text-zinc-400">No releases yet. Create one to get started.</p>
      )}

      {releases.length > 0 && (
        <ReleaseTable
          releases={releases}
          onEdit={(r) => {
            setEditingRelease(r);
            setMode("edit");
          }}
          onDelete={handleDelete}
        />
      )}

      {loaded && (pageHistory.length > 0 || nextPageToken) && (
        <Pagination
          page={pageHistory.length + 1}
          hasPrev={pageHistory.length > 0}
          hasNext={!!nextPageToken}
          onPrev={goPrev}
          onNext={goNext}
        />
      )}
    </div>
  );
}
