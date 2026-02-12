# AR4CT - CT Scan Viewer Client

A React-based web application for uploading, viewing, and annotating CT scans. The app integrates with TotalSegmentator for automated segmentation and provides an interactive 2D CT viewer with point annotation capabilities.

## Tech Stack

- **React 19** with TypeScript
- **Vite** - Build tool and dev server
- **TailwindCSS** - Styling
- **shadcn/ui** - UI components (Radix UI + Tailwind)
- **Lucide React** - Icons

## Getting Started

### Prerequisites

- Node.js 18+
- npm or pnpm

### Installation

```bash
npm install
```

### Development

```bash
npm run dev
```

### Build

```bash
npm run build
```

### Preview Production Build

```bash
npm run preview
```

---

## Application Overview

### Core Features

1. **Upload CT Scans** - Upload `.zip` files containing DICOM/NIfTI CT scan data
2. **TotalSegmentator Integration** - Submit scans for automated organ/structure segmentation
3. **2D CT Viewer** - View CT slices in standard axial/sagittal/coronal planes
4. **Point Annotation** - Mark and save anatomical points of interest
5. **Data Persistence** - Server stores 3D scans and user annotations

### Workflow

```
┌─────────────────┐     ┌──────────────────────┐     ┌─────────────────┐
│  Upload .zip    │────▶│  TotalSegmentator    │────▶│  View & Annotate│
│  CT Scan File   │     │  Processing          │     │  in 2D Viewer   │
└─────────────────┘     └──────────────────────┘     └─────────────────┘
                                                              │
                                                              ▼
                                                     ┌─────────────────┐
                                                     │  Save Points    │
                                                     │  to Server      │
                                                     └─────────────────┘
```

---

## Recommended Libraries

### CT/DICOM Viewer Options

#### 1. **@lunit/insight-viewer** (Recommended for simplicity)
A React wrapper around Cornerstone.js with built-in annotation support.

```bash
npm install @lunit/insight-viewer
```

**Pros:**
- React-first API with hooks (`useImage`, `useViewport`, `useInteraction`)
- Built-in annotation overlay component
- Easy pan/zoom/adjust controls
- TypeScript support
- Good documentation: https://insight-viewer.lunit.io/

**Basic Usage:**
```tsx
import InsightViewer, { useImage } from '@lunit/insight-viewer'
import { AnnotationOverlay } from '@lunit/insight-viewer/annotation'

const { image } = useImage({ wadouri: 'path/to/dicom' })
return (
  <InsightViewer image={image}>
    <AnnotationOverlay annotations={annotations} onChange={setAnnotations} />
  </InsightViewer>
)
```

#### 2. **Cornerstone3D** (More powerful, more complex)
The underlying library for most medical imaging web apps.

```bash
npm install @cornerstonejs/core @cornerstonejs/tools @cornerstonejs/dicom-image-loader
```

**Pros:**
- Full control over rendering
- Supports volumes, stacks, 3D rendering
- Active development by OHIF
- GPU-accelerated

**Cons:**
- Steeper learning curve
- More setup required
- Documentation: https://www.cornerstonejs.org/

#### 3. **dwv (DICOM Web Viewer)**
Standalone DICOM viewer library.

```bash
npm install dwv
```

**Pros:**
- All-in-one solution
- Supports many DICOM formats
- Built-in tools

**Cons:**
- Not React-native
- Less flexible

### Recommendation

For a **simple app**, start with **@lunit/insight-viewer**. It provides:
- Easy React integration
- Built-in annotation support (exactly what you need for marking points)
- Minimal setup
- All the viewing features (pan, zoom, window/level adjustment)

---

## TODO / Implementation Plan

### Phase 1: Project Setup & Basic UI
- [ ] Set up routing (React Router or TanStack Router)
- [ ] Create main layout with navigation
- [ ] Create upload page with drag-and-drop zone
- [ ] Set up API client for server communication
- [ ] Add loading states and error handling

### Phase 2: File Upload
- [ ] Implement `.zip` file upload component
- [ ] Add file validation (check for DICOM/NIfTI content)
- [ ] Upload to server with progress indicator
- [ ] Display upload history/list of scans

