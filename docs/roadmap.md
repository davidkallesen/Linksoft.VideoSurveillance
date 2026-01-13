# Linksoft.CameraWall Feature Roadmap

## Vision

A professional-grade WPF camera wall application for live monitoring of multiple RTSP/HTTP camera streams with an intuitive user interface and robust performance.

---

## Phase 1: MVP (Minimum Viable Product) - COMPLETED

**Goal**: Core functionality for basic live monitoring

### Core Features

| Feature | Description | Status |
|---------|-------------|--------|
| Dynamic Grid Layout | Auto-calculate optimal grid based on camera count (1-30 cameras) | Done |
| Camera Tile Overlay | Title/description overlay in configurable corner | Done |
| Context Menu | Edit, Delete, Full Screen, Swap Left/Right, Snapshot, Reconnect | Done |
| Drag-and-Drop | Reorder cameras by dragging tiles within grid | Done |
| Auto-Save | Automatic persistence of camera positions | Done |

### Camera Management

| Feature | Description | Status |
|---------|-------------|--------|
| Add Camera Dialog | Configure RTSP/HTTP camera with credentials | Done |
| Network Scanner | Auto-discover cameras on local network (integrated in dialog) | Done |
| Edit Camera | Edit camera configurations via context menu | Done |
| Delete Camera | Delete with confirmation via context menu | Done |
| Test Connection | Validate camera connectivity before saving | Done |

### Layout Management

| Feature | Description | Status |
|---------|-------------|--------|
| Named Layouts | Create and save multiple layout configurations | Done |
| Startup Layout | Designate a layout to load on application start | Done |
| Layout Persistence | JSON-based storage in AppData | Done |
| Default Layout | Auto-create default layout if none exists | Done |

### UI/UX

| Feature | Description | Status |
|---------|-------------|--------|
| Fluent.Ribbon Menu | Modern ribbon interface with tabs | Done |
| Dark/Light Theme | Theme toggle with Atc.Wpf.Theming | Done |
| Snapshot Capture | Save current frame as image file | Done |

### Technical

| Feature | Description | Status |
|---------|-------------|--------|
| FlyleafLib Integration | FFmpeg-based video streaming | Done |
| Connection State | Visual indicators (Connected, Connecting, Error) | Done |
| Thread-safe Initialization | SemaphoreSlim for engine initialization | Done |

---

## Phase 2: Enhanced User Experience

**Goal**: Improved usability and reliability

### Settings & Configuration

| Feature | Description | Status |
|---------|-------------|--------|
| Settings Dialog | Configure theme, language, startup behavior | Done |
| Display Settings | Configure overlay visibility and opacity | Done |
| Auto-Save Option | Toggle automatic layout persistence | Done |
| Snapshot Directory | Configurable save location for snapshots | Done |
| Start Maximized | Option to start app maximized | Done |
| Ribbon Collapsed | Option to start with ribbon collapsed | Done |

### Keyboard Navigation

| Feature | Description | Status |
|---------|-------------|--------|
| Arrow Keys | Navigate between camera tiles | Planned |
| F11 | Toggle fullscreen mode | Planned |
| Escape | Exit fullscreen, close dialogs | Done |
| Number Keys (1-9) | Quick jump to camera position | Planned |
| Ctrl+S | Save current layout | Planned |

### Connection Reliability

| Feature | Description | Status |
|---------|-------------|--------|
| Auto-Reconnection | Exponential backoff (5s, 10s, 20s, max 120s) | Planned |
| Health Monitoring | Periodic ping checks | Planned |
| Connection Timeout | Configurable timeout settings | Planned |
| Retry Limits | Maximum reconnection attempts | Planned |
| Connect on Startup | Option to auto-connect cameras on app start | Done |

### Enhanced Overlays

| Feature | Description | Status |
|---------|-------------|--------|
| Timestamp Overlay | Optional live timestamp on each tile | Done |
| Overlay Opacity | Configurable background opacity (0.0-1.0) | Done |
| Overlay Position | Choose corner (TopLeft, TopRight, BottomLeft, BottomRight) | Done |
| Stream Statistics | Frame rate, resolution, bitrate display | Planned |

### Data Management

| Feature | Description | Status |
|---------|-------------|--------|
| Layout Import/Export | JSON file for sharing configurations | Planned |
| Camera Status Tooltips | Hover shows IP, status, uptime | Planned |
| Configuration Backup | Export all settings to file | Planned |

### Help & Information

| Feature | Description | Status |
|---------|-------------|--------|
| About Dialog | Application version and information | Done |
| Check for Updates | GitHub-based version checking | Done |

---

## Phase 3: Advanced Features

**Goal**: Professional monitoring capabilities

### Recording

| Feature | Description | Status |
|---------|-------------|--------|
| Manual Recording | Start/stop recording per camera | Planned |
| Recording Format | MP4/MKV with H.264 passthrough | Planned |
| Storage Management | Disk space monitoring, auto-cleanup | Planned |
| Recording Indicator | Visual indicator when recording active | Planned |

### Display Modes

