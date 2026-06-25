import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { ChatPanel } from './components/ChatPanel'
import type { Message } from './types/api'

describe('ChatPanel', () => {
  it('submits user input', async () => {
    const onSend = vi.fn().mockResolvedValue(undefined)
    render(<ChatPanel messages={[]} onSend={onSend} />)

    fireEvent.change(screen.getByRole('textbox'), { target: { value: 'Make a cube' } })
    fireEvent.click(screen.getByRole('button', { name: 'Send' }))

    expect(onSend).toHaveBeenCalledWith('Make a cube', 'Design')
  })

  it('submits on Enter without Ctrl', async () => {
    const onSend = vi.fn().mockResolvedValue(undefined)
    render(<ChatPanel messages={[]} onSend={onSend} />)

    const textbox = screen.getByRole('textbox')
    fireEvent.change(textbox, { target: { value: 'Make a cube' } })
    fireEvent.keyDown(textbox, { key: 'Enter' })

    expect(onSend).toHaveBeenCalledWith('Make a cube', 'Design')
  })

  it('does not submit on Ctrl+Enter', () => {
    const onSend = vi.fn().mockResolvedValue(undefined)
    render(<ChatPanel messages={[]} onSend={onSend} />)

    const textbox = screen.getByRole('textbox')
    fireEvent.change(textbox, { target: { value: 'Line one' } })
    fireEvent.keyDown(textbox, { key: 'Enter', ctrlKey: true })

    expect(onSend).not.toHaveBeenCalled()
  })

  it('submits ask mode when Ask is selected', async () => {
    const onSend = vi.fn().mockResolvedValue(undefined)
    render(<ChatPanel messages={[]} onSend={onSend} />)

    fireEvent.click(screen.getByRole('button', { name: 'Ask' }))
    fireEvent.change(screen.getByRole('textbox'), { target: { value: 'What does hull() do?' } })
    fireEvent.click(screen.getByRole('button', { name: 'Send' }))

    expect(onSend).toHaveBeenCalledWith('What does hull() do?', 'Ask')
  })

  it('renders messages when the API returns numeric role enums', () => {
    const messages = [{
      id: '1',
      role: 1,
      content: 'Render failed after 4 attempts',
      createdAt: '2026-06-25T19:47:20.0079152+00:00',
    }] as unknown as Message[]

    render(<ChatPanel messages={messages} onSend={vi.fn()} />)

    expect(screen.getByText('Render failed after 4 attempts')).toBeInTheDocument()
  })

  it('shows activity status while working', () => {
    render(
      <ChatPanel
        messages={[]}
        onSend={vi.fn()}
        isWorking
        statusText="Calling Ollama…"
        pendingMessage="Make a cube"
      />,
    )

    expect(screen.getByRole('status')).toHaveTextContent('Calling Ollama…')
    expect(screen.getByText('Make a cube')).toBeInTheDocument()
  })

  it('renders newest messages first', () => {
    const messages: Message[] = [
      { id: '2', role: 'Assistant', content: 'Newer reply', createdAt: '2026-06-25T20:00:00Z' },
      { id: '1', role: 'User', content: 'Older prompt', createdAt: '2026-06-25T19:00:00Z' },
    ]

    render(<ChatPanel messages={messages} onSend={vi.fn()} />)

    const rendered = screen.getAllByRole('article').map((node) => node.textContent)
    expect(rendered[0]).toContain('Newer reply')
    expect(rendered[1]).toContain('Older prompt')
  })
})
