'use client'

import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Activity, Fan, Gauge, LoaderCircle, Plus, Snowflake, Utensils, Waves, X, type LucideIcon } from 'lucide-react'
import { sessionFetch } from '../lib/auth-api'
import type { DevicePoint } from './device-command-controls'

export type CreatedDevice = {
  deviceId: string
  deviceName: string
  deviceType: string
  deviceTypeId: number
  description: string
  readPoints?: DevicePoint[]
}

const deviceTypes = [
  { id: 1, value: 'WALK_IN_FREEZER', label: 'Walk-in freezer', icon: Snowflake, color: 'blue' },
  { id: 2, value: 'STOVE', label: 'Stove', icon: Utensils, color: 'orange' },
  { id: 3, value: 'OVEN', label: 'Oven', icon: Gauge, color: 'orange' },
  { id: 4, value: 'MIXER', label: 'Mixer', icon: Activity, color: 'purple' },
  { id: 5, value: 'EXHAUST_FAN', label: 'Exhaust fan', icon: Fan, color: 'cyan' },
  { id: 6, value: 'BLENDER', label: 'Blender', icon: Activity, color: 'purple' },
  { id: 7, value: 'DISHWASHER', label: 'Dishwasher', icon: Waves, color: 'cyan' },
  { id: 8, value: 'SINK', label: 'Sink', icon: Waves, color: 'cyan' },
] as const

type Props = {
  open: boolean
  onClose: () => void
  onCreated: (device: CreatedDevice) => void
}

export function CreateDeviceDialog({ open, onClose, onCreated }: Props) {
  const [deviceId, setDeviceId] = useState('')
  const [deviceName, setDeviceName] = useState('')
  const [deviceTypeId, setDeviceTypeId] = useState('4')
  const [description, setDescription] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  const dialogRef = useRef<HTMLDialogElement>(null)
  const firstField = useRef<HTMLInputElement>(null)

  useEffect(() => {
    const dialog = dialogRef.current
    if (open) {
      dialog?.showModal()
      firstField.current?.focus()
    }
    return () => dialog?.close()
  }, [open])

  function close() {
    if (busy) return
    setError('')
    onClose()
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!deviceId.trim() || !deviceName.trim()) {
      setError('Enter a device ID and name.')
      return
    }
    setBusy(true)
    setError('')

    try {
      const response = await sessionFetch('/devices', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          deviceId: deviceId.trim(),
          deviceName: deviceName.trim(),
          deviceTypeId: Number(deviceTypeId),
          description: description.trim() || null,
        }),
      })

      const body = await response.json().catch(() => null) as { error?: string } | CreatedDevice | null
      if (!response.ok) {
        throw new Error(body && 'error' in body && body.error ? body.error : `Could not create device (${response.status}).`)
      }

      if (!body || !('deviceId' in body)) throw new Error('The API response did not include the created device.')
      onCreated(body)
      setDeviceId('')
      setDeviceName('')
      setDeviceTypeId('4')
      setDescription('')
      onClose()
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Could not create device.')
    } finally {
      setBusy(false)
    }
  }

  return <dialog ref={dialogRef} className="setpoint-modal command-dialog create-device-modal"
    aria-labelledby="create-device-title" aria-describedby="create-device-description"
    onCancel={(event) => { event.preventDefault(); close() }}>
      <div className="modal-header">
        <div><h2 id="create-device-title">Add a device</h2></div>
        <button type="button" className="modal-close" aria-label="Close add device dialog" onClick={close} disabled={busy}><X size={18} /></button>
      </div>
      <p id="create-device-description" className="modal-description">Register equipment to add it to the monitoring dashboard.</p>
      <form onSubmit={submit} aria-busy={busy}>
        <div className="modal-form-grid">
          <label className="modal-field"><span>Device ID</span><input required readOnly={busy} maxLength={100} value={deviceId} onChange={(event) => setDeviceId(event.target.value)} placeholder="e.g. MIX-003" ref={firstField} /></label>
          <label className="modal-field"><span>Device name</span><input required readOnly={busy} maxLength={200} value={deviceName} onChange={(event) => setDeviceName(event.target.value)} placeholder="e.g. Prep mixer 3" /></label>
        </div>
        <fieldset className="device-type-picker" disabled={busy}><legend>Device type</legend><div className="device-type-grid">{deviceTypes.map((type) => { const Icon: LucideIcon = type.icon; const selected = deviceTypeId === String(type.id); return <button key={type.id} type="button" className={`device-type-option ${selected ? 'selected' : ''} ${type.color}`} aria-pressed={selected} onClick={() => setDeviceTypeId(String(type.id))}><span className="device-type-option-icon"><Icon size={19} /></span><span>{type.label}</span>{selected && <span className="device-type-check">✓</span>}</button> })}</div></fieldset>
        <label className="modal-field"><span>Description <small>(optional)</small></span><input readOnly={busy} maxLength={1000} value={description} onChange={(event) => setDescription(event.target.value)} placeholder="Where this equipment is used" /></label>
        {error && <p className="modal-error" role="alert">{error}</p>}
        <div className="modal-actions"><button type="button" className="modal-cancel" onClick={close} disabled={busy}>Cancel</button><button type="submit" className="modal-submit" disabled={busy}><>{busy ? <LoaderCircle className="spin" size={16} /> : <Plus size={16} />}</>{busy ? 'Creating…' : 'Create device'}</button></div>
      </form>
  </dialog>
}
