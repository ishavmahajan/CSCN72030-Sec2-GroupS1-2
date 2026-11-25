'use client'

import { useCallback, useEffect, useState } from 'react'
import { Activity, AlertTriangle, Bell, CalendarDays, ChevronDown, ChevronRight, ClipboardList, Clock3, Fan, Gauge, Layers3, LayoutGrid, Menu, Power, Settings, Snowflake, Square, Utensils, Waves, Wifi, WifiOff, X, Zap, type LucideIcon } from 'lucide-react'
import { DeviceCommandControls, type DevicePoint } from '../components/device-command-controls'
import { CommandToasts } from '../components/command-toasts'
import { useSession } from '../components/auth-gate'
import { sessionFetch } from '../lib/auth-api'
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
  status: 'Online' | 'Offline'
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

type ApiDevice = { deviceId: string; deviceName: string; deviceType: string; description: string; readPoints: DevicePoint[] }
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

const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:8080'

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
  const reading = findPoint(device.readPoints, 'temperature') ?? findPoint(device.readPoints, 'speed', 'fanSpeed', 'flow', 'airflow', 'waterLevel', 'heating', 'motor', 'compressor', 'cycle', 'valve')
  const power = findPoint(device.readPoints, 'power')
  const energy = findPoint(device.readPoints, 'energy')
  const setpoint = findPoint(device.readPoints, 'setpoint')
  const door = findPoint(device.readPoints, 'door')
  const mode = findPoint(device.readPoints, 'mode', 'powerState')
  return {
    id: device.deviceId,
    deviceType: configuredDeviceType,
    name: device.deviceName,
    description: device.description,
    type: metadata.type,
    value: displayValue(reading?.value),
    unit: reading?.unit ?? '',
    status: 'Online',
    icon: metadata.icon,
    color: metadata.color,
    mode: displayValue(mode?.value),
    points: device.readPoints,
    power: power ? `${displayValue(power.value)} ${power.unit ?? ''}`.trim() : '—',
    energy: energy ? `${displayValue(energy.value)} ${energy.unit ?? ''}`.trim() : '—',
    setpoint: typeof setpoint?.value === 'number' ? setpoint.value : undefined,
    door: door ? displayValue(door.value) : undefined,
  }
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

function StatusPill({ status }: { status: Device['status'] }) { return <span className={`status-pill ${status === 'Online' ? 'online' : 'offline'}`}><i />{status}</span> }

function DeviceCard({ device, selected, running, connected, onSelect }: { device: Device; selected: boolean; running: boolean | undefined; connected: boolean; onSelect: () => void }) {
  const Icon = device.icon
  const unit = device.unit === 'C' ? '°C' : device.unit
  return <button aria-pressed={selected} className={`device-card ${selected ? 'selected' : ''} ${!connected ? 'disconnected' : ''}`} onClick={onSelect}>
    <span className="card-selection" aria-hidden="true" />
    <span className="device-card-head">
      <span className={`device-icon ${device.color}`}><Icon size={22} /></span>
      <span className="device-identity"><strong>{device.name}</strong><span className="device-type">{device.type}</span></span>
      <ChevronRight className="card-chevron" size={19} />
    </span>
    <span className="device-card-body">
      <span className="reading-label">Primary reading</span>
      <span key={`${device.value}-${device.power}-${device.energy}`} className="device-reading"><span>{device.value || '—'}</span><small>{unit}</small></span>
    </span>
    <span className="device-card-foot">
      <span className={`operating-state ${running ? 'running' : 'idle'}`}>{running === undefined ? 'State unknown' : running ? 'Running' : 'Stopped'}</span>
      <StatusPill status={connected ? 'Online' : 'Offline'} />
      <span className="freshness">Just now</span>
    </span>
  </button>
}

