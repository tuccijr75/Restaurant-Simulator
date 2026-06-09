# 06 Realism Calibration

## Purpose

This document defines the first source-backed realism baseline for Restaurant Simulator.

The current working target is a **normal-volume burger/chicken quick-service restaurant with drive-thru**, because that archetype covers the shared operational systems Michael called out:

- burgers or chicken mains
- fries
- fountain drinks
- drive-thru
- front counter/lobby
- mobile pickup
- delivery
- prep/inventory
- staffing pressure
- food-safety checks

This file is not a brand manual. It uses public real-world data to create generic calibration ranges. Brand-specific operating assumptions are prohibited unless explicitly configured and approved.

## Core realism rule

Every number produced by the game must be one of:

1. directly sourced from public real-world data,
2. derived from public real-world data using documented math,
3. explicitly marked as `operator_calibration_required` until validated by a trusted operator/source.

The simulator must not present arbitrary constants as realistic.

## Current realism diagnosis

The previous functional build is not realism-valid because station load is cumulative. Orders add pressure, but completed tickets do not clear work from the board. This creates false overload.

Required correction:

```text
orders arrive
→ tickets enter station queues
→ station capacity processes work
→ completed tickets leave the active board
→ station load decreases
```

A board may overload only when active work-in-process and queue depth exceed station capacity for a sustained interval.

## Source-backed public data

### Annual sales and unit volume

Publicly reported major QSR averages provide the scale for daily sales and ticket counts:

- Chick-fil-A standalone restaurants: about `$9.3M` annual sales per restaurant.
- McDonald's U.S. locations: about `$3.7M` annual sales per restaurant.
- Taco Bell: about `$1.9M` annual sales per restaurant.
- Chick-fil-A drive-thru sales: about `60%` of revenue.
- Chick-fil-A can serve `100+` cars in peak hours.

Sources:

- Business Insider / Technomic reporting: https://www.businessinsider.com/how-chick-fil-a-makes-such-high-sales-drive-thru-2024-4

### Drive-thru service time

Public 2024-2025 drive-thru studies give usable service-time targets:

- 2024 average drive-thru total time: about `5.5 minutes`.
- 2024 Taco Bell total time: about `4.3 minutes`.
- 2024 Chick-fil-A total time: about `8 minutes`.
- 2025 Taco Bell total time: about `256.81 seconds`.
- 2025 reported average across major chains: about `5 minutes 35 seconds`.
- 2025 slowest reported chain: about `7 minutes 6 seconds`.

Sources:

- Washington Post / Intouch Insight 2024 analysis: https://www.washingtonpost.com/business/2025/03/28/wait-time-drive-through-restaurants/
- Food & Wine summary of QSR/Intouch Insight 2025 report: https://www.foodandwine.com/fastest-drive-thru-in-america-qsr-report-2025-11838795
- NY Post summary of QSR/Intouch Insight 2025 report: https://nypost.com/2025/10/15/lifestyle/fastest-fast-food-chains-in-the-us-ranked-by-drive-thru-speeds/

### Channel mix

Drive-thru remains the dominant QSR service channel:

- Intouch/Washington Post reported drive-thru share at about `63%`, down from COVID-era `83%`.
- Chick-fil-A reported drive-thru revenue share around `60%`.

Initial generic archetype target:

```yaml
channel_mix_target:
  drive_thru: 0.55-0.65
  front_counter: 0.12-0.20
  mobile_pickup: 0.10-0.18
  delivery: 0.05-0.12
```

Sources:

- Washington Post / Intouch Insight 2024 analysis: https://www.washingtonpost.com/business/2025/03/28/wait-time-drive-through-restaurants/
- Business Insider / Technomic reporting: https://www.businessinsider.com/how-chick-fil-a-makes-such-high-sales-drive-thru-2024-4

### Delivery time

Delivery should be modeled separately from in-store ready time:

```text
kitchen ready time + driver pickup wait + travel time = total delivery time
```

Public survey data:

- Customers start to get impatient with restaurant food delivery at about `29 minutes` when the restaurant is within `10 miles`.

Initial generic delivery calibration:

```yaml
delivery_time_target_seconds:
  kitchen_ready: 420-900
  total_customer_time: 1440-2100
  frustration_threshold: 1740
```

Source:

- NY Post / Talker Research survey: https://nypost.com/2024/10/31/lifestyle/how-long-should-food-delivery-take/

### Food-safety temperatures

FDA Food Code is the source of truth for safety temperatures.

Use these baseline safety gates:

```yaml
food_safety_temperature_targets:
  cold_holding_max_f: 41
  hot_holding_min_f: 135
  ground_or_non_intact_meat_min_f: 155
  poultry_min_f: 165
  cooling_135_to_70_max_hours: 2
  cooling_135_to_41_total_max_hours: 6
```

Source:

- FDA Food Code 2022: https://www.fda.gov/media/164194/download?attachment=

