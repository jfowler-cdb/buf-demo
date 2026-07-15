package service

import (
	"context"
	"sort"
	"sync"

	"connectrpc.com/connect"
	"github.com/google/uuid"
	"google.golang.org/protobuf/types/known/durationpb"
	"google.golang.org/protobuf/types/known/timestamppb"

	pb "github.com/jfowler-cdb/buf-demo/gateway/gen/cdbaby/demo/v1beta1"
	"github.com/jfowler-cdb/buf-demo/gateway/gen/cdbaby/demo/v1beta1/demov1beta1connect"
)

var _ demov1beta1connect.TrackServiceHandler = (*TrackService)(nil)

type TrackService struct {
	mu     sync.RWMutex
	tracks map[string]*pb.Track
}

func NewTrackService() *TrackService {
	return &TrackService{
		tracks: make(map[string]*pb.Track),
	}
}

func (s *TrackService) GetTrack(_ context.Context, req *connect.Request[pb.GetTrackRequest]) (*connect.Response[pb.GetTrackResponse], error) {
	s.mu.RLock()
	defer s.mu.RUnlock()

	track, ok := s.tracks[req.Msg.Id]
	if !ok {
		return nil, connect.NewError(connect.CodeNotFound, nil)
	}
	return connect.NewResponse(&pb.GetTrackResponse{Track: track}), nil
}

func (s *TrackService) ListTracks(_ context.Context, req *connect.Request[pb.ListTracksRequest]) (*connect.Response[pb.ListTracksResponse], error) {
	s.mu.RLock()
	defer s.mu.RUnlock()

	pageSize := int(req.Msg.PageSize)
	if pageSize <= 0 {
		pageSize = 50
	}

	// Collect and optionally filter
	var filtered []*pb.Track
	for _, t := range s.tracks {
		if req.Msg.ReleaseId != "" {
			found := false
			for _, rid := range t.ReleaseIds {
				if rid == req.Msg.ReleaseId {
					found = true
					break
				}
			}
			if !found {
				continue
			}
		}
		filtered = append(filtered, t)
	}

	// Sort by title
	sort.Slice(filtered, func(i, j int) bool {
		return filtered[i].Title < filtered[j].Title
	})

	// Paginate
	startIndex := 0
	if req.Msg.PageToken != "" {
		for i, t := range filtered {
			if t.Id == req.Msg.PageToken {
				startIndex = i
				break
			}
		}
	}

	end := startIndex + pageSize
	if end > len(filtered) {
		end = len(filtered)
	}
	page := filtered[startIndex:end]

	var nextPageToken string
	if end < len(filtered) {
		nextPageToken = filtered[end].Id
	}

	return connect.NewResponse(&pb.ListTracksResponse{
		Tracks:        page,
		NextPageToken: nextPageToken,
	}), nil
}

func (s *TrackService) CreateTrack(_ context.Context, req *connect.Request[pb.CreateTrackRequest]) (*connect.Response[pb.CreateTrackResponse], error) {
	track := cloneTrack(req.Msg.Track)
	track.Id = uuid.New().String()
	now := timestamppb.Now()
	track.CreateTime = now
	track.UpdateTime = now

	s.mu.Lock()
	s.tracks[track.Id] = track
	s.mu.Unlock()

	return connect.NewResponse(&pb.CreateTrackResponse{Track: track}), nil
}

func (s *TrackService) UpdateTrack(_ context.Context, req *connect.Request[pb.UpdateTrackRequest]) (*connect.Response[pb.UpdateTrackResponse], error) {
	s.mu.Lock()
	defer s.mu.Unlock()

	existing, ok := s.tracks[req.Msg.Track.Id]
	if !ok {
		return nil, connect.NewError(connect.CodeNotFound, nil)
	}

	updated := cloneTrack(req.Msg.Track)
	updated.CreateTime = existing.CreateTime
	updated.UpdateTime = timestamppb.Now()
	s.tracks[updated.Id] = updated

	return connect.NewResponse(&pb.UpdateTrackResponse{Track: updated}), nil
}

func (s *TrackService) DeleteTrack(_ context.Context, req *connect.Request[pb.DeleteTrackRequest]) (*connect.Response[pb.DeleteTrackResponse], error) {
	s.mu.Lock()
	defer s.mu.Unlock()

	track, ok := s.tracks[req.Msg.Id]
	if !ok {
		return nil, connect.NewError(connect.CodeNotFound, nil)
	}
	delete(s.tracks, req.Msg.Id)

	return connect.NewResponse(&pb.DeleteTrackResponse{Track: track}), nil
}

func cloneTrack(t *pb.Track) *pb.Track {
	clone := &pb.Track{
		Id:          t.Id,
		Title:       t.Title,
		Artist:      t.Artist,
		TrackNumber: t.TrackNumber,
		Isrc:        t.Isrc,
		ReleaseIds:  append([]string{}, t.ReleaseIds...),
	}
	if t.Duration != nil {
		clone.Duration = durationpb.New(t.Duration.AsDuration())
	}
	if t.CreateTime != nil {
		clone.CreateTime = timestamppb.New(t.CreateTime.AsTime())
	}
	if t.UpdateTime != nil {
		clone.UpdateTime = timestamppb.New(t.UpdateTime.AsTime())
	}
	return clone
}
