import { Link } from 'react-router-dom'
import type { SessionSummary } from '../types/api'

interface SessionListProps {
  sessions: SessionSummary[]
  onCreate: () => void
  creating?: boolean
}

export function SessionList({ sessions, onCreate, creating }: SessionListProps) {
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
            <Link to={`/sessions/${session.id}`}>
              <strong>{session.title}</strong>
              <span>{session.status}</span>
              <time>{new Date(session.updatedAt).toLocaleString()}</time>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  )
}
