/**
 * Visually hidden prose summary of a chart. Pairs with the `role="img"`
 * label on the chart body so screen-reader users get the same headline numbers
 * a sighted user reads from the bars without depending on hover tooltips.
 */
export function AnalyticsChartSummary({ children }: { children: string }) {
  return <p className="an-sr">{children}</p>
}
