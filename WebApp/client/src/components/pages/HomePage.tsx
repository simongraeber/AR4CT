import { Link } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { Upload, QrCode, Eye, ArrowRight } from "lucide-react"
import { motion } from "framer-motion"
import Page from "@/components/shared/Page"
import { scrollAnimation } from "@/components/shared/scrollAnimation"

function HomePage() {
    return (
        <Page
            backgroundElements={
                <>
                    <div
                        className="absolute top-0 left-0 w-64 h-64 bg-gradient-to-br from-cyan-400 to-blue-500
                        rounded-full filter blur-3xl opacity-15 animate-pulse"
                    />
                    <div
                        className="absolute bottom-0 right-0 w-64 h-64 bg-gradient-to-br from-cyan-400 to-blue-500
                        rounded-full filter blur-3xl opacity-15 animate-pulse delay-200"
                    />
                </>
            }
        >
            {/* Hero Section */}
            <motion.section
                className="flex flex-col items-center text-center pt-12 pb-16 px-4 w-full"
                variants={scrollAnimation}
                initial="hidden"
                animate="visible"
                transition={{ duration: 0.8 }}
            >
                <div className="flex items-center gap-4 mb-6">
                    <motion.img
                        src="/AR4CT.svg"
                        alt="AR4CT Logo"
                        className="h-20 w-20"
                        initial={{ scale: 0, rotate: -180 }}
                        animate={{ scale: 1, rotate: 0 }}
                        transition={{ type: "spring", stiffness: 200, damping: 15, delay: 0.2 }}
                    />
                    <h1 className="text-5xl sm:text-6xl font-bold tracking-tight">AR4CT</h1>
                </div>
                <p className="text-xl sm:text-2xl text-muted-foreground max-w-2xl mb-2">
                    Medical Augmented Reality for CT Scans
                </p>
                <p className="text-base text-muted-foreground max-w-xl mb-10">
                    Upload CT scans, generate 3D reconstructions, and visualize them in augmented reality.
                    A project for the Medical Augmented Reality course at the Technical University of Munich.
                </p>
                <div className="flex gap-4 flex-wrap justify-center">
                    <Link to="/scans/new">
                        <Button size="lg" className="gap-2">
                            <Upload className="w-4 h-4" />
                            Upload New Scan
                        </Button>
                    </Link>
                    <Link to="/scans/demo">
                        <Button size="lg" variant="outline" className="gap-2">
                            <Eye className="w-4 h-4" />
                            View Demo Scan
                        </Button>
                    </Link>
                </div>
            </motion.section>

            {/* Features Section */}
            <motion.section
                className="w-full max-w-5xl pb-16"
                variants={scrollAnimation}
                initial="hidden"
                animate="visible"
                transition={{ delay: 0.3, duration: 0.8 }}
            >
                <h2 className="text-3xl font-bold text-center mb-10">How It Works</h2>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
                    <FeatureCard
                        icon={<Upload className="w-10 h-10 text-primary" />}
                        title="Upload CT Scans"
                        description="Upload your CT scan files. They are automatically converted into 3D models in the background."
                        delay={0.4}
                    />
                    <FeatureCard
                        icon={<QrCode className="w-10 h-10 text-primary" />}
                        title="Generate QR Codes"
                        description="Each scan gets a unique QR code that links to its 3D model for instant AR access."
                        delay={0.6}
                    />
                    <FeatureCard
                        icon={<Eye className="w-10 h-10 text-primary" />}
                        title="View in AR"
                        description="Scan the QR code with the AR app to display the 3D CT reconstruction in augmented reality."
                        delay={0.8}
                    />
                </div>
            </motion.section>

            {/* CTA Section */}
            <motion.section
                className="w-full max-w-3xl pb-16"
                variants={scrollAnimation}
                initial="hidden"
                whileInView="visible"
                transition={{ delay: 0.5, duration: 0.8 }}
                viewport={{ once: true, amount: 0.2 }}
            >
                <motion.div
                    className="rounded-xl border bg-card p-8 text-center shadow-sm"
                    whileHover={{ scale: 1.02, boxShadow: "0px 8px 24px rgba(0, 0, 0, 0.1)" }}
                    transition={{ duration: 0.3 }}
                >
                    <h3 className="text-2xl font-semibold mb-3">Ready to get started?</h3>
                    <p className="text-muted-foreground mb-6">
                        Upload your first CT scan or open an existing one to explore the 3D viewer.
                    </p>
                    <div className="flex gap-4 justify-center flex-wrap">
                        <Link to="/scans/new">
                            <Button className="gap-2">
                                Get Started <ArrowRight className="w-4 h-4" />
                            </Button>
                        </Link>
                    </div>
                </motion.div>
            </motion.section>
        </Page>
    )
}

interface FeatureCardProps {
    icon: React.ReactNode
    title: string
    description: string
    delay: number
}

function FeatureCard({ icon, title, description, delay }: FeatureCardProps) {
    return (
        <motion.div
            className="rounded-xl border bg-card p-6 text-center shadow-sm flex flex-col items-center gap-4"
            initial={{ opacity: 0, y: 50 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay, duration: 0.6 }}
            whileHover={{ scale: 1.05, boxShadow: "0px 10px 20px rgba(0, 0, 0, 0.15)", transition: { duration: 0.3 } }}
        >
            {icon}
            <h3 className="text-lg font-semibold">{title}</h3>
            <p className="text-sm text-muted-foreground">{description}</p>
        </motion.div>
    )
}

export default HomePage
