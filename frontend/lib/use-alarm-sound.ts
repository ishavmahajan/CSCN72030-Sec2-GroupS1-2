import { useCallback, useEffect, useRef, useState } from 'react'

export function useAlarmSound() {
  const audioRef = useRef<AudioContext | null>(null)
  const lastTone = useRef(-Infinity)
  const [soundEnabled, setSoundEnabled] = useState(false)
  const [soundError, setSoundError] = useState('')

  useEffect(() => () => {
    const audio = audioRef.current
    audioRef.current = null
    if (audio) void audio.close().catch(() => {})
  }, [])

  const playAlarmSound = useCallback((severity: string) => {
    const audio = audioRef.current
    if (!audio || audio.state !== 'running') return

    const now = audio.currentTime
    if (now - lastTone.current < 0.75) return
    lastTone.current = now

    const tone = audio.createOscillator()
    const volume = audio.createGain()
    const duration = severity === 'critical' ? 0.42 : 0.22
    tone.frequency.value = severity === 'critical' ? 880 : severity === 'warning' ? 660 : 440
    volume.gain.setValueAtTime(0, now)
    volume.gain.linearRampToValueAtTime(0.14, now + 0.02)
    volume.gain.exponentialRampToValueAtTime(0.001, now + duration)
    tone.connect(volume)
    volume.connect(audio.destination)
    tone.onended = () => {
      tone.disconnect()
      volume.disconnect()
    }
    tone.start(now)
    tone.stop(now + duration)
  }, [])

  async function toggleAlarmSound() {
    setSoundError('')
    const current = audioRef.current
    if (current) {
      audioRef.current = null
      setSoundEnabled(false)
      await current.close().catch(() => {})
      return
    }

    let audio: AudioContext | undefined
    try {
      audio = new AudioContext()
      audioRef.current = audio
      await audio.resume()
      if (audioRef.current !== audio) return
      lastTone.current = -Infinity
      setSoundEnabled(true)
      playAlarmSound('info')
    } catch {
      if (audio && audioRef.current !== audio) return
      audioRef.current = null
      if (audio) void audio.close().catch(() => {})
      setSoundEnabled(false)
      setSoundError('Alarm sound could not be enabled.')
    }
  }

  return { soundEnabled, soundError, toggleAlarmSound, playAlarmSound }
}
