import { useState, useEffect } from 'react'
import { Button } from '@/components/ui/button'

function App() {
  const [message, setMessage] = useState<string>('')
  const [loading, setLoading] = useState(false)

  const fetchHello = async () => {
    setLoading(true)
    try {
      const response = await fetch('http://localhost:8000/api/hello')
      const data = await response.json()
      setMessage(data.message)
    } catch (error) {
      setMessage('Failed to connect to server')
    }
    setLoading(false)
  }

  useEffect(() => {
    fetchHello()
  }, [])

  return (
    <div className="min-h-screen flex flex-col items-center justify-center gap-6 p-8">
      <h1 className="text-4xl font-bold">AR4CT Web App</h1>
      <p className="text-muted-foreground">
        {loading ? 'Loading...' : message || 'Click the button to fetch from API'}
      </p>
      <Button onClick={fetchHello} disabled={loading}>
        {loading ? 'Loading...' : 'Refresh from API'}
      </Button>
    </div>
  )
}

export default App
