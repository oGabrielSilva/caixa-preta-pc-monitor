import { describe, expect, it } from 'vitest'
import { inlineParts, noteBlocks } from '~/utils/release-notes'

describe('noteBlocks', () => {
  it('reads the GitHub generated notes: headings, bullet lists and paragraphs', () => {
    const notes = '## What\'s Changed\n* Memory and disks by @dev in https://github.com/o/r/pull/1\n* Export\n\n**Full Changelog**: https://github.com/o/r/commits/v1.1.0'
    expect(noteBlocks(notes)).toEqual([
      { kind: 'heading', text: 'What\'s Changed' },
      { kind: 'list', items: ['Memory and disks by @dev in https://github.com/o/r/pull/1', 'Export'] },
      { kind: 'paragraph', text: 'Full Changelog: https://github.com/o/r/commits/v1.1.0' },
    ])
  })

  it('keeps HTML as plain text (the page escapes it)', () => {
    expect(noteBlocks('First release.\n\n<script>alert(1)</script>')).toEqual([
      { kind: 'paragraph', text: 'First release.' },
      { kind: 'paragraph', text: '<script>alert(1)</script>' },
    ])
  })

  it('gives nothing for empty notes', () => {
    expect(noteBlocks('  \n')).toEqual([])
  })
})

describe('inlineParts', () => {
  it('turns https links into anchors and keeps the rest as text', () => {
    expect(inlineParts('See https://github.com/o/r/pull/1 now')).toEqual([
      { text: 'See ' },
      { text: 'https://github.com/o/r/pull/1', href: 'https://github.com/o/r/pull/1' },
      { text: ' now' },
    ])
  })

  it('reads markdown links, and only http(s) ones', () => {
    expect(inlineParts('[PR 1](https://github.com/o/r/pull/1) and [x](javascript:alert(1))')).toEqual([
      { text: 'PR 1', href: 'https://github.com/o/r/pull/1' },
      { text: ' and [x](javascript:alert(1))' },
    ])
  })
})