| Feature | Description | Status |
|---------|-------------|--------|
| Full Screen Camera | Single camera in full screen window | Done |
| Picture-in-Picture | Floating always-on-top window | Planned |
| Digital Zoom | Pause and zoom on frame (2x, 4x, 8x) | Planned |
| Region Selection | Click-drag to zoom to area | Planned |

### Audio

| Feature | Description | Status |
|---------|-------------|--------|
| Per-Camera Audio | Mute/unmute individual streams | Planned |
| Master Volume | Global volume control | Planned |
| Audio Focus | Only selected camera plays audio | Planned |

### Performance

| Feature | Description | Status |
|---------|-------------|--------|
| Hardware Acceleration | GPU decoding (D3D11VA, DXVA2) | Planned |
| Adaptive Quality | Auto-adjust based on system resources | Planned |
| Memory Optimization | Efficient stream management | Planned |

---

## Phase 4: Professional Features

**Goal**: Enterprise-grade functionality

### Multi-Monitor

| Feature | Description | Status |
|---------|-------------|--------|
| Multi-Window Mode | Separate windows for different monitors | Planned |
| Monitor Assignment | Drag cameras to specific monitors | Planned |
| Layout Per Monitor | Independent layouts per display | Planned |

### PTZ Control

| Feature | Description | Status |
|---------|-------------|--------|
| ONVIF Integration | Pan/Tilt/Zoom control protocol | Planned |
| On-Screen Controls | Virtual joystick overlay | Planned |
| Preset Positions | Save and recall camera positions | Planned |
| PTZ Tour Mode | Automated position cycling | Planned |

### Intelligence

| Feature | Description | Status |
|---------|-------------|--------|
| Motion Detection | Simple frame comparison algorithm | Planned |
| Visual Alerts | Border flash, notification badge | Planned |
| Sensitivity Config | Threshold and area settings | Planned |

### Organization

| Feature | Description | Status |
|---------|-------------|--------|
| Camera Grouping | Hierarchical folder structure | Planned |
| Group Views | Display all cameras in a group | Planned |
| Group Operations | Bulk enable/disable, configuration | Planned |

### Administration

| Feature | Description | Status |
|---------|-------------|--------|
| Event Logging | Connection events, user actions | Planned |
| Audit Trail | Configuration change history | Planned |
| Report Export | CSV/PDF report generation | Planned |
| Full Backup/Restore | All settings in single file | Planned |

---

## Phase 5: Distribution

**Goal**: Production-ready deployment

### Installer

| Feature | Description | Status |
|---------|-------------|--------|
| WiX5 MSI Installer | Professional Windows installer | Planned |
| Silent Install | Command-line deployment option | Planned |
| Update Checking | Check for new versions on GitHub | Done |
| Update Installation | Download and install updates | Planned |
| Uninstaller | Clean removal of all components | Planned |

### NuGet Package

| Feature | Description | Status |
|---------|-------------|--------|
| Linksoft.Wpf.CameraWall | Reusable library package | Planned |
| Documentation | XML docs and README | Planned |
| Samples | Example usage projects | Planned |
| Symbol Package | Debugging support | Planned |

---

## Technical Debt & Maintenance

### Ongoing

| Task | Description |
|------|-------------|
| Dependency Updates | Keep NuGet packages current |
| Security Patches | Address vulnerabilities promptly |
| Performance Profiling | Regular optimization reviews |
| Code Quality | Maintain analyzer compliance |

### Documentation

| Task | Description |
|------|-------------|
| API Documentation | XML documentation for library |
| User Guide | End-user documentation |
| Developer Guide | Integration documentation |
| Release Notes | Version changelog |

---

## Success Metrics

### Phase 1 (MVP) - Achieved
- Successfully display 30 simultaneous RTSP streams
- < 5 second stream initialization time
- Configuration persists across restarts
- Drag-and-drop works smoothly

### Phase 2 (Enhanced) - Partially Achieved
- Settings dialog with full theme and display configuration ✓
- Configurable overlay (position, opacity, timestamp) ✓
- Full screen camera mode ✓
- GitHub update checking ✓
- Auto-reconnection succeeds > 95% of the time (pending)
- Keyboard navigation fully functional (pending)

### Phase 3 (Advanced)
- Recording maintains stream quality
- Hardware acceleration reduces CPU by 50%+
- Snapshots save in < 500ms

### Phase 4 (Professional)
- PTZ control responds in < 200ms
- Motion detection < 5% false positive rate
- Multi-monitor operates independently

---

## Research & References

### Video Libraries
- [FlyleafLib](https://github.com/SuRGeoNix/Flyleaf) - Selected, FFmpeg-based
- [LibVLCSharp](https://github.com/videolan/libvlcsharp) - Alternative option

### Surveillance Software
- [GeoVision Video Wall](https://www.geovision.com.tw/product/GV-Video%20Wall)
- [Camera.ui](https://github.com/seydx/camera.ui)
- [Agent DVR](https://www.ispyconnect.com/)

### UX Patterns
- [Drag-and-Drop UX Guidelines](https://smart-interface-design-patterns.com/articles/drag-and-drop-ux/)

### Protocols
- [RTSP Streaming Protocol Guide](https://www.videosdk.live/developer-hub/rtmp/rtsp-streaming-protocol)
- [ONVIF Specification](https://www.onvif.org/)
