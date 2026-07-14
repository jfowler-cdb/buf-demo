"use client";

import { useState } from "react";
import { ConnectError } from "@connectrpc/connect";
import type { Track } from "@/gen/cdbaby/demo/v1beta1/tracks_pb";
import { create } from "@bufbuild/protobuf";
import {
  TrackSchema,
  CreateTrackRequestSchema,
} from "@/gen/cdbaby/demo/v1beta1/tracks_pb";
import { createValidator } from "@bufbuild/protovalidate";

const validator = createValidator();

export type TrackFormData = {
  title: string;
  artist: string;
  durationMinutes: string;
  durationSeconds: string;
  trackNumber: string;
  isrc: string;
  releaseIds: string[];
};

export function trackToFormData(track?: Track): TrackFormData {
  const totalSeconds = track?.duration
    ? Number(track.duration.seconds)
    : 0;
  return {
    title: track?.title ?? "",
    artist: track?.artist ?? "",
    durationMinutes: String(Math.floor(totalSeconds / 60)),
    durationSeconds: String(totalSeconds % 60).padStart(2, "0"),
    trackNumber: track?.trackNumber ? String(track.trackNumber) : "1",
    isrc: track?.isrc ?? "",
    releaseIds: track?.releaseIds ? [...track.releaseIds] : [],
  };
}

function formDataToTrack(data: TrackFormData, id?: string): Track {
  const minutes = parseInt(data.durationMinutes) || 0;
  const seconds = parseInt(data.durationSeconds) || 0;
  return create(TrackSchema, {
    id: id ?? "",
    title: data.title,
    artist: data.artist,
    duration: { seconds: BigInt(minutes * 60 + seconds), nanos: 0 },
    trackNumber: parseInt(data.trackNumber) || 1,
    isrc: data.isrc,
    releaseIds: data.releaseIds,
  });
}

export function validateTrack(data: TrackFormData): string[] {
  const track = formDataToTrack(data);
  const request = create(CreateTrackRequestSchema, { track });
  const result = validator.validate(CreateTrackRequestSchema, request);
  if (result.kind === "valid") return [];
  return (result.violations ?? []).map(
    (v) => `${v.field?.toString() ?? "unknown"}: ${v.message}`
  );
}

export function TrackForm({
  initial,
  onSubmit,
  onCancel,
  submitLabel,
  availableReleases,
}: {
  initial: TrackFormData;
  onSubmit: (data: TrackFormData) => Promise<void>;
  onCancel: () => void;
  submitLabel: string;
  availableReleases: { id: string; title: string }[];
}) {
  const [form, setForm] = useState(initial);
  const [errors, setErrors] = useState<string[]>([]);
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const validationErrors = validateTrack(form);
    if (validationErrors.length > 0) {
      setErrors(validationErrors);
      return;
    }
    setErrors([]);
    setSubmitting(true);
    try {
      await onSubmit(form);
    } catch (err) {
      const message =
        err instanceof ConnectError ? err.message : "An error occurred";
      setErrors([message]);
    } finally {
      setSubmitting(false);
    }
  };

  const toggleRelease = (id: string) => {
    setForm((prev) => ({
      ...prev,
      releaseIds: prev.releaseIds.includes(id)
        ? prev.releaseIds.filter((r) => r !== id)
        : [...prev.releaseIds, id],
    }));
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {errors.length > 0 && (
        <div className="rounded bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-400">
          {errors.map((e, i) => (
            <p key={i}>{e}</p>
          ))}
        </div>
      )}
      <div className="grid grid-cols-2 gap-4">
        <label className="block">
          <span className="text-sm font-medium text-zinc-700 dark:text-zinc-300">Title *</span>
          <input type="text" value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })}
            className="mt-1 block w-full rounded border border-zinc-300 px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-800" />
        </label>
        <label className="block">
          <span className="text-sm font-medium text-zinc-700 dark:text-zinc-300">Artist *</span>
          <input type="text" value={form.artist} onChange={(e) => setForm({ ...form, artist: e.target.value })}
            className="mt-1 block w-full rounded border border-zinc-300 px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-800" />
        </label>
        <div className="block">
          <span className="text-sm font-medium text-zinc-700 dark:text-zinc-300">Duration *</span>
          <div className="mt-1 flex gap-2 items-center">
            <input type="number" min="0" value={form.durationMinutes} onChange={(e) => setForm({ ...form, durationMinutes: e.target.value })}
              className="w-16 rounded border border-zinc-300 px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-800" />
            <span className="text-zinc-500">m</span>
            <input type="number" min="0" max="59" value={form.durationSeconds} onChange={(e) => setForm({ ...form, durationSeconds: e.target.value })}
              className="w-16 rounded border border-zinc-300 px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-800" />
            <span className="text-zinc-500">s</span>
          </div>
        </div>
        <label className="block">
          <span className="text-sm font-medium text-zinc-700 dark:text-zinc-300">Track # *</span>
          <input type="number" min="1" value={form.trackNumber} onChange={(e) => setForm({ ...form, trackNumber: e.target.value })}
            className="mt-1 block w-full rounded border border-zinc-300 px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-800" />
        </label>
        <label className="block col-span-2">
          <span className="text-sm font-medium text-zinc-700 dark:text-zinc-300">ISRC</span>
          <input type="text" value={form.isrc} onChange={(e) => setForm({ ...form, isrc: e.target.value.toUpperCase() })}
            placeholder="e.g. USRC17607839"
            className="mt-1 block w-full rounded border border-zinc-300 px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-800" />
        </label>
      </div>
      {availableReleases.length > 0 && (
        <div>
          <span className="text-sm font-medium text-zinc-700 dark:text-zinc-300">Releases</span>
          <div className="mt-1 flex flex-wrap gap-2">
            {availableReleases.map((r) => (
              <button key={r.id} type="button" onClick={() => toggleRelease(r.id)}
                className={`rounded-full px-3 py-1 text-xs font-medium border ${
                  form.releaseIds.includes(r.id)
                    ? "bg-zinc-900 text-white border-zinc-900 dark:bg-zinc-100 dark:text-zinc-900 dark:border-zinc-100"
                    : "bg-white text-zinc-700 border-zinc-300 dark:bg-zinc-800 dark:text-zinc-300 dark:border-zinc-700"
                }`}
              >
                {r.title}
              </button>
            ))}
          </div>
        </div>
      )}
      <div className="flex gap-2">
        <button type="submit" disabled={submitting}
          className="rounded bg-zinc-900 px-4 py-2 text-sm font-medium text-white hover:bg-zinc-700 disabled:opacity-50 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-300">
          {submitting ? "Saving…" : submitLabel}
        </button>
        <button type="button" onClick={onCancel}
          className="rounded border border-zinc-300 px-4 py-2 text-sm font-medium text-zinc-700 hover:bg-zinc-50 dark:border-zinc-700 dark:text-zinc-300 dark:hover:bg-zinc-800">
          Cancel
        </button>
      </div>
    </form>
  );
}

export { formDataToTrack };
