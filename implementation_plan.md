# Goal Description

Expand "The Little Adventurers" from a prototype into a more complete game by implementing a full combat loop, RPG progression systems, persistent game state across scenes, and general environment polish. This includes utilizing the "RPG Monster DUO PBR Polyart" assets (Slime and Turtle Shell) for enemies on the Big Island.

## User Review Required

> [!IMPORTANT]  
> This plan outlines a significant amount of work spanning multiple systems. We can implement all of this in phases, or tackle specific sections first based on your priority. Please review the proposed changes and let me know if you agree with the direction!

## Open Questions

> [!WARNING]  
> 1. **Phase 1 Priority:** Should we prioritize Combat, RPG systems (Inventory/Dialogue), or Scene Management first? 
> 2. **Game Data:** How do you want to handle saving data? (e.g., simple `PlayerPrefs` for now, or a more robust JSON save system?)
> 3. **Spawning Mechanism:** For the Enemy Spawner on the Big Island, are you using Unity's NavMesh for pathfinding, or should I write a custom Raycast-based spawner to drop them onto the terrain?

## Proposed Changes

### Combat System & Enemy Spawning

Completing the combat loop so the player and the new RPG Monster DUO enemies can interact.

#### [NEW] Assets/Scripts/EnemySpawner.cs
- A manager script placed in the `BigIsland` scene to randomly spawn the `SlimePolyart` and `TurtleShellPolyart` prefabs within a defined radius or designated spawn zones.

#### [NEW] Assets/Scripts/EnemyHealth.cs
- Add health management for both the Slime and Turtle Shell enemies.
- Handle death logic (playing the respective death animations from the Polyart pack, destroying the GameObject, and triggering loot drops).

#### [NEW] Assets/Scripts/EnemyAI.cs
- Replaces the current `SlimeWander.cs` logic with a robust state machine that works for both the Slime and Turtle Shell prefabs.
- **Wander State:** Enemies roam randomly when idle.
- **Aggro State:** Enemies chase the player if they get within a certain radius.
- **Attack State:** Enemies trigger their specific attack animations from the Polyart animator controller and damage the player.

#### [MODIFY] Assets/Scripts/PlayerCombat.cs
- Update the hit detection to interface with `EnemyHealth.cs` and apply actual damage instead of just `Debug.Log`.
- Add hit-stun/knockback effects and trigger particle/sound effects on hit.

#### [MODIFY] Assets/Scripts/PlayerHealth.cs
- Update the `Die()` method to trigger a respawn sequence (e.g., reloading the `Village` scene or teleporting to a spawn point).

---

### RPG & Progression Systems

Adding reasons to explore and interact with the world.

#### [NEW] Assets/Scripts/InventorySystem/Inventory.cs
- Keep track of collected items (coins, gems, potions).
- Provide methods to add/remove items.

#### [NEW] Assets/Scripts/InventorySystem/Collectible.cs
- Attach to items in the world so they can be picked up by the player.

#### [NEW] Assets/Scripts/DialogueSystem/NPCDialogue.cs
- Simple interaction script for NPCs in the `Village` scene.
- Uses TextMeshPro to display text when the player is near and presses an interact button.

#### [MODIFY] Assets/Scripts/PlayerStats.cs (or new)
- Handle Experience Points (XP) and leveling up.

---

### Architecture & Scene Management

Ensuring the game state flows correctly between the `Village` and `BigIsland`.

#### [NEW] Assets/Scripts/Core/GameManager.cs
- A singleton marked with `DontDestroyOnLoad`.
- Keeps track of persistent data (player health, inventory, current quests) across scene loads.

#### [NEW] Assets/Scripts/UI/PauseMenu.cs
- Listens for the Escape key (via `InputReader.cs`).
- Sets `Time.timeScale = 0` to pause the game and brings up a UI Canvas with "Resume" and "Quit" buttons.

#### [MODIFY] Assets/Scripts/Environment/ScenePortal.cs
- Ensure it syncs player data with the `GameManager` before loading the new scene.

---

### Environment & Polish

#### [MODIFY] Assets/Scripts/ThirdPersonCamera.cs
- Add a SphereCast/Raycast from the player to the camera to detect walls.
- Adjust the camera distance dynamically to prevent clipping through the environment.

## Verification Plan

### Automated Tests
- N/A for these gameplay features.

### Manual Verification
- **Combat & Spawning:** Load `BigIsland` and verify the Spawner correctly instantiates the `SlimePolyart` and `TurtleShellPolyart` prefabs. Check that they wander, aggro, attack, and take damage.
- **Progression:** Pick up a collectible and verify the inventory count goes up. Talk to an NPC and verify dialogue shows up.
- **Persistence:** Travel from the `Village` to `BigIsland` after taking damage and collecting items; verify health and items remain exactly as they were.
- **Polish:** Stand near a wall and rotate the camera to ensure it doesn't clip through the wall.
