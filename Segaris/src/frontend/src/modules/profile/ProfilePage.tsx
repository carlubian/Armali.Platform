import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Check, ImageUp, Lock, Trash2, TriangleAlert } from 'lucide-react'
import { useEffect, useMemo, useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useBeforeUnload, useBlocker } from 'react-router-dom'
import { z } from 'zod'

import { isApiError } from '@/app/api/errors'
import { sessionApi, type Profile } from '@/app/api/session'
import { useSession } from '@/app/session/SessionContext'
import {
  Avatar,
  Badge,
  Button,
  Card,
  Dialog,
  Input,
  Select,
  Toast,
} from '@/components/ui'

import './ProfilePage.css'

const profileQueryKey = ['session', 'profile'] as const
const maximumAvatarSize = 25 * 1024 * 1024
const allowedAvatarTypes = new Set(['image/jpeg', 'image/png', 'image/webp'])
const supportedLanguages = [{ value: 'en-GB', label: 'English (en-GB)' }] as const

function hasProblem(error: unknown, field: string): boolean {
  if (!isApiError(error)) return false
  return (error.problem?.errors?.[field]?.length ?? 0) > 0
}

export function ProfilePage() {
  const { t } = useTranslation('platform')
  const { session } = useSession()
  const queryClient = useQueryClient()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [avatarError, setAvatarError] = useState<string | null>(null)
  const [passwordSaved, setPasswordSaved] = useState(false)

  const profileSchema = useMemo(
    () =>
      z.object({
        displayName: z
          .string()
          .trim()
          .min(1, t('profile.details.displayNameRequired'))
          .max(200, t('profile.details.displayNameTooLong')),
        language: z.enum(['en-GB']),
      }),
    [t],
  )
  type ProfileValues = z.infer<typeof profileSchema>

  const passwordSchema = useMemo(
    () =>
      z
        .object({
          currentPassword: z.string().min(1, t('profile.password.currentRequired')),
          newPassword: z
            .string()
            .min(1, t('profile.password.newRequired'))
            .min(12, t('profile.password.newTooShort')),
          confirmPassword: z.string().min(1, t('profile.password.confirmRequired')),
        })
        .refine((values) => values.newPassword === values.confirmPassword, {
          path: ['confirmPassword'],
          message: t('profile.password.mismatch'),
        }),
    [t],
  )
  type PasswordValues = z.infer<typeof passwordSchema>

  const profileForm = useForm<ProfileValues>({
    resolver: zodResolver(profileSchema),
    values: {
      displayName: session?.displayName ?? '',
      language: session?.language === 'en-GB' ? 'en-GB' : 'en-GB',
    },
  })
  const passwordForm = useForm<PasswordValues>({
    resolver: zodResolver(passwordSchema),
    defaultValues: { currentPassword: '', newPassword: '', confirmPassword: '' },
  })

  const hasUnsavedChanges =
    profileForm.formState.isDirty || passwordForm.formState.isDirty
  const blocker = useBlocker(hasUnsavedChanges)
  useBeforeUnload(
    (event) => {
      if (!hasUnsavedChanges) return
      event.preventDefault()
      event.returnValue = ''
    },
    { capture: true },
  )

  const profileMutation = useMutation({
    mutationFn: (values: ProfileValues) => sessionApi.updateProfile(values),
    onSuccess: (profile) => {
      queryClient.setQueryData(profileQueryKey, profile)
      profileForm.reset({
        displayName: profile.displayName,
        language: profile.language as 'en-GB',
      })
    },
    onError: (error) => {
      if (hasProblem(error, 'displayName')) {
        profileForm.setError('displayName', {
          message: t('profile.details.displayNameRequired'),
        })
      }
      if (hasProblem(error, 'language')) {
        profileForm.setError('language', {
          message: t('profile.details.languageUnsupported'),
        })
      }
    },
  })

  const passwordMutation = useMutation({
    mutationFn: ({ currentPassword, newPassword }: PasswordValues) =>
      sessionApi.changePassword({ currentPassword, newPassword }),
    onSuccess: () => {
      passwordForm.reset()
      setPasswordSaved(true)
    },
    onError: (error) => {
      if (hasProblem(error, 'newPassword')) {
        passwordForm.setError('currentPassword', {
          message: t('profile.password.rejected'),
        })
      }
      passwordForm.setFocus('currentPassword')
    },
  })

  const avatarMutation = useMutation({
    mutationFn: (file: File) => sessionApi.uploadAvatar(file),
    onSuccess: (avatar) => {
      const current = queryClient.getQueryData<Profile>(profileQueryKey)
      if (current != null) {
        queryClient.setQueryData(profileQueryKey, {
          ...current,
          avatarUrl: `${avatar.avatarUrl}?v=${Date.now()}`,
        })
      }
      setAvatarError(null)
    },
    onError: (error) =>
      setAvatarError(
        hasProblem(error, 'file')
          ? t('profile.avatar.invalidType')
          : t('profile.avatar.uploadError'),
      ),
  })

  const removeAvatarMutation = useMutation({
    mutationFn: () => sessionApi.removeAvatar(),
    onSuccess: () => {
      const current = queryClient.getQueryData<Profile>(profileQueryKey)
      if (current != null)
        queryClient.setQueryData(profileQueryKey, { ...current, avatarUrl: null })
      setAvatarError(null)
    },
    onError: () => setAvatarError(t('profile.avatar.removeError')),
  })

  useEffect(() => {
    if (!passwordSaved) return
    const timeout = window.setTimeout(() => setPasswordSaved(false), 5_000)
    return () => window.clearTimeout(timeout)
  }, [passwordSaved])

  const selectAvatar = (file: File | undefined) => {
    if (file == null) return
    if (!allowedAvatarTypes.has(file.type)) {
      setAvatarError(t('profile.avatar.invalidType'))
      return
    }
    if (file.size > maximumAvatarSize) {
      setAvatarError(t('profile.avatar.tooLarge'))
      return
    }
    avatarMutation.mutate(file)
  }

  const profileError =
    profileMutation.isError &&
    !hasProblem(profileMutation.error, 'displayName') &&
    !hasProblem(profileMutation.error, 'language')
      ? t('profile.details.saveError')
      : null
  const passwordError = passwordMutation.isError ? t('profile.password.error') : null
  const isAdmin = session?.roles.includes('Admin') ?? false

  return (
    <main className="seg-profile-page armali-aurora">
      <section className="seg-profile-page__head">
        <div className="armali-eyebrow">{t('profile.eyebrow')}</div>
        <h1>{t('profile.title')}</h1>
      </section>

      <div className="seg-profile-layout">
        <aside className="seg-profile-side">
          <Avatar
            name={session?.displayName ?? session?.userName ?? ''}
            src={session?.avatarUrl ?? undefined}
            size="lg"
            status="online"
          />
          <div>
            <div className="seg-profile-side__name">{session?.displayName}</div>
            <div className="seg-profile-side__username">@{session?.userName}</div>
          </div>
          <div className="seg-profile-side__badges">
            <Badge tone="azure">
              {isAdmin ? t('profile.adminRole') : t('profile.userRole')}
            </Badge>
            <Badge tone="success" dot>
              {t('profile.active')}
            </Badge>
          </div>
          <input
            ref={fileInputRef}
            className="seg-profile-side__file"
            type="file"
            accept="image/jpeg,image/png,image/webp"
            aria-label={t('profile.avatar.inputLabel')}
            onChange={(event) => {
              selectAvatar(event.target.files?.[0])
              event.target.value = ''
            }}
          />
          <Button
            variant="outline"
            size="sm"
            iconLeft={<ImageUp size={16} />}
            disabled={avatarMutation.isPending || removeAvatarMutation.isPending}
            onClick={() => fileInputRef.current?.click()}
          >
            {session?.avatarUrl
              ? t('profile.avatar.replace')
              : t('profile.avatar.change')}
          </Button>
          {session?.avatarUrl != null && (
            <Button
              variant="ghost"
              size="sm"
              iconLeft={<Trash2 size={16} />}
              disabled={avatarMutation.isPending || removeAvatarMutation.isPending}
              onClick={() => removeAvatarMutation.mutate()}
            >
              {t('profile.avatar.remove')}
            </Button>
          )}
          <p className="seg-profile-side__hint">{t('profile.avatar.hint')}</p>
          {avatarError != null && (
            <p className="seg-profile-side__error" role="alert">
              {avatarError}
            </p>
          )}
        </aside>

        <div className="seg-profile-main">
          <form
            onSubmit={(event) =>
              void profileForm.handleSubmit((values) => profileMutation.mutate(values))(
                event,
              )
            }
            noValidate
          >
            <Card
              title={t('profile.details.title')}
              subtitle={t('profile.details.subtitle')}
            >
              {profileError != null && (
                <div className="seg-profile-form-error" role="alert">
                  <TriangleAlert size={17} /> {profileError}
                </div>
              )}
              <div className="seg-profile-form-grid">
                <Input
                  label={t('profile.details.displayName')}
                  error={profileForm.formState.errors.displayName?.message}
                  {...profileForm.register('displayName')}
                />
                <Input
                  label={t('profile.details.username')}
                  value={session?.userName ?? ''}
                  hint={t('profile.details.usernameHint')}
                  disabled
                  readOnly
                />
                <label className="seg-profile-select-label">
                  <span>{t('profile.details.language')}</span>
                  <Select
                    options={supportedLanguages}
                    aria-invalid={profileForm.formState.errors.language != null}
                    {...profileForm.register('language')}
                  />
                </label>
              </div>
              <div className="seg-profile-actions">
                <Button
                  variant="ghost"
                  disabled={!profileForm.formState.isDirty || profileMutation.isPending}
                  onClick={() => profileForm.reset()}
                >
                  {t('profile.details.discard')}
                </Button>
                <Button
                  type="submit"
                  iconLeft={<Check size={17} />}
                  disabled={!profileForm.formState.isDirty || profileMutation.isPending}
                >
                  {profileMutation.isPending
                    ? t('profile.details.saving')
                    : t('profile.details.save')}
                </Button>
              </div>
            </Card>
          </form>

          <form
            onSubmit={(event) =>
              void passwordForm.handleSubmit((values) =>
                passwordMutation.mutate(values),
              )(event)
            }
            noValidate
          >
            <Card
              title={t('profile.password.title')}
              subtitle={t('profile.password.subtitle')}
            >
              {passwordError != null && (
                <div className="seg-profile-form-error" role="alert">
                  <TriangleAlert size={17} /> {passwordError}
                </div>
              )}
              <div className="seg-profile-form-grid">
                <Input
                  label={t('profile.password.current')}
                  type="password"
                  autoComplete="current-password"
                  iconLeft={<Lock size={16} />}
                  error={passwordForm.formState.errors.currentPassword?.message}
                  {...passwordForm.register('currentPassword')}
                />
                <span />
                <Input
                  label={t('profile.password.new')}
                  type="password"
                  autoComplete="new-password"
                  error={passwordForm.formState.errors.newPassword?.message}
                  {...passwordForm.register('newPassword')}
                />
                <Input
                  label={t('profile.password.confirm')}
                  type="password"
                  autoComplete="new-password"
                  error={passwordForm.formState.errors.confirmPassword?.message}
                  {...passwordForm.register('confirmPassword')}
                />
              </div>
              <div className="seg-profile-actions">
                <Button
                  type="submit"
                  disabled={
                    !passwordForm.formState.isDirty || passwordMutation.isPending
                  }
                >
                  {passwordMutation.isPending
                    ? t('profile.password.submitting')
                    : t('profile.password.submit')}
                </Button>
              </div>
            </Card>
          </form>
        </div>
      </div>

      {passwordSaved && (
        <div className="seg-profile-toast">
          <Toast
            tone="success"
            title={t('profile.password.successTitle')}
            onClose={() => setPasswordSaved(false)}
            closeLabel={t('common.close')}
          >
            {t('profile.password.successBody')}
          </Toast>
        </div>
      )}

      <Dialog
        open={blocker.state === 'blocked'}
        title={t('profile.unsaved.title')}
        description={t('profile.unsaved.description')}
        closeLabel={t('common.close')}
        onClose={() => blocker.reset?.()}
        footer={
          <>
            <Button variant="ghost" onClick={() => blocker.reset?.()}>
              {t('profile.unsaved.stay')}
            </Button>
            <Button variant="danger" onClick={() => blocker.proceed?.()}>
              {t('profile.unsaved.leave')}
            </Button>
          </>
        }
      />
    </main>
  )
}
