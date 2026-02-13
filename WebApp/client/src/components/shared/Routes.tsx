import { Routes, Route } from "react-router-dom"
import { lazy, Suspense } from "react"
import HomePage from "@/components/pages/HomePage"
import ImprintPage from "@/components/pages/ImprintPage"
import PrivacyPage from "@/components/pages/PrivacyPage"
import AppPage from "@/components/pages/AppPage"
import NotFound from "@/components/shared/NotFound"

const ScanPage = lazy(() => import("@/components/pages/ScanPage"))

const RoutesComponent = () => {
    return (
        <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/scans/:scanId" element={
                <Suspense fallback={<div className="flex items-center justify-center min-h-[50vh]"><p className="text-muted-foreground">Loading...</p></div>}>
                    <ScanPage />
                </Suspense>
            } />
            <Route path="/imprint" element={<ImprintPage />} />
            <Route path="/privacy" element={<PrivacyPage />} />
            <Route path="/app" element={<AppPage />} />
            <Route path="/app/*" element={<AppPage />} />
            <Route path="*" element={<NotFound />} />
        </Routes>
    )
}

export default RoutesComponent
