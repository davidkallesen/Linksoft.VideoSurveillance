# Long-Running Stability Audit (24/7/365)

Scope: WPF camera-wall app + ASP.NET Core server edition. Reference workload used throughout: 4 RTSP cameras, 15-minute segmentation, 30-day recording retention, 7-day snapshot retention. ~96 segment cycles per camera per day, ~35,000 per year.

This report focuses only on issues that would surface during long-running unattended operation. Cosmetic and "could be cleaner" findings are excluded.

## Already fixed

- **`ThumbnailGeneratorService` race** — fire-and-forget `CaptureFrameAsync` (`ConfigureAwait(false)`) added to `List<Bitmap?>` on a thread-pool thread while `StopCapture` iterated it on the dispatcher → "Collection was modified" crash. Fixed via `Lock` + snapshot, plus `ScheduledCaptures` to stop over-firing on the timer.
- **Missing stack trace in unhandled-exception log** — `LogDispatcherUnhandledException` / `LogCurrentDomainUnhandledException` now take an `Exception` parameter so Serilog's `{Exception}` template captures the trace.
- **P1.5 Demuxer FFmpeg leaks** — `Demuxer.Open` now wraps the FFmpeg setup in try/catch+finally; `infoDict` and `dict` are always freed; on any failure `CleanupOnOpenFailure()` releases `fmtCtx`, `gcHandle`, `interruptDelegate`, `packet`. Fixes ~12–18 MB/year RAM leak from RTSP reconnect storms.
- **P1.1 Atomic JSON saves** — new `SafeJsonFile.TryWrite` / `TryRead` helper in Core writes to `*.tmp`, round-trip verifies, then `File.Replace`s into place leaving the previous version as `*.bak`. Read path falls back to `*.bak` if primary is missing/corrupt. Refactored `CameraStorageService`, `ApplicationSettingsService`, `JsonCameraStorageService`, `JsonApplicationSettingsService`. Covered by 11 new unit tests in `SafeJsonFileTests` (write success, overwrite preserves backup, mid-write failure leaves prior file intact, corrupt-primary falls back to backup, etc.).
- **P1.2 DispatcherTimer.Tick safety** — converted `MediaCleanupService` async-void event handler, `RecordingSegmentationService` segmentation tick, and `RecordingService` post-motion tick to wrap their work in try/catch with `LogPeriodicTickFailed` / `LogSegmentationTickFailed` / `LogPostMotionTickFailed`. Eliminates the entire class of "exception in Tick handler crashes the WPF dispatcher" bugs.
- **P1.3 MediaCleanupService skips active recordings** — injected `IRecordingService`, snapshots `GetActiveSessions().CurrentFilePath` (and the `.png` thumbnail companion) before each cleanup pass, and skips those files. Eliminates sharing-violation IOExceptions and the resulting orphan-undeletable-file leaks.
- **P1.4 Cleanup enumeration is now crash-safe** — `MediaCleanupService.CleanDirectoryAsync` now also catches `UnauthorizedAccessException` and `IOException` from `Directory.EnumerateFiles` (previously only `DirectoryNotFoundException` was caught). Symlink loops, dropped network drives, and permission-denied subtrees no longer take down the dispatcher. The `CreateDirectory` calls in `RecordingService`, `ThumbnailGeneratorService`, `TimelapseService` are already inside outer try/catches that prevent crashes; deferred fallback-to-default-path behavior to a P2/P3 cycle (intrusive UX change).
- **P2.8 Bound `recordingCooldowns`** — entries are pruned opportunistically on each motion-stop so the dictionary stays bounded by 24h of motion events. (`postMotionTimers` was found to be self-healing — `CheckPostMotionState` removes stale timers on next tick — so no fix needed there.)
- **P2.9 Robust segmentation slot detection** — extracted `RecordingSlotCalculator` to Core. Slot identity is now `(DateOnly, int)` and the high-water mark only advances forward, so backward NTP corrections, DST fall-back, and midnight rollover never double-segment or skip a slot. 12 unit tests cover normal progression, midnight, DST forward/backward, and backward clock jumps.
- **P2.10 Thumbnail write atomicity** — `ThumbnailGeneratorService` now writes to `*.tmp` and atomically `File.Move(..., overwrite: true)` into place, so the recordings browser cannot observe a partially-written PNG.
- **P2.12 Filename collision prevention** — extracted `UniqueFilename.EnsureUnique` to Core, plus switched all 3 filename generators (`RecordingService`, `TimelapseService`, `ServerRecordingService`) to millisecond-resolution timestamps (`yyyyMMdd_HHmmss_fff`). Collisions now require sub-millisecond timing AND `EnsureUnique` falls back to a `_2/_3/…/_999` suffix and finally a UTC-millisecond fallback. 7 unit tests.
- **P2.14 MotionDetectionService scheduler atomicity** — `StartDetection`/`StopDetection` now mutate `contexts` and `scheduledCameras` together under `schedulerLock`, eliminating the interleaving that could leave a camera scheduled but missing from contexts (silent no-analysis state).
- **P2.13 Server motion loop no longer wastes frames** — `ServerMotionDetectionService.RunDetectionLoopAsync` was capturing a frame every 200 ms and discarding it (≈1.5 MB/s per camera). Now logs a one-time `LogMotionDetectionAlgorithmNotImplemented` warning and waits on the cancellation token; analysis kicks back in cleanly when the differencing algorithm is implemented.
- **P2.11 SwapChainPresenter device-lost detection** — `Present` and `ResizeBuffers` return values are now inspected for `DXGI_ERROR_DEVICE_REMOVED` / `_HUNG` / `_RESET` / `DRIVER_INTERNAL_ERROR`. On detection a `volatile bool deviceLost` flag latches, the `DeviceLost` event fires (outside the lock so subscribers can re-enter the pipeline), and subsequent `Present` calls short-circuit. Stops the post-driver-crash busy loop; full upstream pipeline recovery still has to subscribe to the event (deferred — VideoHost has no logger or DI yet).
- **P3.18 GitHubReleaseService no longer holds the lock across HTTP** — `cacheLock.WaitAsync` now uses a 2-second timeout, the lock is released before the HTTP call, and `HttpClient.Timeout` caps at 10 s. A network stall on update-check can no longer freeze unrelated callers indefinitely.
- **P3.19 Atomic recording segment switch** — `Remuxer` now exposes `SwitchTo` that holds `syncLock` across `CloseLocked + OpenLocked`. New `IMediaPipeline.SwitchRecording` (default impl falls back to Stop+Start) is overridden in both `VideoEngineMediaPipeline`s to call `VideoPlayer.SwitchRecording` → `Remuxer.SwitchTo`. `RecordingService.SegmentRecording` uses the atomic path so packets arriving from the demux thread mid-switch land in the previous or new segment, never the close/open gap.
- **P3.20 GPU allocation try/finally** — `VideoProcessorRenderer.EnsurePipeline` wraps `CreateVideoProcessorEnumerator → CreateVideoProcessor → CreateTexture2D → CreateVideoProcessorOutputView` in try/catch with `ReleasePipeline()` cleanup. `GpuSnapshotCapture.EnsureEncoder` has the same shape with `FreeEncoder()` cleanup. Failures during rotation/resolution churn no longer strand half-allocated GPU resources.
- **P3.22 FFmpeg cleanup on failure** — `FrameCapture.EnsureContexts` and `GpuSnapshotCapture.EnsureEncoder` now wrap their multi-step allocation (sws + frame + buffer + encoder) in try/catch with `FreeContexts/FreeEncoder()` cleanup, plus `av_dict_free(ref opts)` in finally. Failures between alloc steps no longer leak `AVFrame` buffers or option dictionaries.
- **P3.23 Reconnect uses capped exponential backoff** — extracted `ReconnectBackoff.ComputeDelay` to Core (30 s → 60 s → 120 s → 240 s → 480 s → 900 s cap), wired into `CameraConnectionManager` via a per-camera `BackoffState`. A persistently dead camera now contributes a small constant rate of failed attempts instead of ~1 M/year. Cleared on successful Connected event. 11 unit tests.
- **P3.24 CancellationToken plumbed through `StartStream`** — SignalR injects the connection-aborted token, `Task.Delay` and the trailing `SendAsync` both honour it, and an early cancel stops the FFmpeg transcoder. A flapping client no longer leaves a 30 s playlist-wait running on the server.
- **P3.25 Server credential storage** — DEFERRED. RTSP creds stay in plaintext JSON for now. Encryption-at-rest needs DPAPI (Windows-only) or an AES-with-machine-key scheme that breaks JSON-file portability and migration. Operational note: protect `cameras.json` via filesystem ACL; rotate via service restart until a hot-reload endpoint lands.
- **P3.21 Shared D3D11 device** — DEFERRED. Per-camera device overhead is ~80 MB GPU which is acceptable for the 4-camera reference workload. Sharing across cameras is a multi-class rearchitecture (device pool, reference counting, threading-model verification) with regression risk that doesn't pay off until camera count grows or GPU pressure becomes evidenced in production.
- **P1.6 Bounded broadcast send time** — `SurveillanceEventBroadcaster` already caught exceptions in its async-void handlers; added a per-call `CancellationTokenSource(5s)` so a slow/disconnected SignalR client cannot accumulate unbounded in-flight `SendAsync` tasks while events keep firing.
- **P1.7 CORS allowlist** — replaced `policy.SetIsOriginAllowed(_ => true)` with `WithOrigins(allowedOrigins)`. Default allowlist is loopback-only (Blazor.App debug ports + standard 5000/5001); production hosts add their origins via `Cors:AllowedOrigins` config. Eliminates the open-CORS attack surface against the SignalR hub.
- **P2.15 HLS viewer lease/heartbeat** — `StreamingService` now tracks `LastActivityUtc` per session (refreshed on `StartStream`/`Heartbeat`) and a 30 s reaper closes sessions idle past 2 minutes. New `SurveillanceHub.StreamHeartbeat(cameraId)` method lets the Blazor client refresh activity. Flapping clients no longer leak orphan FFmpeg transcoders.
- **P2.16 StartRecordingHandler awaits Connected** — handler now subscribes to `pipeline.ConnectionStateChanged`, waits up to 15 s for `Connected` (or short-circuits on `Error`), and returns a definite success/failure to the API client. Pipeline is disposed on timeout/error so an immediate retry doesn't leak the prior RTSP slot. Cancellation token is honoured.
- **P2.17 Defensive disposal in pipeline factories** — both `VideoEngineMediaPipelineFactory` (WPF + API) now wrap construction in try/catch and dispose any half-built `player`/`pipeline` on failure. A throw between `videoPlayerFactory.Create()` and `new VideoEngineMediaPipeline(...)` no longer strands the player; a throw inside `pipeline.Open` no longer strands the wrapping pipeline.
- **G3.1 Server-side recording segmentation** — `ServerRecordingService.SegmentRecording` was a no-op returning `false`, so server recordings grew into single multi-day files. Now uses the atomic `IMediaPipeline.SwitchRecording` to roll over to a fresh file on demand, mirroring the WPF behavior minus thumbnails. Injects `ICameraStorageService` to look up the camera by id.
- **G3.2 Server-side media cleanup** — `MediaCleanupService` was WPF-only, so the server would have eventually filled its disk. Extracted the file-system pass to a portable Core helper `MediaCleanupRunner` (with 11 unit tests covering retention cutoff, skip-set, companion thumbnails, recursive scan, missing-root, and empty-directory removal); refactored the WPF service to delegate to it; added `ServerMediaCleanupBackgroundService` (`IHostedService` on a `System.Threading.Timer`, 4-hour interval) and registered it in `Program.cs`.
- **G2.4 Off-UI timers for cleanup and segmentation** — `MediaCleanupService` and `RecordingSegmentationService` now use `System.Threading.Timer` instead of `DispatcherTimer` so ticks fire even when the WPF UI thread is busy rendering 10+ tiles. The actual work already ran on the thread pool; only the trigger has moved.
- **G2.5 Tighter HLS reaper + active SignalR-disconnect cleanup** — `StreamingService` inactivity 2 min → 45 s, reaper 10 s. New ownership map (`connectionId → cameraIds`) is populated in `StartStream(cameraId, connectionId)` and consulted by `OnConnectionDisconnected`, which is now wired into `SurveillanceHub.OnDisconnectedAsync`. A closed-tab client releases its CPU-heavy FFmpeg transcoders within seconds instead of waiting up to 2.5 minutes.

