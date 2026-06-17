/**
 * `travel` i18n namespace. Wave 0 registers launcher and route-shell strings;
 * later waves extend it with table, dialog, itinerary, and expense copy.
 */
export const travel = {
  launcher: {
    title: 'Travel',
    description: 'Plan household trips, itinerary notes, and travel expenses.',
    attention: 'A planned or ongoing trip needs attention.',
  },
  page: {
    eyebrow: 'Travel',
    title: 'Travel',
    description: 'Browse and maintain trips, itineraries, and expenses.',
  },
} as const
