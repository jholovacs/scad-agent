import { useState } from 'react'

interface DiagnosticsPanelProps {
  title: string
  summary?: string
  details?: string
}

export function DiagnosticsPanel({ title, summary, details }: DiagnosticsPanelProps) {
  const [copied, setCopied] = useState(false)
  const reportText = details ?? summary

  if (!reportText)
    return null

  async function copyToClipboard(text: string) {
    await navigator.clipboard.writeText(text)
    setCopied(true)
    window.setTimeout(() => setCopied(false), 2000)
  }

  return (
    <section className="diagnostics-panel" role="alert">
      <header className="diagnostics-panel__header">
        <h3>{title}</h3>
        <button type="button" onClick={() => void copyToClipboard(reportText)}>
          {copied ? 'Copied' : 'Copy report'}
        </button>
      </header>
      {summary && summary !== details && (
        <p className="diagnostics-panel__summary">{summary}</p>
      )}
      <pre className="diagnostics-panel__body">{reportText}</pre>
    </section>
  )
}
