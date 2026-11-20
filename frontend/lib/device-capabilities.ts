import deviceConfig from '../data/device-config.json'

export type CapabilityOption = { value: string; label: string }

export type ConfiguredCommand = {
  commandName: string
  label: string
  pointName?: string
  valueType?: string
  unit?: string
  min?: number
  max?: number
  step?: number
  componentRequired: boolean
  options?: CapabilityOption[]
}

export type DeviceProfile = {
  deviceType: string
  status: string
  modes: CapabilityOption[]
  commands: ConfiguredCommand[]
}

export type CapabilityProvider = { load(): Promise<readonly DeviceProfile[]> }

export const capabilityProvider: CapabilityProvider = {
  async load() {
    return deviceConfig.devices
  },
}

export function getCommandRequest(command: ConfiguredCommand, input: string, component: string) {
  if (command.componentRequired && !component) return
  const args = component ? { component } : {}
  const value = input.trim()
  const options = command.options ?? []

  switch (command.commandName) {
    case 'start':
    case 'stop':
    case 'isOnline':
      return { commandName: command.commandName, arguments: args }
    case 'setMode':
      if (options.some((option) => option.value === input)) {
        return { commandName: command.commandName, arguments: { ...args, mode: input } }
      }
      return
    case 'setValue': {
      if (!value || !command.pointName) return
      const { min, max, step, unit } = command
      if (command.valueType === 'number') {
        const number = Number(value)
        if (!Number.isFinite(number) || min === undefined || max === undefined || number < min || number > max) return
        if (step !== undefined) {
          const steps = (number - min) / step
          if (!(step > 0) || Math.abs(steps - Math.round(steps)) > 1e-8) return
        }
        return { commandName: command.commandName, arguments: { ...args, name: command.pointName, value: number, unit } }
      }
      if (command.valueType === 'string' && options.some((option) => option.value === input)) {
        return { commandName: command.commandName, arguments: { ...args, name: command.pointName, value: input, unit } }
      }
      if (command.valueType === 'boolean' && (input === 'true' || input === 'false')) {
        return { commandName: command.commandName, arguments: { ...args, name: command.pointName, value: input === 'true', unit } }
      }
    }
  }
}

export type CommandRequest = NonNullable<ReturnType<typeof getCommandRequest>>
