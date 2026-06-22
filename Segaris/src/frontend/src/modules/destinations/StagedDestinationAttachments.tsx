import { FileText, Paperclip, X } from 'lucide-react'
import { useRef } from 'react'
import { useTranslation } from 'react-i18next'

import { Button } from '@/components/ui'

import { attachmentAccept, formatFileSize, rejectionFor } from './attachments'

export interface StagedDestinationAttachmentsProps {
  files: File[]
  onChange: (files: File[]) => void
}

export function StagedDestinationAttachments({
  files,
  onChange,
}: StagedDestinationAttachmentsProps) {
  const { t } = useTranslation('destinations')
  const input = useRef<HTMLInputElement>(null)

  const removeAt = (index: number) =>
    onChange(files.filter((_, position) => position !== index))

  return (
    <div className="seg-destinations-attach">
      <div className="seg-destinations-attach__head">
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
          className="seg-destinations-attach__input"
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
        <p className="seg-destinations-attach__empty">
          {t('editor.attachments.empty')}
        </p>
      ) : (
        <>
          <p className="seg-destinations-editor__hint">
            {t('editor.attachments.stagedHint')}
          </p>
          <ul className="seg-destinations-attach__list">
            {files.map((file, index) => {
              const rejection = rejectionFor(file)
              return (
                <li
                  key={`${file.name}-${index}`}
                  className={
                    'seg-destinations-attach__item' +
                    (rejection != null ? ' seg-destinations-attach__item--error' : '')
                  }
                >
                  <FileText
                    size={18}
                    aria-hidden="true"
                    className="seg-destinations-attach__icon"
                  />
                  <span className="seg-destinations-attach__meta">
                    <span className="seg-destinations-attach__name">{file.name}</span>
                    <span className="seg-destinations-attach__size">
                      {rejection === 'tooLarge'
                        ? t('editor.attachments.errors.tooLarge')
                        : rejection === 'type'
                          ? t('editor.attachments.errors.type')
                          : formatFileSize(file.size)}
                    </span>
                  </span>
                  <button
                    type="button"
                    className="seg-destinations-attach__action"
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
