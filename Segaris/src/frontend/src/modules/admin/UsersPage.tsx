import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  ChevronLeft,
  ChevronRight,
  Clock,
  KeyRound,
  Pencil,
  UserCheck,
  UserPlus,
  UserX,
} from 'lucide-react'
import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'

import {
  adminUsersApi,
  type AdminUser,
  type AdminUserSortField,
  type SortDirection,
} from '@/app/api/adminUsers'
import { isApiError } from '@/app/api/errors'
import { formatDate } from '@/app/i18n/formatters'
import { useSession } from '@/app/session/SessionContext'
import { AccessDenied, ServiceUnavailable } from '@/components/feedback/SystemScreens'
import {
  Avatar,
  Badge,
  Button,
  Dialog,
  Input,
  Select,
  Spinner,
  Toast,
} from '@/components/ui'

import './UsersPage.css'

const pageSize = 12

type SortOption = 'newest' | 'oldest' | 'nameAsc' | 'nameDesc'

const sortMap: Record<
  SortOption,
  { sort: AdminUserSortField; sortDirection: SortDirection }
> = {
  newest: { sort: 'createdAt', sortDirection: 'desc' },
  oldest: { sort: 'createdAt', sortDirection: 'asc' },
  nameAsc: { sort: 'userName', sortDirection: 'asc' },
  nameDesc: { sort: 'userName', sortDirection: 'desc' },
}

function hasProblem(error: unknown, field: string): boolean {
  if (!isApiError(error)) return false
  return (error.problem?.errors?.[field]?.length ?? 0) > 0
}

interface ToastState {
  title: string
  body: string
}

