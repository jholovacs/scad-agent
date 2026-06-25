import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import { ChatPanel } from '../components/ChatPanel'
import { DiagnosticsPanel } from '../components/DiagnosticsPanel'
import { IterationTimeline } from '../components/IterationTimeline'
import { Viewport3D } from '../components/Viewport3D'
import { useAgentHub } from '../hooks/useAgentHub'
import type { Iteration } from '../types/api'
import { toSessionStatus, toIterationStatus } from '../types/api'

export function WorkspacePage() {
  const { id = '' } = useParams()
  const queryClient = useQueryClient()
  const [selectedIteration, setSelectedIteration] = useState<Iteration | undefined>()

  const sessionQuery = useQuery({
    queryKey: ['session', id],
    queryFn: () => api.getSession(id),
    enabled: Boolean(id),
    refetchInterval: (query) =>
      toSessionStatus(query.state.data?.status) === 'Iterating' ? 2000 : false,
  })

  const iterationsQuery = useQuery({
    queryKey: ['iterations', id],
    queryFn: () => api.getIterations(id),
    enabled: Boolean(id),
  })

  const { progress, lastFailure } = useAgentHub(id)

  const messageMutation = useMutation({
    mutationFn: (content: string) => api.postMessage(id, content),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['session', id] })
      void queryClient.invalidateQueries({ queryKey: ['iterations', id] })
    },
  })

  const activeIteration = useMemo(() => {
    if (selectedIteration)
      return selectedIteration

    const fromList = iterationsQuery.data
      ?.slice()
      .sort((a, b) => b.version - a.version)
      .find((i) => i.id === sessionQuery.data?.currentIterationId)
      ?? iterationsQuery.data?.slice().sort((a, b) => b.version - a.version)[0]

    return fromList ?? sessionQuery.data?.currentIteration
  }, [selectedIteration, sessionQuery.data?.currentIteration, sessionQuery.data?.currentIterationId, iterationsQuery.data])

  const sessionStatus = toSessionStatus(sessionQuery.data?.status)
  const activeIterationStatus = activeIteration
    ? toIterationStatus(activeIteration.status)
    : undefined

  const stlUrl = activeIteration?.hasStl ? api.stlUrl(activeIteration.id) : undefined
  const isBusy = sessionStatus === 'Iterating' || messageMutation.isPending

  const diagnosticText =
    activeIteration?.diagnosticLog
    ?? lastFailure?.details
    ?? activeIteration?.renderError

  const diagnosticSummary =
    lastFailure?.message
    ?? activeIteration?.renderError
    ?? (sessionStatus === 'Failed' ? 'The last iteration failed.' : undefined)

  if (sessionQuery.isError) {
    return (
      <main className="page workspace">
        <p className="chat-panel__error">
          {sessionQuery.error instanceof Error ? sessionQuery.error.message : 'Failed to load session.'}
        </p>
      </main>
    )
  }

  return (
    <main className="page workspace">
      <header className="workspace__header">
        <Link to="/">← Sessions</Link>
        <h2>{sessionQuery.data?.title ?? 'Loading…'}</h2>
        <span className={`badge badge--${sessionQuery.data ? sessionStatus.toLowerCase() : 'draft'}`}>
          {sessionQuery.data ? sessionStatus : '…'}
        </span>
      </header>

      {(sessionStatus === 'Failed' || diagnosticText) && (
        <DiagnosticsPanel
          title="Iteration diagnostics"
          summary={diagnosticSummary}
          details={diagnosticText}
        />
      )}

      <div className="workspace__grid">
        <ChatPanel
          messages={sessionQuery.data?.messages ?? []}
          disabled={isBusy}
          onSend={async (content) => {
            await messageMutation.mutateAsync(content)
          }}
          statusText={isBusy ? progress?.message : undefined}
          errorText={messageMutation.isError && messageMutation.error instanceof Error
            ? messageMutation.error.message
            : undefined}
        />

        <div className="workspace__viewer">
          <Viewport3D stlUrl={stlUrl} />
          {activeIterationStatus === 'Failed' && !activeIteration?.hasStl && (
            <p className="viewport-fallback">
              No model to preview. See the diagnostic report above for Ollama/OpenSCAD details.
            </p>
          )}
          <IterationTimeline
            iterations={iterationsQuery.data ?? []}
            selectedId={activeIteration?.id}
            onSelect={setSelectedIteration}
          />
        </div>
      </div>
    </main>
  )
}
