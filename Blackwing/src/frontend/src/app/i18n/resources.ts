/**
 * Platform translation resources.
 *
 * The interface is Spanish, but it is translated from day one: no component may
 * contain a visible literal string. Every user-facing text lives here and is
 * read through `useTranslation`. Copy follows sentence case and carries no
 * emoji, per the Armali design language.
 */
export const platform = {
  app: {
    name: 'Blackwing',
    description: 'Tu biblioteca de fotos privada',
  },
  common: {
    close: 'Cerrar',
    loading: 'Cargando',
    tryAgain: 'Reintentar',
  },
  shell: {
    primaryNavigation: 'Navegación principal',
    comingLater: 'Disponible en una fase posterior',
    nav: {
      gallery: {
        label: 'Galería',
        eyebrow: 'Todo tu archivo',
        title: 'Galería',
      },
      upload: {
        label: 'Subir',
        eyebrow: 'Entrada de imágenes',
        title: 'Subir imágenes',
      },
      review: {
        label: 'Revisión',
        eyebrow: 'Cola de revisión',
        title: 'Revisión',
      },
      tags: {
        label: 'Tags',
        eyebrow: 'Navegar por contenido',
        title: 'Tags',
      },
      settings: {
        label: 'Ajustes',
        eyebrow: 'Tu cuenta',
        title: 'Ajustes',
      },
      admin: {
        label: 'Cuentas',
        eyebrow: 'Administración',
        title: 'Cuentas',
      },
    },
  },
  auth: {
    signOut: 'Cerrar sesión',
    signingOut: 'Cerrando sesión',
    signOutErrorTitle: 'No se pudo cerrar la sesión',
    signOutErrorBody: 'Tu sesión sigue activa. Inténtalo de nuevo.',
    login: {
      title: 'Bienvenido de vuelta',
      subtitle: 'Entra con tu cuenta para abrir tu biblioteca.',
      usernameLabel: 'Usuario',
      usernamePlaceholder: 'Tu nombre de usuario',
      passwordLabel: 'Contraseña',
      passwordPlaceholder: 'Tu contraseña',
      submit: 'Entrar',
      submitting: 'Entrando',
      footer: 'Las cuentas las crea el administrador de la casa.',
      usernameRequired: 'Escribe tu usuario.',
      passwordRequired: 'Escribe tu contraseña.',
      errorInvalid:
        'No hemos podido entrar. Revisa tu usuario y tu contraseña e inténtalo de nuevo.',
      errorRateLimited: 'Demasiados intentos. Espera un momento antes de reintentar.',
      errorGeneric: 'Algo ha fallado al entrar. Inténtalo de nuevo.',
    },
  },
  session: {
    loading: 'Cargando Blackwing',
    unavailableEyebrow: 'Servicio no disponible',
    unavailableTitle: 'Blackwing no alcanza el servidor',
    unavailableBody:
      'Tu biblioteca está en pausa hasta que vuelva la conexión. No se ha perdido nada.',
    notFoundCode: '404',
    notFoundTitle: 'No encontramos esa página',
    notFoundBody:
      'El enlace puede estar caducado, o apuntar a una sección que no existe en esta biblioteca.',
    returnToGallery: 'Volver a la galería',
  },
  admin: {
    users: {
      description:
        'Crea cuentas, asigna su rol y actívalas o desactívalas. Nadie ve las imágenes de otra cuenta, tampoco tú.',
      newUser: 'Nueva cuenta',
      handle: '@{{name}}',
      roleAdmin: 'Administración',
      roleUser: 'Usuario',
      statusActive: 'Activa',
      statusInactive: 'Desactivada',
      joined: 'Alta el {{date}}',
      resetPassword: 'Resetear contraseña',
      activate: 'Reactivar',
      deactivate: 'Desactivar',
      empty: 'Todavía no hay ninguna cuenta.',
      loadError: 'No se ha podido cargar la lista de cuentas. Inténtalo de nuevo.',
      ownDeactivateLocked: 'No puedes desactivar tu propia cuenta.',
      actionError: 'No se ha podido completar la acción. Inténtalo de nuevo.',
      create: {
        title: 'Nueva cuenta',
        subtitle: 'Da de alta a alguien. Entrará con su usuario y su contraseña.',
        username: 'Usuario',
        usernamePlaceholder: 'usuario',
        usernameRequired: 'Escribe un nombre de usuario.',
        usernameInvalid: 'Elige otro nombre de usuario. Puede que ya esté en uso.',
        role: 'Rol',
        password: 'Contraseña inicial',
        passwordPlaceholder: '••••••••••••',
        passwordHint: 'Mínimo 12 caracteres. Sin más requisitos de composición.',
        passwordRequired: 'Escribe una contraseña inicial.',
        passwordTooShort: 'La contraseña debe tener al menos 12 caracteres.',
        passwordInvalid: 'Elige una contraseña que cumpla los requisitos.',
        submit: 'Crear cuenta',
        submitting: 'Creando cuenta',
        cancel: 'Cancelar',
        error:
          'No se ha podido crear la cuenta. Revisa el formulario e inténtalo de nuevo.',
        successTitle: 'Cuenta creada',
        successBody: '{{name}} ya puede entrar en su biblioteca.',
      },
      reset: {
        title: 'Resetear contraseña',
        subtitle: 'Asigna una contraseña nueva a {{name}}. Sus sesiones se cerrarán.',
        new: 'Contraseña nueva',
        newRequired: 'Escribe la contraseña nueva.',
        newTooShort: 'La contraseña debe tener al menos 12 caracteres.',
        confirm: 'Repite la contraseña nueva',
        confirmRequired: 'Repite la contraseña nueva.',
        mismatch: 'Las contraseñas no coinciden.',
        submit: 'Resetear contraseña',
        submitting: 'Reseteando',
        cancel: 'Cancelar',
        error: 'No se ha podido resetear la contraseña. Elige una contraseña válida.',
        successTitle: 'Contraseña reseteada',
        successBody:
          'La contraseña de {{name}} se ha reseteado y sus sesiones se han cerrado.',
      },
      deactivateConfirm: {
        title: '¿Desactivar la cuenta?',
        description:
          'Se cerrarán las sesiones de {{name}} y no podrá entrar hasta que la reactives. Sus imágenes se conservan.',
        confirm: 'Desactivar',
        cancel: 'Mantener activa',
      },
    },
  },
  startup: {
    cardTitle: 'Cimientos listos',
    cardSubtitle: 'Fuentes, colores, aurora y componentes base ya en su sitio',
    body: 'Esta es la pantalla de arranque del cliente. Las pantallas reales de galería, subida, revisión y tags llegan en fases posteriores.',
    action: 'Entendido',
    acknowledged: 'Anotado. Nada más que hacer aquí por ahora.',
    working: 'Comprobando los cimientos',
  },
}
