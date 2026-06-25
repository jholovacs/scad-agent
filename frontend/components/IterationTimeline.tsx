import type { Iteration } from '../types/api'
import { toIterationStatus } from '../types/api'

interface IterationTimelineProps {
  iterations: Iteration[]
  selectedId?: string
  onSelect: (iteration: Iteration) => void
}

export function IterationTimeline({ iterations, selectedId, onSelect }: IterationTimelineProps) {
  if (iterations.length === 0)
    return null

  return (
    <div className="iteration-timeline">
      <h3>Iterations</h3>
      <ul>
        {iterations.map((iteration) => (
          <li key={iteration.id}>
            <button
              type="button"
              className={iteration.id === selectedId ? 'active' : ''}
              onClick={() => onSelect(iteration)}
            >
              v{iteration.version} · {toIterationStatus(iteration.status)}
            </button>
          </li>
        ))}
      </ul>
    </div>
  )
}