---

## 1. Timer-driven services & concurrent state

| # | Severity | Location | Issue |
|---|----------|----------|-------|
| 1.1 | LEAK | `RecordingService.cs:154,187` | `recordingCooldowns` `ConcurrentDictionary` is added to on every motion-recording stop; never pruned. Grows unbounded over weeks. |
| 1.2 | SILENT-CORRUPTION | `RecordingSegmentationService.cs:67–68,150–153` | Slot calculation uses `(now.Hour*60 + now.Minute)/intervalMinutes`. At midnight, slot drops 1439→0; depending on timer jitter and DST, a slot can be processed twice or skipped. |
| 1.3 | CRASH | `RecordingService.cs:510`, `RecordingSegmentationService.cs:124`, `MediaCleanupService.cs:192` | `DispatcherTimer.Tick += (_, _) => Method(...)` with no try/catch. Any exception inside crashes the dispatcher (the same shape as the bug we just fixed). |
| 1.4 | SILENT-CORRUPTION | `MediaCleanupService.cs:192` | `periodicTimer.Tick += async (_, _) => await RunCleanupAsync().ConfigureAwait(false);` — `async void` over `EventHandler`. Exceptions vanish; cleanup can stop running silently and disk fills up undetected. |
| 1.5 | SILENT-CORRUPTION | `MotionDetectionService.cs:156–165` | Iterates `scheduledCameras` then calls `contexts.TryGetValue` for each — separate `ConcurrentDictionary`, no consistency. A camera removed mid-iteration is silently skipped and may never be rescheduled. |
| 1.6 | UI-FREEZE | `GitHubReleaseService.cs:165,188` | `await cacheLock.WaitAsync()` with no timeout, holding the lock across HTTP `await`. Network stall on update-check freezes any other caller indefinitely. |
| 1.7 | LEAK | `MotionDetectionService.cs:223–224` | `_ = CaptureAndProcessAsync(...)` fire-and-forget. The `IsAnalyzing` guard helps but doesn't bound the in-flight queue under sustained network latency. |
| 1.8 | LEAK | `RecordingService.cs:497–522` | `postMotionTimers` entries can survive a camera disconnect. Stopped timers stay referenced in the dictionary, leak per-disconnect. |

