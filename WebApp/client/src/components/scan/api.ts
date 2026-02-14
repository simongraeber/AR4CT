export type ScanStatus =
  | "uploaded"
  | "processing"
  | "segmented"
  | "post_processing"
  | "completed"
  | "error";

export interface ScanMetadata {
  scan_id: string;
  created_at: string;
  original_filename?: string;
  file_size?: number;
  status: ScanStatus;
  point?: { x: number; y: number; z: number; label?: string } | null;
  has_fbx: boolean;
  runpod_job_id?: string;
  processing_started_at?: string;
  processing_completed_at?: string;
  processing_error?: string;
  organs_processed?: string[];
  fbx_size?: number;
}

export interface ProcessingStatus {
  scan_id: string;
  status: ScanStatus;
  runpod_job_id?: string;
  organs_processed: string[];
  has_fbx: boolean;
  has_body: boolean;
  fbx_size?: number;
  error?: string;
}

export interface UploadResult {
  scan_id: string;
  filename: string;
  size: number;
  status: ScanStatus;
  has_fbx: boolean;
  message: string;
}

const API_BASE = "/api";

export async function fetchScan(scanId: string): Promise<ScanMetadata> {
  const res = await fetch(`${API_BASE}/scans/${scanId}`);
  if (!res.ok) {
    const err = new Error("Scan not found") as Error & { status: number };
    err.status = res.status;
    throw err;
  }
  return res.json();
}

export async function fetchProcessingStatus(
  scanId: string,
): Promise<ProcessingStatus> {
  const res = await fetch(`${API_BASE}/scans/${scanId}/process/status`);
  if (!res.ok) throw new Error(`Failed to fetch status (${res.status})`);
  return res.json();
}

export function uploadScan(
  file: File,
  onProgress?: (percent: number) => void,
): Promise<UploadResult> {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    const formData = new FormData();
    formData.append("file", file);

    xhr.upload.addEventListener("progress", (e) => {
      if (e.lengthComputable && onProgress) {
        onProgress(Math.round((e.loaded / e.total) * 100));
      }
    });

    xhr.addEventListener("load", () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        resolve(JSON.parse(xhr.responseText));
      } else {
        try {
          const err = JSON.parse(xhr.responseText);
          reject(new Error(err.detail || "Upload failed"));
        } catch {
          reject(new Error(`Upload failed (${xhr.status})`));
        }
      }
    });

    xhr.addEventListener("error", () => reject(new Error("Network error")));
    xhr.addEventListener("abort", () => reject(new Error("Upload cancelled")));

    xhr.open("POST", `${API_BASE}/scans/upload`);
    xhr.send(formData);
  });
}

export async function triggerProcessing(scanId: string): Promise<{ status: string; message: string }> {
  const res = await fetch(`${API_BASE}/scans/${scanId}/process`, { method: "POST" });
  if (!res.ok) {
    const body = await res.json().catch(() => ({ detail: `Request failed (${res.status})` }));
    throw new Error(body.detail || "Failed to start processing");
  }
  return res.json();
}

export async function triggerPostProcessing(scanId: string): Promise<{ status: string; message: string }> {
  const res = await fetch(`${API_BASE}/scans/${scanId}/postprocess`, { method: "POST" });
  if (!res.ok) {
    const body = await res.json().catch(() => ({ detail: `Request failed (${res.status})` }));
    throw new Error(body.detail || "Failed to start post-processing");
  }
  return res.json();
}

export interface CTInfo {
  scan_id: string;
  dimensions: [number, number, number];
  spacing: [number, number, number];
  origin: [number, number, number];
  min_value: number;
  max_value: number;
  slice_sizes: {
    axial: [number, number];
    sagittal: [number, number];
    coronal: [number, number];
  };
}

export async function fetchCTInfo(scanId: string): Promise<CTInfo> {
  const res = await fetch(`${API_BASE}/scans/${scanId}/ct/info`);
  if (!res.ok) {
    const body = await res.json().catch(() => ({ detail: `Request failed (${res.status})` }));
    throw new Error(body.detail || "Failed to load CT info");
  }
  return res.json();
}

export function ctSliceUrl(
  scanId: string,
  axis: "axial" | "sagittal" | "coronal",
  index: number,
  wc: number,
  ww: number,
): string {
  return `${API_BASE}/scans/${scanId}/ct/slice/${axis}/${index}?wc=${wc}&ww=${ww}`;
}

export interface PointData {
  x: number;
  y: number;
  z: number;
  label?: string;
}

export async function setPoint(
  scanId: string,
  point: PointData,
): Promise<{ scan_id: string; point: PointData; message: string }> {
  const res = await fetch(`${API_BASE}/scans/${scanId}/point`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(point),
  });
  if (!res.ok) {
    const body = await res.json().catch(() => ({ detail: `Request failed (${res.status})` }));
    throw new Error(body.detail || "Failed to save point");
  }
  return res.json();
}

export async function fetchPoint(
  scanId: string,
): Promise<{ scan_id: string; point: PointData | null }> {
  const res = await fetch(`${API_BASE}/scans/${scanId}/point`);
  if (!res.ok) throw new Error("Failed to fetch point");
  return res.json();
}
