# Copilot Code References

This file documents the sources and inspirations for code generated or significantly modified by GitHub Copilot in this project.

## Chunk Spawning (`LevelGenerator.cs`, `ChunkEndTrigger.cs` concepts)

- **Source:** Code Monkey - Endless Runner Level Generator in Unity Tutorial
- **Link:** [https://www.youtube.com/watch?v=NtY_R0g8L8E](https://www.youtube.com/watch?v=NtY_R0g8L8E)
- **Notes:** The conceptual structure for the `LevelGenerator` script (managing chunk prefabs, spawning based on triggers) and the `ChunkEndTrigger` script (detecting the player and signaling the generator) was based on the patterns presented in this tutorial, adapted for the specific needs of this project.

## Isometric Movement Speed Correction (`PlayerController.cs` - `FixedUpdate` logic)

- **Source:** General Vector Mathematics Principles / Recall
- **Concepts:** `Vector2.Dot` product for projection, speed adjustment based on projection.
- **Notes:** The specific implementation using the dot product to maintain constant forward speed while allowing lateral steering in an isometric view was developed based on recalling general techniques for handling non-orthogonal movement vectors. It involves projecting the combined naive velocity onto the desired forward direction and scaling the forward component to compensate, ensuring consistent speed along that axis. No single external tutorial or article was directly referenced for this specific fix in the development log.