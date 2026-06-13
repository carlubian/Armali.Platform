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
    signOut: 'Sign out',
    login: {
      title: 'Welcome home',
      subtitle: 'Sign in to your household to open Segaris.',
      usernameLabel: 'Username',
      usernamePlaceholder: 'Your username',
      passwordLabel: 'Password',
      passwordPlaceholder: 'Your password',
      submit: 'Sign in',
      submitting: 'Signing in…',
      footer: 'Accounts are created by your household administrator.',
      usernameRequired: 'Enter your username.',
      passwordRequired: 'Enter your password.',
      errorInvalid:
        'We could not sign you in. Check your username and password and try again.',
      errorRateLimited: 'Too many sign-in attempts. Wait a moment before trying again.',
      errorGeneric: 'Something went wrong while signing in. Please try again.',
    },
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
