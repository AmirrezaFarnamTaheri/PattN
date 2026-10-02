package main

import (
	"bufio"
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"os"
	"sync"

	"pattn-discovery/engine"
	"pattn-discovery/protocol"
)

func main() {
	if err := run(context.Background(), os.Stdin, os.Stdout); err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}
}

const maxRequestFrameBytes = 16 * 1024 * 1024

func run(ctx context.Context, in io.Reader, out io.Writer) error {
	eng := engine.New()
	scanner := bufio.NewScanner(in)
	// Requests can carry large target batches; keep framing bounded but well above Scanner's default 64 KiB.
	scanner.Buffer(make([]byte, 64*1024), maxRequestFrameBytes)
	enc := json.NewEncoder(out)
	enc.SetEscapeHTML(false)
	var writeMu sync.Mutex
	emit := func(response protocol.Response) error {
		writeMu.Lock()
		defer writeMu.Unlock()
		return enc.Encode(response)
	}
	serverCtx, stopServer := context.WithCancel(ctx)
	defer stopServer()

	const maxConcurrentUnary = 8
	const maxConcurrentStreams = 2
	unarySlots := make(chan struct{}, maxConcurrentUnary)
	streamSlots := make(chan struct{}, maxConcurrentStreams)
	var workers sync.WaitGroup
	var activeMu sync.Mutex
	activeUnary := make(map[string]context.CancelFunc)
	// Handlers report request-level failures as responses and return non-nil only when emitting
	// to the client fails. The first such failure stops all in-flight work and becomes run's result.
	var runErrMu sync.Mutex
	var runErr error
	recordRunErr := func(err error) {
		if err == nil || (errors.Is(err, context.Canceled) && ctx.Err() == nil) {
			return
		}
		runErrMu.Lock()
		if runErr == nil {
			runErr = err
			stopServer()
		}
		runErrMu.Unlock()
	}

	cancelUnary := func(requestID string) bool {
		activeMu.Lock()
		cancel := activeUnary[requestID]
		activeMu.Unlock()
		if cancel == nil {
			return false
		}
		cancel()
		return true
	}

	for scanner.Scan() {
		var req protocol.Request
		if err := json.Unmarshal(scanner.Bytes(), &req); err != nil {
			if err := emit(protocol.Response{Version: protocol.Version, Error: &protocol.Error{Code: "invalid_json", Message: err.Error()}}); err != nil {
				return err
			}
			continue
		}
		if req.Method == "request.cancel" {
			var p struct {
				RequestID string `json:"requestId"`
			}
			response := protocol.Response{Version: protocol.Version, ID: req.ID}
			if req.Version != protocol.Version {
				response.Error = &protocol.Error{Code: "protocol_version_mismatch", Message: fmt.Sprintf("supported=%d requested=%d", protocol.Version, req.Version)}
			} else if err := json.Unmarshal(req.Params, &p); err != nil || p.RequestID == "" {
				response.Error = &protocol.Error{Code: "invalid_params", Message: "requestId is required"}
			} else if !cancelUnary(p.RequestID) {
				response.Error = &protocol.Error{Code: "request_not_found", Message: "active request not found: " + p.RequestID}
			} else {
				response.Data = map[string]any{"requestId": p.RequestID, "state": "cancelling"}
			}
			if err := emit(response); err != nil {
				return err
			}
			continue
		}
		if engine.IsStreamingMethod(req.Method) {
			select {
			case streamSlots <- struct{}{}:
			default:
				if err := emit(protocol.Response{
					Version: protocol.Version,
					ID: req.ID,
					Error: &protocol.Error{Code: "server_busy", Message: "too many concurrent streaming requests"},
				}); err != nil {
					return err
				}
				continue
			}
			workers.Add(1)
			go func(request protocol.Request) {
				defer workers.Done()
				defer func() { <-streamSlots }()
				if err := eng.HandleStream(serverCtx, request, emit); err != nil {
					recordRunErr(err)
				}
			}(req)
			continue
		}

		select {
		case unarySlots <- struct{}{}:
		default:
			if err := emit(protocol.Response{
				Version: protocol.Version,
				ID: req.ID,
				Error: &protocol.Error{Code: "server_busy", Message: "too many concurrent unary requests"},
			}); err != nil {
				return err
			}
			continue
		}

		requestCtx, cancel := context.WithCancel(serverCtx)
		activeMu.Lock()
		if _, exists := activeUnary[req.ID]; exists || req.ID == "" {
			activeMu.Unlock()
			cancel()
			<-unarySlots
			code := "duplicate_request_id"
			message := "request id is already active"
			if req.ID == "" {
				code = "invalid_request"
				message = "unary request id is required"
			}
			if err := emit(protocol.Response{Version: protocol.Version, ID: req.ID, Error: &protocol.Error{Code: code, Message: message}}); err != nil {
				return err
			}
			continue
		}
		activeUnary[req.ID] = cancel
		activeMu.Unlock()

		workers.Add(1)
		go func(request protocol.Request, requestCtx context.Context, cancel context.CancelFunc) {
			defer workers.Done()
			defer func() { <-unarySlots }()
			defer cancel()
			defer func() {
				activeMu.Lock()
				delete(activeUnary, request.ID)
				activeMu.Unlock()
			}()
			if err := eng.HandleStream(requestCtx, request, emit); err != nil {
				recordRunErr(err)
			}
		}(req, requestCtx, cancel)
	}
	if err := scanner.Err(); err != nil {
		stopServer()
		workers.Wait()
		if errors.Is(err, bufio.ErrTooLong) {
			if emitErr := emit(protocol.Response{
				Version: protocol.Version,
				Error: &protocol.Error{
					Code:    "frame_too_large",
					Message: fmt.Sprintf("NDJSON request exceeds %d bytes", maxRequestFrameBytes),
				},
			}); emitErr != nil {
				return errors.Join(err, emitErr)
			}
		}
		return err
	}
	stopServer()
	workers.Wait()
	runErrMu.Lock()
	err := runErr
	runErrMu.Unlock()
	return err
}
