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