## 2. FFmpeg / VideoEngine / D3D11 resources

| # | Severity | Location | Issue |
|---|----------|----------|-------|
| 2.1 | MEM-LEAK | `VideoEngine\Demuxing\Demuxer.cs:107–111` | `AVDictionary* infoDict` allocated, then thrown over without `av_dict_free`. ~4 KB per failed `avformat_find_stream_info`; piles up on RTSP reconnect storms. |
| 2.2 | MEM-LEAK | `VideoEngine\Demuxing\Demuxer.cs:102–104` | On `avformat_open_input` failure, `fmtCtx = null` without `avformat_close_input`. The native context is orphaned. |
| 2.3 | GPU-LEAK | `VideoEngine.DirectX\VideoProcessorRenderer.cs:181–213` | Sequence `CreateVideoProcessorEnumerator → CreateVideoProcessor → CreateTexture2D → CreateVideoProcessorOutputView` has no try/finally. A failure at the last step leaves the earlier objects unreleased; rotation/resolution churn leaks VRAM. |
| 2.4 | CRASH (silent freeze) | `VideoEngine.DirectX\SwapChainPresenter.cs:182` | `swapChain.Present` return value is unchecked. `DXGI_ERROR_DEVICE_REMOVED` (driver crash, sleep/resume) leaves the tile permanently frozen with no recovery path. |
| 2.5 | DATA-LOSS | `VideoEngine\VideoPlayer.cs:115–143` + `Recording\Remuxer.cs:19,148–169` | Segment boundary: `StopRecording → av_write_trailer + avio_closep`; immediate `StartRecording(nextPath)`. `WritePacket` silently drops packets if a frame arrives between the two. Over 35k segments/year, output files contain micro-gaps. |
| 2.6 | MEM-LEAK | `VideoEngine\Capture\FrameCapture.cs:110–148` and `VideoEngine.DirectX\GpuSnapshotCapture.cs:129–167` | `av_frame_alloc` + `av_frame_get_buffer` happen before `avcodec_open2`. Failure between them skips `FreeContexts`. |
| 2.7 | GPU-WASTE / GPU-LEAK | `VideoEngine.DirectX\D3D11AcceleratorFactory.cs:9–20` | Each camera creates its own `ID3D11Device`. 4 devices ≈ 300 MB+ of redundant context. Increases fragmentation; raises probability of `DEVICE_REMOVED`. |
| 2.8 | MEM-LEAK | `VideoEngine\VideoPlayer.cs:183–199` | `av_frame_clone` has try/finally, but a throw before the try (e.g. on null `latestFrame`) leaks. |
| 2.9 | GPU-LEAK | `VideoEngine.DirectX\GpuSnapshotCapture.cs:43–44,75–99,101–171` | `EnsureStagingTexture` then `EnsureEncoder` failure: `FreeEncoder` runs but doesn't dispose `stagingTexture`. Per-camera resolution change leaks GPU memory. |

