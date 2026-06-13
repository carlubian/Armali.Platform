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
import { sessionApi, type Profile, type Session } from '@/app/api/session'
import { i18n } from '@/app/i18n/i18n'

const sessionQueryKey = ['session'] as const
const profileQueryKey = ['session', 'profile'] as const

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

async function loadSession(signal?: AbortSignal): Promise<Session | null> {
  try {
    return await sessionApi.getSession(signal)
  } catch (error) {
    if (isApiError(error) && error.kind === 'authentication-expired') return null
    throw error
  }
}

export function SessionProvider({ children }: PropsWithChildren) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const sessionQuery = useQuery({
    queryKey: sessionQueryKey,
    queryFn: ({ signal }) => loadSession(signal),
    staleTime: 30_000,
  })
  const profileQuery = useQuery({
    queryKey: profileQueryKey,
    queryFn: ({ signal }) => sessionApi.getProfile(signal),
    enabled: sessionQuery.data != null,
    staleTime: 30_000,
  })

  const expireSession = useCallback(() => {
    queryClient.setQueryData(sessionQueryKey, null)
    queryClient.removeQueries({ queryKey: profileQueryKey })
    resetCsrfToken()
    void navigate('/login', { replace: true })
  }, [navigate, queryClient])

  useEffect(() => {
    window.addEventListener(SESSION_EXPIRED_EVENT, expireSession)
    return () => window.removeEventListener(SESSION_EXPIRED_EVENT, expireSession)
  }, [expireSession])

  const session = useMemo(() => {
    if (sessionQuery.data == null) return null
    const profile: Profile | undefined = profileQuery.data
    return profile == null ? sessionQuery.data : { ...sessionQuery.data, ...profile }
  }, [profileQuery.data, sessionQuery.data])

  useEffect(() => {
    if (session?.language != null) void i18n.changeLanguage(session.language)
  }, [session?.language])

  const refresh = useCallback(async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: sessionQueryKey }),
      queryClient.invalidateQueries({ queryKey: profileQueryKey }),
    ])
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
