// Shared by both api clients — mirrors the backend's actual failure body, the bare
// `{ "error": "..." }` ErrorDto (api/src/PoesieDuLundi/Api/ResultExtensions.cs), a deliberately
// simpler shape than RFC 7807 problem+json for this app's one small admin surface
// (docs/ENGINEERING_PRACTICES.md "Result pattern for expected failures"). `title`/`detail` stay
// optional so a future problem+json body (or a generic `Results.Problem(...)`, e.g. /healthz)
// still extracts a message.
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  error?: string
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