### Fries

Fries are required in this archetype because burger/chicken QSR formats generally share fries as a high-volume side item. Fries must be modeled as a distinct station/inventory item because they drive fryer load, hold-quality risk, and rush timing.

Publicly sourced facts:

- Frozen/pre-cut/blanched fries are widely used in fast-food and restaurant operations.
- Standard fry preparation is deep frying in hot fat/oil.
- Two-stage fry methods commonly use a lower-temperature blanch and a higher-temperature finish fry.
- Restaurant-style finish fry ranges found in public culinary references commonly fall in the `1-3 minute` range depending on fry cut, product, and method.

Initial simulation handling:

```yaml
fries:
  station: fryer
  cook_seconds_target: 120-210
  quality_hold_minutes: operator_calibration_required
  safety_class: usually_non_tcs_when plain fries, but local rules/product toppings may vary
```

Sources:

- French fry preparation overview: https://en.wikipedia.org/wiki/French_fries
- EatingWell restaurant fries/blanching discussion: https://www.eatingwell.com/article/8003960/why-french-fries-taste-better-at-a-restaurant/

### Drinks

Drinks are required in this archetype because burger/chicken QSR formats generally share fountain beverages as a high-frequency order component. Drinks should be modeled separately from kitchen hot-food work because they load counter/expo/service capacity, not grill/fryer capacity.

Publicly sourced facts:

- Fast-food systems commonly include soft drinks/fountain beverages.
- Coca-Cola Freestyle and similar fountain systems are deployed in chain restaurants and can dispense many beverage choices from one machine footprint.

Initial simulation handling:

```yaml
drinks:
  station: beverage_expo
  service_seconds_target: derived_from_drive_thru_total_time
  drink_attachment_rate: operator_calibration_required
  safety_class: non_tcs for standard fountain drinks
```

Sources:

- Coca-Cola Freestyle restaurant fountain context: https://en.wikipedia.org/wiki/Coca-Cola_Freestyle
- Fast-food restaurant common menu context: https://en.wikipedia.org/wiki/Fast-food_restaurant

## Derived baseline: normal-volume burger/chicken QSR

Use McDonald's-like normal-volume annual unit volume as the first calibration center, without hardcoding the brand.

```yaml
normal_volume_qsr:
  annual_sales_target_usd: 3000000-4500000
  daily_sales_target_usd: 8200-12300
  average_check_target_usd: 10-12
  ticket_count_daily_target: 680-1230
  operating_hours_model: 18
  average_tickets_per_hour: 38-68
  peak_tickets_per_hour: 80-140
  peak_tickets_per_30_min: 40-70
```

Math basis:

```text
$3.0M/year / 365 = ~$8.2K/day
$4.5M/year / 365 = ~$12.3K/day
$8.2K/day / $12 avg check = ~683 tickets/day
$12.3K/day / $10 avg check = ~1,230 tickets/day
```

## Daypart distribution baseline

The first baseline uses a realistic QSR-shaped distribution, not a flat trickle.

```yaml
daypart_ticket_share:
  breakfast: 0.18
  mid_morning: 0.07
  lunch: 0.30
  afternoon: 0.10
  dinner: 0.30
  late_night: 0.05
```

Status: `operator_calibration_required` for exact percentages by store type, but shape is required: breakfast, lunch, and dinner must be visibly distinct.

## Required simulator changes

1. Replace cumulative station load with active work queues.
2. Add ticket completion and board clearing.
3. Track `created`, `queued`, `in_progress`, `ready`, `handed_off`, and `completed` ticket states.
4. Process station work by capacity per simulated minute.
5. Generate `ticket.updated` when status changes.
6. Compute sales per 30 and 60 minutes.
7. Compute tickets per 30 and 60 minutes.
8. Compute channel-specific service times.
9. Treat fries and drinks as first-class menu/station work contributors.
10. Keep food safety separate from food quality.

## Acceptance criteria

A normal simulated full day is not realism-valid until:

```yaml
realism_acceptance:
  tickets_per_day_within_target: true
  peak_30_min_ticket_count_within_target: true
  drive_thru_total_time_within_target: true
  active_board_clears_when_capacity_exceeds_arrivals: true
  overload_requires_active_queue_breach: true
  recovery_requires_queue_clearance_or_capacity_improvement: true
  fries_contribute_to_fryer_load: true
  drinks_contribute_to_beverage_or_expo_load: true
  hot_and_cold_hold_targets_match_food_code: true
  arbitrary_constants_labeled_or_removed: true
```

## Security and boundary notes

- All calibration values are for synthetic simulation.
- Do not import real POS, staffing, or customer data without explicit approval.
- Do not use individual employee performance scoring.
- Do not hardcode Wendy's, McDonald's, Chick-fil-A, Taco Bell, or any other brand as the simulator default.
- Public brand data may inform generic calibration ranges only.
