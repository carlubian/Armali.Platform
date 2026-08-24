import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Clock, KeyRound, UserCheck, UserPlus, UserX } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'

import { adminUsersApi, type AdminUser } from '@/app/api/adminUsers'
import { isApiError } from '@/app/api/errors'
import { formatDate } from '@/app/i18n/formatters'
import { useSession } from '@/app/session/SessionContext'
import { ServiceUnavailable } from '@/components/feedback/SystemScreens'
import {
  Badge,
  Button,
  Dialog,
  Input,
  Select,
  Spinner,
  useToast,
} from '@/components/ui'

import './UsersPage.css'

const usersQueryKey = ['admin', 'users'] as const

/** True when the backend reported a field-level problem for `field`. */
function hasProblem(error: unknown, field: string): boolean {
  if (!isApiError(error)) return false
  return (error.problem?.errors?.[field]?.length ?? 0) > 0
}

/**
 * Account administration: the whole administrative surface of Blackwing.
 *
 * Creating an account, resetting its password and switching it on or off is all
 * an administrator can do. There is no way from here to another account's
 * images, and there is not meant to be one.
 */
export function UsersPage() {
  const { t, i18n } = useTranslation('platform')
  const { session } = useSession()
  const queryClient = useQueryClient()
  const { show } = useToast()

  const [createOpen, setCreateOpen] = useState(false)
  const [resetTarget, setResetTarget] = useState<AdminUser | null>(null)
  const [deactivateTarget, setDeactivateTarget] = useState<AdminUser | null>(null)

  const usersQuery = useQuery({
    queryKey: usersQueryKey,
    queryFn: ({ signal }) => adminUsersApi.list(signal),
  })

  const invalidateUsers = () =>
    queryClient.invalidateQueries({ queryKey: usersQueryKey })

  const createForm = useForm<CreateValues>({
    resolver: zodResolver(createSchema(t)),
    defaultValues: { userName: '', role: 'User', password: '' },
  })
  const resetForm = useForm<ResetValues>({
    resolver: zodResolver(resetSchema(t)),
    defaultValues: { newPassword: '', confirmPassword: '' },
  })

  const createMutation = useMutation({
    mutationFn: (values: CreateValues) =>
      adminUsersApi.create({
        userName: values.userName.trim(),
        role: values.role,
        password: values.password,
      }),
    onSuccess: async (created) => {
      await invalidateUsers()
      setCreateOpen(false)
      createForm.reset()
      show({
        title: t('admin.users.create.successTitle'),
        body: t('admin.users.create.successBody', { name: created.userName }),
      })
    },
    // A problem document with `errors` belongs on the offending field; anything
    // else has no field to attach to and is announced as a toast instead.
    onError: (error) => {
      if (hasProblem(error, 'userName')) {
        createForm.setError('userName', {
          message: t('admin.users.create.usernameInvalid'),
        })
      }
      if (hasProblem(error, 'password')) {
        createForm.setError('password', {
          message: t('admin.users.create.passwordInvalid'),
        })
      }
      if (!hasProblem(error, 'userName') && !hasProblem(error, 'password')) {
        show({ title: t('admin.users.create.error'), tone: 'danger' })
      }
    },
  })

  const resetMutation = useMutation({
    mutationFn: ({ id, newPassword }: { id: number; newPassword: string }) =>
      adminUsersApi.resetPassword(id, newPassword),
    onSuccess: () => {
      const name = resetTarget?.userName ?? ''
      setResetTarget(null)
      resetForm.reset()
      show({
        title: t('admin.users.reset.successTitle'),
        body: t('admin.users.reset.successBody', { name }),
      })
    },
    onError: (error) => {
      if (hasProblem(error, 'newPassword')) {
        resetForm.setError('newPassword', { message: t('admin.users.reset.error') })
        return
      }
      show({ title: t('admin.users.reset.error'), tone: 'danger' })
    },
  })

  const setActiveMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: number; isActive: boolean }) =>
      isActive ? adminUsersApi.activate(id) : adminUsersApi.deactivate(id),
    onSuccess: async () => {
      setDeactivateTarget(null)
      await invalidateUsers()
    },
    onError: () => {
      setDeactivateTarget(null)
      show({ title: t('admin.users.actionError'), tone: 'danger' })
    },
  })

  const submitCreate = createForm.handleSubmit((values) =>
    createMutation.mutate(values),
  )
  const submitReset = resetForm.handleSubmit((values) => {
    if (resetTarget === null) return
    resetMutation.mutate({ id: resetTarget.id, newPassword: values.newPassword })
  })

  const users = usersQuery.data ?? []

  if (
    usersQuery.isError &&
    isApiError(usersQuery.error) &&
    ['unavailable', 'transient'].includes(usersQuery.error.kind)
  ) {
    return <ServiceUnavailable onRetry={() => void usersQuery.refetch()} />
  }

  return (
    <div className="bw-users">
      <div className="bw-users__intro">
        <p className="bw-users__description">{t('admin.users.description')}</p>
        <Button
          variant="primary"
          iconLeft={<UserPlus size={17} aria-hidden="true" />}
          onClick={() => {
            createForm.reset()
            setCreateOpen(true)
          }}
        >
          {t('admin.users.newUser')}
        </Button>
      </div>

      {usersQuery.isPending ? (
        <div className="bw-users__loading">
          <Spinner label={t('common.loading')} />
        </div>
      ) : usersQuery.isError ? (
        <p className="bw-users__empty" role="alert">
          {t('admin.users.loadError')}
        </p>
      ) : users.length === 0 ? (
        <p className="bw-users__empty">{t('admin.users.empty')}</p>
      ) : (
        <ul className="bw-users__list">
          {users.map((user) => (
            <li
              key={user.id}
              className={'bw-ucard' + (user.isActive ? '' : ' bw-ucard--inactive')}
            >
              <div className="bw-ucard__id">
                <strong>{user.displayName}</strong>
                <em>{t('admin.users.handle', { name: user.userName })}</em>
              </div>
              <div className="bw-ucard__meta">
                <Badge tone={user.roles.includes('Admin') ? 'azure' : 'neutral'}>
                  {user.roles.includes('Admin')
                    ? t('admin.users.roleAdmin')
                    : t('admin.users.roleUser')}
                </Badge>
                {user.isActive ? (
                  <Badge tone="success" dot>
                    {t('admin.users.statusActive')}
                  </Badge>
                ) : (
                  <Badge tone="neutral" dot>
                    {t('admin.users.statusInactive')}
                  </Badge>
                )}
                <span className="bw-ucard__joined">
                  <Clock size={14} aria-hidden="true" />
                  {t('admin.users.joined', {
                    date: formatDate(user.createdAt, i18n.language),
                  })}
                </span>
              </div>
              <div className="bw-ucard__actions">
                <Button
                  size="sm"
                  variant="outline"
                  iconLeft={<KeyRound size={15} aria-hidden="true" />}
                  onClick={() => {
                    resetForm.reset()
                    setResetTarget(user)
                  }}
                >
                  {t('admin.users.resetPassword')}
                </Button>
                {user.isActive ? (
                  <Button
                    size="sm"
                    variant="ghost"
                    iconLeft={<UserX size={15} aria-hidden="true" />}
                    // An administrator cannot deactivate their own account; the
                    // backend refuses it too, this only saves the round trip.
                    disabled={
                      setActiveMutation.isPending || user.id === session?.userId
                    }
                    title={
                      user.id === session?.userId
                        ? t('admin.users.ownDeactivateLocked')
                        : undefined
                    }
                    onClick={() => setDeactivateTarget(user)}
                  >
                    {t('admin.users.deactivate')}
                  </Button>
                ) : (
                  <Button
                    size="sm"
                    variant="ghost"
                    iconLeft={<UserCheck size={15} aria-hidden="true" />}
                    disabled={setActiveMutation.isPending}
                    onClick={() =>
                      setActiveMutation.mutate({ id: user.id, isActive: true })
                    }
                  >
                    {t('admin.users.activate')}
                  </Button>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}

      <Dialog
        open={createOpen}
        title={t('admin.users.create.title')}
        description={t('admin.users.create.subtitle')}
        closeLabel={t('common.close')}
        width={460}
        onClose={() => setCreateOpen(false)}
        footer={
          <>
            <Button variant="ghost" onClick={() => setCreateOpen(false)}>
              {t('admin.users.create.cancel')}
            </Button>
            <Button
              variant="primary"
              iconLeft={<UserPlus size={17} aria-hidden="true" />}
              disabled={createMutation.isPending}
              onClick={() => void submitCreate()}
            >
              {createMutation.isPending
                ? t('admin.users.create.submitting')
                : t('admin.users.create.submit')}
            </Button>
          </>
        }
      >
        <form
          className="bw-users__form"
          noValidate
          onSubmit={(event) => void submitCreate(event)}
        >
          <Input
            label={t('admin.users.create.username')}
            placeholder={t('admin.users.create.usernamePlaceholder')}
            autoComplete="off"
            error={createForm.formState.errors.userName?.message}
            {...createForm.register('userName')}
          />
          <label className="bw-users__field">
            <span>{t('admin.users.create.role')}</span>
            <Select
              options={[
                { value: 'User', label: t('admin.users.roleUser') },
                { value: 'Admin', label: t('admin.users.roleAdmin') },
              ]}
              {...createForm.register('role')}
            />
          </label>
          <Input
            label={t('admin.users.create.password')}
            type="password"
            autoComplete="new-password"
            placeholder={t('admin.users.create.passwordPlaceholder')}
            hint={t('admin.users.create.passwordHint')}
            error={createForm.formState.errors.password?.message}
            {...createForm.register('password')}
          />
        </form>
      </Dialog>

      <Dialog
        open={resetTarget !== null}
        title={t('admin.users.reset.title')}
        description={t('admin.users.reset.subtitle', {
          name: resetTarget?.userName ?? '',
        })}
        closeLabel={t('common.close')}
        width={460}
        onClose={() => setResetTarget(null)}
        footer={
          <>
            <Button variant="ghost" onClick={() => setResetTarget(null)}>
              {t('admin.users.reset.cancel')}
            </Button>
            <Button
              variant="primary"
              iconLeft={<KeyRound size={16} aria-hidden="true" />}
              disabled={resetMutation.isPending}
              onClick={() => void submitReset()}
            >
              {resetMutation.isPending
                ? t('admin.users.reset.submitting')
                : t('admin.users.reset.submit')}
            </Button>
          </>
        }
      >
        <form
          className="bw-users__form"
          noValidate
          onSubmit={(event) => void submitReset(event)}
        >
          <Input
            label={t('admin.users.reset.new')}
            type="password"
            autoComplete="new-password"
            error={resetForm.formState.errors.newPassword?.message}
            {...resetForm.register('newPassword')}
          />
          <Input
            label={t('admin.users.reset.confirm')}
            type="password"
            autoComplete="new-password"
            error={resetForm.formState.errors.confirmPassword?.message}
            {...resetForm.register('confirmPassword')}
          />
        </form>
      </Dialog>

      <Dialog
        open={deactivateTarget !== null}
        title={t('admin.users.deactivateConfirm.title')}
        description={t('admin.users.deactivateConfirm.description', {
          name: deactivateTarget?.userName ?? '',
        })}
        closeLabel={t('common.close')}
        onClose={() => setDeactivateTarget(null)}
        footer={
          <>
            <Button variant="ghost" onClick={() => setDeactivateTarget(null)}>
              {t('admin.users.deactivateConfirm.cancel')}
            </Button>
            <Button
              variant="danger"
              iconLeft={<UserX size={16} aria-hidden="true" />}
              disabled={setActiveMutation.isPending}
              onClick={() => {
                if (deactivateTarget === null) return
                setActiveMutation.mutate({ id: deactivateTarget.id, isActive: false })
              }}
            >
              {t('admin.users.deactivateConfirm.confirm')}
            </Button>
          </>
        }
      />
    </div>
  )
}

type TFunc = ReturnType<typeof useTranslation<'platform'>>['t']

function createSchema(t: TFunc) {
  return z.object({
    userName: z.string().trim().min(1, t('admin.users.create.usernameRequired')),
    role: z.enum(['User', 'Admin']),
    password: z
      .string()
      .min(1, t('admin.users.create.passwordRequired'))
      // Mirrors the backend policy: twelve characters, no composition rules.
      .min(12, t('admin.users.create.passwordTooShort')),
  })
}

type CreateValues = z.infer<ReturnType<typeof createSchema>>

interface ResetValues {
  newPassword: string
  confirmPassword: string
}

function resetSchema(t: TFunc) {
  return z
    .object({
      newPassword: z
        .string()
        .min(1, t('admin.users.reset.newRequired'))
        .min(12, t('admin.users.reset.newTooShort')),
      confirmPassword: z.string().min(1, t('admin.users.reset.confirmRequired')),
    })
    .refine((values) => values.newPassword === values.confirmPassword, {
      path: ['confirmPassword'],
      message: t('admin.users.reset.mismatch'),
    })
}
