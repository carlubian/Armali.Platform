/**
 * `capex` i18n namespace. Domain strings for the Capex module live here, separate
 * from the platform namespace, and are registered alongside it in `app/i18n`.
 */
export const capex = {
  launcher: {
    title: 'Capex',
    description: 'Record household income and expenses as individual movements.',
  },
  entries: {
    eyebrow: 'Capex',
    title: 'Entries',
    description: 'Browse, search, and refine every movement you can access.',
    columns: {
      title: 'Title',
      type: 'Type',
      status: 'Status',
      dueDate: 'Date',
      category: 'Category',
      supplier: 'Supplier',
      costCenter: 'Cost center',
      total: 'Total',
      currency: 'Currency',
    },
    type: {
      Income: 'Income',
      Expense: 'Expense',
    },
    status: {
      Planning: 'Planning',
      Completed: 'Completed',
      Canceled: 'Canceled',
    },
    visibility: {
      Public: 'Public',
      Private: 'Private',
    },
    none: '—',
    sort: {
      label: 'Sort by {{column}}',
      ascending: 'ascending',
      descending: 'descending',
    },
    states: {
      loading: 'Loading entries…',
      empty: 'No entries match this view yet.',
      emptyFiltered: 'No entries match the current filters.',
      loadError: 'The entries could not be loaded. Please try again.',
    },
    count_one: '{{count}} entry',
    count_other: '{{count}} entries',
    filters: {
      searchLabel: 'Search',
      searchPlaceholder: 'Search title, notes, or items',
      from: 'From',
      to: 'To',
      type: 'Type',
      status: 'Status',
      category: 'Category',
      supplier: 'Supplier',
      costCenter: 'Cost center',
      currency: 'Currency',
      visibility: 'Visibility',
      myEntries: 'My entries',
      anyOption: 'All',
      more: 'More filters',
      fewer: 'Fewer filters',
      clearAll: 'Clear all',
      activeLabel: 'Active filters',
      remove: 'Remove {{label}} filter',
      chip: {
        search: 'Search: {{value}}',
        from: 'From {{value}}',
        to: 'To {{value}}',
        mine: 'My entries',
      },
    },
    pagination: {
      label: 'Entries pagination',
      previous: 'Previous',
      next: 'Next',
      status: 'Page {{page}} of {{pages}}',
      rowsPerPage: 'Rows per page',
    },
  },
} as const
