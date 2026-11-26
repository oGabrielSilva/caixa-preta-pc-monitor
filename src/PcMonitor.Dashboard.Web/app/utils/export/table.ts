/** A table to export: headers and rows of text, numbers or missing values. */
export interface ExportTable {
  readonly headers: readonly string[]
  readonly rows: readonly (readonly (string | number | null)[])[]
}

/** A table with a sheet name, for the Excel workbook. */
export interface ExportSheet extends ExportTable {
  readonly name: string
}
