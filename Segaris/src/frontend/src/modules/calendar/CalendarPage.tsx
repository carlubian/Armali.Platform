import {
  CalendarDays,
  Cake,
  ChevronLeft,
  ChevronRight,
  CircleDot,
  Contact,
  Layers,
  ListChecks,
  Luggage,
  Package,
  Plane,
  RotateCcw,
  StickyNote,
  Warehouse,
  Wrench,
  type LucideIcon,
} from 'lucide-react'
import { useMemo } from 'react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import {
  calendarIndicatorPriority,
  calendarSourceModules,
  calendarVisualFamilies,
  type CalendarEntry,
  type CalendarSourceModule,
  type CalendarVisualFamily,
} from '@/app/api/calendar'
import { isApiError } from '@/app/api/errors'
import { ServiceUnavailable } from '@/components/feedback/SystemScreens'
import { Badge, Button, IconButton, Spinner } from '@/components/ui'

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
import { useCalendarEntries } from './queries'

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
  const {
    state,
    setMonth,
    setDay,
    setSourceModules,
    setVisualFamilies,
  } = useCalendarState()
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
          <aside className="seg-cal__selected" aria-label={t('page.selectedDay')}>
            <div>
              <div className="armali-eyebrow">{t('page.selectedDay')}</div>
              <h2>{formatDayLabel(selectedDay, i18n.language)}</h2>
              <p>{t('grid.selectedHint')}</p>
            </div>
            {selectedEntries.length === 0 ? (
              <p className="seg-cal__selected-empty">{t('grid.selectedEmpty')}</p>
            ) : (
              <div className="seg-cal__selected-list">
                {selectedEntries.map((entry) => (
                  <EntrySummary key={entry.id} entry={entry} />
                ))}
              </div>
            )}
          </aside>
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
                  toggleAllowList(
                    calendarVisualFamilies,
                    selectedFamilies,
                    family,
                  ),
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

function EntrySummary({ entry }: { entry: CalendarEntry }) {
  const { t } = useTranslation('calendar')
  const Icon = familyIcons[entry.visualFamily]
  const SourceIcon = sourceIcons[entry.sourceModule]
  return (
    <article
      className={['seg-cal-entry', familyClasses[entry.visualFamily]].join(' ')}
    >
      <span className="seg-cal-entry__icon">
        <Icon size={16} />
      </span>
      <span className="seg-cal-entry__body">
        <strong>{entry.title}</strong>
        {entry.subtitle != null ? <span>{entry.subtitle}</span> : null}
        <small>
          <SourceIcon size={12} />
          {t(`sources.${entry.sourceModule}`)}
          {entry.status != null ? ` · ${entry.status}` : ''}
        </small>
      </span>
    </article>
  )
}
