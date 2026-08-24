import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ComponentPropsWithRef,
  type PropsWithChildren,
  type ReactNode,
} from 'react'
import { createPortal } from 'react-dom'

import './Toast.css'

export type ToastTone = 'info' | 'success' | 'danger' | 'gold'

export interface ToastProps extends Omit<ComponentPropsWithRef<'div'>, 'title'> {
  title?: ReactNode
  tone?: ToastTone
  /** Custom leading icon; falls back to a tone-appropriate glyph. */
  icon?: ReactNode
  /** When provided, renders a dismiss button that invokes this handler. */
  onClose?: () => void
  /**
   * Accessible label for the dismiss button. Required: the interface is Spanish
   * and every user-facing text, visible or announced, comes from i18next.
   */
  closeLabel: string
}

const ICONS: Record<ToastTone, string> = {
  info: 'M12 8h.01M11 12h1v4h1',
  success: 'm5 13 4 4L19 7',
  danger: 'M12 9v4m0 4h.01',
  gold: 'M12 9v4m0 4h.01',
}

/**
 * Project Armali glass toast with a tone rail and optional dismiss.
 *
 * Ported from the design-system reference (`components/feedback/Toast.jsx`).
 * Unlike the prototype, `closeLabel` is mandatory so the dismiss control always
 * carries a translated accessible name. This is the presentational toast; the
 * queue that stacks and expires them is `ToastProvider`, below.
 */
export function Toast({
  title,
  children,
  tone = 'info',
  icon,
  onClose,
  closeLabel,
  className = '',
  ...rest
}: ToastProps) {
  const toneCls = tone !== 'info' ? `arm-toast--${tone}` : ''
  return (
    <div
      className={['arm-toast', 'arm-toast--enter', toneCls, className]
        .filter(Boolean)
        .join(' ')}
      role="status"
      {...rest}
    >
      <span className="arm-toast__icon">
        {icon ?? (
          <svg
            width="20"
            height="20"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
            aria-hidden="true"
          >
            <circle cx="12" cy="12" r="9" opacity="0.35" />
            <path d={ICONS[tone]} />
          </svg>
        )}
      </span>
      <div className="arm-toast__body">
        {title != null && <div className="arm-toast__title">{title}</div>}
        {children != null && <div className="arm-toast__msg">{children}</div>}
      </div>
      {onClose && (
        <button
          type="button"
          className="arm-toast__close"
          aria-label={closeLabel}
          onClick={onClose}
        >
          <svg
            width="15"
            height="15"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2.2"
            strokeLinecap="round"
            aria-hidden="true"
          >
            <path d="M18 6 6 18M6 6l12 12" />
          </svg>
        </button>
      )}
    </div>
  )
}

export interface ToastRequest {
  title: string
  body?: string
  tone?: ToastTone
}

interface QueuedToast extends ToastRequest {
  id: number
}

interface ToastContextValue {
  /** Queues a toast; it expires on its own or when the reader dismisses it. */
  show: (toast: ToastRequest) => void
}

const ToastContext = createContext<ToastContextValue | null>(null)

/** How long a toast stays on screen before it retires itself. */
const toastLifetimeMs = 6_000

export interface ToastProviderProps extends PropsWithChildren {
  /**
   * Accessible label for the dismiss button of every queued toast. It is passed
   * in rather than translated here so this primitive stays free of i18next, the
   * way `Spinner`'s `label` and `IconButton`'s `label` already do.
   */
  closeLabel: string
}

/**
 * Application-wide toast queue.
 *
 * Screens raise transient confirmations through `useToast()` instead of owning
 * their own state, so several of them stack inside a single live region instead
 * of overlapping, and a toast is not lost when its screen navigates away.
 */
export function ToastProvider({ closeLabel, children }: ToastProviderProps) {
  const [toasts, setToasts] = useState<QueuedToast[]>([])
  const nextId = useRef(0)
  const timers = useRef(new Map<number, ReturnType<typeof setTimeout>>())

  const dismiss = useCallback((id: number) => {
    const timer = timers.current.get(id)
    if (timer !== undefined) {
      clearTimeout(timer)
      timers.current.delete(id)
    }
    setToasts((current) => current.filter((toast) => toast.id !== id))
  }, [])

  const show = useCallback(
    (toast: ToastRequest) => {
      const id = nextId.current++
      setToasts((current) => [...current, { ...toast, id }])
      timers.current.set(
        id,
        setTimeout(() => dismiss(id), toastLifetimeMs),
      )
    },
    [dismiss],
  )

  useEffect(() => {
    // Capture the map itself: the ref object outlives this effect, and reading
    // `.current` from inside the cleanup would look it up after unmount.
    const pending = timers.current
    return () => {
      for (const timer of pending.values()) clearTimeout(timer)
      pending.clear()
    }
  }, [])

  const value = useMemo(() => ({ show }), [show])

  return (
    <ToastContext.Provider value={value}>
      {children}
      {toasts.length > 0 &&
        createPortal(
          <div className="arm-toast-stack">
            {toasts.map((toast) => (
              <Toast
                key={toast.id}
                tone={toast.tone ?? 'success'}
                title={toast.title}
                closeLabel={closeLabel}
                onClose={() => dismiss(toast.id)}
              >
                {toast.body}
              </Toast>
            ))}
          </div>,
          document.body,
        )}
    </ToastContext.Provider>
  )
}

export function useToast(): ToastContextValue {
  const value = useContext(ToastContext)
  if (value === null) throw new Error('useToast must be used inside ToastProvider.')
  return value
}
