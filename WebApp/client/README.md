# AR4CT — Client

React-based web application for uploading, viewing, and annotating CT scans. Provides a 2D CT slice viewer, automated segmentation via TotalSegmentator, downloadable FBX models, and printable QR codes for the AR app.

## Tech Stack

- **React 19** with TypeScript
- **Vite 7** — Build tool and dev server
- **TailwindCSS 4** — Styling
- **shadcn/ui** — UI components (Radix UI + Tailwind)
- **Framer Motion** — Animations
- **Lucide React** — Icons
- **React Router 7** — Client-side routing

## Getting Started

### Prerequisites

- Node.js 18+

### Development

```bash
npm install
npm run dev
```

> When running via Docker Compose (recommended), the client runs at `http://localhost:5173` with hot-reload.

### Build

```bash
npm run build
```

---

## Application Overview

### Core Features

1. **Upload CT Scans** — Upload `.zip`, `.nii`, `.nii.gz`, `.mhd`, or `.nrrd` files (or a pre-made `.fbx`)
2. **TotalSegmentator Integration** — Automated organ segmentation on a GPU (117 organs + body surface)
3. **Processing Timeline** — Visual 4-step progress: Uploaded → Segmentation → 3D Model → Ready
4. **2D CT Viewer** — Browse axial, sagittal, and coronal slices with windowing controls
5. **Point Annotation** — Click on a CT slice to mark a target point for the AR app
6. **QR Code & Print** — Download a printable A4 PDF with QR code and tool tracking marker

### Pages

| Route | Page | Description |
|-------|------|-------------|
| `/` | Home | Landing page with feature overview |
| `/scans/new` | Scan (new) | Upload a new CT scan or FBX |
| `/scans/:scanId` | Scan (detail) | Processing status, CT viewer, annotations, downloads |
| `/app`, `/app/*` | App | Deep link handler for the Unity AR app |
| `/imprint` | Imprint | Legal information |
| `/privacy` | Privacy | Privacy policy |

### CT Viewer

The CT viewer renders slices server-side as JPEG images (no client-side DICOM parsing). It supports:
- **Axis selection** — Axial, sagittal, coronal planes
- **Slice navigation** — Scroll or slider
- **Windowing** — Adjustable window centre and width (presets for soft tissue, lung, bone)
- **Point selection** — Click to set an annotation point in CT world coordinates

---

## Project Structure

```
src/
├── App.tsx                        # Router + layout (NavBar, Footer)
├── main.tsx                       # React entry point
├── index.css                      # Global styles
├── lib/
│   └── utils.ts                   # cn() utility
├── components/
│   ├── ui/                        # shadcn/ui primitives (Button, Accordion, etc.)
│   ├── shared/
│   │   ├── Navbar.tsx
│   │   ├── Footer.tsx
│   │   ├── Routes.tsx             # Route definitions
│   │   ├── Page.tsx               # Page layout wrapper
│   │   ├── NotFound.tsx
│   │   ├── ScrollToTop.tsx
│   │   └── GreetingsAnimation.tsx
│   ├── pages/
│   │   ├── HomePage.tsx           # Landing page
│   │   ├── ScanPage.tsx           # Upload + processing + viewer + downloads
│   │   ├── AppPage.tsx            # AR app deep links
│   │   ├── ImprintPage.tsx
│   │   └── PrivacyPage.tsx
│   ├── scan/
│   │   ├── FileUpload.tsx         # Drag-and-drop upload component
│   │   ├── ProcessingTimeline.tsx # 4-step visual progress
│   │   ├── ScanViewer.tsx         # Download buttons (FBX, Print PDF)
│   │   ├── api.ts                 # All server API calls
│   │   └── useScanPolling.ts      # Polling hook for processing status
│   └── viewer/
│       └── CTViewer.tsx           # 2D CT slice viewer with windowing + annotation
```

---

## Notes

- The `@cornerstonejs/*`, `three`, and `dicom-parser` packages are listed in `package.json` but are **not currently used** in the implementation. The CT viewer uses server-rendered JPEG slices instead. These dependencies may be useful for future client-side DICOM rendering.
- Annotation points are stored in CT world coordinates (mm) and transformed to FBX model space by the server's bundle endpoint.
