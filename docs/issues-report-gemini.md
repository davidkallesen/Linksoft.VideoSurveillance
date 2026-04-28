# Video Surveillance System - Findings & Performance Report

This report summarizes findings regarding bugs, performance bottlenecks, and architectural issues relevant to 24/7/365 multi-camera operation (10+ cameras).

## Status (2026-04-28)

| ID | Status | Notes |
|---|---|---|
| 2.1 GPU device sharing | DEFERRED | Tracked as P3.21 in `issues-report.md`; multi-class refactor with regression risk, per-camera ~80 MB acceptable for 4-camera workload. |
| 2.2 HLS CPU transcoding | OPEN | Stream-copy fallback / NVENC requires per-camera codec detection; tracked for next iteration. |
| 2.3 Synchronous remuxer disk I/O | OPEN | Background write queue is a substantial Remuxer refactor; tracked for next iteration. |
| 2.4 UI-thread DispatcherTimer | ✅ DONE | `MediaCleanupService` and `RecordingSegmentationService` switched to `System.Threading.Timer`; ticks fire even when UI is busy rendering many tiles. |
| 2.5 HLS reaper too lenient | ✅ DONE | Inactivity 2 min → 45 s, reaper 30 s → 10 s; `SurveillanceHub.OnDisconnectedAsync` now calls `StreamingService.OnConnectionDisconnected` to reap streams the closing connection owned. |
| 3.1 Server segmentation no-op | ✅ DONE | `ServerRecordingService.SegmentRecording` implemented using the atomic `IMediaPipeline.SwitchRecording`; mirrors WPF behavior minus thumbnails. |
| 3.2 Server media cleanup missing | ✅ DONE | New `ServerMediaCleanupBackgroundService` (`IHostedService` on a `System.Threading.Timer`) registered in `Program.cs`; shares logic with WPF via the Core helper `MediaCleanupRunner` (11 unit tests). |
| 3.3 Placeholder motion detection | DEFERRED | Frame-differencing is a feature task; service stub is non-allocating after the earlier P2.13 fix. |
| 4.1 Suboptimal viewing path | OPEN (architectural) | Recommendation noted; native-RTSP / WebRTC requires UX changes. |
| 4.2 Disconnected implementations | PARTIAL | Cleanup logic now shared via `MediaCleanupRunner`; segmentation slot logic shared via `RecordingSlotCalculator`. Full shared library still aspirational. |

## 1. High-Level Summary
The current architecture has significant gaps for 24/7/365 reliability, particularly on the server/service side (`Linksoft.VideoSurveillance.Api`). While individual components like the `VideoEngine` are well-structured, the orchestration layer lacks critical features for long-term autonomous operation without manual intervention or disk exhaustion.

## 2. Performance Bottlenecks

### 2.1 GPU Resource Exhaustion (Critical)
*   **Issue**: In `D3D11Accelerator`, every instance creates a new `D3D11Device` and `ID3D11Device`.
*   **Impact**: For 10+ cameras, this results in 10+ independent D3D11 devices. This leads to excessive GPU memory consumption, overhead from multiple context switches, and potential driver instability.
*   **Recommendation**: Implement a shared/singleton `D3D11Device` that is passed to all `VideoPlayer` instances.

### 2.2 CPU Overload in Streaming (High)
*   **Issue**: `StreamingService` (API) spawns a separate external FFmpeg process for every camera being viewed and performs CPU-based `libx264` transcoding.
*   **Impact**: Viewing 10+ cameras simultaneously will likely saturate the server's CPU. The `-preset ultrafast` helps but doesn't eliminate the fundamental cost of software encoding.
*   **Recommendation**: Use `stream copy` if possible, or employ GPU-accelerated encoders (NVENC/QuickSync).

### 2.3 Synchronous Disk I/O in Demux Thread (Medium)
*   **Issue**: `Remuxer.WritePacket` calls `av_interleaved_write_frame` synchronously within the `VideoPlayer` demux loop.
*   **Impact**: Slow disk I/O (especially on HDDs or during high-load cleanup) will block the demux thread. This can cause the RTSP buffer to overflow, leading to dropped frames or stream disconnects.
*   **Recommendation**: Use a background queue for disk writes to decouple capture from storage.

