# System Contracts

## MissionConfig → MissionState
Engineering/design UI produces `MissionConfig`.  
Call `GameManager.ApplyDesignedMission(config)` or `MissionState.ApplyMissionConfig(config)`.  
Gameplay **reads** this config; it must not invent a second config store.

## LobbySession
- `TryAddPlayer` / `RemovePlayer`  
- `ClaimRole` / `AutoAssignRoles` / `CyclePlayerRole`  
- `SetPlayerReady` / `CanCommanderStart` / `TryCommanderStart`  
Seat cap: `LobbySession.MaxPlayers` (4).  

## PlayerInputManager
Installed Input System exposes `maxPlayerCount` as **read-only**.  
Do not assign it at runtime. Enforce seat limits in `LobbySession`.

## SessionPlayMode
Enum name avoids clash with `UnityEngine.PlayMode`.
