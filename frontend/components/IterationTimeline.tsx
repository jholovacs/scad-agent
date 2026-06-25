import { useEffect, useRef } from 'react'
import type { Iteration } from '../types/api'
import { toIterationStatus } from '../types/api'

interface IterationTimelineProps {
  iterations: Iteration[]
  selectedId?: string
  isLoading?: boolean
  hasOlderIterations?: boolean
  loadingOlder?: boolean
  onLoadOlder?: () => void
  onSelect: (iteration: Iteration) => void
}

export function IterationTimeline({
  iterations,
  selectedId,
  isLoading,
  hasOlderIterations,
  loadingOlder,
  onLoadOlder,
  onSelect,
}: IterationTimelineProps) {
  const listRef = useRef<HTMLDivElement>(null)
  const loadMoreRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const sentinel = loadMoreRef.current
    const container = listRef.current
    if (!sentinel || !container || !hasOlderIterations || !onLoadOlder)
      return

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((entry) => entry.isIntersecting) && !loadingOlder)
          onLoadOlder()
      },
      { root: container, rootMargin: '120px' },
    )

    observer.observe(sentinel)
    return () => observer.disconnect()
  }, [hasOlderIterations, loadingOlder, onLoadOlder, iterations.length])

  if (!isLoading && iterations.length === 0)
    return null

  return (
    <div className="iteration-timeline">
      <h3 className="iteration-timeline__heading">Versions</h3>

      <div className="iteration-timeline__list" ref={listRef}>
        {isLoading && iterations.length === 0 && (
          <p className="muted iteration-timeline__empty">Loading versions…</p>
        )}

        <ul>
          {iterations.map((iteration) => {
            const status = toIterationStatus(iteration.status)
            return (
              <li key={iteration.id}>
                <button
                  type="button"
                  className={iteration.id === selectedId ? 'active' : ''}
                  onClick={() => onSelect(iteration)}
                >
                  <span className="iteration-timeline__title">
                    v{iteration.version} · {status}
                  </span>
                  {iteration.summary && (
                    <span className="iteration-timeline__summary">{iteration.summary}</span>
                  )}
                </button>
              </li>
            )
          })}
        </ul>

        {hasOlderIterations && (
          <div className="iteration-timeline__load-more" ref={loadMoreRef}>
            {loadingOlder ? 'Loading older versions…' : 'Scroll for older versions'}
          </div>
        )}
      </div>
    </div>
  )
}
