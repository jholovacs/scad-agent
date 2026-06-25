import { OrbitControls, Stage } from '@react-three/drei'
import { Canvas } from '@react-three/fiber'
import { Suspense, useEffect, useState } from 'react'
import * as THREE from 'three'
import { STLLoader } from 'three/examples/jsm/loaders/STLLoader.js'

interface Viewport3DProps {
  stlUrl?: string
}

function useViewportBackground() {
  const [background, setBackground] = useState('#e8ecf3')

  useEffect(() => {
    const sync = () => {
      const value = getComputedStyle(document.documentElement)
        .getPropertyValue('--color-viewport-bg')
        .trim()
      if (value)
        setBackground(value)
    }

    sync()
    const media = window.matchMedia('(prefers-color-scheme: dark)')
    media.addEventListener('change', sync)
    return () => media.removeEventListener('change', sync)
  }, [])

  return background
}

function StlModel({ url }: { url: string }) {
  const [geometry, setGeometry] = useState<THREE.BufferGeometry | null>(null)

  useEffect(() => {
    const loader = new STLLoader()
    let cancelled = false

    loader.load(
      url,
      (loaded) => {
        if (!cancelled) {
          loaded.center()
          loaded.computeVertexNormals()
          setGeometry(loaded)
        }
      },
      undefined,
      () => {
        if (!cancelled)
          setGeometry(null)
      },
    )

    return () => {
      cancelled = true
    }
  }, [url])

  if (!geometry)
    return null

  return (
    <mesh geometry={geometry} castShadow receiveShadow>
      <meshStandardMaterial color="#4f8cff" metalness={0.2} roughness={0.45} />
    </mesh>
  )
}

export function Viewport3D({ stlUrl }: Viewport3DProps) {
  const viewportBackground = useViewportBackground()

  if (!stlUrl) {
    return (
      <div className="viewport viewport--empty">
        <p>No rendered model yet. Send a design prompt to begin.</p>
      </div>
    )
  }

  return (
    <div className="viewport">
      <Canvas shadows camera={{ position: [80, 80, 80], fov: 45 }} style={{ background: viewportBackground }}>
        <Suspense fallback={null}>
          <Stage intensity={0.5} environment="city" adjustCamera={1.2}>
            <StlModel url={stlUrl} />
          </Stage>
          <OrbitControls makeDefault />
        </Suspense>
      </Canvas>
    </div>
  )
}
