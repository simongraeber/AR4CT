import { useState, useRef, useCallback } from "react";
import { Upload, FileUp, AlertCircle, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { uploadScan } from "./api";

const ACCEPTED_EXTENSIONS = [".fbx", ".zip", ".nii", ".nii.gz", ".mhd", ".nrrd"];
const MAX_SIZE_MB = 500;

interface FileUploadProps {
  onUploadComplete: (scanId: string) => void;
}

function validateFile(file: File): string | null {
  const name = file.name.toLowerCase();
  const valid = ACCEPTED_EXTENSIONS.some((ext) => name.endsWith(ext));
  if (!valid) {
    return `Unsupported file type. Accepted: ${ACCEPTED_EXTENSIONS.join(", ")}`;
  }
  if (file.size > MAX_SIZE_MB * 1024 * 1024) {
    return `File too large. Maximum size is ${MAX_SIZE_MB} MB.`;
  }
  return null;
}

export default function FileUpload({ onUploadComplete }: FileUploadProps) {
  const [dragOver, setDragOver] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [progress, setProgress] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const handleFile = useCallback((file: File) => {
    setError(null);
    const err = validateFile(file);
    if (err) {
      setError(err);
      return;
    }
    setSelectedFile(file);
  }, []);

  const handleDrop = useCallback(
    (e: React.DragEvent) => {
      e.preventDefault();
      setDragOver(false);
      const file = e.dataTransfer.files[0];
      if (file) handleFile(file);
    },
    [handleFile],
  );

  const handleUpload = async () => {
    if (!selectedFile) return;
    setUploading(true);
    setProgress(0);
    setError(null);

    try {
      const result = await uploadScan(selectedFile, setProgress);
      onUploadComplete(result.scan_id);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Upload failed");
      setUploading(false);
    }
  };

  return (
    <div className="w-full">
      {/* Drop zone */}
      <div
        className={cn(
          "relative rounded-xl border-2 border-dashed p-12 text-center transition-colors cursor-pointer",
          dragOver
            ? "border-primary bg-primary/5"
            : "border-muted-foreground/25 hover:border-muted-foreground/50",
          error && "border-destructive/50",
        )}
        onDragOver={(e) => {
          e.preventDefault();
          setDragOver(true);
        }}
        onDragLeave={() => setDragOver(false)}
        onDrop={handleDrop}
        onClick={() => !uploading && inputRef.current?.click()}
      >
        <input
          ref={inputRef}
          type="file"
          className="hidden"
          accept={ACCEPTED_EXTENSIONS.join(",")}
          onChange={(e) => {
            const file = e.target.files?.[0];
            if (file) handleFile(file);
          }}
        />

        {selectedFile ? (
          <div className="flex flex-col items-center gap-3">
            <FileUp className="w-12 h-12 text-primary" />
            <p className="text-lg font-medium">{selectedFile.name}</p>
            <p className="text-sm text-muted-foreground">
              {(selectedFile.size / (1024 * 1024)).toFixed(1)} MB
            </p>
          </div>
        ) : (
          <div className="flex flex-col items-center gap-3">
            <Upload className="w-12 h-12 text-muted-foreground" />
            <p className="text-lg font-medium">Drop your CT scan here</p>
            <p className="text-sm text-muted-foreground">
              or click to browse &mdash; .nii, .nii.gz, .mhd, .nrrd, .zip, or
              .fbx
            </p>
            <p className="text-xs text-muted-foreground">
              Max file size: {MAX_SIZE_MB} MB
            </p>
          </div>
        )}
      </div>

      {/* Validation error */}
      {error && (
        <div className="flex items-center gap-2 mt-3 text-sm text-destructive">
          <AlertCircle className="w-4 h-4 shrink-0" />
          {error}
        </div>
      )}

      {/* Action buttons */}
      {selectedFile && !uploading && (
        <div className="flex gap-3 mt-4">
          <Button onClick={handleUpload} className="gap-2">
            <Upload className="w-4 h-4" />
            Upload
          </Button>
          <Button
            variant="outline"
            onClick={() => {
              setSelectedFile(null);
              setError(null);
            }}
          >
            Cancel
          </Button>
        </div>
      )}

      {/* Upload progress */}
      {uploading && (
        <div className="mt-4 space-y-2">
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Loader2 className="w-4 h-4 animate-spin" />
            Uploading&hellip; {progress}%
          </div>
          <div className="h-2 rounded-full bg-secondary overflow-hidden">
            <div
              className="h-full bg-primary rounded-full transition-all duration-300"
              style={{ width: `${progress}%` }}
            />
          </div>
        </div>
      )}
    </div>
  );
}
