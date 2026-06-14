import { describe, expect, it } from 'vitest'

import {
  attachmentAccept,
  formatFileSize,
  maxAttachmentBytes,
  rejectionFor,
} from './attachments'

function fileOfSize(name: string, size: number, type = 'application/pdf'): File {
  const file = new File(['x'], name, { type })
  // Construct an oversized file without allocating its bytes.
  Object.defineProperty(file, 'size', { value: size })
  return file
}

describe('attachment client guards', () => {
  it('accepts a supported file within the size limit', () => {
    expect(rejectionFor(fileOfSize('invoice.pdf', 1024))).toBeNull()
    expect(rejectionFor(fileOfSize('PHOTO.JPG', 2048, 'image/jpeg'))).toBeNull()
  })

  it('rejects an unsupported extension', () => {
    expect(rejectionFor(fileOfSize('malware.exe', 1024))).toBe('type')
    expect(rejectionFor(fileOfSize('no-extension', 1024))).toBe('type')
  })

  it('rejects a supported file that exceeds the size limit', () => {
    expect(rejectionFor(fileOfSize('huge.pdf', maxAttachmentBytes + 1))).toBe(
      'tooLarge',
    )
  })

  it('lists every accepted extension in the accept attribute', () => {
    expect(attachmentAccept).toContain('.pdf')
    expect(attachmentAccept).toContain('.xlsx')
    expect(attachmentAccept.startsWith('.')).toBe(true)
  })

  it('formats sizes in human-readable units', () => {
    expect(formatFileSize(512)).toBe('512 B')
    expect(formatFileSize(2048)).toBe('2.0 KB')
    expect(formatFileSize(5 * 1024 * 1024)).toBe('5.0 MB')
  })
})
