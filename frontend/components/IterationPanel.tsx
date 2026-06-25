import { api } from '../api/client'
import type { Iteration } from '../types/api'
import { toIterationStatus, toLinearUnits } from '../types/api'

interface IterationPanelProps {
  iteration: Iteration
  exportTitle?: string
}

function exportUnitsHint(iteration: Iteration): string {
  const scadUnits = toLinearUnits(iteration.scadUnits)
  const stlUnits = toLinearUnits(iteration.stlExportUnits)

  if (scadUnits === 'Inches')
    return `OpenSCAD source uses inches. STL is exported in ${stlUnits.toLowerCase()} (×25.4) for slicers.`

  return `Dimensions and STL export use ${stlUnits.toLowerCase()} (standard for 3D printing).`
}

export function IterationPanel({ iteration, exportTitle }: IterationPanelProps) {
  const status = toIterationStatus(iteration.status)
  const canExport = status === 'Succeeded'

  return (
    <section className="iteration-panel">
      <header className="iteration-panel__header">
        <h3>Version {iteration.version}</h3>
        <span className={`badge badge--${status.toLowerCase()}`}>{status}</span>
      </header>

      {iteration.summary && (
        <p className="iteration-panel__summary">{iteration.summary}</p>
      )}

      {canExport && (
        <div className="iteration-panel__exports">
          <a href={api.scadUrl(iteration.id)} download>
            Download .scad
          </a>
          <a href={api.stlUrl(iteration.id)} download>
            Download .STL (mm)
          </a>
          <span className="iteration-panel__export-hint">
            {exportUnitsHint(iteration)}
          </span>
          {exportTitle && (
            <span className="iteration-panel__export-hint">
              Files use the design title: {exportTitle}
            </span>
          )}
        </div>
      )}
    </section>
  )
}
