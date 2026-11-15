# BAGS EXPLORE — Architecture

## Identity
Educational lunar mission-engineering game for NASA Space Apps Challenge 2026.  
Unity 6 LTS, C#, local co-op 1–4 players, Windows + WebGL targets.

## Shared runtime (single sources of truth)
| System | Responsibility |
|---|---|
| `GameManager` | Scene/flow orchestration only. One instance. |
| `MissionState` | Shared mission configuration + live simulation values. |
| `LobbySession` | Local player seats, roles, ready, commander start. |
| `LocalMultiplayerBootstrap` / `LocalPlayerManager` | Input System join/leave bridge. |
| `MissionSimulationController` | Applies `MissionConfig` into rover/relay/science. |

Do **not** create a second GameManager or parallel mission state.

## Flow
Main Menu → Player Mode → Lobby (co-op) → Mission Selection → Intro → Design → Validation → Launch → Lunar Simulation → Results.

## UI contract
Cursor/UI scenes own visuals. Gameplay exposes bridges:
- `LobbyPresenterBridge`
- `HudPresenterBridge`

## Physics policy
Rover/relay use **simplified gameplay physics** with documented assumptions. Not verified orbital/vehicle models.

## Package policy
Only add packages when required (Input System is required). Avoid duplicate networking stacks.
