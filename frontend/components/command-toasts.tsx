import { useEffect } from 'react'
import { AlertCircle, AlertTriangle, Bell, CheckCircle2, Clock3, X } from 'lucide-react'
import type { CommandToast } from '../lib/use-device-commands'

const icons = { pending: Clock3, success: CheckCircle2, error: AlertCircle, alarm: Bell, warning: AlertTriangle, critical: AlertTriangle }

function Toast({ toast, onDismiss }: { toast: CommandToast; onDismiss: (id: number) => void }) {
  const { id, kind } = toast
  const Icon = icons[kind]

  useEffect(() => {
    if (kind === 'pending' || kind === 'error') return
    const timer = window.setTimeout(() => onDismiss(id), kind === 'success' ? 6000 : 12000)
    return () => window.clearTimeout(timer)
  }, [id, kind, onDismiss])

  return <li className={`command-toast ${kind}`}>
    <Icon size={19} aria-hidden="true" />
    <div role={kind === 'error' || kind === 'critical' ? 'alert' : 'status'}><strong>{toast.deviceName}</strong><p>{toast.text}</p></div>
    <button type="button" aria-label={`Dismiss notification for ${toast.deviceName}`} onClick={() => onDismiss(id)}><X size={17} aria-hidden="true" /></button>
  </li>
}

export function CommandToasts({ toasts, onDismiss, onDismissAll }: {
  toasts: CommandToast[]
  onDismiss: (id: number) => void
  onDismissAll: () => void
}) {
  if (!toasts.length) return null
  return <ul className="command-toasts" aria-label="Notifications">
    {toasts.length > 1 && <li className="toast-toolbar">
      <button type="button" className="ack" onClick={onDismissAll}>Dismiss all</button>
    </li>}
    {toasts.map((toast) => <Toast key={toast.id} toast={toast} onDismiss={onDismiss} />)}
  </ul>
}
