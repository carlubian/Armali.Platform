/**
 * `mood` i18n namespace. Wave 0 registers the launcher card, module navigation,
 * and criteria/scale labels that freeze the user-facing vocabulary. Wave 4 adds
 * the weekly log and entry dialog copy, Wave 5 adds the dashboard charts, and the
 * derived-emotion code labels are filled alongside the Wave 1 matrix.
 */
export const mood = {
  launcher: {
    title: 'Mood',
    description: 'Log private mood check-ins and review your own trends.',
  },
  page: {
    eyebrow: 'Mood',
    title: 'Mood',
  },
  nav: {
    log: 'Log',
    dashboard: 'Dashboard',
  },
  criteria: {
    energy: {
      label: 'Energy',
      Low: 'Low',
      Medium: 'Medium',
      High: 'High',
    },
    alignment: {
      label: 'Alignment',
      Negative: 'Negative',
      Medium: 'Medium',
      Positive: 'Positive',
    },
    direction: {
      label: 'Direction',
      Harmony: 'Harmony',
      Defensive: 'Defensive',
      Offensive: 'Offensive',
      Stability: 'Stability',
    },
    source: {
      label: 'Source',
      Internal: 'Internal',
      External: 'External',
    },
  },
  dashboard: {
    scale: {
      year: 'Year',
      semester: 'Semester',
      quarter: 'Quarter',
      month: 'Month',
    },
  },
} as const
