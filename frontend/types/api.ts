export type SessionStatus = 'Draft' | 'Iterating' | 'Ready' | 'Failed'
export type MessageRole = 'User' | 'Assistant' | 'System'
export type IterationStatus = 'Pending' | 'Rendering' | 'Succeeded' | 'Failed'

const SESSION_STATUSES: SessionStatus[] = ['Draft', 'Iterating', 'Ready', 'Failed']
const MESSAGE_ROLES: MessageRole[] = ['User', 'Assistant', 'System']
const ITERATION_STATUSES: IterationStatus[] = ['Pending', 'Rendering', 'Succeeded', 'Failed']

function coerceEnum<T extends string>(value: unknown, options: readonly T[], fallback: T): T {
  if (typeof value === 'number' && value >= 0 && value < options.length)
    return options[value]

  if (typeof value === 'string' && options.includes(value as T))
    return value as T

  return fallback
}

export function toSessionStatus(value: unknown): SessionStatus {
  return coerceEnum(value, SESSION_STATUSES, 'Draft')
}

export function toMessageRole(value: unknown): MessageRole {
  return coerceEnum(value, MESSAGE_ROLES, 'User')
}

export function toIterationStatus(value: unknown): IterationStatus {
  return coerceEnum(value, ITERATION_STATUSES, 'Pending')
}

export interface SessionSummary {
  id: string
  title: string
  status: SessionStatus
  updatedAt: string
}

export interface Message {
  id: string
  role: MessageRole
  content: string
  createdAt: string
}

export interface Iteration {
  id: string
  version: number
  status: IterationStatus
  scadContent: string
  assistantSummary?: string
  renderError?: string
  diagnosticLog?: string
  hasStl: boolean
  hasPreview: boolean
  createdAt: string
}

export interface SessionDetail {
  id: string
  title: string
  status: SessionStatus
  currentIterationId?: string
  createdAt: string
  updatedAt: string
  currentIteration?: Iteration
  messages: Message[]
}

export interface AgentProgress {
  sessionId: string
  iterationId?: string
  phase: string
  message: string
  details?: string
}

export function isDiagnosticReport(content: string): boolean {
  return content.includes('=== SCAD Agent Diagnostic Report ===')
}
