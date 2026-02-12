import { BrowserRouter as Router } from "react-router-dom"
import NavBar from "@/components/shared/Navbar"
import Footer from "@/components/shared/Footer"
import RoutesComponent from "@/components/shared/Routes"
import ScrollToTop from "@/components/shared/ScrollToTop"

function App() {
  return (
    <Router>
      <ScrollToTop />
      <div className="flex min-h-screen w-full flex-col">
        <NavBar />
        <div className="flex-grow overflow-auto">
          <RoutesComponent />
        </div>
        <Footer />
      </div>
    </Router>
  )
}

export default App
