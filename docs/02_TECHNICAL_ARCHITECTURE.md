# 02 Technical Architecture

## Purpose

Official technical architecture for Restaurant Simulator.

## Stack

- Godot 4.
- C# / .NET.
- Windows desktop executable.
- JSON configs.
- JSONL event streams.
- SQLite for local saves and receipts.
- Local HTTP/WebSocket adapter for optional ASC integration.

## Layers

1. Godot UI and scenes.
2. C# simulation core.
3. POS module.
4. KDS module.
5. Inventory and prep module.
6. Staffing and labor module.
7. Food safety module.
8. Customer demand module.
9. Equipment/weather/traffic module.
10. ASC adapter module.

## Rule

The game must run without ASC. ASC can observe state and submit recommendations, but cannot be required for gameplay.

## Data

All run data is synthetic unless explicitly approved otherwise.
