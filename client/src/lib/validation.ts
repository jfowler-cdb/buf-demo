import { create, toJson, fromJson } from "@bufbuild/protobuf";
import { createValidator } from "@bufbuild/protovalidate";
import {
  ReleaseSchema,
  CreateReleaseRequestSchema,
  type Release,
} from "@/gen/cdbaby/demo/v1beta1/releases_pb";

const validator = createValidator();

export function releaseToFormData(release?: Release) {
  return {
    title: release?.title ?? "",
    artist: release?.artist ?? "",
    label: release?.label ?? "",
    releaseDate: release?.releaseDate
      ? new Date(Number(release.releaseDate.seconds) * 1000)
          .toISOString()
          .split("T")[0]
      : new Date().toISOString().split("T")[0],
  };
}

export type ReleaseFormData = ReturnType<typeof releaseToFormData>;

export function formDataToRelease(
  data: ReleaseFormData,
  id?: string
): Release {
  const date = new Date(data.releaseDate + "T00:00:00Z");
  return create(ReleaseSchema, {
    id: id ?? "",
    title: data.title,
    artist: data.artist,
    label: data.label,
    releaseDate: {
      seconds: BigInt(Math.floor(date.getTime() / 1000)),
      nanos: 0,
    },
  });
}

export function validateRelease(
  data: ReleaseFormData,
  id?: string
): string[] {
  const release = formDataToRelease(data, id);
  const request = create(CreateReleaseRequestSchema, { release });
  const result = validator.validate(CreateReleaseRequestSchema, request);
  if (result.kind === "valid") return [];
  return (result.violations ?? []).map(
    (v) => `${v.field?.toString() ?? "unknown"}: ${v.message}`
  );
}
