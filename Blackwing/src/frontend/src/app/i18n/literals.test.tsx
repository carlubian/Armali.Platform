import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import { adminSession, defaultUsers, mockBackend } from '@/test/backend'

import { platform } from './resources'

/**
 * No component may contain a visible literal string.
 *
 * This walks the rendered application and asserts that every piece of text a
 * user can read or hear — text nodes plus the `aria-label`, `title`, `alt` and
 * `placeholder` attributes that become accessible names — is a value declared in
 * the translation resources. A hardcoded label fails here, not in review.
 */
function leafValues(value: object): string[] {
  return Object.values(value).flatMap((child) =>
    typeof child === 'string' ? [child] : leafValues(child as object),
  )
}

const translations = leafValues(platform)

function escapeForRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
}

/**
 * Interpolated resources such as `{{count}} imágenes` render with real values,
 * so they are matched as patterns rather than compared literally.
 */
const patterns = translations
  .filter((value) => value.includes('{{'))
  .map(
    (value) =>
      new RegExp(`^${escapeForRegExp(value).replace(/\\\{\\\{.+?\\\}\\\}/g, '.+')}$`),
  )

const exactValues = new Set(translations)

function isTranslated(text: string): boolean {
  return exactValues.has(text) || patterns.some((pattern) => pattern.test(text))
}

/** Every non-blank text node in the document, trimmed. */
function visibleTexts(): { text: string; where: string }[] {
  const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT)
  const found: { text: string; where: string }[] = []
  let node = walker.nextNode()
  while (node !== null) {
    const text = node.textContent?.trim() ?? ''
    const parent = node.parentElement
    const tag = parent?.tagName.toLowerCase() ?? ''
    if (text !== '' && tag !== 'script' && tag !== 'style') {
      found.push({ text, where: `<${tag}>` })
    }
    node = walker.nextNode()
  }
  return found
}

/** Every attribute value that assistive technology reads out as text. */
function announcedTexts(): { text: string; where: string }[] {
  const attributes = ['aria-label', 'title', 'alt', 'placeholder']
  return [...document.body.querySelectorAll('*')].flatMap((element) =>
    attributes.flatMap((attribute) => {
      const value = element.getAttribute(attribute)?.trim() ?? ''
      if (value === '') return []
      return [
        {
          text: value,
          where: `<${element.tagName.toLowerCase()} ${attribute}>`,
        },
      ]
    }),
  )
}

/**
 * Screens that show stored data render values no resource file can contain — an
 * account's display name, for instance. Those exact values are declared by the
 * caller, so anything else still fails: the guarantee is unchanged, it is only
 * told which strings came from the backend fixture.
 */
function expectEverythingTranslated(dataValues: readonly string[] = []) {
  const data = new Set(dataValues)
  for (const { text, where } of [...visibleTexts(), ...announcedTexts()]) {
    expect(
      isTranslated(text) || data.has(text),
      `untranslated literal ${JSON.stringify(text)} in ${where}`,
    ).toBe(true)
  }
}

/** Display names of the fixture accounts, the only stored text these screens show. */
const accountNames = [
  adminSession.displayName,
  ...defaultUsers().map((user) => user.displayName),
]

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/')
})

afterEach(() => vi.restoreAllMocks())

describe('user-facing text comes from i18next', () => {
  it('has no literal strings on the login screen', async () => {
    mockBackend({ session: null })
    render(<App />)
    await screen.findByRole('heading', { name: platform.auth.login.title })
    expect(visibleTexts().length).toBeGreaterThan(5)
    expectEverythingTranslated()
  })

  it('has no literal strings on the boot screen', async () => {
    mockBackend({ session: adminSession })
    render(<App />)
    await screen.findByText(platform.startup.cardTitle)
    // Guard against a vacuous pass if the tree failed to render.
    expect(visibleTexts().length).toBeGreaterThan(5)
    expectEverythingTranslated(accountNames)
  })

  it('has no literal strings after interacting with the boot screen', async () => {
    const user = userEvent.setup()
    mockBackend({ session: adminSession })
    render(<App />)
    await screen.findByText(platform.startup.cardTitle)
    await user.click(screen.getByRole('button', { name: platform.startup.action }))
    expectEverythingTranslated(accountNames)
  })

  it('has no literal strings on the account administration screen', async () => {
    const user = userEvent.setup()
    mockBackend({ session: adminSession })
    render(<App />)

    await user.click(
      await screen.findByRole('link', { name: platform.shell.nav.admin.label }),
    )
    await screen.findAllByRole('listitem')
    expectEverythingTranslated(accountNames)

    // And in its dialogs, which never render until they are opened.
    await user.click(screen.getByRole('button', { name: platform.admin.users.newUser }))
    const dialog = await screen.findByRole('dialog', {
      name: platform.admin.users.create.title,
    })
    expect(within(dialog).getAllByRole('button').length).toBeGreaterThan(1)
    expectEverythingTranslated(accountNames)
  })
})
