# Belfalas API Surface

> Wave 0 froze this contract surface; later waves implement it. Eras + quest authoring
> (Waves 1–2) and the progression engine (Wave 3) are live. Routes for the world model
> (Wave 4) still return `501 Not Implemented`.

## Health

- `GET /health`

## Eras

- `GET /api/eras`
- `GET /api/eras/active`
- `GET /api/eras/{eraId}`
- `POST /api/eras`
- `POST /api/eras/{eraId}/archive`

Era creation/detail/summary carry `xpPerLevel` (flat XP cost of one level, shared by every
area; defaults to `100`). Areas progress `0..50`.

## Quests

- `GET /api/quests/daily` — today's daily habits (Europe/Madrid) with a `completed` flag.
- `GET /api/quests/weekly` — the current week's drawn goal set, each with a `completed` flag.
- `POST /api/quests/daily/{dailyHabitId}/complete` — mark done for today; credits the area's
  XP and levels up immediately on threshold. Idempotent. Body `{ "completedOn": "<date>" }`
  is optional and, if given, must be today. Returns the completion outcome (XP/level delta).
- `DELETE /api/quests/daily/{dailyHabitId}/complete` — undo today's completion, reverting XP/level. Idempotent.
- `POST /api/quests/weekly/{weeklyGoalId}/complete` — mark a goal of the current week's set
  done; body `{ "weekIndex": <n> }` must be the current week. Rejected (`400`) if the goal is
  not in the current set. Idempotent.
- `DELETE /api/quests/weekly/{weeklyGoalId}/complete` — undo the current week's completion. Idempotent.

Completion responses report `{ areaId, areaName, completed, xpDelta, areaXp, areaLevel,
previousLevel, levelChanged }`. Completing/un-completing an archived era returns `409`.

## Progression

- `GET /api/progression/summary` — active era's `globalLevel` (average of area levels) plus
  per-area `{ level, xp, xpPerLevel, xpIntoLevel, xpForNextLevel, maxLevel, isComplete }`.
- `GET /api/progression/areas/{areaId}` — the same per-area progression for a single area.

## World

- `GET /api/world`
- `GET /api/world/templates`
- `GET /api/world/eras/{eraId}`

## Admin

- `GET /api/admin/eras/{eraId}/daily-habits`
- `POST /api/admin/eras/{eraId}/daily-habits`
- `PUT /api/admin/daily-habits/{dailyHabitId}`
- `DELETE /api/admin/daily-habits/{dailyHabitId}`
- `GET /api/admin/eras/{eraId}/weekly-goals`
- `POST /api/admin/eras/{eraId}/weekly-goals`
- `PUT /api/admin/weekly-goals/{weeklyGoalId}`
- `DELETE /api/admin/weekly-goals/{weeklyGoalId}`
- `PUT /api/admin/eras/{eraId}/weekly-sets/{weekIndex}`
