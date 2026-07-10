import { fileURLToPath, URL } from 'node:url'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

const target = new URL(process.env.BLACKWING_FRONTEND_PROXY_TARGET ?? 'http://localhost:5054').origin
export default defineConfig({ plugins: [react()], resolve: { alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) } }, server: { proxy: { '/api': { target, changeOrigin: false } } }, test: { environment: 'jsdom', setupFiles: ['./src/test/setup.ts'], include: ['src/**/*.test.{ts,tsx}'] } })
