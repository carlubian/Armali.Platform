/**
 * Client-side attachment helpers for Travel. They mirror the Inventory, Capex,
 * and Opex helpers so the trip and expense editors give the same immediate
 * feedback the server enforces (allowed extensions and the 25 MB ceiling) before
 * a request is ever sent.
 */

export const maxAttachmentBytes = 25 * 1024 * 1024

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

export const attachmentAccept = allowedAttachmentExtensions.join(',')

export type AttachmentRejection = 'type' | 'tooLarge' | null

function extensionOf(fileName: string): string {
  const dot = fileName.lastIndexOf('.')
  return dot < 0 ? '' : fileName.slice(dot).toLowerCase()
}

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

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  const kib = bytes / 1024
  if (kib < 1024) return `${kib < 10 ? kib.toFixed(1) : Math.round(kib)} KB`
  const mib = kib / 1024
  return `${mib < 10 ? mib.toFixed(1) : Math.round(mib)} MB`
}
