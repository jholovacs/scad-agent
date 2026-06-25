import { HubConnectionBuilder, type HubConnection } from '@microsoft/signalr'
import { useEffect, useRef, useState } from 'react'
import type { AgentProgress } from '../types/api'

export function useAgentHub(sessionId?: string) {
  const connectionRef = useRef<HubConnection | null>(null)
  const [progress, setProgress] = useState<AgentProgress | null>(null)
  const [lastFailure, setLastFailure] = useState<AgentProgress | null>(null)
  const [connected, setConnected] = useState(false)

  useEffect(() => {
    if (!sessionId)
      return

    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/agent')
      .withAutomaticReconnect()
      .build()

    connectionRef.current = connection

    connection.on('IterationStarted', (payload: AgentProgress) => {
      setLastFailure(null)
      setProgress(payload)
    })
    connection.on('IterationProgress', (payload: AgentProgress) => setProgress(payload))
    connection.on('IterationCompleted', (payload: AgentProgress) => {
      setLastFailure(null)
      setProgress(payload)
    })
    connection.on('IterationFailed', (payload: AgentProgress) => {
      setLastFailure(payload)
      setProgress(payload)
    })

    connection
      .start()
      .then(() => connection.invoke('JoinSession', sessionId))
      .then(() => setConnected(true))
      .catch(() => setConnected(false))

    return () => {
      void connection.invoke('LeaveSession', sessionId).finally(() => {
        void connection.stop()
      })
    }
  }, [sessionId])

  return { progress, lastFailure, connected }
}
