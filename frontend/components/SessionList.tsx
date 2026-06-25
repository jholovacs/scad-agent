import { Link } from 'react-router-dom'
import type { SessionSummary } from '../types/api'

interface SessionListProps {
  sessions: SessionSummary[]
  onCreate: () => void
  onDelete: (sessionId: string) => void
  creating?: boolean
  deletingId?: string | null
}

export function SessionList({ sessions, onCreate, onDelete, creating, deletingId }: SessionListProps) {
  return (
    <section className="session-list">
      <header>
        <h1>SCAD Agent</h1>
        <button onClick={onCreate} disabled={creating}>
          {creating ? 'Creating…' : 'New design session'}
        </button>
      </header>
      <ul>
        {sessions.map((session) => (
          <li key={session.id}>
            <Link to={`/sessions/${session.id}`} className="session-list__link">
              <strong>{session.title}</strong>
              <span>{session.status}</span>
              <time>{new Date(session.updatedAt).toLocaleString()}</time>
            </Link>
            <button
              type="button"
              className="session-list__delete"
              aria-label={`Delete ${session.title}`}
              disabled={deletingId === session.id}
              onClick={() => {
                if (window.confirm(`Delete "${session.title}"? This cannot be undone.`))
                  onDelete(session.id)
              }}
            >
              {deletingId === session.id ? 'Deleting…' : 'Delete'}
            </button>
          </li>
        ))}
      </ul>
    </section>
  )
}
