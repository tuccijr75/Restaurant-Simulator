# 02 Technical Architecture

## Purpose

This file defines the official technical architecture for Restaurant Simulator.

## Locked stack

- Engine: Godot 4.
- Language: C# / .NET.
- Target: Windows desktop executable.
- Config: JSON scenario and tuning files.
- Replay/export: JSONL operational event streams.
- Local persistence: SQLite when needed.
- ASC adapter: local HTTP and WebSocket only until explicitly