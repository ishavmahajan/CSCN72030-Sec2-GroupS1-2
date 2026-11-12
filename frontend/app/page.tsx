'use client'

import { useEffect, useMemo, useState } from 'react'
import { Activity, AlertTriangle, Bell, CalendarDays, ChevronDown, ClipboardList, Clock3, Fan, Gauge, Layers3, LayoutGrid, Menu, Play, Power, Settings, SlidersHorizontal, Snowflake, Square, Utensils, Waves, X, Zap, type LucideIcon } from 'lucide-react'

type Device = {
  id: string
  name: string
  description: string
  type: string
  value: string
  unit: string
  status: 'Online' | 'Offline'
  icon: LucideIcon
  color: string
  mode: string
  modes: string[]
  power: string
  energy: string
  setpoint: number
  hasSetpoint: boolean
  door?: string
}

type DevicePoint = { name: string; value: unknown; unit?: string | null; component?: string | null }
type ApiDevice = { deviceId: string; deviceName: string; deviceType: string; description: string; readPoints: DevicePoint[] }
type DeviceEvent = { deviceId: string; message?: { values?: DevicePoint[] } }

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
  return points.find((item) => names.includes(item.name))
}

function displayValue(value: unknown) {
  return typeof value === 'number' ? String(Math.round(value * 100) / 100) : String(value ?? '')
}

function createDevice(device: ApiDevice): Device {
  const metadata = deviceMetadata[device.deviceType] ?? { icon: Activity, color: 'blue', type: device.deviceType }
  const reading = findPoint(device.readPoints, 'temperature') ?? findPoint(device.readPoints, 'speed', 'fanSpeed', 'flow', 'airflow', 'waterLevel', 'heating', 'motor', 'compressor', 'cycle', 'valve')
  const power = findPoint(device.readPoints, 'power')
  const energy = findPoint(device.readPoints, 'energy')
  const setpoint = findPoint(device.readPoints, 'setpoint')
  const door = findPoint(device.readPoints, 'door')
  const mode = findPoint(device.readPoints, 'mode', 'powerState')

  return {
    id: device.deviceId,
    name: device.deviceName,
    description: device.description,
    type: metadata.type,
    value: displayValue(reading?.value),
    unit: reading?.unit ?? '',
    status: 'Online',
    icon: metadata.icon,
    color: metadata.color,
    mode: mode ? displayValue(mode.value) : 'Idle',
    modes: mode ? [displayValue(mode.value)] : ['Idle'],
    power: power ? `${displayValue(power.value)} ${power.unit ?? ''}`.trim() : '—',
    energy: energy ? `${displayValue(energy.value)} ${energy.unit ?? ''}`.trim() : '—',
    setpoint: typeof setpoint?.value === 'number' ? setpoint.value : 0,
    hasSetpoint: Boolean(setpoint),
    door: door ? displayValue(door.value) : undefined,
  }
}

function updateDevice(device: Device, points: DevicePoint[]): Device {
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
    modes: mode ? [displayValue(mode.value)] : device.modes,
  }
}

function StatusPill({ status }: { status: Device['status'] }) { return <span className={`status-pill ${status === 'Online' ? 'online' : 'offline'}`}><i />{status}</span> }
function Sparkline() { return <svg className="sparkline" viewBox="0 0 74 28" aria-hidden="true"><path d="M1 19 C10 20 12 7 19 15 S29 21 36 13 S45 7 51 17 S61 18 73 4" /></svg> }

function DeviceCard({ device, selected, active, onSelect }: { device: Device; selected: boolean; active: boolean; onSelect: () => void }) {
  const Icon = device.icon
  return <button className={`device-card ${selected ? 'selected' : ''}`} onClick={onSelect}><div className="device-top"><span className={`device-icon ${device.color}`}><Icon size={24} /></span><span className="device-menu">•••</span></div><strong>{device.name}</strong><span className="device-type">{device.type}</span><div key={`${device.value}-${device.power}-${device.energy}`} className="device-reading"><span>{active && device.value === 'Off' ? 'On' : device.value}</span><small>{device.unit}</small></div><StatusPill status={active ? 'Online' : 'Offline'} /></button>
}

