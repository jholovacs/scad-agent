import type {
  Iteration,
  IterationsPage,
  Message,
  MessagesPage,
  SessionDetail,
  SessionSummary,
} from '../types/api'
import { toIterationStatus, toLinearUnits, toMessageIntent, toMessageRole, toSessionStatus } from '../types/api'

const baseUrl = ''

function normalizeMessage(message: Message): Message {
  return {
    ...message,
    role: toMessageRole(message.role),
    iterationId: message.iterationId,
    intent: toMessageIntent(message.intent),
  }
}

function normalizeIteration(iteration: Iteration): Iteration {
  return {
    ...iteration,
    status: toIterationStatus(iteration.status),
    summary: iteration.summary ?? iteration.assistantSummary,
    scadUnits: toLinearUnits(iteration.scadUnits),
    stlExportUnits: toLinearUnits(iteration.stlExportUnits),
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
  getMessages: async (id: string, options?: { limit?: number; before?: string; iterationId?: string }) => {
    const params = new URLSearchParams()
    if (options?.limit)
      params.set('limit', String(options.limit))
    if (options?.before)
      params.set('before', options.before)
    if (options?.iterationId)
      params.set('iterationId', options.iterationId)

    const query = params.toString()
    const page = await request<MessagesPage>(
      `/api/sessions/${id}/messages${query ? `?${query}` : ''}`,
    )
    return {
      ...page,
      messages: page.messages.map(normalizeMessage),
    }
  },
  getIterations: async (id: string, options?: { limit?: number; beforeVersion?: number }) => {
    const params = new URLSearchParams()
    if (options?.limit)
      params.set('limit', String(options.limit))
    if (options?.beforeVersion !== undefined)
      params.set('beforeVersion', String(options.beforeVersion))

    const query = params.toString()
    const page = await request<IterationsPage>(
      `/api/sessions/${id}/iterations${query ? `?${query}` : ''}`,
    )
    return {
      ...page,
      iterations: page.iterations.map(normalizeIteration),
    }
  },
  postMessage: async (id: string, content: string) =>
    normalizeSession(await request<SessionDetail>(`/api/sessions/${id}/messages`, {
      method: 'POST',
      body: JSON.stringify({ content }),
    })),
  updateSessionTitle: async (id: string, title: string) =>
    normalizeSession(await request<SessionDetail>(`/api/sessions/${id}`, {
      method: 'PATCH',
      body: JSON.stringify({ title }),
    })),
  deleteSession: async (id: string) => {
    await request<void>(`/api/sessions/${id}`, { method: 'DELETE' })
  },
  stlUrl: (iterationId: string) => `/api/iterations/${iterationId}/artifacts/stl`,
  scadUrl: (iterationId: string) => `/api/iterations/${iterationId}/artifacts/scad`,
  previewUrl: (iterationId: string) => `/api/iterations/${iterationId}/artifacts/preview`,
}
