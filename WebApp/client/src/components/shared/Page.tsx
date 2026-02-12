import React from "react"

interface PageProps {
    children: React.ReactNode;
}

const Page: React.FC<PageProps> = ({ children }) => {
    return (
        <div className="bg-gradient-to-b from-background to-muted
             text-foreground min-h-[calc(100vh-64px)] overflow-hidden">
            <div className="relative mx-auto px-4 py-6 sm:px-6 flex flex-col items-center justify-start max-w-screen-xl">
                {children}
            </div>
        </div>
    )
}

export default Page
