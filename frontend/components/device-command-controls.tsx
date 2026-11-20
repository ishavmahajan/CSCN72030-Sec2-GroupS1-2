import { useEffect, useId, useRef, useState } from 'react'
import { ChevronRight, Play, RefreshCw, Settings2, SlidersHorizontal, Square, X } from 'lucide-react'
import { getCommandRequest, type CommandRequest, type ConfiguredCommand, type DeviceProfile } from '../lib/device-capabilities'

export type DevicePoint = { name: string; value: unknown; unit?: string | null; component?: string | null }

type Props = {
  profile?: DeviceProfile
  points: DevicePoint[]
  deviceName: string
  running: boolean | undefined
  disabled: boolean
  onSubmit: (request: CommandRequest) => void
}

type CommandDialogState =
  | { kind: 'setpoint'; command?: ConfiguredCommand }
  | { kind: 'mode' | 'component'; command: ConfiguredCommand }

export function DeviceCommandControls({ profile, points, deviceName, running, disabled, onSubmit }: Props) {
  const [editor, setEditor] = useState<CommandDialogState | null>(null)

  if (profile?.status === 'onHold') return <p className="command-feedback">Equipment controls are on hold.</p>
  if (!profile?.commands.length) return <p className="command-feedback">No equipment controls available.</p>

  const powerAction = running === undefined ? undefined : profile.commands.find(
    (command) => command.commandName === (running ? 'stop' : 'start'),
  )
  const status = profile.commands.find((command) => command.commandName === 'isOnline')
  const mode = profile.commands.find((command) => command.commandName === 'setMode')
  const setpoints = profile.commands.filter((command) => command.commandName === 'setValue')
  const locked = disabled

  function submit(request: CommandRequest) {
    if (locked) return
    if (request.commandName === 'stop' && !window.confirm(`Stop ${deviceName}?`)) return
    onSubmit(request)
    setEditor(null)
  }

  function run(command: ConfiguredCommand) {
    if (command.componentRequired) {
      setEditor({ kind: 'component', command })
      return
    }
    const request = getCommandRequest(command, '', '')
    if (request) submit(request)
  }

  return <section className="device-controls" aria-label={`Controls for ${deviceName}`}>
    {powerAction && <div className="detail-actions device-controls-row">
      <button type="button" className={running ? 'stop-action' : 'primary-action'} disabled={locked} onClick={() => run(powerAction)}>
        {running ? <Square size={15} aria-hidden="true" /> : <Play size={16} aria-hidden="true" />}
        {running ? 'Stop' : 'Start'}
      </button>
    </div>}
    {(setpoints.length > 0 || mode || status) && <div className="detail-actions device-controls-row">
      {setpoints.length > 0 && <button type="button" aria-haspopup="dialog" disabled={locked} onClick={() => setEditor({ kind: 'setpoint' })}><SlidersHorizontal size={16} aria-hidden="true" /> Setpoint</button>}
      {mode && <button type="button" aria-haspopup="dialog" disabled={locked} onClick={() => setEditor({ kind: 'mode', command: { ...mode, options: mode.options ?? profile.modes } })}><Settings2 size={16} aria-hidden="true" /> Change Mode</button>}
      {status && <button type="button" disabled={locked} onClick={() => run(status)}><RefreshCw size={16} aria-hidden="true" /> Refresh status</button>}
    </div>}
    {editor && <CommandDialog
      editor={editor}
      setpoints={setpoints}
      points={points}
      deviceName={deviceName}
      disabled={locked}
      onChoose={(command) => setEditor({ kind: 'setpoint', command })}
      onBack={() => setEditor({ kind: 'setpoint' })}
      onClose={() => setEditor(null)}
      onSubmit={submit}
    />}
  </section>
}

