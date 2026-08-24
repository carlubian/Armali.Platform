import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { ArrowRight, Feather, Lock, TriangleAlert, UserRound } from 'lucide-react'
import { useMemo } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Navigate, useNavigate } from 'react-router-dom'
import { z } from 'zod'

import { isApiError } from '@/app/api/errors'
import { sessionApi } from '@/app/api/session'
import { useSession } from '@/app/session/SessionContext'
import { LoadingScreen } from '@/components/feedback/LoadingScreen'
import { Button, Input } from '@/components/ui'

import './LoginPage.css'

/**
 * The only public screen.
 *
 * Blackwing has no logo of its own: the brand is the `Feather` icon the shell
 * already uses, in the same aqua-to-azure mark.
 */
export function LoginPage() {
  const { t } = useTranslation('platform')
  const { status, refresh } = useSession()
  const navigate = useNavigate()

  const schema = useMemo(
    () =>
      z.object({
        userName: z.string().min(1, t('auth.login.usernameRequired')),
        password: z.string().min(1, t('auth.login.passwordRequired')),
      }),
    [t],
  )
  type LoginValues = z.infer<typeof schema>

  const {
    register,
    handleSubmit,
    setFocus,
    formState: { errors },
  } = useForm<LoginValues>({
    resolver: zodResolver(schema),
    defaultValues: { userName: '', password: '' },
  })

  const mutation = useMutation({
    mutationFn: (values: LoginValues) => sessionApi.signIn(values),
    onSuccess: async () => {
      await refresh()
      void navigate('/', { replace: true })
    },
    // A failed sign-in keeps the user on this screen; return focus to the first
    // field so a keyboard user can correct and retry immediately.
    onError: () => setFocus('userName'),
  })

  if (status === 'loading') return <LoadingScreen />
  if (status === 'authenticated') return <Navigate to="/" replace />

  const submit = handleSubmit((values) => mutation.mutate(values))

  // Failures are never specific about credentials: a wrong username, a wrong
  // password and a deactivated account are reported identically, because the
  // backend answers all three the same way and the screen must not invent a
  // distinction the API deliberately refuses to make.
  const formError = (() => {
    if (!mutation.isError) return null
    const error = mutation.error
    if (isApiError(error)) {
      if (error.status === 429) return t('auth.login.errorRateLimited')
      if (error.kind === 'authentication-expired' || error.kind === 'validation') {
        return t('auth.login.errorInvalid')
      }
    }
    return t('auth.login.errorGeneric')
  })()

  return (
    <main className="bw-login armali-aurora">
      <form
        className="bw-login__card"
        onSubmit={(event) => void submit(event)}
        aria-busy={mutation.isPending}
        noValidate
      >
        <div className="bw-login__brand">
          <span className="bw-login__brand-mark">
            <Feather size={26} aria-hidden="true" />
          </span>
          <span className="bw-login__brand-name">{t('app.name')}</span>
        </div>

        <h1 className="bw-login__title">{t('auth.login.title')}</h1>
        <p className="bw-login__sub">{t('auth.login.subtitle')}</p>

        {formError != null && (
          <div className="bw-login__error" role="alert">
            <TriangleAlert size={17} aria-hidden="true" />
            <span>{formError}</span>
          </div>
        )}

        <div className="bw-login__fields">
          <Input
            label={t('auth.login.usernameLabel')}
            placeholder={t('auth.login.usernamePlaceholder')}
            autoComplete="username"
            iconLeft={<UserRound size={17} aria-hidden="true" />}
            error={errors.userName?.message}
            {...register('userName')}
          />
          <Input
            label={t('auth.login.passwordLabel')}
            type="password"
            placeholder={t('auth.login.passwordPlaceholder')}
            autoComplete="current-password"
            iconLeft={<Lock size={17} aria-hidden="true" />}
            error={errors.password?.message}
            {...register('password')}
          />
        </div>

        <Button
          type="submit"
          block
          size="lg"
          disabled={mutation.isPending}
          iconRight={
            mutation.isPending ? undefined : <ArrowRight size={18} aria-hidden="true" />
          }
        >
          {mutation.isPending ? t('auth.login.submitting') : t('auth.login.submit')}
        </Button>

        <p className="bw-login__foot">{t('auth.login.footer')}</p>
      </form>
    </main>
  )
}
