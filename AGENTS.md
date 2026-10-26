# Break & Drop — Codex Project Guide

## Mission

Build a small, deterministic Unity 2D physics puzzle before expanding the product. The player removes supports so the hex target reaches the goal safely.

## Product invariants

- The complete core interaction is a single tap/click.
- Difficulty comes from support relationships and order, not input complexity.
- The same click sequence should produce materially the same outcome in repeated runs.
- Every click must have immediate visual feedback and a readable physical consequence.
- A failure must be understandable from the resulting motion.
- Do not add Meta, currencies, ads, accounts, or online services before the core prototype is validated.

## Engineering rules

- Target Unity `2022.3.13f1c1` and 2D physics.
- Keep runtime code under `Assets/BreakDrop/Scripts/Runtime` and editor-only code under `Assets/BreakDrop/Editor`.
- Store authored levels as JSON under `Assets/BreakDrop/Resources/Levels` until a production content pipeline replaces it.
- Avoid scene-authored gameplay dependencies: the bootstrap should be able to recreate the prototype from data.
- Prefer small components and explicit game-state transitions over a monolithic manager.
- Preserve replayability and deterministic testing when changing physics parameters.
- After script changes, compile in Unity and check the Console before declaring the work complete.

## Current acceptance checks

1. Project opens without compile errors.
2. Prototype scene exists or can be generated from the editor menu.
3. Clicking a breakable block removes its collider and produces feedback.
4. The target settles after each move before another move is accepted.
5. Reaching the goal produces Win; leaving bounds produces Fail.
6. Undo restores the target and all blocks to the pre-click state.
7. Hint highlights the next authored block without completing the move.
8. Restart reconstructs the current JSON level.
9. Next loads the next available prototype level.

## Work sequence

Read `docs/GDD.md`, then `docs/ROADMAP.md`. Keep work scoped to the active milestone. If a request conflicts with a product invariant, surface the tradeoff before changing the invariant.
