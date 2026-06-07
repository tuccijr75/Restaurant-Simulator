# 04 ASC Integration Spec

## Purpose

Define how Restaurant Simulator will support AI Shift Commander testing.

## Core rule

The game must work fully without ASC. ASC is optional.

## Integration mode

- Local HTTP for state snapshots.
- Local WebSocket for live events.
- JSONL for replay export.
- JSON for recommendation intake.

## ASC can observe

- Orders.
- KDS tickets.
- Station load.
- Inventory.
- Prep.
- Waste.
- Staff coverage.
- Labor percent.
- SOS metrics.
- Sanitation.
- Temperatures.
- Equipment.
- Customer satisfaction.
- Weather.
- Traffic.
- Local events.

## ASC can recommend

- Prep more.
- Shift staff.
- Monitor queue.
- Manual count.
- Check temperature.
- Change sanitizer.
- Wipe boards.
- Mark item unavailable.
- Address equipment issue.
- Recover complaint.

## Evaluation

The game logs whether each recommendation was useful, late, ignored, accepted, rejected, false positive, or false negative.

## Boundary

ASC must not be required for gameplay. ASC must not score employees or make HR judgments.
