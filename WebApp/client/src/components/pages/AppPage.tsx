import { motion } from "framer-motion"
import { Smartphone, ExternalLink } from "lucide-react"
import Page from "@/components/shared/Page"
import { scrollAnimation } from "@/components/shared/scrollAnimation"
import { Button } from "@/components/ui/button"

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

                    <div className="pt-2">
                        <Button variant="outline" disabled>
                            <ExternalLink className="mr-2 h-4 w-4" />
                            Coming soon on Google Play
                        </Button>
                    </div>
                </div>
            </motion.section>
        </Page>
    )
}

export default AppPage
