'use client'

import { useCallback, useEffect, useRef, useState } from 'react'
import { Activity, AlertTriangle, Bell, ClipboardList, ChevronRight, Fan, Gauge, Layers3, LayoutGrid, Menu, Plus, Snowflake, Square, Utensils, Waves, Wifi, WifiOff, X, Zap, type LucideIcon } from 'lucide-react'
import { DeviceCommandControls, type DevicePoint } from '../components/device-command-controls'
import { CommandToasts } from '../components/command-toasts'
import { CreateDeviceDialog, type CreatedDevice } from '../components/create-device-dialog'
import { useSession } from '../components/auth-gate'
import { apiUrl, sessionFetch } from '../lib/auth-api'
import { capabilityProvider, type CommandRequest, type DeviceProfile } from '../lib/device-capabilities'
import { useDeviceCommands, type CommandResult } from '../lib/use-device-commands'

type Device = {
  id: string
  deviceType: string
  name: string
  description: string
  type: string
  value: string
  unit: string
  icon: LucideIcon
  color: string
  mode: string
  points: DevicePoint[]
  power: string
  energy: string
  setpoint?: number
  door?: string
  acknowledgedRunning?: boolean
}

type ApiDevice = {
  deviceId: string
  deviceName: string
  deviceType: string
  description: string
  readPoints?: DevicePoint[]
}
type AlarmEvent = { id: string; deviceId: string; deviceType: string; code: string; severity: string; text: string; timestamp: string }
type DeviceEvent = {
  eventName: string
  deviceId: string
  deviceType: string
  timestamp: string
  message?: Partial<CommandResult> & {
    values?: DevicePoint[]
    code?: string
    severity?: string
    text?: string
    timestamp?: string
  }
}

const deviceMetadata: Record<string, { icon: LucideIcon; color: string; type: string }> = {
  WALK_IN_FREEZER: { icon: Snowflake, color: 'blue', type: 'Cold storage' },
  STOVE: { icon: Utensils, color: 'orange', type: 'Cooking line' },
  OVEN: { icon: Gauge, color: 'orange', type: 'Cooking line' },
  MIXER: { icon: Activity, color: 'purple', type: 'Prep station' },
  EXHAUST_FAN: { icon: Fan, color: 'cyan', type: 'Ventilation' },
  BLENDER: { icon: Activity, color: 'purple', type: 'Prep station' },
  DISHWASHER: { icon: Waves, color: 'cyan', type: 'Back of house' },
  SINK: { icon: Waves, color: 'cyan', type: 'Back of house' },
}

function findPoint(points: DevicePoint[], ...names: string[]) {
  for (const name of names) {
    const point = points.find((item) => item.name === name)
    if (point) return point
  }
}

function displayValue(value: unknown) {
  return typeof value === 'number' ? String(Math.round(value * 100) / 100) : String(value ?? '')
}

const deviceTypeAliases: Record<string, string> = {
  WalkInFreezer: 'WALK_IN_FREEZER',
  Stove: 'STOVE',
  Oven: 'OVEN',
  Mixer: 'MIXER',
  ExhaustFan: 'EXHAUST_FAN',
  Blender: 'BLENDER',
  Dishwasher: 'DISHWASHER',
  Sink: 'SINK',
}

function getConfiguredDeviceType(deviceType: string) {
  return deviceTypeAliases[deviceType] ?? deviceType.toUpperCase()
}

const runningPointNames = ['isRunning', 'motor', 'powerState', 'cycle', 'mode']

function isRunning(device: Device): boolean | undefined {
  if (device.acknowledgedRunning !== undefined) return device.acknowledgedRunning
  const runningPoint = findPoint(device.points, 'isRunning')?.value
  if (typeof runningPoint === 'boolean') return runningPoint

  const state = runningPointNames.slice(1)
    .map((name) => findPoint(device.points, name)?.value)
    .find((value) => typeof value === 'string' && value.trim())
  if (typeof state !== 'string') return undefined
  return !['off', 'stopped', 'idle', 'completed', 'complete', 'fault'].includes(state.trim().toLowerCase())
}

