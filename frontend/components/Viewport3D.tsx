import { Bounds, Center, Environment, OrbitControls } from '@react-three/drei'
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

function ViewportScene({ url }: { url: string }) {
  return (
    <>
      <ambientLight intensity={0.55} />
      <directionalLight position={[6, 10, 6]} intensity={1.1} castShadow />
      <Environment preset="city" />
      <Bounds fit clip observe margin={1.3}>
        <Center>
          <StlModel url={url} />
        </Center>
      </Bounds>
      <OrbitControls makeDefault enableDamping dampingFactor={0.08} />
    </>
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
      <Canvas
        shadows
        dpr={[1, 2]}
        camera={{ fov: 45, near: 0.01, far: 100_000, position: [120, 90, 120] }}
        style={{ width: '100%', height: '100%', background: viewportBackground }}
      >
        <Suspense fallback={null}>
          <ViewportScene url={stlUrl} />
        </Suspense>
      </Canvas>
    </div>
  )
}
