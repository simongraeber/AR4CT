import { Download, Printer } from "lucide-react";
import { Button } from "@/components/ui/button";
import { motion } from "framer-motion";

interface ScanDownloadsProps {
  scanId: string;
}

export default function ScanDownloads({ scanId }: ScanDownloadsProps) {
  const fbxUrl = `/api/scans/${scanId}/fbx`;
  const printUrl = `/api/scans/${scanId}/print.pdf`;

  return (
    <motion.div
      className="rounded-xl border bg-card p-6 shadow-sm"
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.4 }}
    >
      <h3 className="text-lg font-semibold mb-4">Downloads</h3>

      <div className="flex flex-col sm:flex-row gap-3">
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
