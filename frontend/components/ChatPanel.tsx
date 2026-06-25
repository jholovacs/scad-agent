import { type FormEvent, type KeyboardEvent, useEffect, useRef } from 'react'
import { isDiagnosticReport, toMessageRole, type Message } from '../types/api'

interface ChatPanelProps {
  messages: Message[]
  disabled?: boolean
  isWorking?: boolean
  isLoading?: boolean
  statusText?: string
  pendingMessage?: string | null
  hasOlderMessages?: boolean
  loadingOlder?: boolean
  onLoadOlder?: () => void
  onSend: (content: string) => Promise<void>
  errorText?: string
}

function messageLabel(message: Message): string {
  const role = toMessageRole(message.role)
  if (role !== 'User')
    return role

  if (message.intent === 'Ask')
    return 'Question'

  if (message.intent === 'Design')
    return 'Design request'

  return 'You'
}

function MessageItem({ message }: { message: Message }) {
  const role = toMessageRole(message.role)
  const label = messageLabel(message)

  return (
    <article
      className={`message message--${role.toLowerCase()}${isDiagnosticReport(message.content) ? ' message--diagnostic' : ''}`}
    >
      <header>{label}</header>
      {isDiagnosticReport(message.content) ? (
        <pre className="message__diagnostic">{message.content}</pre>
      ) : (
        <p>{message.content}</p>
      )}
    </article>
  )
}

export function ChatPanel({
  messages,
  disabled,
  isWorking,
  isLoading,
  statusText,
  pendingMessage,
  hasOlderMessages,
  loadingOlder,
  onLoadOlder,
  onSend,
  errorText,
}: ChatPanelProps) {
  const inputRef = useRef<HTMLTextAreaElement>(null)
  const loadMoreRef = useRef<HTMLDivElement>(null)
  const messagesRef = useRef<HTMLDivElement>(null)

  async function submitMessage() {
    const input = inputRef.current
    if (!input)
      return

    const content = input.value.trim()
    if (!content || disabled || isWorking)
      return

    input.value = ''
    await onSend(content)
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    await submitMessage()
  }

  function handleKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key !== 'Enter' || event.ctrlKey || event.metaKey || event.altKey)
      return

    event.preventDefault()
    void submitMessage()
  }

  useEffect(() => {
    const sentinel = loadMoreRef.current
    const container = messagesRef.current
    if (!sentinel || !container || !hasOlderMessages || !onLoadOlder)
      return

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((entry) => entry.isIntersecting) && !loadingOlder)
          onLoadOlder()
      },
      { root: container, rootMargin: '120px' },
    )

    observer.observe(sentinel)
    return () => observer.disconnect()
  }, [hasOlderMessages, loadingOlder, onLoadOlder, messages.length])

  const activityText = statusText
    ?? (isWorking ? 'Sending your message…' : undefined)

  const showPending = pendingMessage && !messages.some(
    (message) => message.role === 'User' && message.content === pendingMessage,
  )

  return (
    <div className="chat-panel">
      {activityText && (
        <div className="chat-panel__activity" role="status" aria-live="polite">
          <span className="chat-panel__spinner" aria-hidden="true" />
          <span>{activityText}</span>
        </div>
      )}

      <div className="chat-panel__messages" ref={messagesRef}>
        {isLoading && messages.length === 0 && (
          <p className="muted">Loading chat history…</p>
        )}

        {messages.length === 0 && !showPending && !isWorking && !isLoading && (
          <p className="muted">Describe a 3D object to design, or ask a question about the current model.</p>
        )}

        {showPending && (
          <article className="message message--user message--pending">
            <header>You</header>
            <p>{pendingMessage}</p>
          </article>
        )}

        {messages.map((message) => (
          <MessageItem key={message.id} message={message} />
        ))}

        {hasOlderMessages && (
          <div className="chat-panel__load-more" ref={loadMoreRef}>
            {loadingOlder ? 'Loading older messages…' : 'Scroll for older messages'}
          </div>
        )}
      </div>

      {errorText && (
        <pre className="chat-panel__error">{errorText}</pre>
      )}

      <form className="chat-panel__composer" onSubmit={handleSubmit}>
        <textarea
          ref={inputRef}
          placeholder="Describe a design change, or ask about the current model…"
          rows={3}
          disabled={disabled || isWorking}
          onKeyDown={handleKeyDown}
        />
        <button type="submit" disabled={disabled || isWorking}>
          {isWorking ? 'Working…' : 'Send'}
        </button>
      </form>
    </div>
  )
}
