export const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:8080'
export type SessionUser = { id: string; email: string; roles: string[] }

export async function sessionFetch(path: string, init: RequestInit = {}) {
  const headers = new Headers(init.headers)
  const method = (init.method ?? 'GET').toUpperCase()
  if (!['GET', 'HEAD', 'OPTIONS'].includes(method)) {
    // Tokens are bound to the current identity, so obtain a fresh one after login/logout.
    const response = await fetch(`${apiUrl}/auth/csrf`, { credentials: 'include', cache: 'no-store', signal: init.signal })
    if (!response.ok) throw new Error('Could not prepare a secure request.')
    const { token } = await response.json() as { token: string }
    headers.set('X-CSRF-TOKEN', token)
  }
  const response = await fetch(`${apiUrl}${path}`, { ...init, headers, credentials: 'include', cache: 'no-store' })
  if (response.status === 401 && !['/auth/login', '/auth/me'].includes(path)) window.dispatchEvent(new Event('kitchenos:session-expired'))
  return response
}