function DetailPanel({ device, active, setpoint, mode, onToggle, onModeChange, onEditSetpoint }: { device: Device; active: boolean; setpoint: number; mode: string; onToggle: () => void; onModeChange: (mode: string) => void; onEditSetpoint: () => void }) {
  const Icon = device.icon
  const reading = device.unit ? `${device.value}${device.unit}` : (active ? 'On' : device.value)
  return <section className="detail-panel"><div className="detail-header"><div className="detail-title"><Icon size={27} /><div><h2>{device.name}</h2><p>{device.description}</p></div></div><StatusPill status={active ? 'Online' : 'Offline'} /></div><div className="detail-main"><div className="reading-display"><div className="reading-highlight"><span>{reading}</span><b>{device.unit ? 'Live reading' : 'State'}</b>{device.hasSetpoint && <p>Setpoint: <strong>{setpoint} °C</strong></p>}</div></div><div className="detail-stats"><div><Clock3 /><span>Device ID<strong>{device.id.slice(0, 8)}</strong></span></div><div><Waves /><span>Mode<select aria-label={`Change mode for ${device.name}`} value={mode} onChange={(event) => onModeChange(event.target.value)}>{device.modes.map((option) => <option key={option}>{option}</option>)}</select></span></div>{device.door && <div><Square /><span>Door<strong>{device.door}</strong></span></div>}</div></div><div className="detail-metrics"><div><Zap /><span>Power<strong>{device.power}</strong></span></div><div><Layers3 /><span>Energy<strong>{device.energy}</strong></span></div></div><div className="detail-actions"><button className="primary-action" onClick={onToggle}>{active ? <Square size={15} fill="currentColor" /> : <Play size={17} fill="currentColor" />} {active ? 'Turn off' : 'Turn on'}</button>{device.hasSetpoint && <button onClick={onEditSetpoint}><SlidersHorizontal size={17} /> Setpoint</button>}</div></section>
}

