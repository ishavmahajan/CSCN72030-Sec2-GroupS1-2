# Sprint 2 demo note — KitchenOS

**Review date:** November 20, 2026. **Commit range:** after the previous demo baseline `158ebe3` through `08f7dfc` (November 16–20 changes). Stories below are reconstructed from committed code; acceptance criteria are demo checks, not verified results. Frontend TypeScript checks and a production build using Webpack passed during merge verification. Backend tests were attempted but blocked by missing NuGet packages; the default Turbopack build was blocked by local worker port permissions. End-to-end demo checks still require rehearsal.

**Opening:** “Last demo, we showed simulated equipment and live monitoring. This sprint, we connected operator controls to the simulator: users can submit commands, change supported settings, see whether the equipment accepted the request, and receive simulated equipment alarms labelled with device names.”

## Story 1 — Start and stop equipment remotely

**As a kitchen operator, I want to start and stop supported equipment so I can control it from the monitoring screen.**

**Implementation tasks**

- Add `POST /devices/{deviceId}/commands` and validate requests.
- Generate command IDs and return HTTP 202 with a queued receipt.
- Publish commands through Redis and subscribe in the simulator.
- Invoke the target device operation and publish a correlated result.
- Connect supported dashboard controls to the command API.

**AC to demonstrate:** A valid command receives a queued receipt; the simulator returns an acknowledgement with the same command ID; the intended device's reported state changes. An unknown device returns 404 and malformed requests return 400.

**Demo:** Select the mixer, stop it, then start it. Show the acknowledgement and motor state. Use “Request Current Status” to obtain fresh telemetry after each action; the normal refresh interval may delay readings. Optionally show an invalid-device API request.

**Commit evidence:** `f938ac9`, `b0c4cf1`, `834ae21`, `4c6547b`; mixer test expectation corrected in `29e816f`.

## Story 2 — Change equipment modes and setpoints

**As a kitchen operator, I want to change supported modes and target settings so I can adjust equipment operation.**

**Implementation tasks**

- Implement device-specific writable values and modes.
- Define available controls, units, options, and numeric limits in device configuration.
- Build mode selectors and value-entry controls.
- Send `setMode` and `setValue` requests through the command pipeline.
- Extend simulated-device tests for the new behavior.

**AC to demonstrate:** A supported mode and setpoint can be submitted and acknowledged; fresh telemetry shows the updated values. The UI prevents submission of values outside its configured limits. Controls match the selected device's configured capabilities.

**Demo:** Select the oven, change its mode to Roast, and set a target temperature within 50–220 °C. Request fresh status and show both values. Enter an out-of-range target to demonstrate UI validation.

**Commit evidence:** `1738235`, `4c6547b`.

## Story 3 — Know whether a command succeeded

**As a kitchen operator, I want clear command feedback so I know whether an action was queued, accepted, rejected, or timed out.**

**Implementation tasks**

- Correlate WebSocket command results with HTTP receipt IDs.
- Display queued, success, and error notifications.
- Prevent another submission to the same device while a command is pending.
- Handle results that arrive before the HTTP receipt.
- Clear pending state on failure or after the 15-second timeout.

**AC to demonstrate:** A submitted command produces feedback tied to the correct device; acknowledgement produces success feedback. API failure or missing simulator acknowledgement produces an error or timeout, and controls become available again.

**Demo:** Show a successful command notification. Then stop only the simulator, submit another command, and show the timeout after approximately 15 seconds. Restart the simulator and confirm a new command succeeds. Rehearse this recovery before the meeting.

**Commit evidence:** `4c6547b`; command validation, bus-failure, and correlated-result tests added in `f938ac9` and refined in `b0c4cf1`.

## Story 4 — Identify equipment raising live alarms

**As a kitchen operator, I want live alarms to identify the equipment by name so I can quickly locate the device requiring attention.**

**Implementation tasks**

- Load device-type alarm definitions from `alarm.json` separately from device configuration.
- Select a random loaded device and an applicable configured alarm.
- Publish one alarm every 20 seconds through Redis and the API alarm handler.
- Display alarm text, code, severity, and timestamp in the dashboard.
- Resolve the event's device ID to its equipment name, with the ID as fallback.
- Keep the latest five alarm entries and allow local dismissal.
- Add tests for alarm loading and publication; preserve command controls during the development merge.

**AC to demonstrate:** Within approximately 20 seconds of simulator startup, a configured alarm reaches the dashboard without refreshing. Its equipment label matches the device name, and its code, text, severity, and timestamp are visible. Further alarms appear on subsequent timer ticks. Dismiss removes the selected entry locally; command controls continue working.

**Demo:** Run the simulator and wait for an alarm. Match the displayed name to an equipment card, inspect its details, then dismiss it. Send a device command afterwards to demonstrate alarms and command acknowledgements coexist.

**Commit evidence:** `1ba23c1`, `22380c5` (alarm generation and display); `a01d634` (development merge and device-name labels); merged into development in `08f7dfc`.

**Scope:** These are randomly selected test alarms, not faults detected from telemetry thresholds. Dismissal is browser-local and does not acknowledge or clear a persisted alarm. The displayed count covers the retained entries, not a durable active-alarm register.

## Supporting work and limits

- **PostgreSQL environment:** Added a Compose service, persistent volume, readiness check, API connection configuration, and `.env.example` (`0f7b020`, `1107f91`). This is database infrastructure; EF Core models, migrations, and history persistence are not present in the reviewed API.
- **Alarm scenarios:** Initial definitions were added in `9cb48fd`; their live publication and display are covered by Story 4. Persistent alarm history and acknowledgement workflows remain outside the delivered scope.
- **Device coverage:** Stove controls are marked on hold in frontend configuration. Demonstrate supported equipment, rather than claiming every device supports every command.
- **Validation scope:** Numeric limits are configured in the UI; the API validates command names, device/component/point existence, and value types. Do not claim comprehensive server-side range or mode validation.

## Built-app preparation

Use one laptop and the exact rehearsed commit. Run compiled Release API/simulator binaries and a production frontend build, with the editor closed. Default Compose still uses `dotnet watch` and `next dev`, so it does not establish release-candidate readiness.

Before the demo: run the backend test suite and frontend build, confirm `/health`, `/devices`, WebSocket telemetry, commands, alarm delivery/device-name labels, and timeout recovery. Configure PostgreSQL environment values if using Compose. Record the results against the candidate commit; backend test success and full runtime readiness are not established by this note.

## Quick retrospective

Suggested discussion prompts: **Start** rehearsing acceptance criteria against a built release and checking command/result correlation. **Stop** treating configuration files or database containers as completed end-to-end features. **Continue** focused PRs, shared contracts, and device/command regression tests. Confirm the team's actual experience during the discussion.
