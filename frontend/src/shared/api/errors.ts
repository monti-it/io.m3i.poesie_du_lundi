// Shared by both api clients — mirrors the backend's RFC 7807 problem+json failure shape
// (docs/ENGINEERING_PRACTICES.md "Result pattern for expected failures").
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
}

export class ApiError extends Error {
  /** HTTP status of the failing response. */
  readonly status: number
  /** The raw problem body, for consumers that need more than `message`. */
  readonly problem: ProblemDetails | undefined

  constructor(message: string, status = 0, problem?: ProblemDetails) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

export const isNotFound = (error: unknown): boolean => error instanceof ApiError && error.status === 404

