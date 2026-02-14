import { useParams, useNavigate } from "react-router-dom";
import { useCallback, useState, useEffect } from "react";
import { Loader2, Crosshair } from "lucide-react";
import Page from "@/components/shared/Page";
import FileUpload from "@/components/scan/FileUpload";
import ProcessingTimeline from "@/components/scan/ProcessingTimeline";
import ScanDownloads from "@/components/scan/ScanViewer";
import CTViewer from "@/components/viewer/CTViewer";
import { useScanPolling } from "@/components/scan/useScanPolling";
import { triggerProcessing, triggerPostProcessing } from "@/components/scan/api";
import {
  Accordion,
  AccordionItem,
  AccordionTrigger,
  AccordionContent,
} from "@/components/ui/accordion";

function ScanPage() {
  const { scanId } = useParams<{ scanId: string }>();
  const navigate = useNavigate();
  const isNew = scanId === "new";

  const { scanData, processingStatus, loading, error, notFound, refetch } =
    useScanPolling({
      scanId: scanId ?? "",
      enabled: !!scanId && !isNew,
    });

  const [retrying, setRetrying] = useState(false);
  const [retryError, setRetryError] = useState<string | null>(null);
  const autoTriggeredRef = useState(() => new Set<string>())[0];

  // Auto-trigger post-processing when status becomes 'segmented'
  const status = scanData?.status;
  const effectiveScanId = scanId;
  useEffect(() => {
    if (
      status === "segmented" &&
      effectiveScanId &&
      !autoTriggeredRef.has(effectiveScanId)
    ) {
      autoTriggeredRef.add(effectiveScanId);
      triggerPostProcessing(effectiveScanId)
        .then(() => refetch())
        .catch((err) =>
          console.error("Auto post-processing trigger failed:", err),
        );
    }
  }, [status, effectiveScanId, refetch, autoTriggeredRef]);

  const handleRetry = useCallback(async () => {
    if (!scanId || !scanData) return;
    setRetrying(true);
    setRetryError(null);
    try {
      const hasOrgans =
        processingStatus?.organs_processed &&
        processingStatus.organs_processed.length > 0;
      if (
        (scanData.status === "error" && hasOrgans) ||
        scanData.status === "segmented"
      ) {
        await triggerPostProcessing(scanId);
      } else {
        await triggerProcessing(scanId);
      }
      await refetch();
    } catch (err) {
      setRetryError(
        err instanceof Error ? err.message : "Failed to start processing",
      );
    } finally {
      setRetrying(false);
    }
  }, [scanId, scanData, processingStatus, refetch]);

  const handleUploadComplete = useCallback(
    (newScanId: string) => {
      navigate(`/scans/${newScanId}`, { replace: true });
    },
    [navigate],
  );

  const showUpload = isNew || notFound;
  const showScan = !isNew && !notFound && !loading && scanData !== null;

  return (
    <Page>
      <div className="w-full max-w-4xl mt-4">
        {showUpload && (
          <div className="w-full">
            <h1 className="text-3xl font-bold mb-2">Upload CT Scan</h1>
            <p className="text-muted-foreground mb-6">
              Upload a CT scan file to generate a 3D model for AR
              visualization.
            </p>
            <FileUpload onUploadComplete={handleUploadComplete} />
          </div>
        )}

        {!isNew && !notFound && loading && (
          <div className="rounded-xl border bg-card p-8 shadow-sm flex items-center justify-center gap-3">
            <Loader2 className="w-5 h-5 animate-spin text-muted-foreground" />
            <p className="text-muted-foreground">Loading scan&hellip;</p>
          </div>
        )}

        {!isNew && !notFound && !loading && error && (
          <div className="rounded-xl border bg-card p-8 text-center shadow-sm">
            <p className="text-destructive">{error}</p>
          </div>
        )}

        {showScan && scanData && scanId && (
          <div className="space-y-6">
            <div className="rounded-xl border bg-card px-6 py-4 shadow-sm">
              <div className="flex flex-wrap items-center gap-x-6 gap-y-1 text-sm">
                <h1 className="text-lg font-bold mr-auto">
                  {scanId === "demo" ? "Demo Scan" : "Scan"}
                </h1>
                <span>
                  <span className="text-muted-foreground">Status:</span>{" "}
                  <span className="font-medium capitalize">
                    {scanData.status.replace("_", " ")}
                  </span>
                </span>
                <span>
                  <span className="text-muted-foreground">Created:</span>{" "}
                  <span className="font-medium">
                    {new Date(scanData.created_at).toLocaleString()}
                  </span>
                </span>
                <span>
                  <span className="text-muted-foreground">File:</span>{" "}
                  <span className="font-medium">
                    {scanData.original_filename ?? "—"}
                  </span>
                </span>
                {scanData.file_size != null && (
                  <span>
                    <span className="text-muted-foreground">Size:</span>{" "}
                    <span className="font-medium">
                      {(scanData.file_size / (1024 * 1024)).toFixed(1)} MB
                    </span>
                  </span>
                )}
              </div>
            </div>

            <div className="rounded-xl border bg-card p-6 shadow-sm">
              <ProcessingTimeline
                status={scanData.status}
                error={retryError || processingStatus?.error}
                organsProcessed={processingStatus?.organs_processed}
                onRetry={handleRetry}
                retrying={retrying}
              />
            </div>

            <div className="rounded-xl border bg-card shadow-sm overflow-hidden">
              <Accordion type="single" collapsible>
                <AccordionItem value="ct-viewer" className="border-b-0">
                  <AccordionTrigger className="px-6 py-4 hover:no-underline">
                    <span className="inline-flex items-center gap-2">
                      <Crosshair className="w-4 h-4 text-muted-foreground" />
                      Select Target Point
                    </span>
                  </AccordionTrigger>
                  <AccordionContent className="pb-0">
                    <CTViewer
                      scanId={scanId}
                      existingPoint={scanData.point}
                      onPointSaved={refetch}
                    />
                  </AccordionContent>
                </AccordionItem>
              </Accordion>
            </div>

            {scanData.has_fbx && <ScanDownloads scanId={scanId} />}
          </div>
        )}
      </div>
    </Page>
  );
}

export default ScanPage;
