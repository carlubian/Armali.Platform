import { useQuery, useQueryClient } from '@tanstack/react-query'
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  type PropsWithChildren,
} from 'react'
import { useNavigate } from 'react-router-dom'

import { resetCsrfToken, SESSION_EXPIRED_EVENT } from '@/app/api/client'
import { isApiError } from '@/app/api/errors'
import { sessionApi, type Session } from '@/app/api/session'

const sessionQueryKey = ['session'] as const

export type SessionStatus =
  | 'loading'
  | 'authenticated'
  | 'unauthenticated'
  | 'unavailable'

interface SessionContextValue {
  status: SessionStatus
  session: Session | null
  refresh: () => Promise<void>
  signOut: () => Promise<void>
}

const SessionContext = createContext<SessionContextValue | null>(null)

/**
 * A `401` from `/api/session` is not an error: it is the answer "nobody is
 * signed in". Anything else propagates so the guards can tell an anonymous
 * visitor from an unreachable backend.
 */
async function loadSession(signal?: AbortSignal): Promise<Session | null> {
  try {
    return await sessionApi.getSession(signal)
  } catch (error) {
    if (isApiError(error) && error.kind === 'authentication-expired') return null
    throw error
  }
}

/**
 * Single source of truth for who is signed in.
 *
 * Blackwing has no profile and no language switch, so this is only the session
 * query, the global expiry listener, and the two commands the shell needs.
 */
export function SessionProvider({ children }: PropsWithChildren) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const sessionQuery = useQuery({
    queryKey: sessionQueryKey,
    queryFn: ({ signal }) => loadSession(signal),
    staleTime: 30_000,
  })

  const expireSession = useCallback(() => {
    queryClient.setQueryData(sessionQueryKey, null)
    // The cached antiforgery token belonged to the session that just ended; the
    // next mutation must fetch one bound to whoever signs in afterwards.
    resetCsrfToken()
    void navigate('/login', { replace: true })
  }, [navigate, queryClient])

  useEffect(() => {
    window.addEventListener(SESSION_EXPIRED_EVENT, expireSession)
    return () => window.removeEventListener(SESSION_EXPIRED_EVENT, expireSession)
  }, [expireSession])

  const session = sessionQuery.data ?? null

  const refresh = useCallback(async () => {
    await queryClient.invalidateQueries({ queryKey: sessionQueryKey })
  }, [queryClient])

  const signOut = useCallback(async () => {
    await sessionApi.signOut()
    expireSession()
  }, [expireSession])

  let status: SessionStatus = 'loading'
  if (
    sessionQuery.isError &&
    isApiError(sessionQuery.error) &&
    ['unavailable', 'transient'].includes(sessionQuery.error.kind)
  ) {
    status = 'unavailable'
  } else if (sessionQuery.isSuccess && sessionQuery.data === null) {
    status = 'unauthenticated'
  } else if (sessionQuery.isSuccess && sessionQuery.data !== null) {
    status = 'authenticated'
  }

  const value = useMemo(
    () => ({ status, session, refresh, signOut }),
    [refresh, session, signOut, status],
  )

  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>
}

export function useSession(): SessionContextValue {
  const value = useContext(SessionContext)
  if (value === null) throw new Error('useSession must be used inside SessionProvider.')
  return value
}
