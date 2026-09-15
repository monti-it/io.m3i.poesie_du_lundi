import type { ReactNode } from 'react'

export function EmptyState({ children }: { children: ReactNode }) {
  return (
    <p role="status" className="empty-state">
      {children}
    </p>
  )
}
