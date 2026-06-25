import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import { ChatPanel } from '../components/ChatPanel'
import { DiagnosticsPanel } from '../components/DiagnosticsPanel'
import { IterationPanel } from '../components/IterationPanel'
import { IterationTimeline } from '../components/IterationTimeline'
import { SessionTitleEditor } from '../components/SessionTitleEditor'
import { Viewport3D } from '../components/Viewport3D'
import { useAgentHub } from '../hooks/useAgentHub'
import { useSessionIterations } from '../hooks/useSessionIterations'
import { useSessionMessages } from '../hooks/useSessionMessages'
import type { Iteration } from '../types/api'
import { toSessionStatus, toIterationStatus } from '../types/api'

export function WorkspacePage() {
  const { id = '' } = useParams()
  const queryClient = useQueryClient()
  const [selectedIteration, setSelectedIteration] = useState<Iteration | undefined>()
  const [pendingMessage, setPendingMessage] = useState<string | null>(null)

  const sessionQuery = useQuery({
    queryKey: ['session', id],
    queryFn: () => api.getSession(id),
    enabled: Boolean(id),
    refetchInterval: (query) =>
      toSessionStatus(query.state.data?.status) === 'Iterating' ? 2000 : false,
  })

  const {
    iterations,
    hasOlderIterations,
    loadOlderIterations,
    isLoadingOlder: isLoadingOlderIterations,
    isLoading: isLoadingIterations,
    refreshIterations,
  } = useSessionIterations(id)

  const activeIteration = useMemo(() => {
    if (selectedIteration)
      return selectedIteration

    const fromList = iterations
      .find((i) => i.id === sessionQuery.data?.currentIterationId)
      ?? iterations[0]

    return fromList ?? sessionQuery.data?.currentIteration
  }, [selectedIteration, sessionQuery.data?.currentIteration, sessionQuery.data?.currentIterationId, iterations])

  const {
    messages,
    hasOlderMessages,
    loadOlderMessages,
    isLoadingOlder,
    isLoading: isLoadingMessages,
    isError: messagesError,
    error: messagesLoadError,
    refreshMessages,
  } = useSessionMessages(id)

  const { progress, lastFailure } = useAgentHub(id)

  const messageMutation = useMutation({
    mutationFn: (content: string) => api.postMessage(id, content),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['session', id] })
      void queryClient.invalidateQueries({ queryKey: ['iterations', id] })
      void queryClient.invalidateQueries({ queryKey: ['messages', id] })
    },
    onSettled: () => {
      setPendingMessage(null)
    },
  })

  const titleMutation = useMutation({
    mutationFn: (title: string) => api.updateSessionTitle(id, title),
    onSuccess: (session) => {
      queryClient.setQueryData(['session', id], session)
      void queryClient.invalidateQueries({ queryKey: ['sessions'] })
    },
  })

  const sessionStatus = toSessionStatus(sessionQuery.data?.status)
  const activeIterationStatus = activeIteration
    ? toIterationStatus(activeIteration.status)
    : undefined

  const stlUrl = activeIteration?.hasStl ? api.stlUrl(activeIteration.id) : undefined
  const isWorking = messageMutation.isPending || sessionStatus === 'Iterating'

  const statusText = progress?.message
    ?? (messageMutation.isPending && !progress ? 'Sending your message…' : undefined)

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
        {sessionQuery.data ? (
          <SessionTitleEditor
            title={sessionQuery.data.title}
            disabled={isWorking || titleMutation.isPending}
            onSave={async (title) => {
              await titleMutation.mutateAsync(title)
            }}
          />
        ) : (
          <h2>Loading…</h2>
        )}
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
          messages={messages}
          pendingMessage={pendingMessage}
          isWorking={isWorking}
          isLoading={isLoadingMessages}
          statusText={statusText}
          hasOlderMessages={hasOlderMessages}
          loadingOlder={isLoadingOlder}
          onLoadOlder={() => void loadOlderMessages()}
          disabled={isWorking}
          onSend={async (content) => {
            setPendingMessage(content)
            await messageMutation.mutateAsync(content)
            refreshMessages()
            refreshIterations()
          }}
          errorText={
            messagesError
              ? (messagesLoadError instanceof Error
                ? messagesLoadError.message
                : 'Failed to load chat history.')
              : messageMutation.isError && messageMutation.error instanceof Error
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
          {activeIteration && (
            <IterationPanel
              iteration={activeIteration}
              exportTitle={sessionQuery.data?.title}
            />
          )}
          <IterationTimeline
            iterations={iterations}
            selectedId={activeIteration?.id}
            isLoading={isLoadingIterations}
            hasOlderIterations={hasOlderIterations}
            loadingOlder={isLoadingOlderIterations}
            onLoadOlder={() => void loadOlderIterations()}
            onSelect={setSelectedIteration}
          />
        </div>
      </div>
    </main>
  )
}
