import { useQueryClient } from '@tanstack/react-query'
import {
  ArrowUpRight,
  CalendarDays,
  Cake,
  ChevronLeft,
  ChevronRight,
  CircleDot,
  Contact,
  Info,
  Layers,
  ListChecks,
  Luggage,
  Package,
  Pencil,
  Plane,
  Plus,
  RotateCcw,
  StickyNote,
  Warehouse,
  Wrench,
  type LucideIcon,
} from 'lucide-react'
import { useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'

import {
  calendarIndicatorPriority,
  calendarSourceModules,
  calendarVisualFamilies,
  type CalendarDailyNote,
  type CalendarEntry,
  type CalendarSourceModule,
  type CalendarVisualFamily,
} from '@/app/api/calendar'
import { isApiError } from '@/app/api/errors'
import { useSession } from '@/app/session/SessionContext'
import { ServiceUnavailable } from '@/components/feedback/SystemScreens'
import { Badge, Button, IconButton, Spinner, Toast, Tooltip } from '@/components/ui'

import {
  addCalendarMonths,
  calendarWeekdayLabels,
  formatCalendarMonth,
  formatCivilDate,
  getVisibleCalendarGrid,
  parseCivilDate,
  resolveCalendarMonth,
  toCalendarEntriesQuery,
  useCalendarState,
  type CalendarGridDay,
} from './calendarState'
import { CalendarNoteDialog, type CalendarNoteMutationKind } from './NoteDialog'
import { calendarKeys, useCalendarEntries } from './queries'

import './CalendarPage.css'

type EntryByDate = Map<string, CalendarEntry[]>

const familyClasses: Record<CalendarVisualFamily, string> = {
  Travel: 'seg-cal-fam--travel',
  Birthday: 'seg-cal-fam--birthday',
  Note: 'seg-cal-fam--note',
  Other: 'seg-cal-fam--other',
}

const familyIcons = {
  Travel: Plane,
  Birthday: Cake,
  Note: StickyNote,
  Other: CircleDot,
} satisfies Record<CalendarVisualFamily, LucideIcon>

const sourceIcons = {
  calendar: StickyNote,
  firebird: Contact,
  travel: Luggage,
  inventory: Package,
  assets: Warehouse,
  maintenance: Wrench,
  processes: ListChecks,
} satisfies Record<CalendarSourceModule, LucideIcon>

function civilDateInRange(date: string, start: string, end: string | null) {
  return date >= start && date <= (end ?? start)
}

function entriesByDay(entries: CalendarEntry[], days: CalendarGridDay[]): EntryByDate {
  return new Map(
    days.map((day) => [
      day.date,
      entries.filter((entry) =>
        civilDateInRange(day.date, entry.startDate, entry.endDate),
      ),
    ]),
  )
}

function orderedFamilies(entries: CalendarEntry[]) {
  const present = new Set(entries.map((entry) => entry.visualFamily))
  return calendarIndicatorPriority.filter((family) => present.has(family))
}

interface DayDetailGroup {
  family: CalendarVisualFamily
  entries: CalendarEntry[]
}

function groupEntriesByFamily(entries: CalendarEntry[]): DayDetailGroup[] {
  return calendarIndicatorPriority
    .map((family) => ({
      family,
      entries: entries.filter((entry) => entry.visualFamily === family),
    }))
    .filter((group) => group.entries.length > 0)
}

const dayInMs = 86_400_000

function tripDayInfo(entry: CalendarEntry, day: string) {
  const start = parseCivilDate(entry.startDate).getTime()
  const end = parseCivilDate(entry.endDate ?? entry.startDate).getTime()
  const current = parseCivilDate(day).getTime()
  const total = Math.round((end - start) / dayInMs) + 1
  const index = Math.round((current - start) / dayInMs) + 1
  return { current: Math.min(Math.max(index, 1), total), total }
}

function isInternalRoute(route: string | null): route is string {
  return route != null && route.startsWith('/')
}

const noteIdPattern = /^calendar:note:(\d+)$/

/**
 * The Calendar-owned daily note id encoded in a projected entry id, or `null`
 * for cross-module projections. Notes are the only entries the user can edit in
 * place, so this drives the day-detail edit affordance.
 */
function calendarNoteId(entry: CalendarEntry): number | null {
  if (entry.sourceModule !== 'calendar') return null
  const match = noteIdPattern.exec(entry.id)
  return match ? Number(match[1]) : null
}

function toggleAllowList<T extends string>(
  allValues: readonly T[],
  current: readonly T[],
  value: T,
) {
  const active = current.length === 0 ? [...allValues] : [...current]
  const next = active.includes(value)
    ? active.filter((item) => item !== value)
    : [...active, value]
  return next.length === allValues.length ? [] : next
}

function formatMonthLabel(month: string, language: string) {
  const date = parseCivilDate(`${month}-01`)
  return new Intl.DateTimeFormat(language, {
    month: 'long',
    year: 'numeric',
  }).format(date)
}

function formatDayLabel(date: string, language: string) {
  return new Intl.DateTimeFormat(language, {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  }).format(parseCivilDate(date))
}

function formatShortDayLabel(date: string, language: string) {
  return new Intl.DateTimeFormat(language, {
    day: 'numeric',
    month: 'short',
  }).format(parseCivilDate(date))
}

export function CalendarPage() {
  const { t, i18n } = useTranslation('calendar')
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { session } = useSession()
  const currentUserId = session?.userId ?? null
  const {
    state,
    dialog,
    setMonth,
    setDay,
    setSourceModules,
    setVisualFamilies,
    openCreateNote,
    openEditNote,
    closeDialog,
  } = useCalendarState()
  const [toast, setToast] = useState<CalendarNoteMutationKind | null>(null)
  const today = formatCivilDate(new Date())
  const currentMonth = resolveCalendarMonth(state.month)
  const days = useMemo(() => getVisibleCalendarGrid(currentMonth), [currentMonth])
  const visibleFrom = days[0]?.date ?? `${currentMonth}-01`
  const visibleTo = days.at(-1)?.date ?? visibleFrom
  const selectedDay = state.day ?? today
  const query = useMemo(
    () => toCalendarEntriesQuery(visibleFrom, visibleTo, state),
    [visibleFrom, visibleTo, state],
  )
  const entriesQuery = useCalendarEntries(query)
  const byDay = useMemo(
    () => entriesByDay(entriesQuery.data ?? [], days),
    [entriesQuery.data, days],
  )
  const selectedEntries = byDay.get(selectedDay) ?? []
  const activeFilters =
    state.filters.sourceModules.length + state.filters.visualFamilies.length

  // A note mutation can change month indicators and the day-detail list, both of
  // which derive from the entries query, and the cached note detail. Refreshing
  // the entries family and the touched note keeps every surface in sync.
  const handleNoteMutated = (
    kind: CalendarNoteMutationKind,
    note: CalendarDailyNote,
  ) => {
    if (kind === 'deleted') {
      queryClient.removeQueries({ queryKey: calendarKeys.note(note.id) })
    } else {
      queryClient.setQueryData(calendarKeys.note(note.id), note)
    }
    void queryClient.invalidateQueries({ queryKey: calendarKeys.entries() })
    setToast(kind)
    closeDialog()
  }

  if (entriesQuery.isError) {
    const error = entriesQuery.error
    if (isApiError(error) && ['unavailable', 'transient'].includes(error.kind)) {
      return <ServiceUnavailable onRetry={() => void entriesQuery.refetch()} />
    }
  }

  return (
    <main className="seg-cal armali-aurora">
      <section className="seg-cal__head">
        <div>
          <div className="armali-eyebrow">{t('page.eyebrow')}</div>
          <h1>{t('page.title')}</h1>
          <p>{t('page.description')}</p>
        </div>
        <div className="seg-cal__actions">
          <MonthNavigator
            label={formatMonthLabel(currentMonth, i18n.language)}
            onPrevious={() => setMonth(addCalendarMonths(currentMonth, -1))}
            onNext={() => setMonth(addCalendarMonths(currentMonth, 1))}
          />
          <Button
            variant="outline"
            iconLeft={<CalendarDays size={16} />}
            onClick={() => {
              setMonth(formatCalendarMonth(new Date()))
              setDay(today)
            }}
          >
            {t('page.today')}
          </Button>
          <Button iconLeft={<Plus size={16} />} onClick={openCreateNote}>
            {t('page.newNote')}
          </Button>
        </div>
      </section>

      <FilterBar
        selectedSources={state.filters.sourceModules}
        selectedFamilies={state.filters.visualFamilies}
        onSources={setSourceModules}
        onFamilies={setVisualFamilies}
        onClear={() => {
          setSourceModules([])
          setVisualFamilies([])
        }}
        activeFilters={activeFilters}
      />

      <section className="seg-cal__statusbar" aria-live="polite">
        <Badge tone="neutral">
          {t('page.visibleRange', {
            from: formatShortDayLabel(visibleFrom, i18n.language),
            to: formatShortDayLabel(visibleTo, i18n.language),
          })}
        </Badge>
        {entriesQuery.isFetching && !entriesQuery.isPending ? (
          <span className="seg-cal__refreshing">{t('grid.loading')}</span>
        ) : null}
      </section>

      {entriesQuery.isPending ? (
        <div className="seg-cal__loading">
          <Spinner />
          <span>{t('grid.loading')}</span>
        </div>
      ) : entriesQuery.isError ? (
        <div className="seg-cal__error" role="alert">
          <p>{t('grid.loadError')}</p>
          <Button variant="outline" onClick={() => void entriesQuery.refetch()}>
            {t('grid.retry')}
          </Button>
        </div>
      ) : (
        <div className="seg-cal__layout">
          <MonthGrid
            days={days}
            entries={byDay}
            selectedDay={selectedDay}
            today={today}
            language={i18n.language}
            onSelect={setDay}
          />
          <aside
            className="seg-cal__selected"
            aria-label={t('detail.label', {
              day: formatDayLabel(selectedDay, i18n.language),
            })}
          >
            <div className="seg-cal__selected-head">
              <div className="armali-eyebrow">{t('page.selectedDay')}</div>
              <h2>{formatDayLabel(selectedDay, i18n.language)}</h2>
            </div>
            {selectedEntries.length === 0 ? (
              <div className="seg-cal__selected-empty">
                <p>{t('detail.emptyHint')}</p>
                <Button
                  variant="outline"
                  size="sm"
                  iconLeft={<Plus size={15} />}
                  onClick={openCreateNote}
                >
                  {t('detail.addNote')}
                </Button>
              </div>
            ) : (
              <div className="seg-cal__selected-list">
                {groupEntriesByFamily(selectedEntries).map((group) => (
                  <DayDetailGroupSection
                    key={group.family}
                    group={group}
                    selectedDay={selectedDay}
                    onOpenRoute={(route) => void navigate(route)}
                    onEditNote={openEditNote}
                  />
                ))}
                <Button
                  variant="outline"
                  size="sm"
                  className="seg-cal__add-note"
                  iconLeft={<Plus size={15} />}
                  onClick={openCreateNote}
                >
                  {t('detail.addNoteToDay')}
                </Button>
              </div>
            )}
          </aside>
        </div>
      )}

      {dialog.mode !== 'closed' && (
        <CalendarNoteDialog
          mode={dialog.mode === 'createNote' ? 'create' : 'edit'}
          noteId={dialog.mode === 'editNote' ? dialog.noteId : undefined}
          defaultDate={selectedDay}
          currentUserId={currentUserId}
          onClose={closeDialog}
          onMutated={handleNoteMutated}
        />
      )}

      {toast != null && (
        <div className="seg-cal__toast">
          <Toast
            tone="success"
            title={t(`toast.${toast}`)}
            onClose={() => setToast(null)}
            closeLabel={t('editor.actions.close')}
          />
        </div>
      )}
    </main>
  )
}

interface MonthNavigatorProps {
  label: string
  onPrevious: () => void
  onNext: () => void
}

function MonthNavigator({ label, onPrevious, onNext }: MonthNavigatorProps) {
  const { t } = useTranslation('calendar')
  return (
    <div className="seg-cal__nav">
      <IconButton
        variant="bare"
        size="sm"
        label={t('page.previousMonth')}
        icon={<ChevronLeft size={18} />}
        onClick={onPrevious}
      />
      <span>{label}</span>
      <IconButton
        variant="bare"
        size="sm"
        label={t('page.nextMonth')}
        icon={<ChevronRight size={18} />}
        onClick={onNext}
      />
    </div>
  )
}

interface FilterBarProps {
  selectedSources: CalendarSourceModule[]
  selectedFamilies: CalendarVisualFamily[]
  activeFilters: number
  onSources: (sources: readonly CalendarSourceModule[]) => void
  onFamilies: (families: readonly CalendarVisualFamily[]) => void
  onClear: () => void
}

function FilterBar({
  selectedSources,
  selectedFamilies,
  activeFilters,
  onSources,
  onFamilies,
  onClear,
}: FilterBarProps) {
  const { t } = useTranslation('calendar')
  return (
    <section className="seg-cal__filters">
      <FilterGroup icon={<Layers size={14} />} label={t('filters.family')}>
        {calendarVisualFamilies.map((family) => {
          const selected =
            selectedFamilies.length === 0 || selectedFamilies.includes(family)
          const Icon = familyIcons[family]
          return (
            <button
              key={family}
              type="button"
              className={[
                'seg-cal-toggle',
                familyClasses[family],
                selected ? 'is-on' : '',
              ]
                .filter(Boolean)
                .join(' ')}
              aria-pressed={selected}
              aria-label={t('filters.familyToggle', {
                label: t(`families.${family}`),
              })}
              onClick={() =>
                onFamilies(
                  toggleAllowList(calendarVisualFamilies, selectedFamilies, family),
                )
              }
            >
              <Icon size={14} />
              <span>{t(`families.${family}`)}</span>
            </button>
          )
        })}
      </FilterGroup>
      <FilterGroup icon={<CircleDot size={14} />} label={t('filters.source')}>
        {calendarSourceModules.map((source) => {
          const selected =
            selectedSources.length === 0 || selectedSources.includes(source)
          const Icon = sourceIcons[source]
          return (
            <button
              key={source}
              type="button"
              className={['seg-cal-toggle', selected ? 'is-on' : '']
                .filter(Boolean)
                .join(' ')}
              aria-pressed={selected}
              aria-label={t('filters.sourceToggle', {
                label: t(`sources.${source}`),
              })}
              onClick={() =>
                onSources(
                  toggleAllowList(calendarSourceModules, selectedSources, source),
                )
              }
            >
              <Icon size={14} />
              <span>{t(`sources.${source}`)}</span>
            </button>
          )
        })}
      </FilterGroup>
      {activeFilters > 0 ? (
        <Button
          variant="ghost"
          size="sm"
          iconLeft={<RotateCcw size={14} />}
          onClick={onClear}
        >
          {t('filters.clear')}
        </Button>
      ) : null}
    </section>
  )
}

function FilterGroup({
  icon,
  label,
  children,
}: {
  icon: ReactNode
  label: string
  children: ReactNode
}) {
  return (
    <div className="seg-cal__filter-group">
      <span className="seg-cal__filter-label">
        {icon}
        {label}
      </span>
      <div className="seg-cal__filter-options">{children}</div>
    </div>
  )
}

interface MonthGridProps {
  days: CalendarGridDay[]
  entries: EntryByDate
  selectedDay: string
  today: string
  language: string
  onSelect: (day: string) => void
}

function MonthGrid({
  days,
  entries,
  selectedDay,
  today,
  language,
  onSelect,
}: MonthGridProps) {
  const { t } = useTranslation('calendar')
  return (
    <section className="seg-cal-gridcard" aria-label={t('grid.label')}>
      <div className="seg-cal-dow" aria-hidden="true">
        {calendarWeekdayLabels.map((day, index) => (
          <div
            key={day}
            className={['seg-cal-dow__cell', index >= 5 ? 'is-weekend' : '']
              .filter(Boolean)
              .join(' ')}
          >
            {day}
          </div>
        ))}
      </div>
      <div className="seg-cal-grid">
        {days.map((day) => {
          const dayEntries = entries.get(day.date) ?? []
          return (
            <DayCell
              key={day.date}
              day={day}
              entries={dayEntries}
              selected={selectedDay === day.date}
              today={today === day.date}
              label={t('grid.dayLabel', {
                date: formatDayLabel(day.date, language),
                count: dayEntries.length,
              })}
              onSelect={() => onSelect(day.date)}
            />
          )
        })}
      </div>
    </section>
  )
}

interface DayCellProps {
  day: CalendarGridDay
  entries: CalendarEntry[]
  selected: boolean
  today: boolean
  label: string
  onSelect: () => void
}

function DayCell({ day, entries, selected, today, label, onSelect }: DayCellProps) {
  const { t } = useTranslation('calendar')
  const families = orderedFamilies(entries)
  const shownFamilies = families.slice(0, 3)
  const hiddenCount = Math.max(0, families.length - shownFamilies.length)
  const dateNumber = parseCivilDate(day.date).getDate()

  return (
    <button
      type="button"
      className={[
        'seg-cal-cell',
        !day.inMonth ? 'is-out' : '',
        selected ? 'is-selected' : '',
        today ? 'is-today' : '',
      ]
        .filter(Boolean)
        .join(' ')}
      aria-label={label}
      aria-current={today ? 'date' : undefined}
      aria-pressed={selected}
      onClick={onSelect}
    >
      <span className="seg-cal-cell__head">
        <span className="seg-cal-cell__num">{dateNumber}</span>
        {entries.length > 0 ? (
          <span className="seg-cal-cell__count">{entries.length}</span>
        ) : null}
      </span>
      <span className="seg-cal-cell__indicators">
        {shownFamilies.map((family) => {
          const entry = entries.find((item) => item.visualFamily === family)
          return (
            <span
              key={family}
              className={['seg-cal-dot', familyClasses[family]].join(' ')}
              aria-label={t('indicators.family', {
                family: t(`families.${family}`),
                title: entry?.title ?? t(`families.${family}`),
              })}
            />
          )
        })}
        {hiddenCount > 0 ? (
          <span
            className="seg-cal-dot seg-cal-dot--more"
            aria-label={t('indicators.more', { count: hiddenCount })}
          >
            {t('grid.more', { count: hiddenCount })}
          </span>
        ) : null}
      </span>
    </button>
  )
}

interface DayDetailGroupProps {
  group: DayDetailGroup
  selectedDay: string
  onOpenRoute: (route: string) => void
  onEditNote: (noteId: number) => void
}

function DayDetailGroupSection({
  group,
  selectedDay,
  onOpenRoute,
  onEditNote,
}: DayDetailGroupProps) {
  const { t } = useTranslation('calendar')
  const Icon = familyIcons[group.family]
  return (
    <section
      className={['seg-cal-group', familyClasses[group.family]].join(' ')}
      aria-label={t(`families.${group.family}`)}
    >
      <header className="seg-cal-group__head">
        <span className="seg-cal-group__icon">
          <Icon size={14} />
        </span>
        <span className="seg-cal-group__lbl">{t(`families.${group.family}`)}</span>
        <span
          className="seg-cal-group__n"
          aria-label={t('detail.groupCount', { count: group.entries.length })}
        >
          {group.entries.length}
        </span>
      </header>
      <div className="seg-cal-group__items">
        {group.entries.map((entry) => (
          <DayDetailEntry
            key={entry.id}
            entry={entry}
            selectedDay={selectedDay}
            onOpenRoute={onOpenRoute}
            onEditNote={onEditNote}
          />
        ))}
      </div>
    </section>
  )
}

interface DayDetailEntryProps {
  entry: CalendarEntry
  selectedDay: string
  onOpenRoute: (route: string) => void
  onEditNote: (noteId: number) => void
}

function DayDetailEntry({
  entry,
  selectedDay,
  onOpenRoute,
  onEditNote,
}: DayDetailEntryProps) {
  const { t } = useTranslation('calendar')
  const Icon = familyIcons[entry.visualFamily]
  const SourceIcon = sourceIcons[entry.sourceModule]
  const sourceLabel = t(`sources.${entry.sourceModule}`)
  const noteId = calendarNoteId(entry)
  const navigable = noteId == null && isInternalRoute(entry.targetRoute)
  const trip =
    entry.visualFamily === 'Travel' && entry.endDate != null
      ? tripDayInfo(entry, selectedDay)
      : null

  const content = (
    <>
      <span className="seg-cal-item__icon">
        <Icon size={16} />
      </span>
      <span className="seg-cal-item__body">
        <span className="seg-cal-item__title">{entry.title}</span>
        {entry.subtitle != null ? (
          <span className="seg-cal-item__sub">{entry.subtitle}</span>
        ) : null}
        <span className="seg-cal-item__meta">
          <span className="seg-cal-item__src">
            <SourceIcon size={12} />
            {sourceLabel}
          </span>
          {trip != null ? (
            <>
              <span className="seg-cal-item__sep" aria-hidden="true" />
              <span className="seg-cal-item__span">{t('detail.tripDay', trip)}</span>
            </>
          ) : null}
          {entry.status != null ? (
            <>
              <span className="seg-cal-item__sep" aria-hidden="true" />
              <span className="seg-cal-item__status">{entry.status}</span>
            </>
          ) : null}
        </span>
      </span>
    </>
  )

  const className = [
    'seg-cal-item',
    familyClasses[entry.visualFamily],
    navigable || noteId != null ? 'is-navigable' : 'is-info',
  ].join(' ')

  if (noteId != null) {
    return (
      <button
        type="button"
        className={className}
        aria-label={t('detail.editNote', { title: entry.title })}
        onClick={() => onEditNote(noteId)}
      >
        {content}
        <span className="seg-cal-item__chev" aria-hidden="true">
          <Pencil size={15} />
        </span>
      </button>
    )
  }

  if (navigable) {
    return (
      <button
        type="button"
        className={className}
        aria-label={t('detail.open', { title: entry.title, source: sourceLabel })}
        onClick={() => onOpenRoute(entry.targetRoute as string)}
      >
        {content}
        <span className="seg-cal-item__chev" aria-hidden="true">
          <ArrowUpRight size={15} />
        </span>
      </button>
    )
  }

  return (
    <div className={className}>
      {content}
      <Tooltip
        className="seg-cal-item__chev"
        label={t('detail.informational')}
        side="top"
      >
        <Info size={15} aria-label={t('detail.informational')} />
      </Tooltip>
    </div>
  )
}
