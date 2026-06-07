# 01 Product Spec

## Product identity

Restaurant Simulator is a standalone Windows desktop restaurant-operations simulation game. It models realistic fast-food restaurant decisions, constraints, and outcomes while also producing deterministic data for future AI Shift Commander testing.

## Primary goals

1. Let a player operate a realistic fast-food restaurant shift.
2. Simulate the operational conditions faced by restaurant operators and floor managers.
3. Support adjustable scenarios that replicate real-world restaurant conditions.
4. Generate replayable event/state data for AI Shift Commander validation.
5. Function fully without AI Shift Commander.

## Target platform

- Windows desktop executable.
- Built in Godot 4.
- C# / .NET as official simulation and game logic language.

## Core game loop

1. Load scenario.
2. Start simulated business day or shift.
3. Customers arrive through drive-thru, front counter, mobile, and delivery channels.
4. POS receives orders.
5. KDS routes tickets to stations.
6. Player manages staffing, prep, inventory, equipment, sanitation, food safety, and customer recovery.
7. Events affect SOS, quality, waste, labor, satisfaction, and inspection risk.
8. End-of-shift report grades performance.
9. Optional ASC mode compares ASC recommendations against actual outcomes.

## Required systems

- POS system.
- KDS system.
- Customer demand engine.
- Drive-thru, front-counter, mobile, and delivery service channels.
- Inventory and prep control.
- Cook times, hold times, and quality decay.
- SOS timers by channel.
- Staffing, scheduling, breaks, call-offs, and reassignment.
- Labor percent controls by role type.
- Synthetic staff trait variation.
- Equipment condition and maintenance.
- Weather, traffic, local events, school events, and holiday effects.
- Health inspections.
- Sanitation tasks.
- Temperature controls.
- Customer satisfaction.
- Pass/fail scenario outcomes.
- Replay/export layer for ASC testing.

## Staff role types

- Crew member.
- Team leader.
- Shift manager.
- Assistant manager.
- Restaurant manager.

Each role has configurable labor cost, availability, station permissions, management authority, and operational effects. Role modeling must not become employee scoring.

## Required service channels

- Drive-thru.
- Front counter / lobby.
- Mobile pickup.
- Delivery marketplace style channel.

Use generic delivery providers in simulation. Do not require real DoorDash, Uber Eats, or other live marketplace integration for core gameplay.

## Player decisions

The player must make decisions including:

- Assign staff to stations.
- Adjust breaks.
- Start prep.
- Discard expired/unsafe product.
- Recover customer complaints.
- Mark items unavailable.
- React to equipment failures.
- Control labor.
- Maintain sanitation.
- Complete temperature checks.
- Respond to health inspections.
- Manage rushes and bottlenecks.
- Balance speed, quality, labor, and safety.

## Pass/fail dimensions

- Sales target.
- Labor percent.
- Food cost and waste.
- Drive-thru SOS.
- Front-counter SOS.
- Delivery ready time.
- Order accuracy.
- Customer satisfaction.
- Inventory control.
- Prep execution.
- Staffing coverage.
- Break compliance.
- Sanitation completion.
- Temperature compliance.
- Health inspection outcome.

## Game modes

- Sandbox Mode: all major variables adjustable.
- Scenario Mode: curated challenge shifts.
- ASC Test Mode: ASC observes and recommends while the sim logs evaluation data.
- Replay Mode: same seed can be replayed for baseline, human-only, ASC-assisted, and rule-based runs.

## Definition of done for product V1

V1 is done when the game can run a full synthetic shift with working POS, KDS, inventory/prep, staffing/labor, service-time metrics, sanitation/temp checks, equipment conditions, customer satisfaction, and end-of-shift scoring.