export default function Page() {
  const [devices, setDevices] = useState<Device[]>([])
  const [selectedId, setSelectedId] = useState('')
  const [active, setActive] = useState<Record<string, boolean>>({})
  const [setpoints, setSetpoints] = useState<Record<string, number>>({})
  const [modes, setModes] = useState<Record<string, string>>({})
  const [connected, setConnected] = useState(false)
  const [menuOpen, setMenuOpen] = useState(false)
  const [alarmVisible, setAlarmVisible] = useState(true)

  useEffect(() => {
    let disposed = false
    const socketUrl = apiUrl.replace(/^http/, 'ws') + '/ws'
    const socket = new WebSocket(socketUrl)

    fetch(`${apiUrl}/devices`)
      .then((response) => {
        if (!response.ok) throw new Error(`Device request failed: ${response.status}`)
        return response.json() as Promise<ApiDevice[]>
      })
      .then((items) => {
        if (disposed) return
        const nextDevices = items.map(createDevice)
        setDevices(nextDevices)
        setSelectedId((current) => current || nextDevices[0]?.id || '')
        setActive(Object.fromEntries(nextDevices.map((device) => [device.id, true])))
        setSetpoints(Object.fromEntries(nextDevices.map((device) => [device.id, device.setpoint])))
        setModes(Object.fromEntries(nextDevices.map((device) => [device.id, device.mode])))
      })
      .catch((error) => console.error('Could not load devices', error))

    socket.onopen = () => setConnected(true)
    socket.onclose = () => setConnected(false)
    socket.onerror = () => setConnected(false)
    socket.onmessage = (event) => {
      try {
        const message = JSON.parse(event.data) as DeviceEvent
        const values = message.message?.values
        if (!message.deviceId || !values) return
        setDevices((current) => current.map((device) => device.id === message.deviceId ? updateDevice(device, values) : device))
        setActive((current) => ({ ...current, [message.deviceId]: true }))
      } catch (error) {
        console.error('Could not parse device event', error)
      }
    }

    return () => {
      disposed = true
      socket.close()
    }
  }, [])

  const selected = useMemo(() => devices.find((device) => device.id === selectedId) ?? devices[0], [selectedId, devices])
  const toggle = (id: string) => setActive((current) => ({ ...current, [id]: !current[id] }))
  const changeMode = (id: string, mode: string) => setModes((current) => ({ ...current, [id]: mode }))
  const online = Object.values(active).filter(Boolean).length

  if (!selected) return <main className="app-shell"><section className="main-area"><div className="page-content"><p>Loading kitchen devices…</p></div></section></main>

  return <main className="app-shell"><aside className={`sidebar ${menuOpen ? 'open' : ''}`}><div className="brand"><img src="/kitchenos-logo.svg" alt="KitchenOS" /><button className="close-menu" onClick={() => setMenuOpen(false)}><X size={20} /></button></div><nav>{[{ label: 'Overview', icon: LayoutGrid }, { label: 'Equipment', icon: ClipboardList }, { label: 'Alarms', icon: Bell, badge: '2' }, { label: 'Trends', icon: Activity }, { label: 'Events', icon: ClipboardList }, { label: 'Settings', icon: Settings }].map(({ label, icon: Icon, badge }) => <a className={label === 'Overview' ? 'active' : ''} href={`#${label.toLowerCase()}`} key={label}><Icon size={21} /><span>{label}</span>{badge && <em>{badge}</em>}</a>)}</nav><div className="sidebar-foot"><span className="version-dot" /> KitchenOS v1.0</div></aside>{menuOpen && <button className="scrim" onClick={() => setMenuOpen(false)} aria-label="Close menu" />}<section className="main-area"><header className="topbar"><button className="mobile-menu" onClick={() => setMenuOpen(true)}><Menu size={22} /></button><div className="top-brand"><img src="/kitchenos-logo.svg" alt="KitchenOS" /></div><div className="system-normal"><i /> {connected ? 'System Normal' : 'Connecting…'}</div><div className="top-meta"><span><CalendarDays size={20} /> Nov 26, 2026</span><span><Clock3 size={20} /> Live</span><span className="operator"><Power size={19} /> Operator <ChevronDown size={17} /></span></div></header><div className="page-content"><section className="kpi-row"><div className="kpi"><span className="kpi-icon"><Layers3 /></span><div><b>{online} / {devices.length}</b><small>Equipment Online</small></div><Sparkline /></div><div className="kpi"><span className="kpi-icon"><Fan /></span><div><b>{connected ? 'Live' : 'Offline'}</b><small>Socket status</small></div><Sparkline /></div><div className="kpi"><span className="kpi-icon"><Zap /></span><div><b>{devices.length}</b><small>Connected devices</small></div><Sparkline /></div><div className="kpi"><span className="kpi-icon"><Activity /></span><div><b>{selected.type}</b><small>Selected device</small></div><Sparkline /></div></section><div className="dashboard-grid"><section id="equipment" className="equipment-section"><div className="section-title"><div><h1><LayoutGrid size={22} /> Equipment Overview</h1><p>Live devices from the SCADA simulator</p></div><button>{connected ? 'Connected' : 'Connecting…'} <span>•</span></button></div><div className="devices-grid">{devices.map((device) => <DeviceCard key={device.id} device={device} selected={device.id === selectedId} active={active[device.id]} onSelect={() => setSelectedId(device.id)} />)}</div></section><DetailPanel device={selected} active={active[selected.id]} setpoint={setpoints[selected.id] ?? selected.setpoint} mode={modes[selected.id] ?? selected.mode} onToggle={() => toggle(selected.id)} onModeChange={(mode) => changeMode(selected.id, mode)} onEditSetpoint={() => {}} /></div><div className="bottom-grid"><section id="alarms" className="info-panel"><div className="panel-title"><h2><AlertTriangle size={20} /> Active Alarms <span>0</span></h2><button>View All →</button></div>{alarmVisible ? <div className="alarm-row"><AlertTriangle className="danger" size={22} /><b>System</b><span>Waiting for live alarm events</span><time>Now</time><button onClick={() => setAlarmVisible(false)} className="ack">Dismiss</button></div> : <div className="cleared"><Power size={18} /> Alarms acknowledged</div>}</section><section className="info-panel events"><div className="panel-title"><h2><ClipboardList size={20} /> Recent Events</h2><button>Live →</button></div><div className="event-row"><i /> <span>{connected ? 'Receiving device telemetry' : 'Waiting for socket connection'}</span><time>Now</time></div></section></div><footer><span><span className="online-dot" /> KitchenOS v1.0</span><span>Last updated just now · <strong>{connected ? 'Connected' : 'Disconnected'}</strong></span></footer></div></section></main>
}
