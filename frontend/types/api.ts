export type MessageIntent = 'Design' | 'Ask'
export type SessionStatus = 'Draft' | 'Iterating' | 'Ready' | 'Failed'
export type MessageRole = 'User' | 'Assistant' | 'System'
export type IterationStatus = 'Pending' | 'Rendering' | 'Succeeded' | 'Failed'
export type LinearUnits = 'Millimeters' | 'Inches'

const SESSION_STATUSES: SessionStatus[] = ['Draft', 'Iterating', 'Ready', 'Failed']
const MESSAGE_INTENTS: MessageIntent[] = ['Design', 'Ask']
const MESSAGE_ROLES: MessageRole[] = ['User', 'Assistant', 'System']
const ITERATION_STATUSES: IterationStatus[] = ['Pending', 'Rendering', 'Succeeded', 'Failed']
const LINEAR_UNITS: LinearUnits[] = ['Millimeters', 'Inches']

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

export function toMessageIntent(value: unknown): MessageIntent | undefined {
  if (value === null || value === undefined)
    return undefined
  return coerceEnum(value, MESSAGE_INTENTS, 'Design')
}

export function toLinearUnits(value: unknown): LinearUnits {
  return coerceEnum(value, LINEAR_UNITS, 'Millimeters')
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
  iterationId?: string
  intent?: MessageIntent
}

export interface Iteration {
  id: string
  version: number
  status: IterationStatus
  scadContent: string
  assistantSummary?: string
  summary?: string
  renderError?: string
  diagnosticLog?: string
  hasStl: boolean
  hasPreview: boolean
  createdAt: string
  scadUnits: LinearUnits
  stlExportUnits: LinearUnits
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

export interface MessagesPage {
  messages: Message[]
  hasMore: boolean
  oldestCreatedAt?: string
}

export interface IterationsPage {
  iterations: Iteration[]
  hasMore: boolean
  oldestVersion?: number
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
