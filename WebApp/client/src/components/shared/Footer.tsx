import { Link } from "react-router-dom"

function Footer() {
    return (
        <div className="w-full h-16 z-0 flex items-center justify-center p-4 border-t bg-card shadow">
            <Link to="/imprint" className="text-sm text-muted-foreground hover:text-foreground p-4 transition-colors">
                Imprint
            </Link>
            <Link to="/privacy" className="text-sm text-muted-foreground hover:text-foreground p-4 transition-colors">
                Privacy Policy
            </Link>
        </div>
    )
}

export default Footer
