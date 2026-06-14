/**
 * Client-side attachment helpers for the Capex editor. The allowed extensions
 * and size limit mirror the backend `AttachmentPolicy` so the UI can reject an
 * obviously invalid file before spending a request, while the server remains the
 * authority on content validation.
 */

/** Maximum upload size, matching the backend `AttachmentPolicy.MaximumFileSize`. */
export const maxAttachmentBytes = 25 * 1024 * 1024

/** Extensions accepted by the backend, lower-cased and dot-prefixed. */
export const allowedAttachmentExtensions = [
  '.pdf',
  '.jpg',
  '.jpeg',
  '.png',
  '.webp',
  '.txt',
  '.csv',
  '.md',
  '.json',
  '.xml',
  '.yaml',
  '.yml',
  '.docx',
  '.xlsx',
  '.pptx',
  '.odt',
  '.ods',
  '.odp',
] as const

/** Value for an `<input type="file" accept="…">` attribute. */
export const attachmentAccept = allowedAttachmentExtensions.join(',')

/** Why a chosen file cannot be uploaded, or `null` when it passes the guards. */
export type AttachmentRejection = 'type' | 'tooLarge' | null

function extensionOf(fileName: string): string {
  const dot = fileName.lastIndexOf('.')
  return dot < 0 ? '' : fileName.slice(dot).toLowerCase()
}

/**
 * Lightweight pre-flight validation. Returns the reason a file is rejected so
 * the caller can show a precise message without a round-trip.
 */
export function rejectionFor(file: File): AttachmentRejection {
  if (
    !(allowedAttachmentExtensions as readonly string[]).includes(extensionOf(file.name))
  ) {
    return 'type'
  }
  if (file.size > maxAttachmentBytes) {
    return 'tooLarge'
  }
  return null
}

/** Compact, locale-agnostic human-readable size (e.g. "1.4 MB", "820 KB"). */
export function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  const kib = bytes / 1024
  if (kib < 1024) return `${kib < 10 ? kib.toFixed(1) : Math.round(kib)} KB`
  const mib = kib / 1024
  return `${mib < 10 ? mib.toFixed(1) : Math.round(mib)} MB`
}
