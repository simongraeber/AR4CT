# AR4CT — Web Application

The web app provides upload, segmentation, annotation, and QR code generation for CT scans. It runs as two Docker containers: a **React client** and a **FastAPI server** (with Blender for 3D model conversion).

---

## Quick Start (Development)

```bash
docker compose up --build
```

| Service | URL | Description |
|---------|-----|-------------|
| Client | `http://localhost:5173` | React 19 + Vite (hot-reload) |
| Server | `http://localhost:8000` | FastAPI + Blender (hot-reload) |

Source files are mounted into both containers, so changes are reflected immediately.

---

## Architecture

```
Browser ──► React Client (:5173) ──► FastAPI Server (:8000)
                                          │
                                          ├── File storage (Docker volume)
                                          ├── Blender headless (STL → FBX)
                                          └── RunPod API (CT → STL segmentation)
```

- **No database** — metadata is stored as JSON files alongside the scan data.
- **CT slices** are rendered server-side to JPEG and sent to the client (no client-side DICOM parsing).
- **Segmentation** is offloaded to a RunPod GPU worker. See [`totalsegmentator/`](totalsegmentator/) for details.

---

## Environment Variables

Set in `docker-compose.yml` or a `.env` file:

| Variable | Description | Default |
|----------|-------------|---------|
| `RUNPOD_API_KEY` | RunPod API key | _(empty — segmentation disabled)_ |
| `RUNPOD_ENDPOINT_ID` | RunPod endpoint ID | _(empty)_ |
| `API_BASE_URL` | Public server URL (for RunPod callbacks) | `https://api.ar4ct.com` |
| `PUBLIC_BASE_URL` | Public client URL (for QR codes) | `https://ar4ct.com` |

---

## API Endpoints

All endpoints are under `/scans`.

### Scans CRUD

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/scans/upload` | Upload CT scan (.zip/.nii/.mhd/.nrrd) or FBX |
| `GET` | `/scans` | List all scans |
| `GET` | `/scans/{id}` | Get scan metadata |
| `DELETE` | `/scans/{id}` | Delete scan and all files |

### Files

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/scans/{id}/fbx` | Download FBX model |
| `POST` | `/scans/{id}/fbx` | Upload / replace FBX |
| `GET` | `/scans/{id}/ct` | Download raw CT file |
| `POST` | `/scans/{id}/ct` | Upload / replace CT |
| `GET` | `/scans/{id}/stl` | List STL files |
| `GET/POST` | `/scans/{id}/stl/{organ}` | Download / upload organ STL |

### Annotation

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/scans/{id}/point` | Set annotation point (x, y, z, label) |
| `GET` | `/scans/{id}/point` | Get annotation point |

### QR & Print

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/scans/{id}/image.png` | QR code PNG (coloured corners) |
| `GET` | `/scans/{id}/print.pdf` | Printable A4 PDF (QR + tool marker) |

### Processing

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/scans/{id}/process` | Trigger RunPod segmentation |
| `GET` | `/scans/{id}/process/status` | Poll processing status |
| `POST` | `/scans/{id}/postprocess` | Trigger STL → FBX conversion |
| `POST` | `/scans/{id}/reset` | Reset scan to "uploaded" state |

### CT Viewer

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/scans/{id}/ct/info` | Volume metadata (dimensions, spacing, value range) |
| `GET` | `/scans/{id}/ct/slice/{axis}/{index}` | JPEG slice (axial/sagittal/coronal, `?wc=40&ww=400`) |

### Bundle (Unity)

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/scans/{id}/bundle` | FBX URL + transformed annotation point + status |

---

## Processing Pipeline

```
Upload CT  →  "uploaded"
  → auto-submit to RunPod GPU
  → "processing"
  → TotalSegmentator: NIfTI masks → marching cubes → STL per organ (117 + body)
  → "segmented"
  → Blender headless: STL → coloured FBX (organ colours + transparency)
  → "completed"
```

---

## Project Structure

```
WebApp/
├── docker-compose.yml          # Dev compose
├── client/                     # React 19 + Vite + TailwindCSS
│   └── src/
│       ├── components/pages/   # HomePage, ScanPage
│       ├── components/scan/    # FileUpload, ProcessingTimeline, CT viewer API
│       └── components/viewer/  # CTViewer (2D slice viewer)
├── server/
│   ├── main.py                 # Uvicorn entry point
│   ├── app/
│   │   ├── config.py           # Settings, organ list
│   │   ├── models.py           # Pydantic models
│   │   ├── storage.py          # JSON file storage
│   │   ├── routes/             # All API routes
│   │   ├── services/           # RunPod integration
│   │   └── scripts/            # stl_to_fbx.py (Blender)
│   └── assets/
│       ├── organ_colors.json   # Colour map for 117 organs
│       └── tool_image.png      # Tool tracking marker
└── totalsegmentator/           # RunPod GPU worker
```

---

## Sub-project READMEs

- [Client README](client/README.md) — React app details, tech stack, component overview
- [TotalSegmentator README](totalsegmentator/README.md) — Docker image build, RunPod configuration