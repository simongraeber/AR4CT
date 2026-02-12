import { Link } from "react-router-dom"
import { Button } from "@/components/ui/button"
import Page from "@/components/shared/Page"

function PrivacyPage() {
    return (
        <Page>
            <h1 className="text-3xl font-bold mt-8 mb-6">Privacy Policy</h1>

            <main className="max-w-2xl mx-auto px-4 text-left">
                <section className="mb-8">
                    <h2 className="text-xl font-semibold mb-3">1. General Information</h2>
                    <p className="text-muted-foreground leading-relaxed">
                        This website is a university project created for the Medical Augmented Reality course
                        at the Technical University of Munich. The protection of your personal data is important to us.
                        Below, we inform you about the handling of your data.
                    </p>
                </section>

                <section className="mb-8">
                    <h2 className="text-xl font-semibold mb-3">2. Data Collection</h2>
                    <p className="text-muted-foreground leading-relaxed mb-3">
                        When you use this application, we may process CT scan data that you voluntarily upload.
                        This data is stored on our servers solely for the purpose of generating 3D reconstructions
                        and enabling AR visualization. Uploaded CT scans may be passed on to third-party
                        services for processing (e.g. segmentation and 3D model generation). Uploaded files
                        can be deleted at any time.
                    </p>
                    <p className="text-muted-foreground leading-relaxed font-medium">
                        Please do not upload any personal or patient-identifiable information. Ensure that all
                        CT scan data is fully anonymized before uploading.
                    </p>
                </section>

                <section className="mb-8">
                    <h2 className="text-xl font-semibold mb-3">3. Cookies</h2>
                    <p className="text-muted-foreground leading-relaxed">
                        This website does not use tracking cookies or analytics tools. Only technically necessary
                        data (such as session information) may be stored temporarily in your browser.
                    </p>
                </section>

                <section className="mb-8">
                    <h2 className="text-xl font-semibold mb-3">4. Third-Party Services</h2>
                    <p className="text-muted-foreground leading-relaxed">
                        This application does not integrate third-party tracking or advertising services.
                        However, uploaded CT scan data may be sent to external services for processing,
                        such as automated segmentation. No personal data beyond the uploaded scan files
                        is shared with these services.
                    </p>
                </section>

                <section className="mb-8">
                    <h2 className="text-xl font-semibold mb-3">5. Your Rights</h2>
                    <p className="text-muted-foreground leading-relaxed">
                        You have the right to request information about your stored data, as well as the right
                        to correction, deletion, and restriction of processing. Please contact the responsible
                        person listed in the imprint for any data-related inquiries.
                    </p>
                </section>

                <section className="mb-8">
                    <h2 className="text-xl font-semibold mb-3">6. Contact</h2>
                    <p className="text-muted-foreground leading-relaxed">
                        For questions regarding data protection, please refer to the contact details in the{" "}
                        <Link to="/imprint" className="text-primary underline hover:no-underline">
                            imprint
                        </Link>.
                    </p>
                </section>
            </main>

            <div className="text-center mb-8 mt-4">
                <Link to="/">
                    <Button>Back to Home</Button>
                </Link>
            </div>
        </Page>
    )
}

export default PrivacyPage