function DetailPanel({ device, connected, children }: { device: Device; connected: boolean; children: React.ReactNode }) {
  const Icon = device.icon
  const reading = device.unit ? `${device.value}${device.unit === 'C' ? '°C' : device.unit}` : device.value
  const running = isRunning(device)
  const motor = device.deviceType === 'MIXER' ? displayValue(findPoint(device.points, 'motor')?.value).trim() : ''

  return <section className="detail-panel">
    <div className="detail-header">
      <div className="detail-title"><Icon size={27} /><div><h2>{device.name}</h2><p>{device.description}</p></div></div>
      <div className="detail-statuses">
        <span className={`operating-state ${running ? 'running' : 'idle'}`}>{running === undefined ? 'State unknown' : running ? 'Running' : 'Stopped'}</span>
        <StatusPill status={connected ? 'Online' : 'Offline'} />
      </div>
    </div>
    <div className="detail-main">
      <div className="reading-display"><div className="reading-highlight">
        <span>{reading || '—'}</span><b>{device.unit ? 'Primary reading' : 'Current state'}</b>
        {device.setpoint !== undefined && <p>Setpoint <strong>{device.setpoint} °C</strong></p>}
      </div></div>
      <div className="detail-stats">
        <div><Clock3 /><span>Device ID<strong>{device.id}</strong></span></div>
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
  const [alarms, setAlarms] = useState<AlarmEvent[]>([])

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
    let disposed = false
    const socketUrl = apiUrl.replace(/^http/, 'ws') + '/ws'
    const socket = new WebSocket(socketUrl)

    sessionFetch('/devices')
      .then((response) => {
        if (!response.ok) throw new Error(`Device request failed: ${response.status}`)
        return response.json() as Promise<ApiDevice[]>
      })
      .then((items) => {
        if (disposed) return
        const nextDevices = items.map(createDevice)
        setDevices(nextDevices)
        setSelectedId((current) => current || nextDevices[0]?.id || '')
      })
      .catch((error) => console.error('Could not load devices', error))

    socket.onopen = () => setConnected(true)
    socket.onclose = () => {
      setConnected(false)
      if (!disposed) void sessionFetch('/auth/me').then((response) => {
        if (response.status === 401) window.dispatchEvent(new Event('kitchenos:session-expired'))
      }).catch(() => { /* An unavailable API is handled by the connection banner. */ })
    }
    socket.onerror = () => setConnected(false)
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

    return () => {
      disposed = true
      socket.close()
    }
  }, [])

  const selected = devices.find((device) => device.id === selectedId) ?? devices[0]
  const online = connected ? devices.length : 0
  const today = new Intl.DateTimeFormat('en-CA', { month: 'short', day: 'numeric', year: 'numeric' }).format(new Date())

  if (!selected) return <main className="app-shell"><section className="main-area"><div className="page-content"><p>Waiting for device inventory. Your session is active.</p><button onClick={() => void logout()}>Sign out</button></div></section></main>

  const navItems = [{ label: 'Overview', icon: LayoutGrid }, { label: 'Equipment', icon: ClipboardList }, { label: 'Alarms', icon: Bell }, { label: 'Trends', icon: Activity }, { label: 'Events', icon: ClipboardList }, { label: 'Settings', icon: Settings }]
  const profile = profiles.find((item) => item.deviceType === selected.deviceType)

  return <main className="app-shell">
    <aside className={`sidebar ${menuOpen ? 'open' : ''}`}>
      <div className="brand"><img src="/kitchenos-logo.svg" alt="KitchenOS" /><button aria-label="Close menu" className="close-menu" onClick={() => setMenuOpen(false)}><X size={20} /></button></div>
      <nav>{navItems.map(({ label, icon: Icon }) => <a className={label === 'Overview' ? 'active' : ''} href={`#${label.toLowerCase()}`} key={label}><Icon size={21} /><span>{label}</span></a>)}</nav>
      <div className="sidebar-foot"><span className="version-dot" /> KitchenOS v1.0</div>
    </aside>
    {menuOpen && <button className="scrim" onClick={() => setMenuOpen(false)} aria-label="Close menu" />}
    <section className="main-area">
      <header className="topbar">
        <button aria-label="Open menu" className="mobile-menu" onClick={() => setMenuOpen(true)}><Menu size={22} /></button>
        <div className="top-brand"><img src="/kitchenos-logo.svg" alt="KitchenOS" /></div>
        <div className={`connection-status ${connected ? 'connected' : 'disconnected'}`}>{connected ? <Wifi size={17} /> : <WifiOff size={17} />}<span>{connected ? 'Connected' : 'Disconnected'}</span></div>
        <div className="system-normal"><i /> {connected ? 'Monitoring active' : 'Status unavailable'}</div>
        <div className="top-meta"><span><CalendarDays size={20} /> {today}</span><span><Clock3 size={20} /> Live</span><span className="operator"><Power size={19} /> {user.roles.join(", ")} <ChevronDown size={17} /></span><button onClick={() => void logout()}>Sign out</button></div>
      </header>
      <div className="page-content">
        {!connected && <section className="connection-banner" role="status"><WifiOff size={20} /><div><strong>Live connection unavailable</strong><span>Equipment values may be stale. Controls are disabled until the connection returns.</span></div></section>}
        <section className="kpi-row">
          <div className="kpi"><span className="kpi-icon"><Layers3 /></span><div><b>{online} / {devices.length}</b><small>Equipment online</small><em>{connected ? 'Updated just now' : 'Telemetry unavailable'}</em></div></div>
          <div className="kpi"><span className="kpi-icon"><Fan /></span><div><b>{devices.filter(isRunning).length}</b><small>Equipment running</small><em>Current operating state</em></div></div>
          <div className="kpi"><span className="kpi-icon"><AlertTriangle /></span><div><b>{alarms.length}</b><small>Recent alarms</small><em>Latest received alarm events</em></div></div>
          <div className="kpi"><span className="kpi-icon"><Activity /></span><div><b>{selected.type}</b><small>Selected equipment</small><em>Updated just now</em></div></div>
        </section>
        <div className="dashboard-grid">
          <section id="equipment" className="equipment-section"><div className="section-title"><div><h1><LayoutGrid size={22} /> Equipment Overview</h1><p>Normal equipment is quiet; exceptions appear prominently.</p></div><span className={`live-label ${connected ? '' : 'offline'}`}>{connected ? 'Live' : 'Offline'}</span></div><div className="devices-grid">{devices.map((device) => <DeviceCard key={device.id} device={device} selected={device.id === selectedId} running={isRunning(device)} connected={connected} onSelect={() => setSelectedId(device.id)} />)}</div></section>
          <DetailPanel device={selected} connected={connected}>
            {!canOperate ? <p className="command-feedback">Viewer access — monitoring only.</p> : capabilityStatus === 'ready'
              ? <DeviceCommandControls key={selected.id} profile={profile} points={selected.points} deviceName={selected.name} running={isRunning(selected)}
                  disabled={!connected || pendingDevices.has(selected.id)} onSubmit={(request) => void submit(selected.id, selected.name, request)} />
              : <p className="command-feedback" role="status">{capabilityStatus === 'loading' ? 'Loading equipment controls…' : 'Equipment controls could not be loaded.'}</p>}
          </DetailPanel>
        </div>
        <section id="alarms" className="info-panel">
          <div className="panel-title">
            <h2><AlertTriangle size={20} /> Active Alarms <span>{alarms.length}</span></h2>
            <button>View All →</button>
          </div>
          {alarms.length === 0 ? (
            <div className="alarm-row"><AlertTriangle className="danger" size={22} /><b>System</b><span>Waiting for live alarm events</span><time>Now</time></div>
          ) : alarms.map((alarm) => (
            <div className="alarm-row" key={alarm.id}>
              <AlertTriangle className="danger" size={22} />
              <b>{devices.find((device) => device.id === alarm.deviceId)?.name || alarm.deviceId}</b>
              <span>{alarm.text} ({alarm.code}) · {alarm.severity}</span>
              <time dateTime={alarm.timestamp}>{new Date(alarm.timestamp).toLocaleTimeString()}</time>
              <button onClick={() => setAlarms((current) => current.filter((item) => item.id !== alarm.id))} className="ack">Dismiss</button>
            </div>
          ))}
        </section>
        <section className="info-panel events"><div className="panel-title"><h2><ClipboardList size={20} /> Recent Events</h2><button>View history →</button></div><div className="event-row"><i /> <span>{connected ? 'Receiving live equipment telemetry' : 'Connection lost — displaying last known values'}</span><time>Now</time></div></section>
        <footer><span><span className="online-dot" /> KitchenOS v1.0</span><span>Last updated just now · <strong>{connected ? 'Connected' : 'Disconnected'}</strong></span></footer>
      </div>
    </section>
    <CommandToasts toasts={toasts} onDismiss={dismissToast} />
  </main>
}

