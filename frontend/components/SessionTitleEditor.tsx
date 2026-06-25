import { useState } from 'react'

interface SessionTitleEditorProps {
  title: string
  disabled?: boolean
  onSave: (title: string) => Promise<void>
}

export function SessionTitleEditor({ title, disabled, onSave }: SessionTitleEditorProps) {
  const [editing, setEditing] = useState(false)
  const [draft, setDraft] = useState(title)
  const [saving, setSaving] = useState(false)

  async function save() {
    const trimmed = draft.trim()
    if (!trimmed || trimmed === title || saving)
      return

    setSaving(true)
    try {
      await onSave(trimmed)
      setEditing(false)
    } finally {
      setSaving(false)
    }
  }

  if (!editing) {
    return (
      <div className="session-title">
        <h2>{title}</h2>
        <button
          type="button"
          className="session-title__edit"
          disabled={disabled}
          onClick={() => {
            setDraft(title)
            setEditing(true)
          }}
        >
          Rename
        </button>
      </div>
    )
  }

  return (
    <form
      className="session-title session-title--editing"
      onSubmit={(event) => {
        event.preventDefault()
        void save()
      }}
    >
      <input
        value={draft}
        onChange={(event) => setDraft(event.target.value)}
        aria-label="Design title"
        disabled={saving}
        autoFocus
      />
      <button type="submit" disabled={saving || !draft.trim()}>
        {saving ? 'Saving…' : 'Save'}
      </button>
      <button
        type="button"
        disabled={saving}
        onClick={() => {
          setDraft(title)
          setEditing(false)
        }}
      >
        Cancel
      </button>
    </form>
  )
}
