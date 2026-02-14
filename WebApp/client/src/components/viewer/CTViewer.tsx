import { useState, useEffect, useRef, useCallback, useMemo } from "react";
import { Loader2, Crosshair, Save, Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  type CTInfo,
  fetchCTInfo,
  ctSliceUrl,
  setPoint,
  type PointData,
} from "@/components/scan/api";

type Axis = "axial" | "sagittal" | "coronal";

interface WindowPreset {
  name: string;
  wc: number;
  ww: number;
}

const WINDOW_PRESETS: WindowPreset[] = [  { name: "Soft Tissue", wc: 40, ww: 400 },
  { name: "Lung", wc: -600, ww: 1500 },
  { name: "Bone", wc: 400, ww: 1800 },
  { name: "Brain", wc: 40, ww: 80 },
  { name: "Abdomen", wc: 40, ww: 350 },
];

const PREFETCH_RADIUS = 3;

export interface CTViewerProps {
  scanId: string;
  onPointSaved?: () => void;
  existingPoint?: { x: number; y: number; z: number } | null;
}

export default function CTViewer({
  scanId,
  onPointSaved,
  existingPoint,
}: CTViewerProps) {
  const [ctInfo, setCTInfo] = useState<CTInfo | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [voxel, setVoxel] = useState<[number, number, number]>([0, 0, 0]);
  const [wc, setWC] = useState(40);
  const [ww, setWW] = useState(400);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);


  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    fetchCTInfo(scanId)
      .then((info) => {
        if (cancelled) return;
        setCTInfo(info);
        setVoxel([
          Math.floor(info.dimensions[0] / 2),
          Math.floor(info.dimensions[1] / 2),
          Math.floor(info.dimensions[2] / 2),
        ]);
        setLoading(false);
      })
      .catch((err) => {
        if (cancelled) return;
        setError(err.message);
        setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [scanId]);


  useEffect(() => {
    if (!existingPoint || !ctInfo) return;
    const vx = Math.round(
      (existingPoint.x - ctInfo.origin[0]) / ctInfo.spacing[0],
    );
    const vy = Math.round(
      (existingPoint.y - ctInfo.origin[1]) / ctInfo.spacing[1],
    );
    const vz = Math.round(
      (existingPoint.z - ctInfo.origin[2]) / ctInfo.spacing[2],
    );
    setVoxel([
      clamp(vx, 0, ctInfo.dimensions[0] - 1),
      clamp(vy, 0, ctInfo.dimensions[1] - 1),
      clamp(vz, 0, ctInfo.dimensions[2] - 1),
    ]);
  }, [existingPoint, ctInfo]);


  const worldPoint: PointData | null = ctInfo
    ? {
        x: voxel[0] * ctInfo.spacing[0] + ctInfo.origin[0],
        y: voxel[1] * ctInfo.spacing[1] + ctInfo.origin[1],
        z: voxel[2] * ctInfo.spacing[2] + ctInfo.origin[2],
      }
    : null;

  const handleClick = useCallback(
    (axis: Axis, normX: number, normY: number) => {
      if (!ctInfo) return;
      const d = ctInfo.dimensions;
      setVoxel((prev) => {
        const next: [number, number, number] = [...prev];
        if (axis === "axial") {
          next[0] = clamp(Math.round(normX * (d[0] - 1)), 0, d[0] - 1);
          next[1] = clamp(Math.round(normY * (d[1] - 1)), 0, d[1] - 1);
        } else if (axis === "sagittal") {
          next[1] = clamp(Math.round(normX * (d[1] - 1)), 0, d[1] - 1);
          next[2] = clamp(Math.round(normY * (d[2] - 1)), 0, d[2] - 1);
        } else {
          next[0] = clamp(Math.round(normX * (d[0] - 1)), 0, d[0] - 1);
          next[2] = clamp(Math.round(normY * (d[2] - 1)), 0, d[2] - 1);
        }
        return next;
      });
      setSaved(false);
    },
    [ctInfo],
  );

  const handleScroll = useCallback(
    (axis: Axis, delta: number) => {
      if (!ctInfo) return;
      const d = ctInfo.dimensions;
      setVoxel((prev) => {
        const next: [number, number, number] = [...prev];
        if (axis === "axial")
          next[2] = clamp(prev[2] + delta, 0, d[2] - 1);
        else if (axis === "sagittal")
          next[0] = clamp(prev[0] + delta, 0, d[0] - 1);
        else next[1] = clamp(prev[1] + delta, 0, d[1] - 1);
        return next;
      });
      setSaved(false);
    },
    [ctInfo],
  );

  const handleSlice = useCallback(
    (axis: Axis, value: number) => {
      setVoxel((prev) => {
        const next: [number, number, number] = [...prev];
        if (axis === "axial") next[2] = value;
        else if (axis === "sagittal") next[0] = value;
        else next[1] = value;
        return next;
      });
      setSaved(false);
    },
    [],
  );

  const handleSave = useCallback(async () => {
    if (!worldPoint) return;
    setSaving(true);
    try {
      await setPoint(scanId, worldPoint);
      setSaved(true);
      onPointSaved?.();
    } catch {
    } finally {
      setSaving(false);
    }
  }, [scanId, worldPoint, onPointSaved]);


  const prefetchUrls = useMemo(() => {
    if (!ctInfo) return [];
    const d = ctInfo.dimensions;
    const urls: string[] = [];
    const axes: { axis: Axis; idx: number; max: number }[] = [
      { axis: "axial", idx: voxel[2], max: d[2] - 1 },
      { axis: "coronal", idx: voxel[1], max: d[1] - 1 },
      { axis: "sagittal", idx: voxel[0], max: d[0] - 1 },
    ];
    for (const { axis, idx, max } of axes) {
      for (let delta = -PREFETCH_RADIUS; delta <= PREFETCH_RADIUS; delta++) {
        if (delta === 0) continue;
        const i = idx + delta;
        if (i >= 0 && i <= max) {
          urls.push(ctSliceUrl(scanId, axis, i, wc, ww));
        }
      }
    }
    return urls;
  }, [scanId, voxel, ctInfo, wc, ww]);

  useEffect(() => {
    if (prefetchUrls.length === 0) return;
    const imgs = prefetchUrls.map((url) => {
      const img = new Image();
      img.src = url;
      return img;
    });
    return () => {
      imgs.forEach((i) => {
        i.src = "";
      });
    };
  }, [prefetchUrls]);


  if (loading) {
    return (
      <div className="flex flex-col items-center justify-center gap-3 p-12 text-muted-foreground">
        <Loader2 className="h-6 w-6 animate-spin" />
        <p className="text-sm">
          Loading CT data&hellip; This may take a moment for large files.
        </p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex flex-col items-center gap-2 p-8 text-center text-sm text-muted-foreground">
        <p>CT viewer is not available for this scan.</p>
        <p className="text-xs opacity-70">{error}</p>
      </div>
    );
  }

  if (!ctInfo) return null;

  const dims = ctInfo.dimensions;

  return (
    <div className="space-y-3 p-3">

      <div className="grid grid-cols-1 md:grid-cols-3 gap-3 max-w-5xl mx-auto">
        <SliceView
          label="Axial"
          sliceLabel={`Z: ${voxel[2]} / ${dims[2] - 1}`}
          src={ctSliceUrl(scanId, "axial", voxel[2], wc, ww)}
          aspectRatio={ctInfo.slice_sizes.axial}
          crosshairX={voxel[0] / (dims[0] - 1)}
          crosshairY={voxel[1] / (dims[1] - 1)}
          axis="axial"
          sliceIndex={voxel[2]}
          maxSlice={dims[2] - 1}
          onClick={handleClick}
          onScroll={handleScroll}
          onSliceChange={handleSlice}
        />
        <SliceView
          label="Coronal"
          sliceLabel={`Y: ${voxel[1]} / ${dims[1] - 1}`}
          src={ctSliceUrl(scanId, "coronal", voxel[1], wc, ww)}
          aspectRatio={ctInfo.slice_sizes.coronal}
          crosshairX={voxel[0] / (dims[0] - 1)}
          crosshairY={voxel[2] / (dims[2] - 1)}
          axis="coronal"
          sliceIndex={voxel[1]}
          maxSlice={dims[1] - 1}
          onClick={handleClick}
          onScroll={handleScroll}
          onSliceChange={handleSlice}
        />
        <SliceView
          label="Sagittal"
          sliceLabel={`X: ${voxel[0]} / ${dims[0] - 1}`}
          src={ctSliceUrl(scanId, "sagittal", voxel[0], wc, ww)}
          aspectRatio={ctInfo.slice_sizes.sagittal}
          crosshairX={voxel[1] / (dims[1] - 1)}
          crosshairY={voxel[2] / (dims[2] - 1)}
          axis="sagittal"
          sliceIndex={voxel[0]}
          maxSlice={dims[0] - 1}
          onClick={handleClick}
          onScroll={handleScroll}
          onSliceChange={handleSlice}
        />
      </div>


      <div className="flex flex-wrap gap-2">
        {WINDOW_PRESETS.map((p) => (
          <button
            key={p.name}
            className={`rounded-md border px-3 py-1 text-xs transition-colors ${
              wc === p.wc && ww === p.ww
                ? "bg-primary text-primary-foreground border-primary"
                : "bg-card hover:bg-accent border-border"
            }`}
            onClick={() => {
              setWC(p.wc);
              setWW(p.ww);
            }}
          >
            {p.name}
          </button>
        ))}
      </div>


      <div className="flex flex-wrap items-center gap-x-6 gap-y-2 text-sm">
        <span className="inline-flex items-center gap-1.5 text-muted-foreground">
          <Crosshair className="h-3.5 w-3.5" />
          Voxel ({voxel[0]}, {voxel[1]}, {voxel[2]})
        </span>
        {worldPoint && (
          <span className="text-muted-foreground">
            World ({worldPoint.x.toFixed(1)}, {worldPoint.y.toFixed(1)},{" "}
            {worldPoint.z.toFixed(1)}) mm
          </span>
        )}
        <Button
          size="sm"
          variant={saved ? "outline" : "default"}
          className="gap-1.5"
          disabled={saving || !worldPoint}
          onClick={handleSave}
        >
          {saving ? (
            <Loader2 className="h-3.5 w-3.5 animate-spin" />
          ) : saved ? (
            <Check className="h-3.5 w-3.5" />
          ) : (
            <Save className="h-3.5 w-3.5" />
          )}
          {saved ? "Point Saved" : "Set Target Point"}
        </Button>
      </div>
    </div>
  );
}