export function UsersPage() {
  const { t, i18n } = useTranslation('platform')
  const { session } = useSession()
  const queryClient = useQueryClient()

  const [page, setPage] = useState(1)
  const [sortOption, setSortOption] = useState<SortOption>('newest')
  const [createOpen, setCreateOpen] = useState(false)
  const [editTarget, setEditTarget] = useState<AdminUser | null>(null)
  const [resetTarget, setResetTarget] = useState<AdminUser | null>(null)
  const [deactivateTarget, setDeactivateTarget] = useState<AdminUser | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [toast, setToast] = useState<ToastState | null>(null)

  const isAdmin = session?.roles.includes('Admin') ?? false
  const sort = sortMap[sortOption]

  const usersQuery = useQuery({
    queryKey: ['admin', 'users', page, sortOption] as const,
    queryFn: ({ signal }) =>
      adminUsersApi.list(
        { page, pageSize, sort: sort.sort, sortDirection: sort.sortDirection },
        signal,
      ),
    enabled: isAdmin,
  })

  const invalidateUsers = () =>
    queryClient.invalidateQueries({ queryKey: ['admin', 'users'] })

  const createForm = useForm<CreateValues>({
    resolver: zodResolver(createSchema(t)),
    defaultValues: { userName: '', role: 'User', password: '' },
  })
  const editForm = useForm<EditValues>({
    resolver: zodResolver(editSchema(t)),
    defaultValues: { displayName: '', role: 'User' },
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
      setToast({
        title: t('admin.users.create.successTitle'),
        body: t('admin.users.create.successBody', { name: created.displayName }),
      })
    },
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
      if (hasProblem(error, 'role')) {
        createForm.setError('role', { message: t('admin.users.create.error') })
      }
    },
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, values }: { id: number; values: EditValues }) =>
      adminUsersApi.update(id, {
        displayName: values.displayName.trim(),
        role: values.role,
      }),
    onSuccess: async (updated) => {
      await invalidateUsers()
      setEditTarget(null)
      editForm.reset()
      setToast({
        title: t('admin.users.edit.successTitle'),
        body: t('admin.users.edit.successBody', { name: updated.displayName }),
      })
    },
    onError: (error) => {
      if (hasProblem(error, 'displayName')) {
        editForm.setError('displayName', {
          message: t('admin.users.edit.displayNameInvalid'),
        })
      }
      if (hasProblem(error, 'role')) {
        editForm.setError('role', { message: t('admin.users.edit.roleError') })
      }
    },
  })

  const resetMutation = useMutation({
    mutationFn: ({ id, newPassword }: { id: number; newPassword: string }) =>
      adminUsersApi.resetPassword(id, newPassword),
    onSuccess: () => {
      const name = resetTarget?.displayName ?? ''
      setResetTarget(null)
      resetForm.reset()
      setToast({
        title: t('admin.users.reset.successTitle'),
        body: t('admin.users.reset.successBody', { name }),
      })
    },
    onError: () => {
      resetForm.setError('newPassword', { message: t('admin.users.reset.error') })
    },
  })

  const setActiveMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: number; isActive: boolean }) =>
      isActive ? adminUsersApi.activate(id) : adminUsersApi.deactivate(id),
    onSuccess: async () => {
      setActionError(null)
      setDeactivateTarget(null)
      await invalidateUsers()
    },
    onError: () => setActionError(t('admin.users.actionError')),
  })

  const submitCreate = createForm.handleSubmit((values) =>
    createMutation.mutate(values),
  )
  const submitEdit = editForm.handleSubmit((values) => {
    if (editTarget == null) return
    updateMutation.mutate({ id: editTarget.id, values })
  })
  const openEdit = (user: AdminUser) => {
    editForm.reset({
      displayName: user.displayName,
      role: user.roles.includes('Admin') ? 'Admin' : 'User',
    })
    setEditTarget(user)
  }
  const editingSelf = editTarget != null && editTarget.id === session?.userId
  const submitReset = resetForm.handleSubmit((values) => {
    if (resetTarget == null) return
    resetMutation.mutate({ id: resetTarget.id, newPassword: values.newPassword })
  })

  const data = usersQuery.data
  const items = useMemo(() => data?.items ?? [], [data])
  const totalCount = data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const activeOnPage = items.filter((user) => user.isActive).length
  const adminsOnPage = items.filter((user) => user.roles.includes('Admin')).length

  const changeSort = (value: string) => {
    setSortOption(value as SortOption)
    setPage(1)
  }

  if (!isAdmin) return <AccessDenied />
  if (usersQuery.isError) {
    const error = usersQuery.error
    if (isApiError(error) && error.kind === 'authorization-denied') {
      return <AccessDenied />
    }
    if (isApiError(error) && ['unavailable', 'transient'].includes(error.kind)) {
      return <ServiceUnavailable onRetry={() => void usersQuery.refetch()} />
    }
  }

  return (
    <main className="seg-users-page armali-aurora">
      <div className="seg-users">
        <div className="seg-users__bar">
          <div>
            <div className="armali-eyebrow">{t('admin.users.eyebrow')}</div>
            <h1>{t('admin.users.title')}</h1>
            <p>{t('admin.users.description')}</p>
          </div>
          <div className="seg-users__stats">
            <div className="seg-stat-pill">
              <strong>{totalCount}</strong>
              <span>{t('admin.users.stats.members')}</span>
            </div>
            <div className="seg-stat-pill">
              <strong>{activeOnPage}</strong>
              <span>{t('admin.users.stats.active')}</span>
            </div>
            <div className="seg-stat-pill">
              <strong>{adminsOnPage}</strong>
              <span>{t('admin.users.stats.admins')}</span>
            </div>
            <Button
              variant="primary"
              iconLeft={<UserPlus size={17} />}
              onClick={() => {
                createForm.reset()
                setCreateOpen(true)
              }}
            >
              {t('admin.users.newUser')}
            </Button>
          </div>
        </div>

        <div className="seg-users__toolbar">
          <label className="seg-users__sort">
            <span>{t('admin.users.sortLabel')}</span>
            <Select
              value={sortOption}
              onChange={(event) => changeSort(event.target.value)}
              options={[
                { value: 'newest', label: t('admin.users.sort.newest') },
                { value: 'oldest', label: t('admin.users.sort.oldest') },
                { value: 'nameAsc', label: t('admin.users.sort.nameAsc') },
                { value: 'nameDesc', label: t('admin.users.sort.nameDesc') },
              ]}
            />
          </label>
        </div>

        {actionError != null && (
          <div className="seg-users__error" role="alert">
            {actionError}
          </div>
        )}

        {usersQuery.isPending ? (
          <div className="seg-users__loading">
            <Spinner />
          </div>
        ) : items.length === 0 ? (
          <p className="seg-users__empty">{t('admin.users.empty')}</p>
        ) : (
          <div className="seg-ucards">
            {items.map((user) => (
              <div
                key={user.id}
                className={'seg-ucard' + (user.isActive ? '' : ' is-inactive')}
              >
                <div className="seg-ucard__top">
                  <Avatar
                    name={user.displayName}
                    src={user.avatarUrl ?? undefined}
                    size="lg"
                    status={user.isActive ? 'online' : undefined}
                  />
                  <div className="seg-ucard__id">
                    <strong>{user.displayName}</strong>
                    <em>@{user.userName}</em>
                  </div>
                </div>
                <div className="seg-ucard__meta">
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
                </div>
                <div className="seg-ucard__joined">
                  <Clock size={14} />{' '}
                  {t('admin.users.lastActive', {
                    date: formatDate(user.createdAt, i18n.language),
                  })}
                </div>
                <div className="seg-ucard__foot">
                  <Button
                    size="sm"
                    variant="outline"
                    iconLeft={<Pencil size={15} />}
                    onClick={() => openEdit(user)}
                  >
                    {t('admin.users.edit.action')}
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    iconLeft={<KeyRound size={15} />}
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
                      iconLeft={<UserX size={15} />}
                      onClick={() => setDeactivateTarget(user)}
                    >
                      {t('admin.users.deactivate')}
                    </Button>
                  ) : (
                    <Button
                      size="sm"
                      variant="ghost"
                      iconLeft={<UserCheck size={15} />}
                      disabled={setActiveMutation.isPending}
                      onClick={() =>
                        setActiveMutation.mutate({ id: user.id, isActive: true })
                      }
                    >
                      {t('admin.users.activate')}
                    </Button>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}

        {totalPages > 1 && (
          <nav className="seg-users__pager" aria-label={t('admin.users.title')}>
            <Button
              variant="ghost"
              size="sm"
              iconLeft={<ChevronLeft size={16} />}
              disabled={page <= 1 || usersQuery.isFetching}
              onClick={() => setPage((current) => Math.max(1, current - 1))}
            >
              {t('admin.users.pagination.previous')}
            </Button>
            <span className="seg-users__page" aria-live="polite">
              {t('admin.users.pagination.status', { page, pages: totalPages })}
            </span>
            <Button
              variant="ghost"
              size="sm"
              iconRight={<ChevronRight size={16} />}
              disabled={page >= totalPages || usersQuery.isFetching}
              onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
            >
              {t('admin.users.pagination.next')}
            </Button>
          </nav>
        )}
      </div>

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
              iconLeft={<UserPlus size={17} />}
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
          id="seg-create-user"
          className="seg-users__form"
          noValidate
          onSubmit={(event) => void submitCreate(event)}
        >
          {createMutation.isError &&
            !hasProblem(createMutation.error, 'userName') &&
            !hasProblem(createMutation.error, 'password') &&
            !hasProblem(createMutation.error, 'role') && (
              <div className="seg-users__error" role="alert">
                {t('admin.users.create.error')}
              </div>
            )}
          <Input
            label={t('admin.users.create.username')}
            placeholder={t('admin.users.create.usernamePlaceholder')}
            autoComplete="off"
            error={createForm.formState.errors.userName?.message}
            {...createForm.register('userName')}
          />
          <label className="seg-users__field">
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
        open={editTarget != null}
        title={t('admin.users.edit.title')}
        description={t('admin.users.edit.subtitle', {
          name: editTarget?.displayName ?? '',
        })}
        closeLabel={t('common.close')}
        width={460}
        onClose={() => setEditTarget(null)}
        footer={
          <>
            <Button variant="ghost" onClick={() => setEditTarget(null)}>
              {t('admin.users.edit.cancel')}
            </Button>
            <Button
              variant="primary"
              iconLeft={<Pencil size={16} />}
              disabled={updateMutation.isPending}
              onClick={() => void submitEdit()}
            >
              {updateMutation.isPending
                ? t('admin.users.edit.submitting')
                : t('admin.users.edit.submit')}
            </Button>
          </>
        }
      >
        <form
          id="seg-edit-user"
          className="seg-users__form"
          noValidate
          onSubmit={(event) => void submitEdit(event)}
        >
          {updateMutation.isError &&
            !hasProblem(updateMutation.error, 'displayName') &&
            !hasProblem(updateMutation.error, 'role') && (
              <div className="seg-users__error" role="alert">
                {t('admin.users.edit.error')}
              </div>
            )}
          <Input
            label={t('admin.users.edit.displayName')}
            autoComplete="off"
            error={editForm.formState.errors.displayName?.message}
            {...editForm.register('displayName')}
          />
          <div className="seg-users__field">
            <span>{t('admin.users.edit.role')}</span>
            <Select
              aria-label={t('admin.users.edit.role')}
              options={[
                { value: 'User', label: t('admin.users.roleUser') },
                { value: 'Admin', label: t('admin.users.roleAdmin') },
              ]}
              disabled={editingSelf}
              {...editForm.register('role')}
            />
            {editingSelf && (
              <span className="arm-field__hint">
                {t('admin.users.edit.ownRoleLocked')}
              </span>
            )}
            {editForm.formState.errors.role?.message != null && (
              <span className="arm-field__hint arm-field__hint--error">
                {editForm.formState.errors.role.message}
              </span>
            )}
          </div>
        </form>
      </Dialog>

      <Dialog
        open={resetTarget != null}
        title={t('admin.users.reset.title')}
        description={t('admin.users.reset.subtitle', {
          name: resetTarget?.displayName ?? '',
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
              iconLeft={<KeyRound size={16} />}
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
          id="seg-reset-password"
          className="seg-users__form"
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
        open={deactivateTarget != null}
        title={t('admin.users.deactivateConfirm.title')}
        description={t('admin.users.deactivateConfirm.description', {
          name: deactivateTarget?.displayName ?? '',
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
              iconLeft={<UserX size={16} />}
              disabled={setActiveMutation.isPending}
              onClick={() => {
                if (deactivateTarget == null) return
                setActiveMutation.mutate({ id: deactivateTarget.id, isActive: false })
              }}
            >
              {t('admin.users.deactivateConfirm.confirm')}
            </Button>
          </>
        }
      />

      {toast != null && (
        <div className="seg-users__toast">
          <Toast
            tone="success"
            title={toast.title}
            onClose={() => setToast(null)}
            closeLabel={t('common.close')}
          >
            {toast.body}
          </Toast>
        </div>
      )}
    </main>
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
      .min(12, t('admin.users.create.passwordTooShort')),
  })
}

type CreateValues = z.infer<ReturnType<typeof createSchema>>

function editSchema(t: TFunc) {
  return z.object({
    displayName: z
      .string()
      .trim()
      .min(1, t('admin.users.edit.displayNameRequired'))
      .max(200, t('admin.users.edit.displayNameTooLong')),
    role: z.enum(['User', 'Admin']),
  })
}

type EditValues = z.infer<ReturnType<typeof editSchema>>

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
