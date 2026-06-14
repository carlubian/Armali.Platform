import { apiRequest } from './client'

/** A single module's attention state from `GET /api/launcher/attention`. */
export interface ModuleAttention {
  module: string
  requiresAttention: boolean
}

export interface LauncherAttentionResponse {
  modules: ModuleAttention[]
}

export const launcherApi = {
  attention: (signal?: AbortSignal) =>
    apiRequest<LauncherAttentionResponse>('/api/launcher/attention', { signal }),
}

/**
 * Shared TanStack Query keys for launcher attention. They live here, with the
 * launcher API, so the launcher screen reads attention and business modules can
 * invalidate it after mutations without depending on each other.
 */
export const launcherKeys = {
  all: ['launcher'] as const,
  attention: () => [...launcherKeys.all, 'attention'] as const,
}