## 3. Disk / Storage / I/O

| # | Severity | Location | Issue |
|---|----------|----------|-------|
| 3.1 | SILENT-CORRUPTION | `CameraStorageService.cs:162`, `ApplicationSettingsService.cs:199`, `Api\Services\JsonCameraStorageService.cs:149`, `JsonApplicationSettingsService.cs:185` | `File.WriteAllText(path, json)` is non-atomic. Power loss mid-write produces a half-written file; load catches `JsonException` and silently starts with empty defaults. **All camera/layout config can be lost in one event.** Use temp+rename. |
| 3.2 | CRASH | `MediaCleanupService.cs:221,232,244` | `Directory.EnumerateFiles(..., SearchOption.AllDirectories)` is unhandled for `UnauthorizedAccessException` / `IOException` / symlink loops. `fileInfo.Delete` only catches generic `Exception` for individual files but not the enumerator itself. |
| 3.3 | DATA-LOSS / DISK-EXHAUSTION | `MediaCleanupService.cs:232,244` | `fileInfo.Delete()` runs without checking if the file is the currently-active recording (sharing-violation `IOException`). The orphaned-but-undeletable file leaks; over time, retention isn't actually enforced. |
| 3.4 | CRASH | `RecordingService.cs:83`, `ThumbnailGeneratorService.cs:270`, `TimelapseService.cs:170`, `Api\...\CaptureSnapshotHandler.cs:32` | `Directory.CreateDirectory(...)` is uncaught. UNC path that drops, USB drive unplug, or path-permission change kills the recording loop. |
| 3.5 | DATA-LOSS | `RecordingService.cs:275`, `TimelapseService.cs:202`, `Api\Services\ServerRecordingService.cs:120`, `CaptureSnapshotHandler.cs:37` | Filenames use `DateTime.Now` at 1-second resolution. NTP backward correction or two near-simultaneous motion triggers cause `Camera_yyyyMMdd_HHmmss.mkv` collisions; the muxer overwrites the prior segment. Use `DateTime.UtcNow` + collision suffix. |
| 3.6 | SILENT-CORRUPTION | `ApplicationSettingsService.cs:188–204`, `CameraStorageService.cs:151–169` | `Save()` swallows exceptions and returns void. Caller has no way to know save failed (disk quota, perms). Settings appear saved in UI but aren't persisted. |
| 3.7 | DATA-LOSS / CRASH | `ThumbnailGeneratorService.cs:274` | `thumbnail.Save(path, Png)` writes without exclusive lock. The recordings browser can read mid-write and either get a corrupt PNG or surface a GDI+ `IOException` to the UI. |

