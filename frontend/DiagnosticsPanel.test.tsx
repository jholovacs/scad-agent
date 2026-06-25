import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { DiagnosticsPanel } from './components/DiagnosticsPanel'

describe('DiagnosticsPanel', () => {
  it('renders copyable diagnostic text', () => {
    render(
      <DiagnosticsPanel
        title="Iteration diagnostics"
        summary="Ollama failed"
        details={'=== SCAD Agent Diagnostic Report ===\nline 2'}
      />,
    )
    expect(screen.getByText('Iteration diagnostics')).toBeInTheDocument()
    expect(screen.getByText(/Diagnostic Report/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Copy report' })).toBeInTheDocument()
  })
})
