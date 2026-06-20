import { describe, expect, it } from 'vitest'

import { pageItems } from './pagination'

describe('pageItems', () => {
  it('lists every page without gaps when there are seven or fewer', () => {
    expect(pageItems(1, 1)).toEqual([1])
    expect(pageItems(3, 7)).toEqual([1, 2, 3, 4, 5, 6, 7])
  })

  it('caps the trailing gap near the start', () => {
    expect(pageItems(2, 20)).toEqual([1, 2, 3, '…', 20])
  })

  it('shows gaps on both sides in the middle', () => {
    expect(pageItems(10, 20)).toEqual([1, '…', 9, 10, 11, '…', 20])
  })

  it('caps the leading gap near the end', () => {
    expect(pageItems(19, 20)).toEqual([1, '…', 18, 19, 20])
  })
})