## 4. API / SignalR server edition

The headless edition is **roughly 70 % ready**. The critical hosted-service plumbing is in place (graceful shutdown, event broadcasting, lifecycle hooks), but several issues prevent unattended 24/7 operation. The WPF-side findings in §1–§3 also apply — the server uses the same `Linksoft.VideoEngine` and `Json…StorageService` code.

| # | Severity | Location | Issue |
|---|----------|----------|-------|
| 4.1 | SERVICE-HANG / SILENT-LOSS | `Api\Hubs\SurveillanceEventBroadcaster.cs:48,73` | `private async void OnRecordingStateChanged(...)` and `OnMotionDetected(...)`. Exceptions in `SendAsync` are unobserved; with a slow/disconnected client they vanish, and the broadcaster keeps queuing tasks. |
| 4.2 | MEM-LEAK | `Api\Services\ServerMotionDetectionService.cs:109` | Detection loop is `// TODO: Implement frame differencing`. Frames are captured and discarded but the loop runs at ~5 Hz per camera. With motion-recording wired to an event that never fires, motion-trigger features are inert and frame allocations are pure waste. |
| 4.3 | SECURITY | `Api\Program.cs:58` | `policy.SetIsOriginAllowed(_ => true)` — any browser can hit the SignalR hub and the REST API. Combined with plaintext credentials (4.4), this is exploitable. |
| 4.4 | SECURITY / SERVICE-HANG | `Api\Services\JsonCameraStorageService.cs:30,158–178` | RTSP credentials in plaintext JSON; `Load()` is one-shot — credential rotation requires a service restart (downtime). |
| 4.5 | SERVICE-HANG | `Api\Endpoints\StartRecordingHandler.cs:29–30` | Handler returns "success" before `pipeline.Open()` completes. RTSP failures surface only on the engine thread; the caller assumes the camera is recording. |
| 4.6 | MEM-LEAK | `Api\Services\StreamingService.cs:47–62,189–265` | HLS viewer counter is only decremented via explicit `StopStream`. A client that drops its socket leaves an FFmpeg transcoder running. Over a month of flaky Wi-Fi clients, hundreds of orphaned `ffmpeg` processes accumulate. |
| 4.7 | SERVICE-HANG | `Api\Endpoints\CaptureSnapshotHandler.cs:24` | `using var pipeline = pipelineFactory.Create(camera);` — if `Create` throws partway through, the RTSP slot can be left held; the camera then refuses subsequent snapshot requests until restart. |
| 4.8 | OPS-NOISE / LEAK | `Api\Services\CameraConnectionManager.cs` | Reconnect uses fixed 30 s interval, no exponential backoff. A dead camera generates ~2,880 failed reconnect log entries per day, ~1 M/year, plus the per-attempt `infoDict` / `fmtCtx` leaks (2.1, 2.2). |
| 4.9 | OPS / SHUTDOWN | Various hub methods | Hub methods accept `CancellationToken` but it is not threaded through long-running ops (e.g. the 30 s "wait for HLS playlist ready"). Client disconnect doesn't cancel server work. |

