import { useParams } from "react-router-dom"
import { useState, useEffect } from "react"
import { Button } from "@/components/ui/button"
import CTViewer from "@/components/viewer/CTViewer"
import Page from "@/components/shared/Page"
import { ArrowLeft } from "lucide-react"
import { Link } from "react-router-dom"

function ScanPage() {
    const { scanId } = useParams<{ scanId: string }>()
    const [showViewer, setShowViewer] = useState(false)
    const [scanData, setScanData] = useState<Record<string, unknown> | null>(null)
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState<string | null>(null)

    useEffect(() => {
        if (!scanId || scanId === "new" || scanId === "demo") {
            setLoading(false)
            return
        }

        const fetchScan = async () => {
            try {
                const response = await fetch(`/api/scans/${scanId}`)
                if (response.ok) {
                    const data = await response.json()
                    setScanData(data)
                } else {
                    setError("Scan not found")
                }
            } catch {
                setError("Failed to connect to server")
            }
            setLoading(false)
        }

        fetchScan()
    }, [scanId])

    if (scanId === "new") {
        return (
            <Page>
                <div className="w-full max-w-2xl mt-8">
                    <Link to="/" className="inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground mb-6 transition-colors">
                        <ArrowLeft className="w-4 h-4" /> Back to Home
                    </Link>
                    <h1 className="text-3xl font-bold mb-4">Upload New Scan</h1>
                    <div className="rounded-xl border bg-card p-8 text-center shadow-sm">
                        <p className="text-muted-foreground mb-4">
                            Upload functionality is coming soon. You will be able to upload CT scan files (.nii, .nii.gz, .mhd, .nrrd) or pre-processed FBX models.
                        </p>
                        <p className="text-sm text-muted-foreground">
                            Maximum file size: 500 MB
                        </p>
                    </div>
                </div>
            </Page>
        )
    }

    return (
        <Page>
            <div className="w-full max-w-4xl mt-4">
                <Link to="/" className="inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground mb-6 transition-colors">
                    <ArrowLeft className="w-4 h-4" /> Back to Home
                </Link>

                <div className="flex items-center justify-between mb-6">
                    <div>
                        <h1 className="text-3xl font-bold">
                            {scanId === "demo" ? "Demo Scan" : `Scan`}
                        </h1>
                        <p className="text-sm text-muted-foreground mt-1 font-mono">
                            {scanId}
                        </p>
                    </div>
                    <Button onClick={() => setShowViewer(!showViewer)} variant="outline">
                        {showViewer ? "Hide CT Viewer" : "Show CT Viewer"}
                    </Button>
                </div>

                {loading && (
                    <div className="rounded-xl border bg-card p-8 text-center shadow-sm">
                        <p className="text-muted-foreground">Loading scan data...</p>
                    </div>
                )}

                {error && (
                    <div className="rounded-xl border bg-card p-8 text-center shadow-sm">
                        <p className="text-destructive">{error}</p>
                    </div>
                )}

                {scanData && (
                    <div className="rounded-xl border bg-card p-6 shadow-sm mb-6">
                        <h2 className="text-lg font-semibold mb-3">Scan Details</h2>
                        <div className="grid grid-cols-2 gap-4 text-sm">
                            <div>
                                <span className="text-muted-foreground">Status:</span>{" "}
                                <span className="font-medium">{scanData.status as string}</span>
                            </div>
                            <div>
                                <span className="text-muted-foreground">Created:</span>{" "}
                                <span className="font-medium">{scanData.created_at as string}</span>
                            </div>
                            <div>
                                <span className="text-muted-foreground">Has FBX:</span>{" "}
                                <span className="font-medium">{scanData.has_fbx ? "Yes" : "No"}</span>
                            </div>
                            <div>
                                <span className="text-muted-foreground">File:</span>{" "}
                                <span className="font-medium">{scanData.original_filename as string ?? "—"}</span>
                            </div>
                        </div>
                    </div>
                )}

                {!loading && !error && !scanData && scanId !== "demo" && (
                    <div className="rounded-xl border bg-card p-8 text-center shadow-sm">
                        <p className="text-muted-foreground">
                            No scan data available. This scan may not exist yet.
                        </p>
                    </div>
                )}

                {showViewer && (
                    <div className="w-full mt-4">
                        <CTViewer />
                    </div>
                )}
            </div>
        </Page>
    )
}

export default ScanPage
