# Schema-Driven Development Demo

This walkthrough demonstrates adding a **TrackService** end-to-end — from protobuf schema to running UI — using a schema-driven workflow with [Buf](https://buf.build), [Atlas](https://atlasgo.io), and [Mapperly](https://mapperly.riok.app).

## What you'll see

Each step is on its own branch. Between steps, examine the diff to see exactly what changed and where contracts are being enforced.

### Define the schema

![Step 1: Schema definition, buf lint, buf breaking](tapes/01-schema.gif)

### Generate code, build, and migrate

![Step 2: buf generate, dotnet build, atlas migrate diff](tapes/02-generate-and-build.gif)

### Safety nets catch mistakes

![Step 3: buf breaking and Mapperly catch proto drift](tapes/03-safety-nets.gif)

## Branches

| Branch | What changes | Contract enforcement |
|--------|-------------|---------------------|
| `demo/step-1-proto` | `tracks.proto` — service, messages, protovalidate | **Schema definition**: `buf lint` catches naming/style issues; protovalidate annotations declare constraints once |
| `demo/step-2-generate` | `buf generate` output (C# + TypeScript) | **Code generation**: typed stubs, no handwritten DTOs, validation descriptors embedded in generated code |
| `demo/step-3-backend` | EF Core entity, Mapperly mapper, gRPC service, Atlas migration | **Compile-time safety**: Mapperly fails the build if proto↔entity fields don't align; Atlas diffs desired schema from EF Core model |
| `demo/step-4-client` | Next.js tracks page with form + protovalidate | **Same rules, both sides**: validation defined in proto, enforced at runtime in C# (interceptor) and TypeScript (protovalidate-js) |
| `demo/break-proto` | Rename `Track.title` → `Track.name` | **Breakage demo**: `buf breaking` rejects it |
| `demo/break-mapper` | Add `genre` to proto, skip entity | **Breakage demo**: `dotnet build` fails — Mapperly catches drift |

## Prerequisites

```sh
# Tools
brew install bufbuild/buf/buf
brew install ariga/tap/atlas
dotnet tool install --global atlas-ef

# Dependencies
dotnet restore
cd client && npm install && cd ..
```

## Setup (start from main)

```sh
git checkout main
task generate        # generate protobuf code
task migrate:apply   # create the SQLite database
task seed            # populate 100 sample releases
task run             # start API (port 5000) + client (port 3000)
```

Open http://localhost:3000 — you should see the Releases page.

---

## Step 1 — Define the schema

```sh
git checkout demo/step-1-proto
```

**Look at**: `proto/cdbaby/demo/v1beta1/tracks.proto`

This is the only file that changed. Notice:
- The `TrackService` with full CRUD RPCs
- The `Track` message with `Duration`, `track_number`, `isrc`, and `release_ids` (many-to-many)
- **protovalidate constraints**: UUID on `id` with `IGNORE_IF_ZERO_VALUE` (output-only), `min_len`, `gte`, regex pattern on ISRC, `IGNORE_ALWAYS` on timestamps
- `buf lint` passes — STANDARD rules enforce consistent naming, package structure, and API design patterns

```sh
# See what's new
git diff main..demo/step-1-proto

# Verify lint passes
buf lint

# Try breaking a rule — rename a field to camelCase and re-lint
```

**Key point**: The schema is the single source of truth. Everything downstream is derived from this file.

---

## Step 2 — Generate code

```sh
git checkout demo/step-2-generate
```

**Look at the diff**:
```sh
git diff demo/step-1-proto..demo/step-2-generate --stat
```

One command (`buf generate`) produced:
- `API/gen/Tracks.cs` — C# message types with all fields strongly typed
- `API/gen/TracksGrpc.cs` — gRPC service base class with method signatures
- `client/src/gen/cdbaby/demo/v1beta1/tracks_pb.{js,d.ts}` — TypeScript types + service descriptor

No hand-written DTOs. No OpenAPI spec to maintain. The wire format (protobuf binary) enforces the contract implicitly — if a field is the wrong type, it cannot be serialized.

**Key point**: Code generation eliminates an entire class of bugs. The generated types are the contract.

---

## Step 3 — Implement the backend

```sh
git checkout demo/step-3-backend
```

```sh
git diff demo/step-2-generate..demo/step-3-backend --stat
```

What was added:

1. **`TrackEntity.cs`** — plain C# POCO for EF Core (separate from the proto type)
2. **`ReleaseTrackEntity.cs`** — join table for the many-to-many relationship
3. **`TrackMapper.cs`** — Mapperly source-generated mapper:
   - `Duration` ↔ `TimeSpan`, `Timestamp` ↔ `DateTime` conversions are compile-time generated
   - If you add a field to the proto and forget the entity, **the build fails**
4. **`TrackServiceImpl.cs`** — gRPC service using EF Core, handles `release_ids` sync
5. **Atlas migration** — auto-generated from the EF Core model diff:
   ```sh
   atlas migrate diff add_tracks --env local
   ```
   Atlas reads the `DbContext`, compares to the current DB, and produces the exact DDL

**Key point**: Mapperly catches proto↔entity drift at compile time. Atlas catches schema↔DB drift declaratively. No manual migration SQL.

---

## Step 4 — Build the client

```sh
git checkout demo/step-4-client
```

```sh
git diff demo/step-3-backend..demo/step-4-client --stat
```

What was added:
- **`track-form.tsx`** — form component with client-side protovalidate validation
- **`track-table.tsx`** — table component
- **`tracks/page.tsx`** — full CRUD page with pagination and release picker

The **same protovalidate rules** from the proto file are enforced here:
- `title` and `artist` must be non-empty
- `track_number` must be ≥ 1
- `isrc` must match the regex pattern
- `duration` is required

Try submitting an empty form — the client catches it before the request is sent. Remove the client validation — the server interceptor catches it. The contract is enforced at every layer.

### Run it

```sh
# Apply the new migration
export PATH="$PATH:$HOME/.dotnet/tools"
atlas migrate apply --env local

# Start both apps
task run
```

Open http://localhost:3000/tracks.

---

## Where contracts are enforced

| Layer | Mechanism | What it catches |
|-------|-----------|-----------------|
| **Proto definition** | `buf lint` (STANDARD rules) | Naming conventions, package structure, API design anti-patterns |
| **Proto definition** | `buf breaking` | Backward-incompatible changes (removed fields, type changes) |
| **Code generation** | `buf generate` | Type mismatches — impossible to send a string where an int is expected |
| **Wire format** | Protobuf binary encoding | Malformed payloads, unknown fields handled gracefully |
| **Server runtime** | `ProtoValidateInterceptor` | Business rules: required fields, string patterns, numeric ranges |
| **Client runtime** | `@bufbuild/protovalidate` | Same rules, instant feedback — no round-trip to server |
| **Proto → Entity** | Mapperly (compile-time) | Missing field mappings, type mismatches between proto and DB model |
| **Entity → DB** | Atlas (schema diff) | Schema drift — EF Core model doesn't match actual DB |

## Additional things to highlight

- **Breaking change detection**: `buf breaking --against '.git#branch=main'` catches removals, type changes, and field number reuse before they ship
- **Managed mode**: `buf.gen.yaml` has `managed: enabled: true` — Buf automatically sets C# namespaces, optimizes for speed, and manages well-known type imports
- **Developer experience**: The entire flow from proto change to running UI is: edit `.proto` → `buf generate` → implement service → `atlas migrate diff` → done
- **Incremental adoption**: You don't need all of this at once. Start with `buf lint` + code generation, add protovalidate later, add Atlas when ready

---

## Bonus: see the safety nets catch mistakes

Two branches are intentionally broken to demonstrate the tooling catching real problems.

### Breaking change detection

```sh
git checkout demo/break-proto
```

This branch renames `Track.title` → `Track.name`. Run:

```sh
buf breaking --against '.git#branch=demo/step-1-proto'
```

Output:
```
Field "2" on message "Track" changed name from "title" to "name".
```

`buf breaking` would catch this in CI before it ever merges. Existing clients would silently lose the `title` field — this prevents that.

### Proto ↔ Entity mapper drift

```sh
git checkout demo/break-mapper
```

This branch adds a `genre` field to `Track` in the proto and regenerates, but **does not** update `TrackEntity`. Run:

```sh
dotnet build API
```

Output:
```
error RMG020: The member Genre on ... Track is not mapped to any member on ... TrackEntity
error RMG012: The member Genre on ... Track was not found on ... TrackEntity
```

Mapperly (with `WarningsAsErrors` set to `RMG012;RMG020` in the csproj) fails the build. You cannot ship a proto change without updating the DB model — the compiler enforces it.

Switch back when done:
```sh
git checkout demo/step-4-client
```