---

## Prioritized action list

The following ordering balances **likelihood × user impact**. P1 items should be fixed soon; P2 within the next sprint; P3 are improvements when convenient.

### P1 — fix before the next 24/7 deployment

1. **✅ DONE — Atomic JSON saves (3.1, 3.6, 4.4).** New `SafeJsonFile` helper in Core; 4 storage services refactored; 11 unit tests pass.
2. **✅ DONE — Wrap every `DispatcherTimer.Tick` handler in try/catch (1.3, 1.4).** All 3 dispatcher-timer sites in `MediaCleanupService`, `RecordingSegmentationService`, `RecordingService` now log-and-continue on tick exceptions.
3. **✅ DONE — Skip currently-recording files in `MediaCleanupService` (3.3).** Cleanup snapshots `IRecordingService.GetActiveSessions()` and skips the live `.mkv` and `.png`.
4. **✅ DONE (partial) — Guard `Directory.CreateDirectory` and `Directory.EnumerateFiles` in long-running paths (3.2, 3.4).** Cleanup enumeration now catches `IOException` / `UnauthorizedAccessException`. The `CreateDirectory` sites are already wrapped by outer service-level try/catches (no crash). Fallback-to-default-path deferred (changes user-visible behavior).
5. **✅ DONE — Free `infoDict` and `fmtCtx` on Demuxer error paths (2.1, 2.2).** `Demuxer.Open` now uses try/catch+finally with `CleanupOnOpenFailure`.
6. **Server-side `async void` event handlers (4.1).** Convert `OnRecordingStateChanged` / `OnMotionDetected` to `async Task` with logged-and-swallowed exceptions; avoid silent message loss and unobserved task leaks.
7. **Lock down CORS before any production exposure (4.3).** Replace `SetIsOriginAllowed(_ => true)` with an explicit allow-list, add SignalR auth. Required regardless of credential storage.

### P2 — fix within the next iteration

