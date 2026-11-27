'use client'

import { useCallback, useEffect, useRef, useState } from 'react'
import { Activity, AlertTriangle, Bell, ClipboardList, ChevronRight, Fan, Gauge, Layers3, LayoutGrid, LoaderCircle, Menu, Plus, Snowflake, Square, Trash2, Utensils, Volume2, VolumeX, Waves, Wifi, WifiOff, X, Zap, type LucideIcon } from 'lucide-react'
import { DeviceCommandControls, type DevicePoint } from '../components/device-command-controls'
import { CommandToasts } from '../components/command-toasts'
import { CreateDeviceDialog, type CreatedDevice } from '../components/create-device-dialog'
import { useSession } from '../components/auth-gate'
import { apiUrl, sessionFetch } from '../lib/auth-api'
import { capabilityProvider, type CommandRequest, type DeviceProfile } from '../lib/device-capabilities'
import { useAlarmSound } from '../lib/use-alarm-sound'
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
type AlarmEvent = {
  alarmId: number
  deviceId: string
  deviceType: string
  code: string
  severity: string
  text: string
  timestamp: string
  isActive: boolean
  acknowledgedByOperatorId: string | null
  acknowledgedByOperator: string | null
  acknowledgedAtUtc: string | null
}
type AlarmAcknowledgement = {
  alarmId: number
  deviceId: string
  operatorId: string
  operatorName: string
  acknowledgedAtUtc: string
  alreadyAcknowledged: boolean
}
type DeviceEvent = {
  eventName: string
  deviceId: string
  deviceType: string
  timestamp: string
  message?: Partial<CommandResult> & {
    alarmId?: number
    deviceId?: string
    deviceType?: string
    values?: DevicePoint[]
    code?: string
    severity?: string
    text?: string
    timestamp?: string
    isActive?: boolean
    acknowledgedByOperatorId?: string | null
    acknowledgedByOperator?: string | null
    acknowledgedAtUtc?: string | null
  }
}

function alarmGroupKey(alarm: AlarmEvent) {
  return JSON.stringify([alarm.deviceId, alarm.code])
}

function mergeAlarm(current: AlarmEvent | undefined, incoming: AlarmEvent): AlarmEvent {
  if (!current) return incoming
  return {
    ...current,
    ...incoming,
    acknowledgedByOperatorId: incoming.acknowledgedByOperatorId ?? current.acknowledgedByOperatorId,
    acknowledgedByOperator: incoming.acknowledgedByOperator ?? current.acknowledgedByOperator,
    acknowledgedAtUtc: incoming.acknowledgedAtUtc ?? current.acknowledgedAtUtc,
  }
}

function mergeAlarms(current: AlarmEvent[], incoming: AlarmEvent[]): AlarmEvent[] {
  const alarms = new Map(current.map((alarm) => [alarm.alarmId, alarm]))
  for (const alarm of incoming) alarms.set(alarm.alarmId, mergeAlarm(alarms.get(alarm.alarmId), alarm))
  return [...alarms.values()]
    .sort((left, right) => Date.parse(right.timestamp) - Date.parse(left.timestamp))
}

