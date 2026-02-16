import { motion } from "framer-motion"
import { Smartphone, ExternalLink, Download } from "lucide-react"
import Page from "@/components/shared/Page"
import { scrollAnimation } from "@/components/shared/scrollAnimation"
import { Button } from "@/components/ui/button"

const APK_URL =
    "https://github.com/simongraeber/AR4CT/releases/latest/download/AR4CT.apk"

function AppPage() {
    return (
        <Page>
            <h1 className="text-3xl font-bold mt-8 mb-8">AR4CT Mobile App</h1>

            <motion.section
                className="w-full px-4 pt-6 mb-16"
                variants={scrollAnimation}
                initial="hidden"
                animate="visible"
                transition={{ duration: 0.8 }}
            >
                <div className="mx-auto max-w-lg p-8 rounded-xl shadow-lg border bg-card text-center space-y-6">
                    <div className="flex justify-center">
                        <div className="rounded-full bg-primary/10 p-4">
                            <Smartphone className="w-12 h-12 text-primary" />
                        </div>
                    </div>

                    <h2 className="text-xl font-semibold">
                        Open in AR4CT App
                    </h2>

                    <p className="text-muted-foreground">
                        If you have the <strong>AR4CT</strong> Android app installed,
                        visiting this page on your device will automatically open it.
                    </p>

                    <p className="text-sm text-muted-foreground">
                        The app lets you explore CT scans in augmented reality
                        directly on your Android device.
                    </p>

                    <div className="pt-2 space-y-3">
                        <a href={APK_URL} download>
                            <Button className="w-full">
                                <Download className="mr-2 h-4 w-4" />
                                Download APK (Debug Build)
                            </Button>
                        </a>

                        <p className="text-xs text-muted-foreground">
                            Enable <em>Install from unknown sources</em> on your
                            Android device, then open the downloaded APK to install.
                        </p>

                        <a
                            href="https://github.com/simongraeber/AR4CT"
                            target="_blank"
                            rel="noopener noreferrer"
                        >
                            <Button variant="outline" className="w-full mt-2">
                                <ExternalLink className="mr-2 h-4 w-4" />
                                Build it yourself on GitHub
                            </Button>
                        </a>
                    </div>
                </div>
            </motion.section>
        </Page>
    )
}

export default AppPage
