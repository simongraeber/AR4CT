import React from "react"

interface PageProps {
    children: React.ReactNode;
    backgroundElements?: React.ReactNode;
}

const Page: React.FC<PageProps> = ({ children, backgroundElements }) => {
    return (
        <div className="relative bg-gradient-to-b from-background to-muted
             text-foreground min-h-[calc(100vh-64px)] overflow-hidden">
            {backgroundElements && (
                <div className="absolute inset-0 z-0 pointer-events-none overflow-hidden">
                    {backgroundElements}
                </div>
            )}
            <div className="relative z-10 mx-auto px-4 py-6 sm:px-6 flex flex-col items-center justify-start max-w-screen-xl">
                {children}
            </div>
        </div>
    )
}

export default Page
