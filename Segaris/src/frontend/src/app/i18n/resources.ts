export const platform = {
  app: {
    name: 'Segaris',
  },
  common: {
    tryAgain: 'Try again',
    reload: 'Reload application',
    returnToLauncher: 'Return to launcher',
  },
  auth: {
    loginTitle: 'Sign in to Segaris',
    loginPending: 'The sign-in form arrives in the next implementation wave.',
    signOut: 'Sign out',
  },
  launcher: {
    eyebrow: 'Household platform',
    title: 'Choose a module',
    description: 'Open a tool to manage that part of your home. Return here to switch.',
    foundation: 'Application foundation ready',
  },
  shell: {
    launcher: 'Launcher',
    profile: 'My profile',
  },
  errors: {
    unavailableEyebrow: 'Service unavailable',
    unavailableTitle: "Segaris can't reach the household server",
    unavailableBody:
      'Your modules are paused until the connection returns. Nothing has been lost.',
    notFoundTitle: "We can't find that page",
    notFoundBody:
      "The link may be old, or the module it pointed to isn't installed in this household.",
    renderTitle: 'This part of Segaris could not be displayed',
    renderBody: 'Try rendering it again or return to the launcher.',
    rootRenderTitle: 'Segaris could not start correctly',
    rootRenderBody: 'Reload the application to try again.',
  },
} as const
