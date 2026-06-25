import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { SessionList } from './components/SessionList'

describe('SessionList', () => {
  it('renders empty state and create button', () => {
    render(
      <MemoryRouter>
        <SessionList sessions={[]} onCreate={() => undefined} onDelete={() => undefined} />
      </MemoryRouter>,
    )
    expect(screen.getByText('SCAD Agent')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'New design session' })).toBeInTheDocument()
  })
})
