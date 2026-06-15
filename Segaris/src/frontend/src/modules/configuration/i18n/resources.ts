/**
 * `configuration` i18n namespace. Strings for the administrative Configuration
 * experience (shared catalogs and Capex categories) live here, separate from the
 * platform namespace, and are registered alongside it in `app/i18n`.
 *
 * Per-catalog labels are keyed by the catalog descriptor key
 * (`suppliers`, `costCenters`, `currencies`, `categories`) so the four known
 * catalogs share one table, dialog, and toast surface without a generic runtime
 * framework.
 */
export const configuration = {
  launcher: {
    title: 'Configuration',
    description:
      'Manage shared catalogs: suppliers, cost centres, currencies, and Capex categories.',
  },
  page: {
    eyebrow: 'Administration',
    title: 'Configuration',
    description:
      'Curate the shared catalogs that household forms depend on. Changes apply the next time a form is opened.',
  },
  sections: {
    label: 'Configuration sections',
    global: 'Global',
    capex: 'Capex',
  },
  catalogs: {
    label: 'Global catalogs',
    suppliers: {
      tab: 'Suppliers',
      title: 'Suppliers',
      description: 'Vendors and providers selectable on entries.',
      addAction: 'New supplier',
      createTitle: 'New supplier',
      editTitle: 'Edit supplier',
      nameLabel: 'Name',
      namePlaceholder: 'Supplier name',
      empty: 'No suppliers yet. Add the first one so forms can use it.',
      itemName: 'supplier',
    },
    costCenters: {
      tab: 'Cost centres',
      title: 'Cost centres',
      description: 'Buckets that group spending across the household.',
      addAction: 'New cost centre',
      createTitle: 'New cost centre',
      editTitle: 'Edit cost centre',
      nameLabel: 'Name',
      namePlaceholder: 'Cost centre name',
      empty: 'No cost centres yet. Add the first one so forms can use it.',
      itemName: 'cost centre',
    },
    currencies: {
      tab: 'Currencies',
      title: 'Currencies',
      description: 'Currencies available for entry amounts.',
      addAction: 'New currency',
      createTitle: 'New currency',
      editTitle: 'Edit currency',
      nameLabel: 'Name',
      namePlaceholder: 'Euro',
      codeLabel: 'Code',
      codePlaceholder: 'EUR',
      empty: 'No currencies yet. Add the first one so forms can use it.',
      itemName: 'currency',
    },
    categories: {
      tab: 'Categories',
      title: 'Capex categories',
      description: 'Categories that classify Capex entries.',
      addAction: 'New category',
      createTitle: 'New category',
      editTitle: 'Edit category',
      nameLabel: 'Name',
      namePlaceholder: 'Category name',
      empty: 'No categories yet. Add the first one so entries can use it.',
      itemName: 'category',
    },
  },
  table: {
    columns: {
      order: 'Order',
      name: 'Name',
      code: 'Code',
      actions: 'Actions',
    },
    loadError: 'This catalog could not be loaded.',
    retry: 'Try again',
    moveUp: 'Move {{name}} up',
    moveDown: 'Move {{name}} down',
    edit: 'Edit {{name}}',
    delete: 'Delete {{name}}',
    moveError: 'The order could not be changed. Please try again.',
  },
  form: {
    cancel: 'Cancel',
    close: 'Close',
    save: 'Save changes',
    saving: 'Saving…',
    create: 'Create',
    creating: 'Creating…',
    nameRequired: 'Enter a name.',
    nameTooLong: 'The name must be 100 characters or fewer.',
    codeRequired: 'Enter a three-letter code.',
    codeInvalid: 'Use exactly three letters, for example EUR.',
    duplicateName: 'Another entry already uses this name.',
    duplicateCode: 'Another currency already uses this code.',
    genericError:
      'The change could not be saved. Please review the form and try again.',
  },
  unsaved: {
    title: 'Discard unsaved changes?',
    description: 'Your changes to this entry have not been saved.',
    stay: 'Keep editing',
    leave: 'Discard and leave',
  },
  remove: {
    directTitle: 'Delete {{name}}?',
    directDescription:
      'This value is not used by any record and will be permanently removed.',
    confirm: 'Delete',
    deleting: 'Deleting…',
    cancel: 'Cancel',
    referencedTitle: 'Remove {{name}}',
    referencedDescription:
      'Some records still use this value. Choose what should happen to them before it is removed.',
    strategyLabel: 'Records currently using this value',
    replaceOption: 'Move them to another value',
    clearOption: 'Leave the value empty on those records',
    replacementLabel: 'Replacement',
    replacementPlaceholder: 'Choose a replacement',
    replacementRequired: 'Choose a replacement value.',
    noCandidates: 'Add another value before removing this one.',
    requiredMin: 'At least one value must remain in this catalog.',
    currencyBlockedTitle: 'This currency is in use',
    currencyBlockedDescription:
      'Existing entries use this currency. Converting and removing a referenced currency will be available in a later update.',
    close: 'Close',
    error: 'The value could not be removed. Please try again.',
    conflict: 'This catalog changed while you were working. Refresh and try again.',
  },
  toast: {
    created: 'Added',
    createdBody: '“{{name}}” was added.',
    updated: 'Saved',
    updatedBody: '“{{name}}” was updated.',
    removed: 'Removed',
    removedBody: '“{{name}}” was removed.',
    close: 'Close',
  },
  states: {
    accessDenied: 'This area is for administrators.',
  },
} as const
