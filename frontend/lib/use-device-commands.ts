import { useCallback, useEffect, useRef, useState } from 'react'
import type { CommandRequest } from './device-capabilities'

export type CommandResult = { commandId: string; status: string; reason?: string }
export type CommandToast = { id: number; deviceName: string; kind: 'pending' | 'success' | 'error'; text: string }

type PendingCommand = {
  toastId: number
  deviceName: string
  request: CommandRequest
  id?: string
  earlyResults: Map<string, CommandResult>
  timer: ReturnType<typeof setTimeout>
  controller: AbortController
}

export function useDeviceCommands(apiUrl: string, onAcknowledged: (deviceId: string, request: CommandRequest) => void) {
  const pending = useRef(new Map<string, PendingCommand>())
  const nextToastId = useRef(0)
  const [pendingDevices, setPendingDevices] = useState<ReadonlySet<string>>(() => new Set())
  const [toasts, setToasts] = useState<CommandToast[]>([])
  const dismissToast = useCallback((id: number) => {
    setToasts((current) => current.filter((toast) => toast.id !== id))
  }, [])

  function notify(entry: PendingCommand, kind: CommandToast['kind'], text: string) {
    setToasts((current) => [
      ...current.filter((toast) => toast.id !== entry.toastId),
      { id: entry.toastId, deviceName: entry.deviceName, kind, text },
    ])
  }

  useEffect(() => {
    const requests = pending.current
    return () => {
      for (const entry of requests.values()) {
        clearTimeout(entry.timer)
        entry.controller.abort()
      }
      requests.clear()
    }
  }, [])

  function finish(deviceId: string, entry: PendingCommand, text: string, kind: 'success' | 'error' = 'error') {
    if (pending.current.get(deviceId) !== entry) return
    clearTimeout(entry.timer)
    pending.current.delete(deviceId)
    setPendingDevices((current) => {
      const next = new Set(current)
      next.delete(deviceId)
      return next
    })
    notify(entry, kind, text)
  }

  function receiveResult(deviceId: string, result: CommandResult) {
    const entry = pending.current.get(deviceId)
    if (!entry) return
    if (!entry.id) {
      // The WebSocket result can arrive before the HTTP response provides its ID.
      entry.earlyResults.set(result.commandId, result)
      return
    }
    if (entry.id !== result.commandId) return
    if (result.status === 'acknowledged') {
      onAcknowledged(deviceId, entry.request)
      finish(deviceId, entry, 'Command acknowledged.', 'success')
    }
    if (result.status === 'rejected') finish(deviceId, entry, result.reason ? `Command rejected. ${result.reason}` : 'Command rejected.')
    if (result.status === 'timedOut') finish(deviceId, entry, 'Command timed out. Check the equipment state before retrying.')
  }

  async function submit(deviceId: string, deviceName: string, request: CommandRequest) {
    if (pending.current.has(deviceId)) return
    const controller = new AbortController()
    const entry: PendingCommand = {
      toastId: ++nextToastId.current,
      deviceName,
      request,
      controller,
      earlyResults: new Map(),
      timer: setTimeout(() => {
        controller.abort()
        finish(deviceId, entry, 'Command timed out. Check the equipment state before retrying.')
      }, 15_000),
    }
    pending.current.set(deviceId, entry)
    setPendingDevices((current) => new Set([...current, deviceId]))

    try {
      const response = await fetch(`${apiUrl}/devices/${encodeURIComponent(deviceId)}/commands`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request),
        signal: controller.signal,
      })
      if (!response.ok) {
        const problem = await response.json().catch(() => null) as { error?: string; detail?: string } | null
        throw new Error(problem?.error ?? problem?.detail ?? `HTTP ${response.status}`)
      }
      const receipt = await response.json() as { commandId?: string }
      if (pending.current.get(deviceId) !== entry) return
      if (!receipt.commandId) throw new Error('The API response omitted the command ID.')
      entry.id = receipt.commandId
      notify(entry, 'pending', 'Command queued. Waiting for the simulator…')
      const result = entry.earlyResults.get(receipt.commandId)
      entry.earlyResults.clear()
      if (result) receiveResult(deviceId, result)
    } catch (error) {
      finish(deviceId, entry, `Could not submit the command. ${error instanceof Error ? error.message : ''}`.trim())
    }
  }

  return { pendingDevices, toasts, dismissToast, receiveResult, submit }
}
