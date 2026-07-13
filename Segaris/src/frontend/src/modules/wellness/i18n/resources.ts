/**
 * `wellness` i18n namespace. Wave 4 adds the launcher card and the today surface:
 * the day's tasks with completion checkboxes, the fixed category labels, the daily
 * score readout, and the loading, empty-catalogue, and error states. Category keys
 * mirror the fixed backend enum names and freeze their user-facing display labels.
 */
export const wellness = {
  launcher: {
    title: 'Wellness',
    description:
      'Six healthy-habit tasks for the day. Tick them off and watch your daily score climb.',
  },
  page: {
    eyebrow: 'Daily habits',
    title: 'Wellness',
    description:
      "Today's set of healthy habits. Complete what you can — your score updates as you go.",
  },
  category: {
    HealthAndBody: 'Health & Body',
    MindAndSleep: 'Mind & Sleep',
    PeopleAndWork: 'People & Work',
  },
  score: {
    eyebrow: 'Today',
    label: 'Daily score',
    caption: '{{completed}} of {{total}} completed',
    reading: '{{score}} percent — {{completed}} of {{total}} tasks completed',
  },
  tasks: {
    heading: "Today's tasks",
    toggle: 'Toggle {{name}}',
    category: 'Category: {{category}}',
  },
  states: {
    loading: "Loading today's tasks...",
    loadError: "Today's Wellness tasks could not be loaded. Please try again.",
    empty: {
      title: 'No tasks for today',
      body: 'The task catalogue is empty. An administrator can add healthy-habit tasks in Configuration.',
    },
  },
  toast: {
    error: 'That change could not be saved',
    errorBody: 'Your task could not be updated. Please try again.',
    close: 'Dismiss',
  },
} as const
