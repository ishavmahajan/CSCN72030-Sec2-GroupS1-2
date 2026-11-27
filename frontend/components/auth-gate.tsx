'use client'

import { Pause, Play } from 'lucide-react'
import { createContext, useContext, useEffect, useRef, useState, type FormEvent } from 'react'
import { sessionFetch, type SessionUser } from '../lib/auth-api'

const SessionContext = createContext<{ user: SessionUser; logout: () => Promise<void> } | null>(null)
export function useSession() {
  const session = useContext(SessionContext)
  if (!session) throw new Error('An authenticated session is required.')
  return session
}

function LoginBackground() {
  const videoRef = useRef<HTMLVideoElement>(null)
  const [playing, setPlaying] = useState(false)

  useEffect(() => {
    const video = videoRef.current
    if (!video) return

    video.defaultPlaybackRate = 0.5
    video.playbackRate = 0.5

    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)')
    function syncMotionPreference() {
      if (reducedMotion.matches) video?.pause()
      else void video?.play().catch(() => {})
    }

    syncMotionPreference()
    reducedMotion.addEventListener('change', syncMotionPreference)
    return () => reducedMotion.removeEventListener('change', syncMotionPreference)
  }, [])

  function togglePlayback() {
    const video = videoRef.current
    if (!video) return
    if (video.paused) void video.play().catch(() => {})
    else video.pause()
  }

  return <>
    <video ref={videoRef} className="auth-video" src="/login-kitchen.mp4" muted loop playsInline preload="none"
      aria-hidden="true" onPlaying={() => setPlaying(true)} onPause={() => setPlaying(false)} />
    <div className="auth-video-overlay" aria-hidden="true" />
    <button type="button" className="auth-video-control" onClick={togglePlayback}
      aria-label={playing ? 'Pause background video' : 'Play background video'}>
      {playing ? <Pause size={18} aria-hidden="true" /> : <Play size={18} aria-hidden="true" />}
    </button>
  </>
}

export function AuthGate({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<SessionUser | null>(null)
  const [loading, setLoading] = useState(true)
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [animateDashboard, setAnimateDashboard] = useState(false)

  async function refresh(afterSignIn = false) {
    const response = await sessionFetch('/auth/me')
    if (response.status === 401) { setUser(null); return }
    if (!response.ok) throw new Error('Could not check your session.')
    const session = await response.json() as SessionUser
    setAnimateDashboard(afterSignIn)
    setUser(session)
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
      await refresh(true)
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
  if (user) return <SessionContext.Provider value={{ user, logout }}>
    <div className={animateDashboard ? 'dashboard-enter' : undefined}>
      {error && <p className="auth-notice" role="alert">{error}</p>}
      {children}
    </div>
  </SessionContext.Provider>
  return <main className="auth-screen auth-screen-with-video">
    <form className="auth-card" onSubmit={login}>
      <h1><img className="auth-logo" src="/kitchenos-logo.svg" alt="KitchenOS" width={460} height={112} /></h1>
      <p className="auth-subtitle">Sign in to monitor your kitchen.</p>
      <label htmlFor="email">Email</label><input id="email" type="email" autoComplete="username" required value={email} onChange={(event) => setEmail(event.target.value)} />
      <label htmlFor="password">Password</label><input id="password" type="password" autoComplete="current-password" required value={password} onChange={(event) => setPassword(event.target.value)} />
      {error && <p role="alert">{error}</p>}
      <button type="submit" disabled={busy}>{busy ? 'Signing in…' : 'Sign in'}</button>
      <small>Ask your administrator if you need an account.</small>
    </form>
    <LoginBackground />
  </main>
}