8. **✅ DONE — Bound `recordingCooldowns` and `postMotionTimers` (1.1, 1.8).** Cooldowns pruned opportunistically; postMotionTimers verified self-healing.
9. **✅ DONE — Robust segmentation slot detection (1.2).** `RecordingSlotCalculator` + 12 unit tests.
10. **✅ DONE — Thumbnail write atomicity (3.7).** Temp + `File.Move(overwrite: true)`.
11. **✅ DONE (presenter half) — `SwapChainPresenter.Present` recovery (2.4).** Device-lost HRESULTs detected, `IsDeviceLost` latched, `DeviceLost` event raised. Upstream pipeline subscriber to actually re-create the chain is deferred to P3.
12. **✅ DONE — Filename collision prevention (3.5).** `UniqueFilename.EnsureUnique` + ms timestamps + 7 unit tests.
13. **✅ DONE (server side) — Bound the motion-analysis fire-and-forget queue (1.7, 4.2).** Server loop no longer captures+discards frames; WPF side already had the `IsAnalyzing` guard.
14. **✅ DONE — `MotionDetectionService` scheduler iteration (1.5).** `contexts` and `scheduledCameras` now mutated together under `schedulerLock`.
15. **HLS viewer lease/heartbeat (4.6).** Per-viewer inactivity timeout (e.g. 30 s without a manifest poll → drop). Reaps orphaned FFmpeg transcoders automatically.
16. **`StartRecordingHandler` should await `Connected` (4.5).** Wait on `ConnectionStateChanged → Connected` (with a timeout) before returning success; or return an explicit "pending" state and let the client poll.
17. **Snapshot handler defensive disposal (4.7).** Wrap `pipelineFactory.Create` in try/catch and dispose on any exception path before re-throwing, so a single bad request doesn't strand an RTSP session.

### P3 — opportunistic

18. **✅ DONE — `GitHubReleaseService` lock release before HTTP (1.6).** Lock released before HTTP, 2 s acquisition timeout, 10 s `HttpClient.Timeout`.
19. **✅ DONE — Remuxer segment-boundary flush (2.5).** Atomic `SwitchTo` on Remuxer + `SwitchRecording` on `IMediaPipeline`/`VideoPlayer`, used by `RecordingService.SegmentRecording`.
20. **✅ DONE — VideoProcessor / GpuSnapshot allocation try/finally (2.3, 2.9).** `ReleasePipeline` / `FreeEncoder` runs on any failure during multi-step allocation.
21. **DEFERRED — Shared D3D11 device (2.7).** Single `ID3D11Device` factory shared across cameras; reduces VRAM pressure and `DEVICE_REMOVED` likelihood. Multi-class rearchitecture; per-camera ~80 MB acceptable for 4-camera reference workload. Revisit when camera count grows.
22. **✅ DONE — `FrameCapture` / `GpuSnapshotCapture` cleanup paths (2.6, 2.8).** Try/catch+finally wrap multi-step allocation; option dicts always freed; failures call `FreeContexts/FreeEncoder()`.
23. **✅ DONE — Exponential backoff in `CameraConnectionManager` (4.8).** `ReconnectBackoff.ComputeDelay` (Core, 11 tests) + per-camera `BackoffState` skips reattempt until the window elapses.
24. **✅ DONE — Thread `CancellationToken` through hub long-running ops (4.9).** `StartStream` accepts and honours the SignalR connection-aborted token.
25. **DEFERRED — Server-side credential storage (4.4).** Plaintext JSON kept for now to avoid breaking JSON-file portability and migration. Operational note: protect `cameras.json` via filesystem ACL; full DPAPI/secrets-store integration tracked separately.

---

## Cross-cutting recommendations

- **Standardize timer-tick wrappers.** Add a small helper, e.g. `DispatcherTimerExtensions.SafeTick(this DispatcherTimer t, Func<Task> action, ILogger logger)`, that catches and logs. Refactor every `Tick +=` site through it. This eliminates an entire class of crashes.
- **Add a "watchdog" hosted service.** Once a minute, verify that each active recording's file is growing on disk and the camera is delivering frames; if not, force a reconnect. Cheap insurance against silent stalls (DXGI device-removed, FFmpeg deadlock, NAS dropout).
- **Add disk-space guard.** Refuse to start new segments when free space < (single-segment-size × cameras × 2). Better than crashing the muxer mid-write.
- **Long-soak test in CI.** A nightly job that runs the engine against four mock RTSP sources for ~12 hours under chaos (network blips, slow disk) would have caught the thumbnail race well before the user's 7-hour run.
- **Memory/handle baselines.** Log process working set, GDI+ handle count, and `GC.GetTotalMemory` once per hour. Trend-over-time data makes leaks obvious before they become crashes.