interface SliceViewProps {
  label: string;
  sliceLabel: string;
  src: string;
  aspectRatio: [number, number];
  crosshairX: number;
  crosshairY: number;
  axis: Axis;
  sliceIndex: number;
  maxSlice: number;
  onClick: (axis: Axis, normX: number, normY: number) => void;
  onScroll: (axis: Axis, delta: number) => void;
  onSliceChange: (axis: Axis, value: number) => void;
}

function SliceView({
  label,
  sliceLabel,
  src,
  aspectRatio,
  crosshairX,
  crosshairY,
  axis,
  sliceIndex,
  maxSlice,
  onClick,
  onScroll,
  onSliceChange,
}: SliceViewProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const scrollAccum = useRef(0);
  const scrollRaf = useRef<number | null>(null);

  useEffect(() => {
    const el = containerRef.current;
    if (!el) return;

    const handler = (e: WheelEvent) => {
      e.preventDefault();
      scrollAccum.current += e.deltaY;

      if (scrollRaf.current === null) {
        scrollRaf.current = requestAnimationFrame(() => {
          const steps = Math.round(scrollAccum.current / 50);
          if (steps !== 0) {
            onScroll(axis, steps);
          }
          scrollAccum.current = 0;
          scrollRaf.current = null;
        });
      }
    };

    el.addEventListener("wheel", handler, { passive: false });
    return () => {
      el.removeEventListener("wheel", handler);
      if (scrollRaf.current !== null) cancelAnimationFrame(scrollRaf.current);
    };
  }, [axis, onScroll]);

  const handleClick = (e: React.MouseEvent<HTMLDivElement>) => {
    const rect = e.currentTarget.getBoundingClientRect();
    const nx = (e.clientX - rect.left) / rect.width;
    const ny = (e.clientY - rect.top) / rect.height;
    onClick(axis, clamp01(nx), clamp01(ny));
  };

  const [w, h] = aspectRatio;

  return (
    <div className="flex flex-col gap-1.5">
      <div className="flex items-center justify-between px-0.5 text-xs text-muted-foreground">
        <span className="font-semibold">{label}</span>
        <span>{sliceLabel}</span>
      </div>

      <div
        ref={containerRef}
        className="relative cursor-crosshair select-none overflow-hidden rounded border border-border bg-black"
        style={{ aspectRatio: `${w} / ${h}` }}
        onClick={handleClick}
      >
        <img
          src={src}
          alt={`${label} slice`}
          draggable={false}
          className="absolute inset-0 h-full w-full object-fill"
          style={{ imageRendering: "pixelated" }}
        />

        <div
          className="pointer-events-none absolute top-0 bottom-0 w-px bg-yellow-400/70"
          style={{ left: `${crosshairX * 100}%` }}
        />
        <div
          className="pointer-events-none absolute left-0 right-0 h-px bg-yellow-400/70"
          style={{ top: `${crosshairY * 100}%` }}
        />
      </div>

      <input
        type="range"
        min={0}
        max={maxSlice}
        value={sliceIndex}
        onChange={(e) => onSliceChange(axis, parseInt(e.target.value))}
        className="w-full accent-primary"
      />
    </div>
  );
}

function clamp(v: number, lo: number, hi: number): number {
  return Math.max(lo, Math.min(hi, v));
}

function clamp01(v: number): number {
  return clamp(v, 0, 1);
}
