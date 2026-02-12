import { Link } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { CloudOff } from "lucide-react"
import Page from "@/components/shared/Page"

function NotFound() {
    return (
        <Page>
            <CloudOff className="w-32 h-32 mt-16 text-muted-foreground" />
            <p className="font-light text-4xl pt-8 text-destructive">
                Page Not Found
            </p>
            <p className="text-xl pt-2 text-muted-foreground">Error 404</p>
            <Link to="/">
                <Button className="mt-8">
                    Go Home
                </Button>
            </Link>
        </Page>
    )
}

export default NotFound