### Phase 3: TotalSegmentator Integration
- [ ] Add "Process with TotalSegmentator" button
- [ ] Implement polling/websocket for processing status
- [ ] Show processing progress
- [ ] Handle processing errors
- [ ] Store segmentation results

### Phase 4: CT Viewer Implementation
- [ ] Install and configure `@lunit/insight-viewer`
- [ ] Create CT viewer component
- [ ] Implement slice navigation (scroll through slices)
- [ ] Add windowing controls (window/level presets for CT)
- [ ] Add pan/zoom functionality
- [ ] Display DICOM metadata (patient info, series info)

### Phase 5: Point Annotation
- [ ] Create annotation mode toggle
- [ ] Implement point placement on click
- [ ] Add point labels/names
- [ ] Display list of annotations
- [ ] Save annotations to server
- [ ] Load existing annotations for a scan
- [ ] Edit/delete annotations

### Phase 6: Polish & UX
- [ ] Add keyboard shortcuts (arrow keys for slices, etc.)
- [ ] Add measurement tools (optional)
- [ ] Responsive design for different screen sizes
- [ ] Add dark/light mode toggle
- [ ] Error boundaries and fallback UI

---

## API Endpoints (Expected)

```
POST   /api/scans/upload          - Upload .zip CT scan
GET    /api/scans                 - List all scans
GET    /api/scans/:id             - Get scan details
DELETE /api/scans/:id             - Delete a scan

POST   /api/scans/:id/segment     - Start TotalSegmentator processing
GET    /api/scans/:id/status      - Get processing status

GET    /api/scans/:id/images      - Get image data for viewer
GET    /api/scans/:id/images/:slice - Get specific slice

GET    /api/scans/:id/annotations - Get all annotations for a scan
POST   /api/scans/:id/annotations - Create new annotation
PUT    /api/annotations/:id       - Update annotation
DELETE /api/annotations/:id       - Delete annotation
```

---

## File Structure (Proposed)

```
src/
├── components/
│   ├── ui/                    # shadcn/ui components
│   ├── layout/
│   │   ├── Header.tsx
│   │   └── Sidebar.tsx
│   ├── upload/
│   │   ├── DropZone.tsx
│   │   └── UploadProgress.tsx
│   ├── viewer/
│   │   ├── CTViewer.tsx       # Main viewer component
│   │   ├── ViewerControls.tsx # Window/level, zoom controls
│   │   ├── SliceSlider.tsx    # Slice navigation
│   │   └── DicomInfo.tsx      # Metadata display
│   └── annotations/
│       ├── AnnotationOverlay.tsx
│       ├── AnnotationList.tsx
│       └── AnnotationForm.tsx
├── pages/
│   ├── HomePage.tsx
│   ├── UploadPage.tsx
│   ├── ScanListPage.tsx
│   └── ViewerPage.tsx
├── hooks/
│   ├── useScans.ts
│   ├── useAnnotations.ts
│   └── useViewer.ts
├── lib/
│   ├── api.ts                 # API client
│   └── utils.ts
├── types/
│   ├── scan.ts
│   └── annotation.ts
├── App.tsx
└── main.tsx
```

---

## Notes

- CT scans are typically stored as DICOM files or NIfTI format
- TotalSegmentator outputs NIfTI segmentation masks
- For serving DICOM files to the viewer, consider using DICOMweb standard
- Point annotations should store 3D coordinates (x, y, slice/z)
- Consider caching viewed slices for better performance

---

## Original Vite Template Info

### ESLint Configuration

If you are developing a production application, we recommend updating the configuration to enable type-aware lint rules:

```js
// eslint.config.js
import reactX from 'eslint-plugin-react-x'
import reactDom from 'eslint-plugin-react-dom'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      // Other configs...
      // Enable lint rules for React
      reactX.configs['recommended-typescript'],
      // Enable lint rules for React DOM
      reactDom.configs.recommended,
    ],
    languageOptions: {
      parserOptions: {
        project: ['./tsconfig.node.json', './tsconfig.app.json'],
        tsconfigRootDir: import.meta.dirname,
      },
      // other options...
    },
  },
])
```
