package main

import (
	"log"
	"net/http"
	"net/http/httputil"
	"net/url"
	"os"
	"os/signal"
	"syscall"

	"connectrpc.com/vanguard"

	"github.com/jfowler-cdb/buf-demo/gateway/gen/cdbaby/demo/v1beta1/demov1beta1connect"
)

func main() {
	csharpGrpcAddr := envOr("CSHARP_GRPC_ADDR", "localhost:5000")
	listenAddr := envOr("LISTEN_ADDR", ":8080")

	// --- Reverse proxy to C# gRPC server (HTTP/2, unencrypted) ---
	backendURL, _ := url.Parse("http://" + csharpGrpcAddr)
	grpcProxy := httputil.NewSingleHostReverseProxy(backendURL)
	h2Transport := &http.Transport{}
	h2Transport.Protocols = new(http.Protocols)
	h2Transport.Protocols.SetHTTP1(false)
	h2Transport.Protocols.SetUnencryptedHTTP2(true)
	grpcProxy.Transport = h2Transport

	// --- Vanguard: translates Connect/gRPC-Web/REST → gRPC to C# backend ---
	grpcTarget := vanguard.WithTargetProtocols(vanguard.ProtocolGRPC)
	protoCodec := vanguard.WithTargetCodecs("proto")
	releaseService := vanguard.NewService(demov1beta1connect.ReleaseServiceName, grpcProxy, grpcTarget, protoCodec)
	trackService := vanguard.NewService(demov1beta1connect.TrackServiceName, grpcProxy, grpcTarget, protoCodec)

	transcoder, err := vanguard.NewTranscoder([]*vanguard.Service{releaseService, trackService})
	if err != nil {
		log.Fatalf("failed to create vanguard transcoder: %v", err)
	}

	// --- Serve OpenAPI spec ---
	mux := http.NewServeMux()
	mux.Handle("/openapi/", http.StripPrefix("/openapi/", http.FileServer(http.Dir("openapi"))))
	mux.Handle("/", transcoder)

	// --- CORS + Start ---
	corsHandler := corsMiddleware(mux)
	server := &http.Server{Addr: listenAddr, Handler: corsHandler}

	go func() {
		log.Printf("Gateway listening on %s", listenAddr)
		log.Printf("  Connect (JSON): browser → Vanguard → gRPC → C# (%s)", csharpGrpcAddr)
		log.Printf("  REST:           /v1beta1/* → Vanguard → gRPC → C# (%s)", csharpGrpcAddr)
		log.Printf("  OpenAPI spec:   /openapi/api.swagger.json")
		if err := server.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			log.Fatal(err)
		}
	}()

	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)
	<-quit
	log.Println("Shutting down...")
	server.Close()
}

func corsMiddleware(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Access-Control-Allow-Origin", "*")
		w.Header().Set("Access-Control-Allow-Methods", "GET, POST, PATCH, DELETE, OPTIONS")
		w.Header().Set("Access-Control-Allow-Headers", "Content-Type, Connect-Protocol-Version, Connect-Timeout-Ms, Grpc-Timeout, X-Grpc-Web, X-User-Agent")
		w.Header().Set("Access-Control-Expose-Headers", "Grpc-Status, Grpc-Message, Grpc-Status-Details-Bin")
		if r.Method == http.MethodOptions {
			w.WriteHeader(http.StatusNoContent)
			return
		}
		next.ServeHTTP(w, r)
	})
}

func envOr(key, fallback string) string {
	if v := os.Getenv(key); v != "" {
		return v
	}
	return fallback
}
