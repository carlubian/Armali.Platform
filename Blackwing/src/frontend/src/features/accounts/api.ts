// Client for the admin-only account management API (/api/admin/accounts). Every
// mutating call carries the antiforgery token, mirroring the rest of the app.
import { csrf } from '@/features/upload/api'

export type Account = { id: string; username: string; role: 'User' | 'Admin' }

/** Surfaces the server's per-field validation messages (e.g. weak password, duplicate name). */
export class AccountError extends Error {}

async function readError(response: Response, fallback: string): Promise<string> {
  try {
    const problem = await response.json() as { title?: string; errors?: Record<string, string[]> }
    const fromErrors = problem.errors ? Object.values(problem.errors).flat().join(' ') : ''
    return fromErrors || problem.title || fallback
  } catch {
    return fallback
  }
}

export async function fetchAccounts(): Promise<Account[]> {
  const response = await fetch('/api/admin/accounts/')
  if (!response.ok) throw new AccountError('Could not load the accounts.')
  return ((await response.json()) as { accounts?: Account[] }).accounts ?? []
}

export async function createAccount(username: string, password: string): Promise<Account> {
  const response = await fetch('/api/admin/accounts/', { method: 'POST', headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': await csrf() }, body: JSON.stringify({ username, password }) })
  if (!response.ok) throw new AccountError(await readError(response, 'Could not create the account.'))
  return response.json() as Promise<Account>
}

export async function renameAccount(id: string, username: string): Promise<Account> {
  const response = await fetch(`/api/admin/accounts/${id}`, { method: 'PUT', headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': await csrf() }, body: JSON.stringify({ username }) })
  if (!response.ok) throw new AccountError(await readError(response, 'Could not rename the account.'))
  return response.json() as Promise<Account>
}

export async function resetPassword(id: string, password: string): Promise<void> {
  const response = await fetch(`/api/admin/accounts/${id}/password`, { method: 'POST', headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': await csrf() }, body: JSON.stringify({ password }) })
  if (!response.ok) throw new AccountError(await readError(response, 'Could not reset the password.'))
}

export async function deleteAccount(id: string): Promise<void> {
  const response = await fetch(`/api/admin/accounts/${id}`, { method: 'DELETE', headers: { 'X-CSRF-TOKEN': await csrf() } })
  if (!response.ok) throw new AccountError(await readError(response, 'Could not delete the account.'))
}
