import { useEffect } from 'react'
import { AlertCircle, CheckCircle2, Clock3, X } from 'lucide-react'
import type { CommandToast } from '../lib/use-device-commands'

const icons = { pending: Clock3, success: CheckCircle2, error: AlertCircle }

function Toast({ toast, onDismiss }: { toast: CommandToast; onDismiss: (id: number) => void }) {
  const { id, kind } = toast
  const Icon = icons[kind]

  useEffect(() => {
    if (kind !== 'success') return
    const timer = window.setTimeout(() => onDismiss(id), 6000)
    return () => window.clearTimeout(timer)
  }, [id, kind, onDismiss])

  return <li className={`command-toast ${kind}`}>
    <Icon size={19} aria-hidden="true" />
    <div role={kind === 'error' ? 'alert' : 'status'}><strong>{toast.deviceName}</strong><p>{toast.text}</p></div>
    <button type="button" aria-label={`Dismiss notification for ${toast.deviceName}`} onClick={() => onDismiss(id)}><X size={17} aria-hidden="true" /></button>
  </li>
}

export function CommandToasts({ toasts, onDismiss }: { toasts: CommandToast[]; onDismiss: (id: number) => void }) {
  if (!toasts.length) return null
  return <ul className="command-toasts" aria-label="Command notifications">
    {toasts.map((toast) => <Toast key={toast.id} toast={toast} onDismiss={onDismiss} />)}
  </ul>
}
