// Thin fetch wrapper over the Belfalas API. Vite proxies `/api` and `/health`
// to the backend in dev (see vite.config.ts); in the container both are served
// from the same origin.

const API_BASE = "/api";

export class ApiError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

async function parseJson<T>(response: Response): Promise<T> {
  const text = await response.text();
  return text ? (JSON.parse(text) as T) : (undefined as T);
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    headers: { "Content-Type": "application/json", ...init?.headers },
    ...init,
  });

  if (!response.ok) {
    throw new ApiError(response.status, `${init?.method ?? "GET"} ${path} → ${response.status}`);
  }

  return parseJson<T>(response);
}

/**
 * GET that treats a 404 as "absent" rather than an error — used for resources
 * that simply do not exist until an era is active (quests, progression, world).
 */
export async function getOrNull<T>(path: string): Promise<T | null> {
  try {
    return await request<T>(path);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      return null;
    }
    throw error;
  }
}

export function get<T>(path: string): Promise<T> {
  return request<T>(path);
}

export function post<T>(path: string, body?: unknown): Promise<T> {
  return request<T>(path, { method: "POST", body: body === undefined ? undefined : JSON.stringify(body) });
}

export function put<T>(path: string, body?: unknown): Promise<T> {
  return request<T>(path, { method: "PUT", body: body === undefined ? undefined : JSON.stringify(body) });
}

export function del<T>(path: string): Promise<T> {
  return request<T>(path, { method: "DELETE" });
}
