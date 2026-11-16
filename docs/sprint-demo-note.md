# Sprint demo note — KitchenOS

Reviewed commits from October 30–November 13, 2026, through `158ebe3` (implementation activity November 6–13). The three stories below are reconstructed from commits and current code; confirm their wording against the team's selected sprint backlog. Acceptance criteria are proposed demo checks, not recorded test results.

**Opening:** “This sprint we implemented the foundation for monitoring kitchen equipment: simulated devices, Redis messaging between services, and a browser dashboard receiving live telemetry.”

## Story 1 — Simulated kitchen equipment

**As an operator, I want simulated equipment data so I can demonstrate monitoring without physical hardware.**

**Implementation tasks**

- Implement simulated kitchen device types and read points.
- Load device definitions and simulation settings from JSON.
- Assign consistent device IDs, names, and descriptions.
- Generate and publish telemetry at configured intervals.
- Run the simulator as a separate background service.
- Add tests for device behavior, configuration loading, and simulator lifecycle.

- **AC to demonstrate:** Configured devices load with IDs, names, types, and read points; the simulator repeatedly publishes telemetry at each device's configured refresh interval.
- **Demo:** Show `/devices`, then the running simulator's telemetry output for several device types (for example, oven, sink, and freezer).
- **Commit evidence:** `80e6acc`, `b4db8ae` (devices/simulation); `5cbe378`, `6c08fdc` (separate simulator process); `4c2b22a` (ID fixes).

## Story 2 — Messaging between simulator and API

**As a system integrator, I want the simulator and API to exchange events through Redis so equipment telemetry reaches the monitoring service.**

**Implementation tasks**

- Implement `IMessageBus` and the Redis message bus.
- Define shared event envelopes and message contracts.
- Publish simulator telemetry to Redis.
- Subscribe to simulator events in the API.
- Route telemetry, alarm, and command-result events to handlers.
- Forward received events to connected WebSocket clients.

- **AC to demonstrate:** The separate simulator publishes on `kitchenos:events`; the API subscribes and forwards telemetry envelopes with the matching device ID to connected WebSocket clients.
- **Demo:** Trace one device's event from simulator output through Redis to a received `/ws` frame in browser developer tools.
- **Commit evidence:** `b2a9499` (message bus); `b601450` (shared contracts); `ef8c0b2` (event listener); `158ebe3` (event handlers).

## Story 3 — Live equipment dashboard

**As an operator, I want to view equipment and incoming readings in a browser so I can monitor the kitchen from one screen.**

**Implementation tasks**

- Create the equipment overview and device detail panels.
- Fetch the device list from the API.
- Allow operators to select equipment and view its readings.
- Connect the dashboard to the WebSocket endpoint.
- Update matching device readings when telemetry arrives.
- Display WebSocket connection status.

- **AC to demonstrate:** The dashboard loads equipment from `/devices`; selecting a card displays that device's details; incoming telemetry updates the matching device without refreshing; the socket indicator reports connection state.
- **Demo:** Open `http://localhost:3020`, select two devices, and show incoming readings alongside WebSocket frames. Values may repeat because the simulator uses configured fixture data.
- **Commit evidence:** `1178144` (WebSockets); `7cd5e3e`, `7340050` (dashboard and live integration).

## Built-app requirement — finish before the meeting

Use one laptop with Redis, the API, simulator, and frontend already running. Build and rehearse the exact commit beforehand; launch published Release binaries and the frontend production build from a terminal or packaged launcher, with the editor closed.

**Current gap:** Default Compose uses `dotnet watch` and `next dev`. The API has a production Docker stage, but the frontend and simulator Dockerfiles only provide development workflows. Prepare and verify a complete built-app launch before claiming release-candidate readiness. Check `/health`, `/devices`, and live browser telemetry on that build.

**Remaining demo preparation tasks**

- Build and launch production versions of the API, simulator, and frontend alongside Redis.
- Rehearse all acceptance criteria on one laptop with the editor closed.
- Prepare a brief start/stop/continue retrospective.

## Scope and retrospective

Do not present start/stop or mode controls as backend control: they currently change browser state only. Setpoint editing is unwired; alarms are placeholders. Alarm/command-result forwarding handlers do not establish a completed end-to-end feature. Test files were added (`fc7d9f5`) and updated (`2824a2e`); no tests or builds were run for this note.

Suggested **start/stop/continue** discussion: **Start** rehearsing a packaged release and checking AC before merging; **stop** leaving contract and device-ID mismatches until integration; **continue** focused feature PRs, simulator tests, and architecture documentation. Confirm these process observations with the team.