function getAlarmTableRows(alarms: AlarmEvent[]) {
  const unacknowledged = new Map<string, AlarmEvent>()
  // Keep the earliest active occurrence so repeated events leave its row in place.
  for (const alarm of alarms.toReversed()) {
    if (alarm.acknowledgedAtUtc) continue
    const key = alarmGroupKey(alarm)
    const existing = unacknowledged.get(key)
    if (!existing || (alarm.isActive && !existing.isActive)) unacknowledged.set(key, alarm)
  }
  return alarms.filter((alarm) => alarm.acknowledgedAtUtc || unacknowledged.get(alarmGroupKey(alarm)) === alarm)
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

function DeviceCard({ device, selected, running, connected, hasCriticalAlarm, onSelect }: {
  device: Device
  selected: boolean
  running: boolean | undefined
  connected: boolean
  hasCriticalAlarm: boolean
  onSelect: () => void
}) {
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
      {hasCriticalAlarm && <span className="device-critical-alarm" title="Unacknowledged critical alarm">
        <AlertTriangle size={16} aria-hidden="true" />
        <span className="sr-only">Unacknowledged critical alarm</span>
      </span>}
    </span>
  </button>
}

function DetailPanel({ device, connected, canRemove, removing, onRemove, children }: { device: Device; connected: boolean; canRemove: boolean; removing: boolean; onRemove: () => void; children: React.ReactNode }) {
  const Icon = device.icon
  const unit = device.unit === 'C' ? '°C' : device.unit
  const running = isRunning(device)
  const motor = device.deviceType === 'MIXER' ? displayValue(findPoint(device.points, 'motor')?.value).trim() : ''

  return <section id="equipment-detail" className="detail-panel" aria-labelledby="equipment-detail-title">
    <div className="detail-header">
      <div className="detail-title"><Icon size={27} /><div><h2 id="equipment-detail-title" tabIndex={-1}>{device.name}</h2><p>{device.description}</p></div></div>
      <div className="detail-statuses">
        <OperatingState running={running} connected={connected} />
        {canRemove && <button className="remove-device-button" onClick={onRemove} disabled={removing}>
          {removing ? <LoaderCircle className="spin" size={16} /> : <Trash2 size={16} />}
          {removing ? 'Removing…' : 'Remove device'}
        </button>}
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
  const { soundEnabled, soundError, toggleAlarmSound, playAlarmSound } = useAlarmSound()
  const { pendingDevices, toasts, dismissToast, dismissAllToasts, showToast, receiveResult, submit } = useDeviceCommands(apiUrl, applyAcknowledgement)
  const [connected, setConnected] = useState(false)
  const [menuOpen, setMenuOpen] = useState(false)
  const menuButton = useRef<HTMLButtonElement>(null)
  const menuCloseButton = useRef<HTMLButtonElement>(null)
  const [inventoryStatus, setInventoryStatus] = useState<'loading' | 'ready' | 'error'>('loading')
  const [inventoryAttempt, setInventoryAttempt] = useState(0)
  const [alarms, setAlarms] = useState<AlarmEvent[]>([])
  const [alarmPage, setAlarmPage] = useState(1)
  const [alarmFilter, setAlarmFilter] = useState<'all' | 'unacknowledged' | 'acknowledged'>('all')
  const [alarmDeviceFilter, setAlarmDeviceFilter] = useState('')
  const [selectedAlarmIds, setSelectedAlarmIds] = useState<Set<number>>(() => new Set())
  const [bulkAcknowledging, setBulkAcknowledging] = useState(false)
  const [dismissedAlarmPage, setDismissedAlarmPage] = useState(1)
  const [locallyDismissedAlarmIds, setLocallyDismissedAlarmIds] = useState<Set<number>>(() => new Set())
  const [acknowledgingAlarmGroups, setAcknowledgingAlarmGroups] = useState<Set<string>>(() => new Set())
  const [alarmError, setAlarmError] = useState('')
  const [createDialogOpen, setCreateDialogOpen] = useState(false)
  const [removingDeviceId, setRemovingDeviceId] = useState('')
  const [removeError, setRemoveError] = useState('')

  const refreshAlarmHistory = useCallback(async (signal?: AbortSignal) => {
    try {
      const response = await sessionFetch('/alarms', { signal })
      if (!response.ok) throw new Error(`Alarm history request failed: ${response.status}`)
      const items = await response.json() as Array<Omit<AlarmEvent, 'timestamp'> & { occurredAtUtc: string }>
      if (signal?.aborted) return
      setAlarms((current) => mergeAlarms(current, items.map((alarm) => ({
        ...alarm,
        timestamp: alarm.occurredAtUtc,
      }))))
      setAlarmError('')
    } catch (error) {
      if (signal?.aborted) return
      console.error('Could not load persisted alarms', error)
      setAlarmError('Alarm history could not be loaded.')
    }
  }, [])

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
    const controller = new AbortController()
    void refreshAlarmHistory(controller.signal)
    return () => controller.abort()
  }, [refreshAlarmHistory])

  useEffect(() => {
    let disposed = false
    let socket: WebSocket
    let retry: ReturnType<typeof setTimeout>
    const socketUrl = apiUrl.replace(/^http/, 'ws') + '/ws'

    function connect() {
      socket = new WebSocket(socketUrl)
      socket.onopen = () => {
        setConnected(true)
        void refreshAlarmHistory()
      }
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
            const alarmId = alarm?.alarmId
            const code = alarm?.code
            const severity = alarm?.severity
            const text = alarm?.text
            if (!alarmId || !message.deviceId || !message.deviceType || !code || !severity || !text) {
              console.error('Ignoring malformed alarm event', message)
              return
            }

            const level = severity.trim().toLowerCase()
            playAlarmSound(level)
            showToast({
              deviceName: `${severity.trim()} alarm · ${message.deviceId}`,
              kind: level === 'critical' || level === 'warning' ? level : 'alarm',
              text: `${text} (${code})`,
            })
            setAlarms((current) => mergeAlarms(current, [{
              alarmId,
              deviceId: message.deviceId,
              deviceType: message.deviceType,
              code,
              severity,
              text,
              timestamp: alarm.timestamp ?? message.timestamp,
              isActive: alarm.isActive ?? true,
              acknowledgedByOperatorId: alarm.acknowledgedByOperatorId ?? null,
              acknowledgedByOperator: alarm.acknowledgedByOperator ?? null,
              acknowledgedAtUtc: alarm.acknowledgedAtUtc ?? null,
            }]))
            return
          }

          if (message.eventName === 'alarmAcknowledged') {
            const alarm = message.message
            const alarmId = alarm?.alarmId
            const deviceId = message.deviceId
            const deviceType = message.deviceType
            const code = alarm?.code
            const severity = alarm?.severity
            const text = alarm?.text
            if (!alarmId || !deviceId || !deviceType || !code || !severity || !text) {
              console.error('Ignoring malformed alarm acknowledgement event', message)
              return
            }
            setAlarms((current) => mergeAlarms(current, [{
              alarmId,
              deviceId,
              deviceType,
              code,
              severity,
              text,
              timestamp: alarm.timestamp ?? message.timestamp,
              isActive: alarm.isActive ?? true,
              acknowledgedByOperatorId: alarm.acknowledgedByOperatorId ?? null,
              acknowledgedByOperator: alarm.acknowledgedByOperator ?? null,
              acknowledgedAtUtc: alarm.acknowledgedAtUtc ?? null,
            }]))
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
  }, [receiveResult, refreshAlarmHistory, showToast, playAlarmSound])

  const selected = devices.find((device) => device.id === selectedId) ?? devices[0]

  const alarmTableRows = getAlarmTableRows(alarms)
  const alarmDevices = new Map(devices.map((device) => [device.id, device.name]))
  for (const alarm of alarms) {
    if (!alarmDevices.has(alarm.deviceId)) alarmDevices.set(alarm.deviceId, alarm.deviceId)
  }
  const alarmDeviceOptions = [...alarmDevices].sort((left, right) => left[1].localeCompare(right[1], undefined, { numeric: true }))
  const matchesAlarmFilters = (alarm: AlarmEvent) =>
    (!alarmDeviceFilter || alarm.deviceId === alarmDeviceFilter)
    && (alarmFilter === 'all' || !!alarm.acknowledgedAtUtc === (alarmFilter === 'acknowledged'))
  const visibleAlarms = alarmTableRows.filter((alarm) => !locallyDismissedAlarmIds.has(alarm.alarmId))
  const filteredAlarms = visibleAlarms.filter(matchesAlarmFilters)
  const selectableAlarms = filteredAlarms.filter((alarm) => !alarm.acknowledgedAtUtc)
  const selectedAlarms = selectableAlarms.filter((alarm) => selectedAlarmIds.has(alarm.alarmId))
  const allAlarmsSelected = selectableAlarms.length > 0 && selectedAlarms.length === selectableAlarms.length
  const criticalDeviceIds = new Set(alarms
    .filter((alarm) => alarm.isActive && !alarm.acknowledgedAtUtc && alarm.severity.trim().toLowerCase() === 'critical')
    .map((alarm) => alarm.deviceId))
  const locallyDismissedAlarms = alarmTableRows.filter((alarm) => locallyDismissedAlarmIds.has(alarm.alarmId))
  const filteredDismissedAlarms = locallyDismissedAlarms.filter(matchesAlarmFilters)

  function resetAlarmSelection() {
    setAlarmPage(1)
    setDismissedAlarmPage(1)
    setSelectedAlarmIds(new Set())
  }

  async function acknowledgeAlarms(rows: AlarmEvent[], bulk = false) {
    const pendingRows = rows.filter((alarm) => !acknowledgingAlarmGroups.has(alarmGroupKey(alarm)))
    if (!canOperate || pendingRows.length === 0) return

    const groups = pendingRows.map(alarmGroupKey)
    setAcknowledgingAlarmGroups((current) => new Set([...current, ...groups]))
    if (bulk) setBulkAcknowledging(true)
    setAlarmError('')
    const succeeded = new Set<number>()
    let firstError = ''

    try {
      for (const alarm of pendingRows) {
        try {
          const response = await sessionFetch('/alarms/acknowledgements', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ alarmId: alarm.alarmId, deviceId: alarm.deviceId }),
          })
          if (!response.ok) {
            const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null
            throw new Error(problem?.detail ?? problem?.title ?? `Acknowledgement failed: ${response.status}`)
          }

          const acknowledgement = await response.json() as AlarmAcknowledgement
          succeeded.add(alarm.alarmId)
          setAlarms((current) => current.map((item) => item.alarmId === acknowledgement.alarmId
            ? {
              ...item,
              acknowledgedByOperatorId: acknowledgement.operatorId,
              acknowledgedByOperator: acknowledgement.operatorName,
              acknowledgedAtUtc: acknowledgement.acknowledgedAtUtc,
            }
            : item))
        } catch (error) {
          firstError ||= error instanceof Error ? error.message : 'Alarm acknowledgement failed.'
        }
      }

      if (succeeded.size > 0) {
        const alarm = pendingRows[0]
        showToast({
          deviceName: bulk ? 'Alarm acknowledgement' : devices.find((device) => device.id === alarm.deviceId)?.name ?? alarm.deviceId,
          kind: 'success',
          text: bulk
            ? `Acknowledged ${succeeded.size} of ${pendingRows.length} selected alarm groups.`
            : `Alarm acknowledged (${alarm.code}).`,
        })
        setSelectedAlarmIds((current) => new Set([...current].filter((id) => !succeeded.has(id))))
        await refreshAlarmHistory()
      }
      if (firstError) {
        const failed = pendingRows.length - succeeded.size
        setAlarmError(bulk ? `${failed} of ${pendingRows.length} selected alarms could not be acknowledged. ${firstError}` : firstError)
      }
    } finally {
      setAcknowledgingAlarmGroups((current) => {
        const next = new Set(current)
        for (const group of groups) next.delete(group)
        return next
      })
      if (bulk) setBulkAcknowledging(false)
    }
  }

  function handleCreated(device: CreatedDevice) {
    const nextDevice = createDeviceFromRegistration(device)
    setDevices((current) => current.some((item) => item.id === nextDevice.id) ? current : [...current, nextDevice])
    setSelectedId(nextDevice.id)
  }

  async function handleRemove(device: Device) {
    if (!window.confirm(`Remove "${device.name}" from the dashboard? This action cannot be undone.`)) return

    setRemovingDeviceId(device.id)
    setRemoveError('')
    try {
      const response = await sessionFetch(`/devices/${encodeURIComponent(device.id)}`, { method: 'DELETE' })
      if (!response.ok) {
        const body = await response.json().catch(() => null) as { error?: string; detail?: string } | null
        throw new Error(body?.error ?? body?.detail ?? `Could not remove device (${response.status}).`)
      }

      setDevices((current) => current.filter((item) => item.id !== device.id))
      setSelectedId((current) => current === device.id ? '' : current)
    } catch (cause) {
      setRemoveError(cause instanceof Error ? cause.message : 'Could not remove device.')
    } finally {
      setRemovingDeviceId('')
    }
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

  function renderAlarmTable(rows: AlarmEvent[], locallyDismissed = false) {
    const label = locallyDismissed ? 'Locally dismissed alarms' : 'Alarm history'
    const showSelection = canOperate && !locallyDismissed && alarmFilter !== 'acknowledged'
    const pageSize = 10
    const pageCount = Math.ceil(rows.length / pageSize)
    const page = Math.min(locallyDismissed ? dismissedAlarmPage : alarmPage, pageCount)
    const setPage = locallyDismissed ? setDismissedAlarmPage : setAlarmPage
    const start = (page - 1) * pageSize
    const pageRows = rows.slice(start, start + pageSize)

    return <>
      <div className="alarm-table-scroll" role="region" aria-label={label} tabIndex={0}>
        <table className="alarm-table" aria-label={label}>
          <colgroup>
            {showSelection && <col className="alarm-selection-col" />}
            <col className="alarm-equipment-col" />
            <col />
            <col className="alarm-severity-col" />
            <col className="alarm-status-col" />
            <col className="alarm-time-col" />
            <col className="alarm-actions-col" />
          </colgroup>
          <thead>
            <tr>
              {showSelection && <th scope="col"><span className="sr-only">Selection</span></th>}
              <th scope="col">Equipment</th>
              <th scope="col">Alarm</th>
              <th scope="col">Severity</th>
              <th scope="col">Status</th>
              <th scope="col">Time</th>
              <th scope="col">Actions</th>
            </tr>
          </thead>
          <tbody>{pageRows.map((alarm) => {
            const acknowledged = !!alarm.acknowledgedAtUtc
            return <tr className="alarm-row" key={alarm.alarmId} data-severity={alarm.severity.trim().toLowerCase()}>
              {showSelection && <td>{!acknowledged && <input type="checkbox"
                className="alarm-checkbox"
                aria-label={`Select ${alarm.code} for ${alarm.deviceId}`}
                checked={selectedAlarmIds.has(alarm.alarmId)}
                disabled={bulkAcknowledging || acknowledgingAlarmGroups.has(alarmGroupKey(alarm))}
                onChange={(event) => {
                  const checked = event.currentTarget.checked
                  setSelectedAlarmIds((current) => {
                    const next = new Set(current)
                    if (checked) next.add(alarm.alarmId)
                    else next.delete(alarm.alarmId)
                    return next
                  })
                }}
              />}</td>}
              <th scope="row">{devices.find((device) => device.id === alarm.deviceId)?.name || alarm.deviceId}</th>
              <td className="alarm-message">{alarm.text}<span className="alarm-code">{alarm.code}</span></td>
              <td><span className="alarm-severity">{alarm.severity}</span></td>
              <td>
                <div className="alarm-status">
                  {locallyDismissed && <span className="alarm-state locally-dismissed-state">Dismissed locally</span>}
                  {(acknowledged || !locallyDismissed) && <span className={`alarm-state ${acknowledged ? 'acknowledged' : alarm.isActive ? 'active' : ''}`}>
                    {acknowledged ? 'Acknowledged' : alarm.isActive ? 'Active' : 'Cleared'}
                  </span>}
                  {acknowledged && <span className="alarm-operator">{alarm.acknowledgedByOperator ?? alarm.acknowledgedByOperatorId}</span>}
                </div>
              </td>
              <td><time dateTime={alarm.timestamp}>{new Date(alarm.timestamp).toLocaleTimeString()}</time></td>
              <td>
                <div className="alarm-actions">
                  {!acknowledged && canOperate && <button
                    disabled={bulkAcknowledging || acknowledgingAlarmGroups.has(alarmGroupKey(alarm))}
                    onClick={() => void acknowledgeAlarms([alarm])}
                    className="ack"
                    aria-label={`Acknowledge ${alarm.code} for ${alarm.deviceId}`}
                  >{acknowledgingAlarmGroups.has(alarmGroupKey(alarm)) ? 'Acknowledging…' : 'Acknowledge'}</button>}
                  <button className="ack"
                    aria-label={`${locallyDismissed ? 'Restore' : 'Dismiss locally'} ${alarm.code} for ${alarm.deviceId}`}
                    onClick={() => {
                      if (pageRows.length === 1 && page > 1) setPage(page - 1)
                      if (!locallyDismissed && selectedAlarmIds.has(alarm.alarmId)) {
                        setSelectedAlarmIds((current) => new Set([...current].filter((id) => id !== alarm.alarmId)))
                      }
                      setLocallyDismissedAlarmIds((current) => {
                        const next = new Set(current)
                        if (locallyDismissed) next.delete(alarm.alarmId)
                        else next.add(alarm.alarmId)
                        return next
                      })
                    }}
                  >{locallyDismissed ? 'Restore' : 'Dismiss locally'}</button>
                </div>
              </td>
            </tr>
          })}</tbody>
        </table>
      </div>
      {pageCount > 1 && <nav className="alarm-pagination" aria-label={`${label} pagination`}>
        <span role="status">Showing {start + 1}–{start + pageRows.length} of {rows.length} · Page {page} of {pageCount}</span>
        <div className="alarm-pagination-buttons">
          <button type="button" className="ack" disabled={page === 1} onClick={() => setPage(page - 1)}>Previous</button>
          <button type="button" className="ack" disabled={page === pageCount} onClick={() => setPage(page + 1)}>Next</button>
        </div>
      </nav>}
    </>
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
          <div className="kpi"><span className="kpi-icon"><AlertTriangle /></span><div><b>{alarms.length}</b><small>Recorded alarms</small></div></div>
        </section>
        {removeError && <p className="remove-device-error" role="alert">{removeError}</p>}
        <div className={`dashboard-grid ${selected ? '' : 'without-selection'}`}>
          <section id="equipment" tabIndex={-1} className="equipment-section" aria-labelledby="equipment-title">
            <div className="section-title"><h2 id="equipment-title">Equipment</h2></div>
            {inventoryStatus === 'error' && <div className="inventory-message"><p role="status">Equipment inventory could not be loaded.</p><button className="sign-out" onClick={() => { setInventoryStatus('loading'); setInventoryAttempt((value) => value + 1) }}>Retry</button></div>}
            {!devices.length && inventoryStatus !== 'error' && <p className="inventory-message" role="status">{inventoryStatus === 'loading' ? 'Loading equipment…' : 'No equipment is registered yet.'}</p>}
            {!!devices.length && <div className="devices-grid">{devices.map((device) => <DeviceCard
              key={device.id} device={device} selected={device.id === selected?.id} running={isRunning(device)}
              connected={connected} hasCriticalAlarm={criticalDeviceIds.has(device.id)} onSelect={() => selectDevice(device.id)}
            />)}</div>}
          </section>
          {selected && <DetailPanel device={selected} connected={connected} canRemove={canCreate}
            removing={removingDeviceId === selected.id} onRemove={() => void handleRemove(selected)}>
            {!canOperate ? <p className="command-feedback">Viewer access · Monitoring only</p> : capabilityStatus === 'ready'
              ? <DeviceCommandControls key={selected.id} profile={profile} points={selected.points} deviceName={selected.name} running={isRunning(selected)}
                  disabled={!connected || pendingDevices.has(selected.id)} onSubmit={(request) => void submit(selected.id, selected.name, request)} />
              : <p className="command-feedback" role="status">{capabilityStatus === 'loading' ? 'Loading equipment controls…' : 'Equipment controls could not be loaded.'}</p>}
          </DetailPanel>}
        </div>
        <section id="alarms" tabIndex={-1} className="info-panel">
          <div className="panel-title">
            <div className="alarm-title-row">
              <h2><AlertTriangle size={20} /> Alarm history</h2>
              <button type="button" className="ack alarm-sound-toggle"
                aria-label={soundEnabled ? 'Mute alarm sound' : 'Enable alarm sound'}
                title={soundEnabled ? 'Mute alarm sound' : 'Enable alarm sound'}
                onClick={() => void toggleAlarmSound()}>
                {soundEnabled ? <Volume2 size={16} aria-hidden="true" /> : <VolumeX size={16} aria-hidden="true" />}
              </button>
            </div>
            <span className="alarm-counts">
              {alarmTableRows.filter((alarm) => alarm.isActive && !alarm.acknowledgedAtUtc).length} active · {alarms.filter((alarm) => !!alarm.acknowledgedAtUtc).length} acknowledged · {locallyDismissedAlarms.length} dismissed locally
            </span>
          </div>
          <div className="alarm-toolbar">
            <div className="alarm-filters">
              <label className="alarm-filter">Status
                <select value={alarmFilter} onChange={(event) => {
                  setAlarmFilter(event.currentTarget.value as typeof alarmFilter)
                  resetAlarmSelection()
                }}>
                  <option value="all">All statuses</option>
                  <option value="unacknowledged">Unacknowledged</option>
                  <option value="acknowledged">Acknowledged</option>
                </select>
              </label>
              <label className="alarm-filter">Device
                <select value={alarmDeviceFilter} onChange={(event) => {
                  setAlarmDeviceFilter(event.currentTarget.value)
                  resetAlarmSelection()
                }}>
                  <option value="">All devices</option>
                  {alarmDeviceOptions.map(([id, name]) => <option key={id} value={id}>{name}</option>)}
                </select>
              </label>
            </div>
            {canOperate && alarmFilter !== 'acknowledged' && <div className="alarm-bulk-actions">
              <button type="button" className="ack" disabled={bulkAcknowledging || selectableAlarms.length === 0}
                onClick={() => setSelectedAlarmIds(allAlarmsSelected ? new Set() : new Set(selectableAlarms.map((alarm) => alarm.alarmId)))}
              >{allAlarmsSelected ? 'Clear selection' : `Select all (${selectableAlarms.length})`}</button>
              <button type="button" className="ack alarm-bulk-submit"
                disabled={bulkAcknowledging || acknowledgingAlarmGroups.size > 0 || selectedAlarms.length === 0}
                onClick={() => void acknowledgeAlarms(selectedAlarms, true)}
              >{bulkAcknowledging ? 'Acknowledging…' : `Acknowledge selected (${selectedAlarms.length})`}</button>
            </div>}
          </div>
          {soundError && <p className="alarm-error" role="alert">{soundError}</p>}
          {alarmError && <p className="alarm-error" role="alert">{alarmError}</p>}
          {visibleAlarms.length === 0 ? (
            <p className="alarm-empty">{alarms.length === 0 ? (connected ? 'No alarms recorded.' : 'No alarms to display. Live monitoring is disconnected.') : 'All alarms were dismissed locally.'}</p>
          ) : filteredAlarms.length === 0 ? (
            <p className="alarm-empty">No alarms match these filters.</p>
          ) : renderAlarmTable(filteredAlarms)}
          {filteredDismissedAlarms.length > 0 && <details className="locally-dismissed">
            <summary>Locally dismissed alarms</summary>
            {renderAlarmTable(filteredDismissedAlarms, true)}
          </details>}
        </section>
      </div>
    </section>
    <CommandToasts toasts={toasts} onDismiss={dismissToast} onDismissAll={dismissAllToasts} />
    {canCreate && <CreateDeviceDialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} onCreated={handleCreated} />}
  </main>
}
