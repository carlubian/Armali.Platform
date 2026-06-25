export const analytics = {
  launcher: {
    title: 'Analytics',
    description:
      'Read-only yearly financial trends pooled across your modules, compared with the previous year.',
  },
  tabs: {
    label: 'Analytics sections',
    overview: 'Overview',
    capex: 'Capex',
    opex: 'Opex',
    inventory: 'Inventory',
    travel: 'Travel',
    'cross-module': 'Cross-module',
  },
  eur: {
    label: 'Amounts in EUR',
  },
  yearNav: {
    label: 'Select year',
    previous: 'Previous year',
    next: 'Next year',
    current: 'This year',
    comparison: 'vs {{year}}',
  },
  tab: {
    overview: {
      eyebrow: 'Overview',
      title: 'Year {{year}} at a glance',
      description:
        'A high-level read of the year across every participating module — totals, monthly trend and net balance, each compared with the previous year.',
      scope: 'All participating modules',
    },
    capex: {
      eyebrow: 'Capex',
      title: 'Capital income & expense',
      description:
        'Completed Capex entries grouped by category, supplier and cost centre — income and expense side by side.',
      scope: 'Completed entries only',
    },
    opex: {
      eyebrow: 'Opex',
      title: 'Operating income & expense',
      description:
        'Realized Opex occurrences grouped by category, supplier and cost centre — income and expense side by side.',
      scope: 'Realized occurrences only',
    },
    inventory: {
      eyebrow: 'Inventory',
      title: 'Order spending',
      description:
        'Received order spending by item category and supplier, average order value, and the year’s top items and suppliers.',
      scope: 'Planning & cancelled excluded',
      averageSub: 'Mean EUR per received order',
      topNote: 'Bars labelled with EUR and share of total Inventory expense.',
    },
    travel: {
      eyebrow: 'Travel',
      title: 'Trip spending',
      description:
        'Travel expenses grouped by category, supplier, cost centre and linked destination.',
      scope: 'Cancelled trips excluded',
    },
    'cross-module': {
      eyebrow: 'Cross-module',
      title: 'Pooled expenses',
      description:
        'Total expenses pooled across Capex, Opex, Inventory and Travel — grouped by supplier, category and cost-centre label.',
      scope: 'Capex · Opex · Inventory · Travel',
      note: 'Categories are matched across modules by normalized display label — this does not create shared category ownership.',
    },
  },
  overview: {
    totals: {
      label: 'Year totals',
      expenses: 'Total expenses',
      income: 'Total income',
      netBalance: 'Net balance',
      comparison: 'vs {{year}}',
    },
  },
  states: {
    loading: 'Loading charts…',
    error: 'These charts could not be loaded.',
    retry: 'Try again',
    configIncompleteTitle: 'Exchange rates are incomplete',
    configIncompleteBody:
      'Some accessible records use a currency without a current exchange rate to EUR, so these charts cannot be aggregated safely.',
    configIncompleteCurrencies: 'Currencies without a rate:',
    configIncompleteCta: 'Manage currencies',
  },
  chart: {
    showTable: 'Show data table',
    showChart: 'Show chart',
    empty: 'No data for this period',
    yoy: 'Year over year {{delta}}',
    summary:
      '{{title}}. In {{year}}: {{highlights}}. Total {{current}} versus {{previous}} in {{previousYear}} ({{delta}}).',
    summaryEmpty: '{{title}}. No data for {{year}}.',
    summaryMonthly:
      '{{title}}. Monthly values for {{year}} and {{previousYear}}. {{year}} totals {{total}}.',
    summaryMore: ', and more',
    rankCaption:
      '{{dimension}} — top {{count}} for {{year}}, with share of total and {{previousYear}} comparison.',
    table: {
      caption: '{{dimension}} — {{year}} versus {{previousYear}}, EUR.',
      yoy: 'YoY',
      month: 'Month',
      shareOfTotal: '% of total',
    },
  },
  charts: {
    overview: {
      monthlyExpense: 'Total expenses by month',
      monthlyIncome: 'Total income by month',
      monthlyNetBalance: 'Net balance by month',
    },
    capex: {
      expenseByCategory: 'Expenses by category',
      expenseBySupplier: 'Expenses by supplier',
      expenseByCostCenter: 'Expenses by cost centre',
      incomeByCategory: 'Income by category',
      incomeBySupplier: 'Income by supplier',
      incomeByCostCenter: 'Income by cost centre',
    },
    opex: {
      expenseByCategory: 'Expenses by category',
      expenseBySupplier: 'Expenses by supplier',
      expenseByCostCenter: 'Expenses by cost centre',
      incomeByCategory: 'Income by category',
      incomeBySupplier: 'Income by supplier',
      incomeByCostCenter: 'Income by cost centre',
    },
    inventory: {
      expenseByItemCategory: 'Expenses by item category',
      expenseBySupplier: 'Expenses by supplier',
      averageOrderBySupplier: 'Average order amount by supplier',
      topItems: 'Top 5 items by spend',
      topSuppliers: 'Top 5 suppliers by spend',
    },
    travel: {
      expenseByCategory: 'Expenses by category',
      expenseBySupplier: 'Expenses by supplier',
      expenseByCostCenter: 'Expenses by cost centre',
      expenseByDestination: 'Expenses by destination',
    },
    crossModule: {
      expenseBySupplier: 'Total expenses by supplier',
      expenseByCategory: 'Total expenses by category',
      expenseByCostCenter: 'Total expenses by cost centre',
    },
  },
}
