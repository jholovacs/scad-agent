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

    expect(onSend).toHaveBeenCalledWith('Make a cube')
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
})