function createDevice(device: ApiDevice): Device {
  const configuredDeviceType = getConfiguredDeviceType(device.deviceType)
  const metadata = deviceMetadata[configuredDeviceType] ?? { icon: Activity, color: 'blue', type: device.deviceType }
  const points = device.readPoints ?? []
  const reading = findPoint(points, 'temperature') ?? findPoint(points, 'speed', 'fanSpeed', 'flow', 'airflow', 'waterLevel', 'heating', 'motor', 'compressor', 'cycle', 'valve')
  const power = findPoint(points, 'power')
  const energy = findPoint(points, 'energy')
  const setpoint = findPoint(points, 'setpoint')
  const door = findPoint(points, 'door')
  const mode = findPoint(points, 'mode', 'powerState')
  return {
    id: device.deviceId,
    deviceType: configuredDeviceType,
    name: device.deviceName,
    description: device.description,
    type: metadata.type,
    value: displayValue(reading?.value),
    unit: reading?.unit ?? '',
    icon: metadata.icon,
    color: metadata.color,
    mode: displayValue(mode?.value),
    points,
    power: power ? `${displayValue(power.value)} ${power.unit ?? ''}`.trim() : '—',
    energy: energy ? `${displayValue(energy.value)} ${energy.unit ?? ''}`.trim() : '—',
    setpoint: typeof setpoint?.value === 'number' ? setpoint.value : undefined,
    door: door ? displayValue(door.value) : undefined,
  }
}

function createDeviceFromRegistration(device: CreatedDevice): Device {
  return createDevice({
    deviceId: device.deviceId,
    deviceName: device.deviceName,
    deviceType: device.deviceType,
    description: device.description,
    readPoints: device.readPoints,
  })
}

function updateDevice(device: Device, updates: DevicePoint[]): Device {
  const points = [
    ...device.points.filter((previous) => !updates.some((point) =>
      point.name === previous.name && (point.component ?? null) === (previous.component ?? null))),
    ...updates,
  ]
  const reading = findPoint(points, 'temperature') ?? findPoint(points, 'speed', 'fanSpeed', 'flow', 'airflow', 'waterLevel', 'heating', 'motor', 'compressor', 'cycle', 'valve')
  const power = findPoint(points, 'power')
  const energy = findPoint(points, 'energy')
  const setpoint = findPoint(points, 'setpoint')
  const door = findPoint(points, 'door')
  const mode = findPoint(points, 'mode', 'powerState')

  return {
    ...device,
    value: reading ? displayValue(reading.value) : device.value,
    unit: reading?.unit ?? device.unit,
    power: power ? `${displayValue(power.value)} ${power.unit ?? ''}`.trim() : device.power,
    energy: energy ? `${displayValue(energy.value)} ${energy.unit ?? ''}`.trim() : device.energy,
    setpoint: typeof setpoint?.value === 'number' ? setpoint.value : device.setpoint,
    door: door ? displayValue(door.value) : device.door,
    mode: mode ? displayValue(mode.value) : device.mode,
    points,
  }
}

function applyAcknowledgedCommand(device: Device, request: CommandRequest): Device {
  const { commandName, arguments: args } = request
  if (commandName === 'start' || commandName === 'stop') {
    if (args.component) return device
    const running = commandName === 'start'
    // The simulator's mixer lifecycle methods set the motor directly.
    if (device.deviceType === 'MIXER') {
      return { ...updateDevice(device, [{ name: 'motor', value: running ? 'on' : 'off' }]), acknowledgedRunning: undefined }
    }
    return { ...device, acknowledgedRunning: running }
  }
  if (commandName !== 'setMode' && commandName !== 'setValue') return device
  const name = commandName === 'setMode' ? 'mode' : args.name
  const value = commandName === 'setMode' ? args.mode : args.value
  const unit = commandName === 'setValue' ? args.unit : undefined

  const previous = device.points.find((point) => point.name === name && (point.component ?? null) === (args.component ?? null))
  const updated = updateDevice(device, [{ name, value, component: args.component, unit: unit ?? previous?.unit }])
  // A freezer mode change does not change its independent running state.
  return device.deviceType !== 'WALK_IN_FREEZER' && runningPointNames.includes(name)
    ? { ...updated, acknowledgedRunning: undefined }
    : updated
}

