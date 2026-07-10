// Client for the asynchronous ingestion API. Uploads stream one file per request so
// each carries its own byte-level progress; the worker's result is then polled.

export const ACCEPTED_TYPES = ['image/jpeg', 'image/png', 'image/webp'] as const
export const MAX_FILE_BYTES = 104_857_600 // 100 MB, mirrored from the server limit.

export type UploadFileStatus = 'accepted' | 'duplicate' | 'rejected'
export type UploadFileResult = { fileName: string; status: UploadFileStatus; jobId: string | null; reason: string | null }
export type UploadBatchResponse = { files: UploadFileResult[] }

export type UploadJobView = {
  id: string
  fileName: string
  status: 'Pending' | 'Processing' | 'Completed' | 'Failed' | 'Duplicate'
  imageId: string | null
  failureCode: string | null
  recoverable: boolean
  bytes: number
  createdAt: string
  updatedAt: string
}

async function csrf(): Promise<string> {
  return (await (await fetch('/api/auth/antiforgery')).json()).requestToken as string
}

/** Uploads a single file, reporting byte-upload progress (0–100) as it streams. */
export function uploadFile(file: File, token: string, onProgress: (percent: number) => void): Promise<UploadFileResult> {
  return new Promise((resolve, reject) => {
    const form = new FormData()
    form.append('files', file, file.name)
    const request = new XMLHttpRequest()
    request.open('POST', '/api/images/uploads')
    request.setRequestHeader('X-CSRF-TOKEN', token)
    request.upload.onprogress = (event) => {
      if (event.lengthComputable) onProgress(Math.round((event.loaded / event.total) * 100))
    }
    request.onload = () => {
      if (request.status >= 200 && request.status < 300) {
        const batch = JSON.parse(request.responseText) as UploadBatchResponse
        const result = batch.files[0]
        if (result) resolve(result)
        else reject(new Error('The upload returned no result.'))
      } else {
        reject(new Error(`The upload failed (${request.status}).`))
      }
    }
    request.onerror = () => reject(new Error('The upload could not reach the server.'))
    request.send(form)
  })
}

export async function fetchJobs(): Promise<UploadJobView[]> {
  const response = await fetch('/api/images/uploads')
  if (!response.ok) throw new Error(`Could not load upload progress (${response.status}).`)
  return (await response.json()).jobs as UploadJobView[]
}

export async function retryJob(id: string): Promise<void> {
  const response = await fetch(`/api/images/uploads/${id}/retry`, {
    method: 'POST',
    headers: { 'X-CSRF-TOKEN': await csrf() },
  })
  if (!response.ok) throw new Error(`Could not retry the upload (${response.status}).`)
}

export { csrf }
