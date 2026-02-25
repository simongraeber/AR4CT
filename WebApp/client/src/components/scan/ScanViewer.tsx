import { Download, Printer, Box } from "lucide-react";
import { Button } from "@/components/ui/button";
import { motion } from "framer-motion";
import { useMemo } from "react";

interface ScanDownloadsProps {
  scanId: string;
}

function supportsARQuickLook(): boolean {
  const a = document.createElement("a");
  return a.relList?.supports?.("ar") ?? false;
}

export default function ScanDownloads({ scanId }: ScanDownloadsProps) {
  const fbxUrl = `/api/scans/${scanId}/fbx`;
  const usdzUrl = `/api/scans/${scanId}/usdz`;
  const printUrl = `/api/scans/${scanId}/print.pdf`;
  const showAR = useMemo(() => supportsARQuickLook(), []);

  return (
    <motion.div
      className="rounded-xl border bg-card p-6 shadow-sm"
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.4 }}
    >
      <h3 className="text-lg font-semibold mb-4">Downloads</h3>

      <div className="flex flex-col sm:flex-row gap-3">
        {showAR && (
          <a rel="ar" href={usdzUrl}>
            <img
              src="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='1' height='1'/%3E"
              alt="View in AR"
              style={{ display: "none" }}
            />
            <Button className="gap-2" asChild={false}>
              <Box className="w-4 h-4" />
              View in AR
            </Button>
          </a>
        )}

        <a href={fbxUrl} download>
          <Button variant="outline" className="gap-2">
            <Download className="w-4 h-4" />
            Download 3D Model (.fbx)
          </Button>
        </a>

        <a href={printUrl} target="_blank" rel="noopener noreferrer">
          <Button variant="outline" className="gap-2">
            <Printer className="w-4 h-4" />
            Print AR Sheet (PDF)
          </Button>
        </a>
      </div>

      <p className="text-xs text-muted-foreground mt-3">
        The PDF contains a QR code and a tool tracking marker. Print it out,
        cut along the lines, and attach the tool marker to your instrument.
        The AR app will show the distance from the tool to the target point.
      </p>
    </motion.div>
  );
}
