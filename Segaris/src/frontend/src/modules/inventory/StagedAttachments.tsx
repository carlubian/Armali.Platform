import { FileText, Paperclip, X } from 'lucide-react'
import { useRef } from 'react'
import { useTranslation } from 'react-i18next'

import { Button } from '@/components/ui'

import { attachmentAccept, formatFileSize, rejectionFor } from './attachments'

export interface StagedAttachmentsProps {
  files: File[]
  onChange: (files: File[]) => void
}

/**
 * In-memory file staging for create mode. The owning item or order does not exist
 * yet, so files are held locally and uploaded after a successful create. Obvious
 * type and size rejections surface here so the user can swap a file early.
 */
export function StagedAttachments({ files, onChange }: StagedAttachmentsProps) {
  const { t } = useTranslation('inventory')
  const input = useRef<HTMLInputElement>(null)

  const removeAt = (index: number) =>
    onChange(files.filter((_, position) => position !== index))

  return (
    <div className="seg-inv-attach">
      <div className="seg-inv-attach__head">
        <Button
          variant="outline"
          size="sm"
          iconLeft={<Paperclip size={15} />}
          onClick={() => input.current?.click()}
        >
          {t('editor.attachments.add')}
        </Button>
        <input
          ref={input}
          type="file"
          multiple
          accept={attachmentAccept}
          className="seg-inv-attach__input"
          tabIndex={-1}
          aria-label={t('editor.attachments.add')}
          onChange={(event) => {
            const chosen = event.target.files
            if (chosen != null) onChange([...files, ...Array.from(chosen)])
            event.target.value = ''
          }}
        />
      </div>
      {files.length === 0 ? (
        <p className="seg-inv-attach__empty">{t('editor.attachments.empty')}</p>
      ) : (
        <>
          <p className="seg-inv-editor__hint">{t('editor.attachments.stagedHint')}</p>
          <ul className="seg-inv-attach__list">
            {files.map((file, index) => {
              const rejection = rejectionFor(file)
              return (
                <li
                  key={`${file.name}-${index}`}
                  className={
                    'seg-inv-attach__item' +
                    (rejection != null ? ' seg-inv-attach__item--error' : '')
                  }
                >
                  <FileText
                    size={18}
                    aria-hidden="true"
                    className="seg-inv-attach__icon"
                  />
                  <span className="seg-inv-attach__meta">
                    <span className="seg-inv-attach__name">{file.name}</span>
                    <span className="seg-inv-attach__size">
                      {rejection === 'tooLarge'
                        ? t('editor.attachments.errors.tooLarge')
                        : rejection === 'type'
                          ? t('editor.attachments.errors.type')
                          : formatFileSize(file.size)}
                    </span>
                  </span>
                  <button
                    type="button"
                    className="seg-inv-attach__action"
                    onClick={() => removeAt(index)}
                    aria-label={t('editor.attachments.removeStaged')}
                  >
                    <X size={16} aria-hidden="true" />
                  </button>
                </li>
              )
            })}
          </ul>
        </>
      )}
    </div>
  )
}