function CommandDialog({ editor, setpoints, points, deviceName, disabled, onChoose, onBack, onClose, onSubmit }: {
  editor: CommandDialogState
  setpoints: ConfiguredCommand[]
  points: DevicePoint[]
  deviceName: string
  disabled: boolean
  onChoose: (command: ConfiguredCommand) => void
  onBack: () => void
  onClose: () => void
  onSubmit: (request: CommandRequest) => void
}) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const titleRef = useRef<HTMLHeadingElement>(null)
  const titleId = useId()
  const command = editor.command

  useEffect(() => {
    const dialog = dialogRef.current
    dialog?.showModal()
    return () => dialog?.close()
  }, [])

  useEffect(() => {
    titleRef.current?.focus()
  }, [command])

  const title = editor.kind === 'mode' ? 'Change Mode' : command?.label ?? 'Choose a setpoint'

  return <dialog ref={dialogRef} className="setpoint-modal command-dialog" aria-labelledby={titleId}
    onCancel={(event) => { event.preventDefault(); onClose() }}
    onClick={(event) => { if (event.target === event.currentTarget) onClose() }}>
    <div className="command-dialog-content">
      <header className="modal-header">
        <h2 id={titleId} ref={titleRef} tabIndex={-1}>{title}</h2>
        <button type="button" className="modal-close" aria-label="Close dialog" onClick={onClose}><X size={18} aria-hidden="true" /></button>
      </header>
      <p className="modal-description">{deviceName}</p>
      {command ? <CommandFields key={`${command.commandName}:${command.pointName ?? ''}`} command={command}
        points={points} disabled={disabled} onSubmit={onSubmit} onBack={editor.kind === 'setpoint' ? onBack : undefined} onClose={onClose} />
        : <div className="setpoint-options">
          {setpoints.map((item) => <button key={item.pointName} type="button" onClick={() => onChoose(item)} disabled={disabled}>
            {item.label}<ChevronRight size={16} aria-hidden="true" />
          </button>)}
        </div>}
    </div>
  </dialog>
}

function CommandFields({ command, points, disabled, onSubmit, onBack, onClose }: {
  command: ConfiguredCommand
  points: DevicePoint[]
  disabled: boolean
  onSubmit: (request: CommandRequest) => void
  onBack?: () => void
  onClose: () => void
}) {
  const [draft, setDraft] = useState<string>()
  const [component, setComponent] = useState('')
  const components = command.componentRequired
    ? [...new Set(points.filter((point) => !command.pointName || point.name === command.pointName).flatMap((point) => point.component ? [point.component] : []))]
    : []
  const pointName = command.commandName === 'setMode' ? 'mode' : command.pointName
  const reported = pointName ? points.find((point) => point.name === pointName && (point.component ?? '') === component)?.value : undefined
  const options = command.valueType === 'boolean'
    ? [{ value: 'true', label: 'Yes' }, { value: 'false', label: 'No' }]
    : command.options ?? []
  const numeric = command.commandName === 'setValue' && command.valueType === 'number'
  const needsValue = command.commandName === 'setMode' || command.commandName === 'setValue'
  const reportedValue = String(reported ?? '')
  const input = draft ?? (numeric ? reportedValue : options.find((option) => option.value.toLowerCase() === reportedValue.trim().toLowerCase())?.value ?? '')
  const request = getCommandRequest(command, input, components.includes(component) ? component : '')
  const unit = command.unit === 'C' ? '°C' : command.unit

  return <form className="command-fields" onSubmit={(event) => {
    event.preventDefault()
    if (!disabled && request) onSubmit(request)
  }}>
    {command.componentRequired && <label className="modal-field">Component
      <select required value={component} disabled={disabled} onChange={(event) => { setComponent(event.target.value); setDraft(undefined) }}>
        <option value="">Select a component</option>
        {components.map((item) => <option key={item}>{item}</option>)}
      </select>
    </label>}
    {needsValue && <label className="modal-field">{pointName === 'mode' ? 'Mode' : 'Value'}
      {numeric ? <>
        <input type="number" required min={command.min} max={command.max} step={command.step ?? 'any'} value={input} disabled={disabled} onChange={(event) => setDraft(event.target.value)} />
        <span>Allowed range {command.min} to {command.max} {unit}</span>
      </> : <select required value={input} disabled={disabled} onChange={(event) => setDraft(event.target.value)}>
        <option value="">Select a value</option>
        {options.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
      </select>}
    </label>}
    {numeric && draft && !request && (!command.componentRequired || component) && <p className="modal-error">Enter a valid number within the allowed range.</p>}
    <div className="modal-actions">
      <button type="button" className="modal-cancel" onClick={onBack ?? onClose}>{onBack ? 'Back' : 'Cancel'}</button>
      <button type="submit" className="modal-submit" disabled={disabled || !request}>{needsValue ? 'Apply' : command.label}</button>
    </div>
  </form>
}
