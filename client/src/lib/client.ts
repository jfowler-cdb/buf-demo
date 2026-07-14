import { createClient } from "@connectrpc/connect";
import { createGrpcWebTransport } from "@connectrpc/connect-web";
import { ReleaseService } from "@/gen/cdbaby/demo/v1beta1/releases_pb";
import { TrackService } from "@/gen/cdbaby/demo/v1beta1/tracks_pb";

const transport = createGrpcWebTransport({
  baseUrl: "http://localhost:5000",
});

export const releaseClient = createClient(ReleaseService, transport);
export const trackClient = createClient(TrackService, transport);
