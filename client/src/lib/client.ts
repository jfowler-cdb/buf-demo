import { createClient } from "@connectrpc/connect";
import { createConnectTransport } from "@connectrpc/connect-web";
import { ReleaseService } from "@/gen/cdbaby/demo/v1beta1/releases_pb";
import { TrackService } from "@/gen/cdbaby/demo/v1beta1/tracks_pb";

// Go gateway — single entrypoint, Connect protocol (JSON in browser DevTools)
const transport = createConnectTransport({
  baseUrl: "http://localhost:8080",
});

export const releaseClient = createClient(ReleaseService, transport);
export const trackClient = createClient(TrackService, transport);
