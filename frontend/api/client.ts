import type {
  Iteration,
  Message,
  SessionDetail,
  SessionSummary,
} from '../types/api'
import { toIterationStatus, toMessageRole, toSessionStatus } from '../types/api'

const baseUrl = ''

function normalizeMessage(message: Message): Message {
  return {
    ...message,
    role: toMessageRole(message.role),
  }
}

function normalizeIteration(iteration: Iteration): Iteration {
  return {
    ...iteration,
    status: toIterationStatus(iteration.status),
  }
}

function normalizeSession(session: SessionDetail): SessionDetail {
  return {
    ...session,
    status: toSessionStatus(session.status),
    currentIteration: session.currentIteration
      ? normalizeIteration(session.currentIteration)
      : undefined,
    messages: session.messages.map(normalizeMessage),
  }
}

function normalizeSessionSummary(session: SessionSummary): SessionSummary {
  return {
    ...session,
    status: toSessionStatus(session.status),
  }
}

async function readError(response: Response): Promise<string> {
  const text = await response.text()
  if (!text)
    return `${response.status} ${response.statusText}`

  try {
    const json = JSON.parse(text) as Record<string, unknown>
    const detail = json.detail ?? json.title ?? json.message
    if (typeof detail === 'string')
      return detail
    return JSON.stringify(json, null, 2)
  } catch {
    return text
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
    headers: { 'Content-Type': 'application/json', ...init?.headers },
    ...init,
  })

  if (!response.ok)
    throw new Error(await readError(response))

  if (response.status === 204)
    return undefined as T

  return response.json() as Promise<T>
}

export const api = {
  listSessions: async () => {
    const sessions = await request<SessionSummary[]>('/api/sessions')
    return sessions.map(normalizeSessionSummary)
  },
  createSession: async (title?: string) =>
    normalizeSession(await request<SessionDetail>('/api/sessions', {
      method: 'POST',
      body: JSON.stringify({ title }),
    })),
  getSession: async (id: string) =>
    normalizeSession(await request<SessionDetail>(`/api/sessions/${id}`)),
  getIterations: async (id: string) => {
    const iterations = await request<Iteration[]>(`/api/sessions/${id}/iterations`)
    return iterations.map(normalizeIteration)
  },
  postMessage: async (id: string, content: string) =>
    normalizeSession(await request<SessionDetail>(`/api/sessions/${id}/messages`, {
      method: 'POST',
      body: JSON.stringify({ content }),
    })),
  stlUrl: (iterationId: string) => `/api/iterations/${iterationId}/artifacts/stl`,
  previewUrl: (iterationId: string) => `/api/iterations/${iterationId}/artifacts/preview`,
}
