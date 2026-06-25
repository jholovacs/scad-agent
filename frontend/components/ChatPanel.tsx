import { type FormEvent, useState } from 'react'
import { isDiagnosticReport, toMessageRole, type Message } from '../types/api'

interface ChatPanelProps {
  messages: Message[]
  disabled?: boolean
  onSend: (content: string) => Promise<void>
  statusText?: string
  errorText?: string
}

export function ChatPanel({ messages, disabled, onSend, statusText, errorText }: ChatPanelProps) {
  const [input, setInput] = useState('')
  const [sending, setSending] = useState(false)
  const [sendError, setSendError] = useState<string | undefined>()

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    if (!input.trim() || sending || disabled)
      return

    setSending(true)
    setSendError(undefined)
    try {
      await onSend(input.trim())
      setInput('')
    } catch (error) {
      setSendError(error instanceof Error ? error.message : 'Request failed.')
    } finally {
      setSending(false)
    }
  }

  const displayError = errorText || sendError

  return (
    <div className="chat-panel">
      <div className="chat-panel__messages">
        {messages.length === 0 && <p className="muted">Describe the 3D object you want to create.</p>}
        {messages.map((message) => {
          const role = toMessageRole(message.role)
          return (
          <article
            key={message.id}
            className={`message message--${role.toLowerCase()}${isDiagnosticReport(message.content) ? ' message--diagnostic' : ''}`}
          >
            <header>{role}</header>
            {isDiagnosticReport(message.content) ? (
              <pre className="message__diagnostic">{message.content}</pre>
            ) : (
              <p>{message.content}</p>
            )}
          </article>
          )
        })}
        {statusText && <p className="status-line">{statusText}</p>}
        {displayError && (
          <pre className="chat-panel__error">{displayError}</pre>
        )}
      </div>
      <form className="chat-panel__composer" onSubmit={handleSubmit}>
        <textarea
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder="e.g. Create a parametric phone stand with 70° angle"
          rows={3}
          disabled={disabled || sending}
        />
        <button type="submit" disabled={disabled || sending || !input.trim()}>
          {sending ? 'Working…' : 'Send'}
        </button>
      </form>
    </div>
  )
}
