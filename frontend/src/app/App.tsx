import { Route, Routes } from 'react-router-dom'
import { PoemEditorPage } from '@modules/authoring/pages/PoemEditorPage'
import { PoemListPage } from '@modules/authoring/pages/PoemListPage'
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
      <Route path="/admin" element={<PoemListPage />} />
      <Route path="/admin/poems/new" element={<PoemEditorPage />} />
      <Route path="/admin/poems/:id" element={<PoemEditorPage />} />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}

export default App
