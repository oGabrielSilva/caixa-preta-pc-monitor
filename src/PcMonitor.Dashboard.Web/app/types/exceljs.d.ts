// The browser build of ExcelJS has the same API as the package's typed entry point.
declare module 'exceljs/dist/exceljs.min.js' {
  import ExcelJS from 'exceljs'

  export default ExcelJS
}
