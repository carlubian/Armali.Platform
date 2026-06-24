export const calendar = {
  launcher: {
    title: 'Calendar',
    description: 'See birthdays, trips, due dates, deliveries, asset dates, and notes.',
  },
  page: {
    eyebrow: 'Calendar',
    title: 'Calendar',
    description:
      'A Monday-first month view of date-bound entries projected from Segaris modules.',
    today: 'Today',
    previousMonth: 'Previous month',
    nextMonth: 'Next month',
    selectedDay: 'Selected day',
    visibleRange: '{{from}} to {{to}}',
  },
  filters: {
    family: 'Family',
    source: 'Source',
    clear: 'Reset filters',
    familyToggle: 'Toggle {{label}} family',
    sourceToggle: 'Toggle {{label}} source',
  },
  families: {
    Birthday: 'Birthday',
    Travel: 'Travel',
    Note: 'Note',
    Other: 'Other',
  },
  sources: {
    calendar: 'Calendar',
    firebird: 'Firebird',
    travel: 'Travel',
    inventory: 'Inventory',
    assets: 'Assets',
    maintenance: 'Maintenance',
    processes: 'Processes',
  },
  grid: {
    label: 'Calendar month grid',
    dayLabel: '{{date}}, {{count}} entries',
    more: '+{{count}}',
    loading: 'Loading calendar entries...',
    empty: 'No entries in this visible range.',
    loadError: 'Calendar entries could not be loaded. Please try again.',
    retry: 'Retry',
    selectedEmpty: 'No entries on the selected day.',
    selectedHint: 'Day details arrive in the next Calendar wave.',
  },
  indicators: {
    family: '{{family}} entry: {{title}}',
    more: '{{count}} more entry families',
  },
} as const
