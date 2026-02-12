import { useEffect, useRef, useState } from 'react'
import {
  RenderingEngine,
  Enums,
  init as csInit,
} from '@cornerstonejs/core'
import {
  init as csToolsInit,
  ToolGroupManager,
  WindowLevelTool,
  PanTool,
  ZoomTool,
  StackScrollTool,
  addTool,
  Enums as csToolsEnums,
} from '@cornerstonejs/tools'
import { init as dicomImageLoaderInit } from '@cornerstonejs/dicom-image-loader'

const { ViewportType } = Enums
const { MouseBindings } = csToolsEnums

interface CTViewerProps {
  imageIds?: string[]
}

// Sample DICOM images from a public server for testing
const SAMPLE_IMAGE_IDS = [
  'wadouri:https://raw.githubusercontent.com/nickreynolds/cornerstone-tools/master/testImages/CT1_J2KR.dcm',
]

export default function CTViewer({ imageIds = SAMPLE_IMAGE_IDS }: CTViewerProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const [isInitialized, setIsInitialized] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const renderingEngineRef = useRef<RenderingEngine | null>(null)

  useEffect(() => {
    let mounted = true

    const initializeCornerstone = async () => {
      try {
        // Initialize Cornerstone and tools
        await csInit()
        await csToolsInit()
        dicomImageLoaderInit({ maxWebWorkers: 1 })

        if (!mounted) return

        // Add tools
        addTool(WindowLevelTool)
        addTool(PanTool)
        addTool(ZoomTool)
        addTool(StackScrollTool)

        setIsInitialized(true)
      } catch (err) {
        console.error('Failed to initialize Cornerstone:', err)
        setError('Failed to initialize viewer')
      }
    }

    initializeCornerstone()

    return () => {
      mounted = false
      if (renderingEngineRef.current) {
        renderingEngineRef.current.destroy()
      }
    }
  }, [])

  useEffect(() => {
    if (!isInitialized || !containerRef.current || imageIds.length === 0) return

    const setupViewer = async () => {
      try {
        const renderingEngineId = 'ctRenderingEngine'
        const viewportId = 'ctViewport'
        const toolGroupId = 'ctToolGroup'

        // Create rendering engine
        const renderingEngine = new RenderingEngine(renderingEngineId)
        renderingEngineRef.current = renderingEngine

        // Create viewport
        const viewportInput = {
          viewportId,
          type: ViewportType.STACK,
          element: containerRef.current!,
          defaultOptions: {
            background: [0, 0, 0] as [number, number, number],
          },
        }

        renderingEngine.enableElement(viewportInput)

        // Get the viewport
        const viewport = renderingEngine.getViewport(viewportId)

        // Set images on the viewport
        await (viewport as any).setStack(imageIds)

        // Create tool group and add tools
        const toolGroup = ToolGroupManager.createToolGroup(toolGroupId)
        if (toolGroup) {
          toolGroup.addViewport(viewportId, renderingEngineId)

          // Add tools to the group
          toolGroup.addTool(WindowLevelTool.toolName)
          toolGroup.addTool(PanTool.toolName)
          toolGroup.addTool(ZoomTool.toolName)
          toolGroup.addTool(StackScrollTool.toolName)

          // Set tool bindings
          toolGroup.setToolActive(WindowLevelTool.toolName, {
            bindings: [{ mouseButton: MouseBindings.Primary }],
          })
          toolGroup.setToolActive(PanTool.toolName, {
            bindings: [{ mouseButton: MouseBindings.Auxiliary }],
          })
          toolGroup.setToolActive(ZoomTool.toolName, {
            bindings: [{ mouseButton: MouseBindings.Secondary }],
          })
          toolGroup.setToolActive(StackScrollTool.toolName, {
            bindings: [{ mouseButton: MouseBindings.Wheel }],
          })
        }

        // Render
        renderingEngine.render()
      } catch (err) {
        console.error('Failed to setup viewer:', err)
        setError('Failed to load images')
      }
    }

    setupViewer()
  }, [isInitialized, imageIds])

  if (error) {
    return (
      <div className="w-full h-[600px] bg-black flex items-center justify-center text-red-500">
        {error}
      </div>
    )
  }

  if (!isInitialized) {
    return (
      <div className="w-full h-[600px] bg-black flex items-center justify-center text-white">
        Initializing viewer...
      </div>
    )
  }

  return (
    <div className="relative">
      <div
        ref={containerRef}
        className="w-full h-[600px] bg-black"
        onContextMenu={(e) => e.preventDefault()}
      />
      <div className="absolute bottom-4 left-4 text-white text-sm bg-black/50 p-2 rounded">
        <p>Left click + drag: Window/Level</p>
        <p>Right click + drag: Zoom</p>
        <p>Middle click + drag: Pan</p>
        <p>Scroll: Navigate slices</p>
      </div>
    </div>
  )
}
