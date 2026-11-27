'use client'

import { createContext, useContext, useEffect, useState, type FormEvent } from 'react'
import { sessionFetch, type SessionUser } from '../lib/auth-api'

const SessionContext = createContext<{ user: SessionUser; logout: () => Promise<void> } | null>(null)
export function useSession() {
  const session = useContext(SessionContext)
  if (!session) throw new Error('An authenticated session is required.')
  return session
}

export function AuthGate({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<SessionUser | null>(null)
  const [loading, setLoading] = useState(true)
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  async function refresh() {
    const response = await sessionFetch('/auth/me')
    if (response.status === 401) { setUser(null); return }
    if (!response.ok) throw new Error('Could not check your session.')
    setUser(await response.json() as SessionUser)
  }
  useEffect(() => {
    let active = true
    const expired = () => { setUser(null); setError('Your session ended. Please sign in again.') }
    window.addEventListener('kitchenos:session-expired', expired)
    refresh().catch(() => { if (active) setError('The API is unavailable. Try signing in again shortly.') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false; window.removeEventListener('kitchenos:session-expired', expired) }
  }, [])

  async function login(event: FormEvent) {
    event.preventDefault()
    setBusy(true); setError('')
    try {
      const response = await sessionFetch('/auth/login', {
        method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email, password }),
      })
      if (!response.ok) throw new Error(response.status === 429 ? 'Too many attempts. Try again in a minute.' : 'Sign-in failed. Check your details or try again later.')
      setPassword('')
      await refresh()
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'Could not sign in.') }
    finally { setBusy(false) }
  }
  async function logout() {
    try {
      const response = await sessionFetch('/auth/logout', { method: 'POST' })
      if (!response.ok && response.status !== 401) throw new Error('Could not sign out. Please retry.')
      setUser(null); setPassword(''); setError('')
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'Could not sign out.') }
  }

  if (loading) return <main className="auth-screen"><p>Checking your session…</p></main>
  if (user) return <SessionContext.Provider value={{ user, logout }}>{error && <p className="auth-notice" role="alert">{error}</p>}{children}</SessionContext.Provider>
  return <main className="auth-screen"><form className="auth-card" onSubmit={login}>
    <h1>KitchenOS</h1><p>Sign in to monitor your kitchen.</p>
    <label htmlFor="email">Email</label><input id="email" type="email" autoComplete="username" required value={email} onChange={(event) => setEmail(event.target.value)} />
    <label htmlFor="password">Password</label><input id="password" type="password" autoComplete="current-password" required value={password} onChange={(event) => setPassword(event.target.value)} />
    {error && <p role="alert">{error}</p>}
    <button type="submit" disabled={busy}>{busy ? 'Signing in…' : 'Sign in'}</button>
    <small>Ask your administrator if you need an account.</small>
  </form></main>
}
