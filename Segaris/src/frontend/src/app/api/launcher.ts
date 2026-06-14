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
