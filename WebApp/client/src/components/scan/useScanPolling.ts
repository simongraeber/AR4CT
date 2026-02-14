import { useState, useEffect, useCallback, useRef } from "react";
import {
  fetchScan,
  fetchProcessingStatus,
  type ScanMetadata,
  type ProcessingStatus,
  type ScanStatus,
} from "./api";

const ACTIVE_STATUSES: Set<ScanStatus> = new Set([
  "uploaded",
  "processing",
  "segmented",
  "post_processing",
]);

interface UseScanPollingOpts {
  scanId: string;
  enabled: boolean;
  interval?: number;
}

export function useScanPolling({
  scanId,
  enabled,
  interval = 10_000,
}: UseScanPollingOpts) {
  const [scanData, setScanData] = useState<ScanMetadata | null>(null);
  const [processingStatus, setProcessingStatus] =
    useState<ProcessingStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notFound, setNotFound] = useState(false);

  const isFetchingRef = useRef(false);

  useEffect(() => {
    setScanData(null);
    setProcessingStatus(null);
    setError(null);
    setNotFound(false);
    setLoading(true);
  }, [scanId]);

  const fetchData = useCallback(async () => {
    if (isFetchingRef.current) return;
    isFetchingRef.current = true;

    try {
      const scan = await fetchScan(scanId);
      setScanData(scan);
      setNotFound(false);
      setError(null);

      try {
        const status = await fetchProcessingStatus(scanId);
        setProcessingStatus(status);
        setScanData((prev) =>
          prev
            ? { ...prev, status: status.status, has_fbx: status.has_fbx }
            : prev,
        );
      } catch {
      }
    } catch (err: unknown) {
      const status = (err as { status?: number }).status;
      if (status === 404) {
        setNotFound(true);
      } else {
        setError(
          err instanceof Error ? err.message : "Failed to fetch scan",
        );
      }
    } finally {
      isFetchingRef.current = false;
      setLoading(false);
    }
  }, [scanId]);

  useEffect(() => {
    if (!enabled) {
      setLoading(false);
      return;
    }
    fetchData();
  }, [enabled, fetchData]);

  useEffect(() => {
    if (!enabled || notFound) return;

    const shouldPoll =
      !scanData || ACTIVE_STATUSES.has(scanData.status);
    if (!shouldPoll) return;

    const id = setInterval(fetchData, interval);
    return () => clearInterval(id);
  }, [enabled, scanData?.status, notFound, fetchData, interval]);

  return { scanData, processingStatus, loading, error, notFound, refetch: fetchData };
}
