import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { KeyRound, Pencil, Trash2, UserPlus, X } from 'lucide-react'
import { AccountError, createAccount, deleteAccount, fetchAccounts, renameAccount, resetPassword } from './api'
import type { Account } from './api'

type RowMode = { id: string; kind: 'rename' | 'password' | 'delete' }
const message = (reason: unknown, fallback: string) => reason instanceof AccountError ? reason.message : fallback

export function AccountsPage({ currentUserId }: { currentUserId: string }) {
  const [accounts, setAccounts] = useState<Account[] | null>(null)
  const [error, setError] = useState('')
  const [creating, setCreating] = useState(false)
  const [row, setRow] = useState<RowMode | null>(null)
  const [busy, setBusy] = useState(false)

  const load = () => fetchAccounts().then(accounts => { setAccounts(accounts); setError('') }).catch(reason => setError(message(reason, 'Could not load the accounts.')))
  useEffect(() => { void load() }, [])

  const open = (mode: RowMode) => { setRow(mode); setError('') }
  const close = () => { setRow(null); setError('') }

  const submitCreate = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    setBusy(true); setError('')
    try {
      await createAccount(String(data.get('username')), String(data.get('password')))
      setCreating(false); await load()
    } catch (reason) { setError(message(reason, 'Could not create the account.')) }
    finally { setBusy(false) }
  }

  const submitRename = async (event: FormEvent<HTMLFormElement>, id: string) => {
    event.preventDefault()
    const username = String(new FormData(event.currentTarget).get('username'))
    setBusy(true); setError('')
    try { await renameAccount(id, username); close(); await load() }
    catch (reason) { setError(message(reason, 'Could not rename the account.')) }
    finally { setBusy(false) }
  }

  const submitPassword = async (event: FormEvent<HTMLFormElement>, id: string) => {
    event.preventDefault()
    const password = String(new FormData(event.currentTarget).get('password'))
    setBusy(true); setError('')
    try { await resetPassword(id, password); close(); await load() }
    catch (reason) { setError(message(reason, 'Could not reset the password.')) }
    finally { setBusy(false) }
  }

  const confirmDelete = async (id: string) => {
    setBusy(true); setError('')
    try { await deleteAccount(id); close(); await load() }
    catch (reason) { setError(message(reason, 'Could not delete the account.')) }
    finally { setBusy(false) }
  }

  return (
    <section className="accounts-page">
      <div className="eyebrow">BLACKWING</div>
      <h1>Manage accounts</h1>
      <p>Create local accounts, rename them and reset passwords. Gallery content is never available to administrators.</p>

      <div className="accounts-toolbar">
        <span className="result-count">{accounts ? `${accounts.length} ${accounts.length === 1 ? 'account' : 'accounts'}` : 'Loading…'}</span>
        <button className="button-outline" onClick={() => { setCreating(value => !value); close() }}><UserPlus size={16} /> New account</button>
      </div>

      {error && <p role="alert" className="form-error">{error}</p>}

      {creating && (
        <form className="account-create" onSubmit={submitCreate}>
          <label>Username<input name="username" required autoComplete="off" autoFocus /></label>
          <label>Password<input name="password" type="password" required minLength={12} autoComplete="new-password" /></label>
          <div className="account-form-actions">
            <button className="button-outline" type="submit" disabled={busy}>{busy ? 'Creating…' : 'Create account'}</button>
            <button className="link-button" type="button" onClick={() => { setCreating(false); setError('') }}>Cancel</button>
          </div>
        </form>
      )}

      {accounts && accounts.length === 0 && !creating && (
        <div className="empty-state"><div className="empty-icon"><UserPlus size={32} /></div><h2>No accounts yet</h2><p>Create the first account to let someone sign in.</p></div>
      )}

      {accounts && accounts.length > 0 && (
        <div className="accounts-list">
          {accounts.map(account => {
            const isSelf = account.id === currentUserId
            const active = row?.id === account.id ? row.kind : null
            return (
              <div className="account-row" key={account.id}>
                <div className="account-head">
                  <div className="account-identity">
                    <span className="account-name">{account.username}</span>
                    {isSelf && <span className="account-you">You</span>}
                  </div>
                  <span className={account.role === 'Admin' ? 'badge badge-reviewed' : 'badge badge-pending'}>{account.role === 'Admin' ? 'Administrator' : 'User'}</span>
                  <div className="account-actions">
                    <button className="link-button" onClick={() => open({ id: account.id, kind: 'password' })}><KeyRound size={15} /> Reset password</button>
                    <button className="link-button" onClick={() => open({ id: account.id, kind: 'rename' })}><Pencil size={15} /> Rename</button>
                    {!isSelf && <button className="link-button danger" onClick={() => open({ id: account.id, kind: 'delete' })}><Trash2 size={15} /> Delete</button>}
                  </div>
                </div>

                {active === 'rename' && (
                  <form className="account-inline" onSubmit={event => void submitRename(event, account.id)}>
                    <label>New username<input name="username" defaultValue={account.username} required autoComplete="off" autoFocus /></label>
                    <div className="account-form-actions">
                      <button className="button-outline" type="submit" disabled={busy}>{busy ? 'Saving…' : 'Save'}</button>
                      <button className="link-button" type="button" onClick={close}>Cancel</button>
                    </div>
                  </form>
                )}

                {active === 'password' && (
                  <form className="account-inline" onSubmit={event => void submitPassword(event, account.id)}>
                    <label>New password<input name="password" type="password" required minLength={12} autoComplete="new-password" autoFocus /></label>
                    <div className="account-form-actions">
                      <button className="button-outline" type="submit" disabled={busy}>{busy ? 'Resetting…' : 'Reset password'}</button>
                      <button className="link-button" type="button" onClick={close}>Cancel</button>
                    </div>
                  </form>
                )}

                {active === 'delete' && (
                  <div className="confirm-row">
                    <span>Delete <strong>{account.username}</strong>? Their images and tags are removed. This cannot be undone.</span>
                    <button className="button-danger" disabled={busy} onClick={() => void confirmDelete(account.id)}>{busy ? 'Deleting…' : 'Delete'}</button>
                    <button className="link-button" onClick={close}><X size={15} /> Cancel</button>
                  </div>
                )}
              </div>
            )
          })}
        </div>
      )}
    </section>
  )
}
