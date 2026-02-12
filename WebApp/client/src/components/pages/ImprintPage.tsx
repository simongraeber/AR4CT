import { Link } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { User, MapPin, Mail } from "lucide-react"
import { motion } from "framer-motion"
import Page from "@/components/shared/Page"
import { scrollAnimation } from "@/components/shared/scrollAnimation"
import GreetingsAnimation from "@/components/shared/GreetingsAnimation"

function ImprintPage() {
    return (
        <Page>
            <h1 className="text-3xl font-bold mt-8 mb-8">Imprint</h1>

            <motion.section
                className="w-full px-4 pt-6 mb-16"
                variants={scrollAnimation}
                initial="hidden"
                animate="visible"
                transition={{ duration: 0.8 }}
            >
                <motion.div
                    whileHover={{
                        scale: 1.05,
                        boxShadow: "0px 10px 20px rgba(0, 0, 0, 0.2)",
                    }}
                    transition={{ duration: 0.3 }}
                    className="relative mx-auto max-w-md p-6 md:py-12 rounded-xl shadow-lg border bg-card"
                >
                    <div
                        className="absolute top-0 left-0 w-40 h-40 bg-gradient-to-br from-cyan-400/20 to-blue-500/20
                        rounded-full filter blur-3xl animate-pulse z-0 pointer-events-none"
                    />

                    <div className="relative z-10 flex flex-col md:flex-row items-center gap-6">
                        <div className="flex-shrink-0 flex flex-col items-center justify-center md:w-2/5">
                            <img
                                src="/AR4CT.svg"
                                alt="AR4CT Logo"
                                className="w-24 h-24 mb-2 object-contain"
                            />
                            <div className="relative overflow-visible">
                                <div className="pl-2 transform scale-[1.35] rotate-[-6deg]">
                                    <GreetingsAnimation />
                                </div>
                            </div>
                        </div>

                        <div className="flex flex-col space-y-4 md:w-3/5">
                            <div className="flex items-center">
                                <User className="text-primary mr-2 w-5 h-5" />
                                <h2 className="font-bold text-xl">Simon Graeber</h2>
                            </div>
                            <div className="h-0.5 w-16 bg-primary mb-2" />

                            <div className="flex flex-col space-y-3 text-sm">
                                <div className="flex items-start">
                                    <MapPin className="text-muted-foreground mr-2 mt-0.5 w-4 h-4 flex-shrink-0" />
                                    <div>
                                        <p>Mitthenheimer Str. 6</p>
                                        <p>85764 Oberschleißheim</p>
                                        <p>Germany</p>
                                    </div>
                                </div>
                                <div className="flex items-center">
                                    <Mail className="text-muted-foreground mr-2 w-4 h-4 flex-shrink-0" />
                                    <Button variant="link" className="p-0 h-auto" asChild>
                                        <a href="mailto:80-read-crewel@icloud.com">
                                            80-read-crewel@icloud.com
                                        </a>
                                    </Button>
                                </div>
                            </div>
                        </div>
                    </div>
                </motion.div>
            </motion.section>

            <div className="text-center mb-8">
                <Link to="/">
                    <Button>Back to Home</Button>
                </Link>
            </div>
        </Page>
    )
}

export default ImprintPage
