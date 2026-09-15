import { Route, Routes } from 'react-router-dom'
import { AdminPage } from '@modules/authoring/pages/AdminPage'
import { ArchivePage } from '@modules/reading/pages/ArchivePage'
import { HomePage } from '@modules/reading/pages/HomePage'
import { PoemPage } from '@modules/reading/pages/PoemPage'
import { SeriesPage } from '@modules/reading/pages/SeriesPage'
import { Layout } from '@shared/components/Layout'
import { NotFoundPage } from './NotFoundPage'

function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/" element={<HomePage />} />
        <Route path="/poems/:slug" element={<PoemPage />} />
        <Route path="/archive" element={<ArchivePage />} />
        <Route path="/series/:slug" element={<SeriesPage />} />
      </Route>
      <Route path="/admin" element={<AdminPage />} />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}

export default App
