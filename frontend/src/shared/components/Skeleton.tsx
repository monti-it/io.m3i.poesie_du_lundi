import type { ReactNode } from 'react'

// A block-shaped loading placeholder, not real Markdown/prose rendering — reading pages show this
// while their query is pending.
export function Skeleton({ lines = 3 }: { lines?: number }) {
  return (
    <div role="status" aria-label="Chargement">
      {Array.from({ length: lines }, (_, index) => (
        <p key={index} className="skeleton-line" />
      ))}
    </div>
  )
}

export function PageSkeleton({ children }: { children?: ReactNode }) {
  return (
    <section>
      <Skeleton lines={1} />
      <Skeleton lines={4} />
      {children}
    </section>
  )
}
