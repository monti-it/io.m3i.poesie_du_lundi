import { Route, Routes } from 'react-router-dom'
import { AdminPage } from '@modules/authoring/pages/AdminPage'
import { HomePage } from '@modules/reading/pages/HomePage'
import { PoemPage } from '@modules/reading/pages/PoemPage'
import { NotFoundPage } from './NotFoundPage'

function App() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/poems/:slug" element={<PoemPage />} />
      <Route path="/admin" element={<AdminPage />} />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}

export default App