function OperatingState({ running, connected }: { running: boolean | undefined; connected: boolean }) {
  const label = running === undefined ? 'State unknown' : running ? 'Running' : 'Stopped'
  return <span className={`operating-state ${running && connected ? 'running' : 'idle'}`}>
    {connected || running === undefined ? label : `Last known: ${label.toLowerCase()}`}
  </span>
}

function DeviceCard({ device, selected, running, connected, onSelect }: { device: Device; selected: boolean; running: boolean | undefined; connected: boolean; onSelect: () => void }) {
  const Icon = device.icon
  const unit = device.unit === 'C' ? '°C' : device.unit
  return <button aria-pressed={selected} className={`device-card ${selected ? 'selected' : ''} ${!connected ? 'disconnected' : ''}`} onClick={onSelect}>
    <span className="device-card-head">
      <span className={`device-icon ${device.color}`}><Icon size={22} /></span>
      <span className="device-identity"><strong>{device.name}</strong><span className="device-type">{device.type}</span></span>
      <ChevronRight className="card-chevron" size={19} />
    </span>
    <span className="device-card-body">
      <span className="reading-label">Primary reading</span>
      <span key={device.value} className="device-reading"><span>{device.value || '—'}</span>{device.value && unit && <small>{unit}</small>}</span>
    </span>
    <span className="device-card-foot">
      <OperatingState running={running} connected={connected} />
    </span>
  </button>
}

function DetailPanel({ device, connected, children }: { device: Device; connected: boolean; children: React.ReactNode }) {
  const Icon = device.icon
  const unit = device.unit === 'C' ? '°C' : device.unit
  const running = isRunning(device)
  const motor = device.deviceType === 'MIXER' ? displayValue(findPoint(device.points, 'motor')?.value).trim() : ''

  return <section id="equipment-detail" className="detail-panel" aria-labelledby="equipment-detail-title">
    <div className="detail-header">
      <div className="detail-title"><Icon size={27} /><div><h2 id="equipment-detail-title" tabIndex={-1}>{device.name}</h2><p>{device.description}</p></div></div>
      <div className="detail-statuses">
        <OperatingState running={running} connected={connected} />
      </div>
    </div>
    <div className="detail-main">
      <div className="reading-display"><div className="reading-highlight">
        <span>{device.value || '—'}{device.value && unit && <small>{unit}</small>}</span><b>Primary reading</b>
        {device.setpoint !== undefined && <p>Setpoint <strong>{device.setpoint} °C</strong></p>}
      </div></div>
      <div className="detail-stats">
        <div><ClipboardList /><span>Device ID<strong>{device.id}</strong></span></div>
        {device.mode && <div><Waves /><span>Reported mode<strong>{device.mode}</strong></span></div>}
        {motor && <div><Activity /><span>Motor state<strong>{motor === 'on' ? 'On' : motor === 'off' ? 'Off' : motor}</strong></span></div>}
        {device.door && <div><Square /><span>Door<strong>{device.door}</strong></span></div>}
      </div>
    </div>
    <div className="detail-metrics"><div><Zap /><span>Power<strong>{device.power}</strong></span></div><div><Layers3 /><span>Energy<strong>{device.energy}</strong></span></div></div>
    {children}
  </section>
}

