import { Link } from "react-router-dom"

function NavBar() {
    return (
        <div
            className="sticky h-[64px] top-0 z-50 flex items-center justify-between px-6 border-b bg-card shadow"
        >
            <Link to="/" className="flex items-center gap-3">
                <img
                    src="/AR4CT.svg"
                    alt="AR4CT Logo"
                    className="h-9 object-contain"
                />
                <span className="text-xl font-bold tracking-tight">AR4CT</span>
            </Link>
            <nav className="flex items-center gap-4">
                <Link to="/" className="text-sm text-muted-foreground hover:text-foreground transition-colors">
                    Home
                </Link>
            </nav>
        </div>
    )
}

export default NavBar
