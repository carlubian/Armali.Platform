# Belfalas API Surface

> Wave 0 contract surface. Routes intentionally return `501 Not Implemented` until
> their implementation wave lands. `/health` is the only operational endpoint.

## Health

- `GET /health`

## Eras

- `GET /api/eras`
- `GET /api/eras/active`
- `GET /api/eras/{eraId}`
- `POST /api/eras`
- `POST /api/eras/{eraId}/archive`

## Quests

- `GET /api/quests/daily`
- `GET /api/quests/weekly`
- `POST /api/quests/daily/{dailyHabitId}/complete`
- `POST /api/quests/weekly/{weeklyGoalId}/complete`

## Progression

- `GET /api/progression/summary`
- `GET /api/progression/areas/{areaId}`

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