### 2.4 UI Thread Dependency (Medium)
*   **Issue**: `RecordingSegmentationService` and `MediaCleanupService` in the WPF app rely on `DispatcherTimer`.
*   **Impact**: If the UI thread is busy (e.g., rendering 10+ video streams), segmentation and cleanup tasks may be delayed. These are background tasks and should not be tied to the UI dispatcher.
*   **Recommendation**: Use `System.Threading.Timer` or a `BackgroundService` for these tasks.

### 2.5 HLS Session Reaper Delay (Medium)
*   **Issue**: `StreamingService` uses a 2-minute `InactivityTimeout` and a 30-second `ReaperInterval`.
*   **Impact**: When a user closes a dashboard with 10+ cameras, the CPU-intensive FFmpeg transcoding processes continue to run for up to 2.5 minutes. If multiple users do this or a user refreshes their browser multiple times, the server can quickly be overwhelmed by "zombie" transcoding processes.
*   **Recommendation**: Reduce the heartbeat/inactivity timeout or implement a more aggressive session termination via SignalR disconnect events.

## 3. Stability & Reliability Issues (24/7/365)

### 3.1 Missing Server-side Recording Segmentation (Critical)
*   **Issue**: `ServerRecordingService.SegmentRecording` is a no-op returning `false`.
*   **Impact**: Recordings on the server will grow into massive, single files. This makes them difficult to play back, prone to corruption if the service crashes, and hard to manage for cleanup.
*   **Recommendation**: Implement the segmentation logic in `ServerRecordingService`, matching the logic used in the WPF app.

### 3.2 Missing Server-side Media Cleanup (Critical)
*   **Issue**: `MediaCleanupService` is not registered or running in the `Linksoft.VideoSurveillance.Api` server.
*   **Impact**: Continuous recording on the server will eventually fill up the disk, causing the system to crash and potentially causing OS instability.
*   **Recommendation**: Register and run the cleanup service as a `BackgroundService` on the API server.

### 3.3 Placeholder Motion Detection (Functional Gap)
*   **Issue**: `ServerMotionDetectionService` contains a placeholder loop that does nothing.
*   **Impact**: No motion-based recording or alerts are possible on the server side.
*   **Recommendation**: Implement frame-differencing logic, ideally leveraging the existing `IVideoPlayer` decoding pipeline.

## 4. Architectural Findings

### 4.1 Suboptimal Viewing Path for Native Client
*   **Finding**: The WPF app views "Live" video by requesting an HLS stream from the API.
*   **Inefficiency**: RTSP (Camera) -> API (Decode/Encode to HLS) -> Disk (Segments) -> API (Web Server) -> WPF (Play HLS). This introduces significant latency (multi-second) and unnecessary server load.
*   **Recommendation**: Native clients (WPF) should connect directly to RTSP if on the same network, or use a lower-latency relay (like a raw RTSP proxy or WebRTC) if the API must be the middleman.

### 4.2 Disconnected Implementations
*   **Finding**: Logic for segmentation and cleanup exists in the WPF app but is missing or placeholder in the API.
*   **Impact**: The "Service" part of the system is not yet ready for production standalone recording.
*   **Recommendation**: Move shared service logic (Segmentation, Cleanup, Motion) into a shared library used by both the WPF app and the API server.

## 5. Summary Table for 10+ Cameras

| Component | Status | Performance for 10+ Cameras |
| :--- | :--- | :--- |
| **Decoding** | Good | Supports HW acceleration; per-camera threads are fine. |
| **GPU Rendering** | Poor | Per-camera D3D11 devices will exhaust resources. |
| **Recording** | Poor | Missing segmentation on server; synchronous I/O. |
| **Cleanup** | Critical | Missing on server; will cause disk exhaustion. |
| **Streaming** | Poor | Heavy CPU transcoding (x264) per viewer. |
| **Latency** | Poor | HLS usage in native client. |
