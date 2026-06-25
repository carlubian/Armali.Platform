export interface AnalyticsDataTableModel {
  caption: string
  columns: string[]
  /** Rows of pre-formatted cells; the first cell is the row header. */
  rows: string[][]
}

/**
 * Accessible table equivalent for a chart. Tooltips are never the only way to
 * read a value, so every chart can toggle to this table of the same numbers.
 */
export function AnalyticsDataTable({
  caption,
  columns,
  rows,
}: AnalyticsDataTableModel) {
  return (
    <div className="an-tablewrap">
      <table className="an-table">
        <caption>{caption}</caption>
        <thead>
          <tr>
            {columns.map((column, index) => (
              <th key={index} scope="col">
                {column}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, rowIndex) => (
            <tr key={rowIndex}>
              {row.map((cell, cellIndex) =>
                cellIndex === 0 ? (
                  <th key={cellIndex} scope="row">
                    {cell}
                  </th>
                ) : (
                  <td key={cellIndex}>{cell}</td>
                ),
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