export default function Page() {
  const { user, logout } = useSession()
  const canOperate = user.roles.some((role) => role === 'Operator' || role === 'Administrator')
  const [devices, setDevices] = useState<Device[]>([])
  const [selectedId, setSelectedId] = useState('')
  const [profiles, setProfiles] = useState<readonly DeviceProfile[]>([])
  const [capabilityStatus, setCapabilityStatus] = useState<'loading' | 'ready' | 'unavailable'>('loading')
  const applyAcknowledgement = useCallback((deviceId: string, request: CommandRequest) => {
    setDevices((current) => current.map((device) =>
      device.id === deviceId ? applyAcknowledgedCommand(device, request) : device))
  }, [])
  const { pendingDevices, toasts, dismissToast, receiveResult, submit } = useDeviceCommands(apiUrl, applyAcknowledgement)
  const [connected, setConnected] = useState(false)
  const [menuOpen, setMenuOpen] = useState(false)
  const menuButton = useRef<HTMLButtonElement>(null)
  const menuCloseButton = useRef<HTMLButtonElement>(null)
  const [inventoryStatus, setInventoryStatus] = useState<'loading' | 'ready' | 'error'>('loading')
  const [inventoryAttempt, setInventoryAttempt] = useState(0)
  const [alarms, setAlarms] = useState<AlarmEvent[]>([])
  const [createDialogOpen, setCreateDialogOpen] = useState(false)

  useEffect(() => {
    let disposed = false
    capabilityProvider.load().then((items) => {
      if (disposed) return
      setProfiles(items)
      setCapabilityStatus('ready')
    }).catch(() => {
      if (!disposed) setCapabilityStatus('unavailable')
    })
  }, [])

  useEffect(() => {
    const controller = new AbortController()
    sessionFetch('/devices', { signal: controller.signal })
      .then((response) => {
        if (!response.ok) throw new Error(`Device request failed: ${response.status}`)
        return response.json() as Promise<ApiDevice[]>
      })
      .then((items) => {
        if (controller.signal.aborted) return
        setDevices((current) => {
          const next = items.map((item) => {
            const previous = current.find((device) => device.id === item.deviceId)
            const refreshed = createDevice(item)
            return previous?.acknowledgedRunning === undefined
              ? refreshed
              : { ...refreshed, acknowledgedRunning: previous.acknowledgedRunning }
          })
          return [...next, ...current.filter((device) => !items.some((item) => item.deviceId === device.id))]
        })
        setInventoryStatus('ready')
      })
      .catch(() => {
        if (!controller.signal.aborted) setInventoryStatus('error')
      })
    return () => controller.abort()
  }, [inventoryAttempt])

  useEffect(() => {
    let disposed = false
    let socket: WebSocket
    let retry: ReturnType<typeof setTimeout>
    const socketUrl = apiUrl.replace(/^http/, 'ws') + '/ws'

    function connect() {
      socket = new WebSocket(socketUrl)
      socket.onopen = () => setConnected(true)
      socket.onclose = () => {
        if (disposed) return
        setConnected(false)
        retry = setTimeout(connect, 3000)
        void sessionFetch('/auth/me').then((response) => {
          if (!disposed && response.status === 401) window.dispatchEvent(new Event('kitchenos:session-expired'))
        }).catch(() => { /* The connection indicator shows the outage. */ })
      }
      socket.onerror = () => socket.close()
      socket.onmessage = (event) => {
        try {
          const message = JSON.parse(event.data) as DeviceEvent
          if (message.eventName === 'alarm') {
            const alarm = message.message
            const code = alarm?.code
            const severity = alarm?.severity
            const text = alarm?.text
            if (!message.deviceId || !message.deviceType || !code || !severity || !text) {
              console.error('Ignoring malformed alarm event', message)
              return
            }

            const timestamp = alarm.timestamp ?? message.timestamp
            setAlarms((current) => [{
              id: `${message.deviceId}-${code}-${timestamp}-${Math.random().toString(36).slice(2)}`,
              deviceId: message.deviceId,
              deviceType: message.deviceType,
              code,
              severity,
              text,
              timestamp,
            }, ...current].slice(0, 5))
            return
          }

          const body = message.message
          if (message.eventName === 'command_result' && body?.commandId && body.status) {
            receiveResult(message.deviceId, { commandId: body.commandId, status: body.status, reason: body.reason })
            return
          }
          const values = body?.values
          if (!message.deviceId || !values) return
          setDevices((current) => current.map((device) => {
            if (device.id !== message.deviceId) return device
            const updated = updateDevice(device, values)
            const statePoints = device.deviceType === 'WALK_IN_FREEZER' ? ['isRunning'] : runningPointNames
            return values.some((point) => statePoints.includes(point.name))
              ? { ...updated, acknowledgedRunning: undefined }
              : updated
          }))
        } catch (error) {
          console.error('Could not parse device event', error)
        }
      }
    }
    connect()

    return () => {
      disposed = true
      clearTimeout(retry)
      socket.onopen = socket.onmessage = socket.onclose = socket.onerror = null
      socket.close()
    }
  }, [receiveResult])

  const selected = devices.find((device) => device.id === selectedId) ?? devices[0]


  function handleCreated(device: CreatedDevice) {
    const nextDevice = createDeviceFromRegistration(device)
    setDevices((current) => current.some((item) => item.id === nextDevice.id) ? current : [...current, nextDevice])
    setSelectedId(nextDevice.id)
  }

  const navItems = [
    { label: 'Overview', target: 'overview', icon: LayoutGrid },
    { label: 'Equipment', target: 'equipment', icon: ClipboardList },
    { label: 'Alarms', target: 'alarms', icon: Bell },
  ]
  const profile = profiles.find((item) => item.deviceType === selected?.deviceType)
  const canCreate = user.roles.includes('Administrator')

  useEffect(() => {
    if (menuOpen) menuCloseButton.current?.focus()
  }, [menuOpen])

  function closeMenu() {
    setMenuOpen(false)
    menuButton.current?.focus()
  }

  function selectDevice(id: string) {
    setSelectedId(id)
    if (window.matchMedia('(max-width: 1100px)').matches) {
      document.getElementById('equipment-detail-title')?.focus({ preventScroll: true })
      document.getElementById('equipment-detail')?.scrollIntoView({ block: 'start' })
    }
  }

  return <main id="overview" tabIndex={-1} className="app-shell" onKeyDown={(event) => {
    if (menuOpen && event.key === 'Escape') closeMenu()
  }}>
    <aside id="site-menu" className={`sidebar ${menuOpen ? 'open' : ''}`}>
      <div className="brand"><img src="/kitchenos-logo.svg" alt="KitchenOS" /><button aria-label="Close menu" ref={menuCloseButton} className="close-menu" onClick={closeMenu}><X size={20} /></button></div>
      <nav aria-label="Dashboard">{navItems.map(({ label, target, icon: Icon }) => <a href={`#${target}`} key={target} onClick={() => {
        setMenuOpen(false)
        document.getElementById(target)?.focus({ preventScroll: true })
      }}><Icon size={20} /><span>{label}</span></a>)}</nav>
      <div className="sidebar-foot">KitchenOS v1.0</div>
    </aside>
    {menuOpen && <button className="scrim" onClick={closeMenu} aria-label="Close menu" tabIndex={-1} />}
    <section className="main-area">
      <header className="topbar">
        <button ref={menuButton} aria-label="Open menu" aria-expanded={menuOpen} aria-controls="site-menu" className="mobile-menu" onClick={() => setMenuOpen(true)}><Menu size={22} /></button>
        <div className="top-brand"><img src="/kitchenos-logo.svg" alt="KitchenOS" /></div>
        <div role="status" className={`connection-status ${connected ? 'connected' : 'disconnected'}`}>{connected ? <Wifi size={17} /> : <WifiOff size={17} />}<span>{connected ? 'Connected' : 'Disconnected'}</span></div>
        <div className="top-meta"><span className="operator">{user.roles.join(', ')}</span><button className="sign-out" onClick={() => void logout()}>Sign out</button></div>
      </header>
      <div className="page-content">
        <div className="page-heading"><h1>Equipment overview</h1>{canCreate && <button className="create-device-button" onClick={() => setCreateDialogOpen(true)}><Plus size={17} /> Add device</button>}</div>
        {!connected && <section className="connection-banner"><WifiOff size={20} /><div><strong>Live connection unavailable</strong><span>Reconnecting automatically. Equipment readings may be stale and controls are paused.</span></div></section>}
        <section className="kpi-row" aria-label="Equipment summary">
          <div className="kpi"><span className="kpi-icon"><Layers3 /></span><div><b>{inventoryStatus === 'ready' || devices.length ? devices.length : '—'}</b><small>Registered equipment</small></div></div>
          <div className="kpi"><span className="kpi-icon"><Fan /></span><div><b>{connected && inventoryStatus === 'ready' ? devices.filter(isRunning).length : '—'}</b><small>Equipment running</small></div></div>
          <div className="kpi"><span className="kpi-icon"><AlertTriangle /></span><div><b>{alarms.length}</b><small>Recent alarms</small></div></div>
        </section>
        <div className={`dashboard-grid ${selected ? '' : 'without-selection'}`}>
          <section id="equipment" tabIndex={-1} className="equipment-section" aria-labelledby="equipment-title">
            <div className="section-title"><h2 id="equipment-title">Equipment</h2></div>
            {inventoryStatus === 'error' && <div className="inventory-message"><p role="status">Equipment inventory could not be loaded.</p><button className="sign-out" onClick={() => { setInventoryStatus('loading'); setInventoryAttempt((value) => value + 1) }}>Retry</button></div>}
            {!devices.length && inventoryStatus !== 'error' && <p className="inventory-message" role="status">{inventoryStatus === 'loading' ? 'Loading equipment…' : 'No equipment is registered yet.'}</p>}
            {!!devices.length && <div className="devices-grid">{devices.map((device) => <DeviceCard key={device.id} device={device} selected={device.id === selected?.id} running={isRunning(device)} connected={connected} onSelect={() => selectDevice(device.id)} />)}</div>}
          </section>
          {selected && <DetailPanel device={selected} connected={connected}>
            {!canOperate ? <p className="command-feedback">Viewer access · Monitoring only</p> : capabilityStatus === 'ready'
              ? <DeviceCommandControls key={selected.id} profile={profile} points={selected.points} deviceName={selected.name} running={isRunning(selected)}
                  disabled={!connected || pendingDevices.has(selected.id)} onSubmit={(request) => void submit(selected.id, selected.name, request)} />
              : <p className="command-feedback" role="status">{capabilityStatus === 'loading' ? 'Loading equipment controls…' : 'Equipment controls could not be loaded.'}</p>}
          </DetailPanel>}
        </div>
        <section id="alarms" tabIndex={-1} className="info-panel">
          <div className="panel-title">
            <h2><AlertTriangle size={20} /> Recent alarms</h2>
          </div>
          {alarms.length === 0 ? (
            <p className="alarm-empty">{connected ? 'No alarms received in this session.' : 'No recent alarms to display. Live monitoring is disconnected.'}</p>
          ) : <ul className="alarm-list">{alarms.map((alarm) => (
            <li className="alarm-row" key={alarm.id}>
              <AlertTriangle className="danger" size={19} aria-hidden="true" />
              <div className="alarm-details"><b>{devices.find((device) => device.id === alarm.deviceId)?.name || alarm.deviceId}</b><span>{alarm.text} ({alarm.code}) · {alarm.severity}</span></div>
              <time dateTime={alarm.timestamp}>{new Date(alarm.timestamp).toLocaleTimeString()}</time>
              <button onClick={() => setAlarms((current) => current.filter((item) => item.id !== alarm.id))} className="ack" aria-label={`Dismiss ${alarm.code} for ${alarm.deviceId}`}>Dismiss</button>
            </li>
          ))}</ul>}
        </section>
      </div>
    </section>
    <CommandToasts toasts={toasts} onDismiss={dismissToast} />
    {canCreate && <CreateDeviceDialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} onCreated={handleCreated} />}
  </main>
}
