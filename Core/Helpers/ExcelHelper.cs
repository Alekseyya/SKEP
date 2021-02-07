using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Core.Helpers
{
    public class ExcelCellStyle
    {
        public enum CellValueDataFormat
        {
            General = 0,
            Integer = (int)ExcelHelper.ExcelPredefinedFormatNumber.IntegerWithSeparator,
            Decimal = (int)ExcelHelper.ExcelPredefinedFormatNumber.Precision2WithSeparator, //165,
            ForcedText = (int)ExcelHelper.ExcelPredefinedFormatNumber.Text,
            DateTime = 167,
            Date = (int)ExcelHelper.ExcelPredefinedFormatDateTime.DayMonthYear4WithSlashes
        }

        public enum CellFillColor
        {
            None = 0,
            Gray125 = 1,
            LightGray = 2,
            Yellow = 3,
            Red = 4,
            Header = 5
        }

        public enum CellFontColor
        {
            General = 0,
            //Red = 1 //не реализовано
        }

        public CellValueDataFormat DataFormat = CellValueDataFormat.General;
        public CellFillColor FillColor = CellFillColor.None;
        public CellFontColor FontColor = CellFontColor.General;

        public bool FontBold = false;
        public bool FontItalic = false; //не реализовано
        public bool FontUnderline = false; //не реализовано

        public override bool Equals(object obj)
        {
            ExcelCellStyle otherObj = (ExcelCellStyle)obj;
            return otherObj.DataFormat == this.DataFormat
                && otherObj.FillColor == this.FillColor
                && otherObj.FontColor == this.FontColor
                && otherObj.FontBold == this.FontBold
                && otherObj.FontItalic == this.FontItalic
                && otherObj.FontUnderline == this.FontUnderline;
        }

        public override int GetHashCode()
        {
            var hashCode = 1406006575;
            hashCode = hashCode * -1521134295 + DataFormat.GetHashCode();
            hashCode = hashCode * -1521134295 + FillColor.GetHashCode();
            hashCode = hashCode * -1521134295 + FontColor.GetHashCode();
            hashCode = hashCode * -1521134295 + FontBold.GetHashCode();
            hashCode = hashCode * -1521134295 + FontItalic.GetHashCode();
            hashCode = hashCode * -1521134295 + FontUnderline.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(ExcelCellStyle op1, ExcelCellStyle op2)
        {
            if (object.ReferenceEquals(op1, null) || object.ReferenceEquals(op2, null))
                return (object.ReferenceEquals(op1, null) && object.ReferenceEquals(op2, null));
            else
                return op1.Equals(op2);

        }
        public static bool operator !=(ExcelCellStyle op1, ExcelCellStyle op2)
        {
            return !(op1 == op2);
        }
    }

    public class ExcelCell
    {
        public ExcelCellStyle Style = null;
        public int ColumnSpan = 1;
        public int RowSpan = 1;
        public object Value = null;
    }
    public class ExcelDataTable
    {
        public DataTable Data { get; set; }
        public UInt32 DataStartRowIndex { get; private set; }
        public UInt32 DataStartColumnIndex { get; private set; }

        public ExcelDataTable(DataTable data, UInt32 rowId, UInt32 colId)
        {
            Data = data;
            DataStartRowIndex = rowId;
            DataStartColumnIndex = colId;
        }
    }

    public delegate void WorksheetGenerate(Worksheet worksheet, SheetData sheetData, MergeCells mergeCells, AutoFilter autoFilter);
    public class ExcelHelper
    {

        public static int ExcelPredefinedFormatGeneral { get { return 0; } }

        public enum ExcelPredefinedFormatNumber : uint
        {
            /// <summary>
            /// General
            /// </summary>
            General = 0,

            /// <summary>
            /// 0
            /// </summary>
            Integer = 1,

            /// <summary>
            /// 0.00
            /// </summary>
            Precision2 = 2,

            /// <summary>
            /// #,##0
            /// </summary>
            IntegerWithSeparator = 3,

            /// <summary>
            /// #,##0.00
            /// </summary>
            Precision2WithSeparator = 4,

            /// <summary>
            /// 0%
            /// </summary>
            PercentInteger = 9,

            /// <summary>
            /// 0.00%
            /// </summary>
            PercentPrecision2 = 10,

            /// <summary>
            /// 0.00E+00
            /// </summary>
            ScientificPrecision2 = 11,

            /// <summary>
            /// # ?/?
            /// </summary>
            FractionPrecision1 = 12,

            /// <summary>
            /// # ??/??
            /// </summary>
            FractionPrecision2 = 13,

            /// <summary>
            /// #,##0 ,(#,##0)
            /// </summary>
            IntegerWithSeparatorAndParens = 37,

            /// <summary>
            /// #,##0 ,[Red](#,##0)
            /// </summary>
            IntegerWithSeparatorAndParensRed = 38,

            /// <summary>
            /// #,##0.00,(#,##0.00)
            /// </summary>
            Precision2WithSeparatorAndParens = 39,

            /// <summary>
            /// #,##0.00,[Red](#,##0.00)
            /// </summary>
            Precision2WithSeparatorAndParensRed = 40,

            /// <summary>
            /// ##0.0E+0
            /// </summary>
            ScientificUpToHundredsAndPrecision1 = 48,

            /// <summary>
            /// @
            /// </summary>
            Text = 49
        }

        public enum ExcelPredefinedFormatDateTime : uint
        {
            /// <summary>
            /// General
            /// </summary>
            General = 0U,

            /// <summary>
            /// d/m/yyyy
            /// </summary>
            DayMonthYear4WithSlashes = 14U,

            /// <summary>
            /// d-mmm-yy
            /// </summary>
            DayMonthAbbrYear2WithDashes = 15U,

            /// <summary>
            /// d-mmm
            /// </summary>
            DayMonthAbbrWithDash = 16U,

            /// <summary>
            /// mmm-yy
            /// </summary>
            MonthAbbrYear2WithDash = 17U,

            /// <summary>
            /// h:mm tt
            /// </summary>
            Hour12MinutesAmPm = 18U,

            /// <summary>
            /// h:mm:ss tt
            /// </summary>
            Hour12MinutesSecondsAmPm = 19U,

            /// <summary>
            /// H:mm
            /// </summary>
            Hour24Minutes = 20U,

            /// <summary>
            /// H:mm:ss
            /// </summary>
            Hour24MinutesSeconds = 21U,

            /// <summary>
            /// m/d/yyyy H:mm
            /// </summary>
            MonthDayYear4WithDashesHour24Minutes = 22U,

            /// <summary>
            /// mm:ss
            /// </summary>
            MinutesSeconds = 45U,

            /// <summary>
            /// [h]:mm:ss
            /// </summary>
            Hour12MinutesSeconds = 46U,

            /// <summary>
            /// mmss.0
            /// </summary>
            MinutesSecondsMillis1 = 47U,

            /// <summary>
            /// @
            /// </summary>
            Text = 49U
        }

        private static IDictionary<int, string> _excelPredefinedFormatCodes;

        internal static IDictionary<int, string> ExcelPredefinedFormatCodes
        {
            get
            {
                if (_excelPredefinedFormatCodes == null)
                {
                    var fCodes = new Dictionary<int, string>
                    {
                        {0, string.Empty},
                        {1, "0"},
                        {2, "0.00"},
                        {3, "#,##0"},
                        {4, "#,##0.00"},
                        {7, "$#,##0.00_);($#,##0.00)"},
                        {9, "0%"},
                        {10, "0.00%"},
                        {11, "0.00E+00"},
                        {12, "# ?/?"},
                        {13, "# ??/??"},
                        {14, "M/d/yyyy"},
                        {15, "d-MMM-yy"},
                        {16, "d-MMM"},
                        {17, "MMM-yy"},
                        {18, "h:mm tt"},
                        {19, "h:mm:ss tt"},
                        {20, "H:mm"},
                        {21, "H:mm:ss"},
                        {22, "M/d/yyyy H:mm"},
                        {37, "#,##0 ;(#,##0)"},
                        {38, "#,##0 ;[Red](#,##0)"},
                        {39, "#,##0.00;(#,##0.00)"},
                        {40, "#,##0.00;[Red](#,##0.00)"},
                        {45, "mm:ss"},
                        {46, "[h]:mm:ss"},
                        {47, "mmss.0"},
                        {48, "##0.0E+0"},
                        {49, "@"}
                    };
                    _excelPredefinedFormatCodes = fCodes;
                }

                return _excelPredefinedFormatCodes;
            }
        }

        private static List<(UInt32Value styleIndex, ExcelCellStyle cellStyle)> _excelCellStyles;

        private static List<(UInt32Value styleIndex, ExcelCellStyle cellStyle)> ExcelCellStyles
        {
            get
            {
                if (_excelCellStyles == null)
                {

                    var styles = new List<(UInt32Value styleIndex, ExcelCellStyle cellStyle)>
                    {
                        (0U, new ExcelCellStyle() { DataFormat = ExcelCellStyle.CellValueDataFormat.General, FillColor = ExcelCellStyle.CellFillColor.None} ),

                        (1U, null ),    //cellFormats.Append(headerCellFormat); //1U
                        (2U, null ),    //cellFormats.Append(titleCellFormat); //2U
                    };

                    UInt32Value index = UInt32Value.FromUInt32(Convert.ToUInt32(styles.Count));

                    foreach (bool fontBold in new[] { false, true })
                    {
                        //foreach (bool fontItalic in new[] { false, true })
                        {
                            //foreach (bool fontUnderline in new[] { false, true })
                            {
                                //foreach(ExcelCellStyle.CellFontColor fontColor in Enum.GetValues(typeof(ExcelCellStyle.CellFontColor)))
                                {
                                    foreach (ExcelCellStyle.CellFillColor fillColor in Enum.GetValues(typeof(ExcelCellStyle.CellFillColor)))
                                    {
                                        foreach (ExcelCellStyle.CellValueDataFormat dataFormat in Enum.GetValues(typeof(ExcelCellStyle.CellValueDataFormat)))
                                        {
                                            ExcelCellStyle cellStyle = new ExcelCellStyle()
                                            {
                                                DataFormat = dataFormat,
                                                FillColor = fillColor,
                                                //FontColor = fontColor,

                                                FontBold = fontBold,
                                                //FontItalic = fontItalic,
                                                //FontUnderline = fontUnderline
                                            };

                                            var s = styles.Where(ecs => ecs.cellStyle != null && ecs.cellStyle == cellStyle).FirstOrDefault();

                                            if (s == default((UInt32Value styleIndex, ExcelCellStyle cellStyle)))
                                            {
                                                styles.Add((index++, cellStyle));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    _excelCellStyles = styles;
                }

                return _excelCellStyles;
            }
        }

        protected static UInt32Value GetStyleIndexByExcelCellStyle(ExcelCellStyle cellStyle)
        {
            UInt32Value styleIndex = 0U;

            var styleInfo = ExcelCellStyles.Where(ecs => ecs.cellStyle != null && ecs.cellStyle == cellStyle).FirstOrDefault();

            if (styleInfo != default((UInt32Value styleIndex, ExcelCellStyle cellStyle)))
            {
                styleIndex = styleInfo.styleIndex;
            }

            return styleIndex;
        }


        protected static bool IsHiddenDataColumn(DataColumn dtColumn)
        {
            bool result = false;
            switch (dtColumn.ColumnName)
            {
                case "_ISGROUPROW_":
                    result = true;
                    break;
                default:
                    result = false;
                    break;
            }

            return result;
        }

        protected static bool IsLightGrayRow(DataRow dtRow)
        {
            bool result = false;
            try
            {
                if (dtRow.Table.Columns.Contains("_ISGROUPROW_") == true
                    && dtRow["_ISGROUPROW_"] != null
                    && String.IsNullOrEmpty(dtRow["_ISGROUPROW_"].ToString()) == false
                    && Convert.ToBoolean(dtRow["_ISGROUPROW_"]) == true)
                {
                    result = true;
                }
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

        protected static uint GetMaxSubCaptionIndexForDataColumn(DataColumn dtColumn)
        {
            uint maxSubCaptionIndex = 0;

            for (uint i = 0; i < 10; i++)
            {
                if (dtColumn.ExtendedProperties.ContainsKey("SubCaption" + (i + 1).ToString()) == true)
                {
                    maxSubCaptionIndex = i + 1;
                }
            }

            return maxSubCaptionIndex;
        }

        protected static uint GetSubCaptionColumnSpanForDataColumn(DataColumn dtColumn,
            uint subCaptionIndex)
        {
            uint subCaptionColumnSpan = 0;

            try
            {
                if (subCaptionIndex > 0
                    && dtColumn.ExtendedProperties.ContainsKey("SubCaptionColumnSpan" + subCaptionIndex))
                {
                    subCaptionColumnSpan = Convert.ToUInt32(dtColumn.ExtendedProperties["SubCaptionColumnSpan" + subCaptionIndex]);
                }
            }
            catch (Exception)
            {
                subCaptionColumnSpan = 0;
            }

            return subCaptionColumnSpan;
        }

        protected static string GetSubCaptionForDataColumn(DataColumn dtColumn,
            uint subCaptionIndex)
        {
            string subCaption = "";

            try
            {
                if (subCaptionIndex > 0
                    && dtColumn.ExtendedProperties.ContainsKey("SubCaption" + subCaptionIndex))
                {
                    subCaption = dtColumn.ExtendedProperties["SubCaption" + subCaptionIndex].ToString();
                }
            }
            catch (Exception)
            {
                subCaption = "";
            }

            return subCaption;
        }

        public static DataTable ExportColumnsAndData(DataTable dt, Stream sheetStream)
        {
            try
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(sheetStream, false))
                {
                    WorkbookPart workbookPart = doc.WorkbookPart;

                    SharedStringTable sst = null;

                    SharedStringTablePart sstpart = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
                    if (sstpart != null)
                    {
                        sst = sstpart.SharedStringTable;
                    }

                    WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
                    Worksheet worksheet = worksheetPart.Worksheet;

                    var rows = worksheet.Descendants<Row>();

                    foreach (var cell in rows.First().Elements<Cell>())
                    {
                        //Название пустых заголовков называются A1, B1, G1  и т.д
                        if (cell.DataType == null)
                            dt.Columns.Add(cell.CellReference.InnerText, typeof(string)).Caption = cell.CellReference.InnerText;
                        else
                        {//Наименование столбцов берется из документа с не пустыми ячейками
                            int ssId = int.Parse(cell.CellValue.Text);
                            dt.Columns.Add(sst.ChildElements[ssId].InnerText, typeof(string)).Caption = cell.CellReference.InnerText;
                        }
                    }

                    dt = ExportDataInternalWithHeaders(dt, worksheet, sst);
                }
            }
            catch (Exception)
            {

            }

            return dt;
        }

        public static DataTable ExportColumnsAndDataBySheetName(DataTable dt, Stream sheetStream, string sheetName)
        {
            try
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(sheetStream, false))
                {
                    WorkbookPart workbookPart = doc.WorkbookPart;

                    SharedStringTable sst = null;

                    SharedStringTablePart sstpart = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
                    if (sstpart != null)
                    {
                        sst = sstpart.SharedStringTable;
                    }

                    var relId = workbookPart.Workbook.Descendants<Sheet>().First(x => sheetName.Equals(x.Name)).Id;
                    WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(relId);
                    Worksheet worksheet = worksheetPart.Worksheet;

                    var rows = worksheet.Descendants<Row>();

                    foreach (var cell in rows.First().Elements<Cell>())
                    {
                        //Название пустых заголовков называются A1, B1, G1  и т.д
                        if (cell.DataType == null)
                            dt.Columns.Add(cell.CellReference.InnerText, typeof(string)).Caption = cell.CellReference.InnerText;
                        else
                        {//Наименование столбцов берется из документа с не пустыми ячейками
                            int ssId = int.Parse(cell.CellValue.Text);
                            dt.Columns.Add(sst.ChildElements[ssId].InnerText, typeof(string)).Caption = cell.CellReference.InnerText;
                        }
                    }

                    dt = ExportDataInternalWithHeaders(dt, worksheet, sst);
                }
            }
            catch (Exception)
            {

            }

            return dt;
        }

        public static DataTable ExportData(DataTable dt, Stream sheetStream)
        {
            try
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(sheetStream, false))
                {
                    WorkbookPart workbookPart = doc.WorkbookPart;

                    SharedStringTable sst = null;

                    SharedStringTablePart sstpart = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
                    if (sstpart != null)
                    {
                        sst = sstpart.SharedStringTable;
                    }

                    WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
                    Worksheet worksheet = worksheetPart.Worksheet;


                    dt = ExportDataInternal(dt, worksheet, sst);
                }
            }
            catch (Exception)
            {

            }

            return dt;
        }

        public static DataTable ExportDataBySheetName(DataTable dt, Stream sheetStream, string sheetName)
        {
            try
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(sheetStream, false))
                {
                    WorkbookPart workbookPart = doc.WorkbookPart;

                    SharedStringTable sst = null;

                    SharedStringTablePart sstpart = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
                    if (sstpart != null)
                    {
                        sst = sstpart.SharedStringTable;
                    }

                    var relId = workbookPart.Workbook.Descendants<Sheet>().First(x => sheetName.Equals(x.Name)).Id;
                    WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(relId);
                    Worksheet worksheet = worksheetPart.Worksheet;

                    dt = ExportDataInternal(dt, worksheet, sst);
                }
            }
            catch (Exception)
            {

            }

            return dt;
        }

        private static DataTable ExportDataInternalWithHeaders(DataTable dt, Worksheet worksheet, SharedStringTable sst)
        {
            if (dt != null && worksheet != null)
            {
                var rows = worksheet.Descendants<Row>();

                int rowNum = 0;
                foreach (Row row in rows)
                {
                    DataRow dataRow = dt.NewRow();

                    int colNum = 0;

                    foreach (Cell c in row.Elements<Cell>())
                    {
                        string strCellValue = "";

                        if (c.CellValue != null)
                        {
                            if ((c.DataType != null) && (c.DataType == CellValues.SharedString)
                                && sst != null)
                            {
                                int ssid = int.Parse(c.CellValue.Text);
                                strCellValue = sst.ChildElements[ssid].InnerText;

                            }
                            else
                            {
                                strCellValue = c.CellValue.Text;
                            }
                        }
                        else if ((c.DataType != null) && (c.DataType == CellValues.InlineString))
                        {
                            Text t = c.InlineString.Descendants<Text>().FirstOrDefault();

                            if (t != null)
                            {
                                strCellValue = t.Text;
                            }
                        }

                        if (String.IsNullOrEmpty(strCellValue) == false
                            && dt.Columns.Count > colNum)
                        {
                            DataColumn dataColumn = dt.Columns[colNum];

                            if (dataColumn != null && dataColumn.DataType != null)
                            {
                                if (dataColumn.DataType.Equals(typeof(DateTime)) == true)
                                {
                                    if (String.IsNullOrEmpty(strCellValue) == false)
                                    {
                                        dataRow[colNum] = DateTime.FromOADate(Convert.ToInt32(strCellValue));
                                    }
                                }
                                else if (dataColumn.DataType.Equals(typeof(double)) == true)
                                {
                                    if (String.IsNullOrEmpty(strCellValue) == false)
                                    {
                                        dataRow[colNum] = Convert.ToDouble(strCellValue.Replace(".", ",").Replace(" ", ""));
                                    }
                                }
                                else
                                {
                                    dataRow[colNum] = Convert.ChangeType(strCellValue, dataColumn.DataType);
                                }
                            }
                        }

                        colNum++;
                    }

                    dt.Rows.Add(dataRow);
                    rowNum++;
                }
            }

            return dt;
        }
        private static DataTable ExportDataInternal(DataTable dt, Worksheet worksheet, SharedStringTable sst)
        {
            if (dt != null && worksheet != null)
            {
                var cells = worksheet.Descendants<Cell>();
                var rows = worksheet.Descendants<Row>();

                int rowNum = 0;
                foreach (Row row in rows)
                {
                    if (rowNum > 0)
                    {
                        DataRow dataRow = dt.NewRow();

                        int colNum = 0;

                        foreach (Cell c in row.Elements<Cell>())
                        {
                            string strCellValue = "";

                            if (c.CellValue != null)
                            {
                                if ((c.DataType != null) && (c.DataType == CellValues.SharedString)
                                    && sst != null)
                                {
                                    int ssid = int.Parse(c.CellValue.Text);
                                    strCellValue = sst.ChildElements[ssid].InnerText;

                                }
                                else
                                {
                                    strCellValue = c.CellValue.Text;
                                }
                            }
                            else if ((c.DataType != null) && (c.DataType == CellValues.InlineString))
                            {
                                Text t = c.InlineString.Descendants<Text>().FirstOrDefault();

                                if (t != null)
                                {
                                    strCellValue = t.Text;
                                }
                            }

                            if (String.IsNullOrEmpty(strCellValue) == false
                                && dt.Columns.Count > colNum)
                            {
                                DataColumn dataColumn = dt.Columns[colNum];

                                if (dataColumn != null && dataColumn.DataType != null)
                                {
                                    if (dataColumn.DataType.Equals(typeof(DateTime)) == true)
                                    {
                                        if (String.IsNullOrEmpty(strCellValue) == false)
                                        {
                                            dataRow[colNum] = DateTime.FromOADate(Convert.ToInt32(strCellValue));
                                        }
                                    }
                                    else if (dataColumn.DataType.Equals(typeof(double)) == true)
                                    {
                                        if (String.IsNullOrEmpty(strCellValue) == false)
                                        {
                                            dataRow[colNum] = Convert.ToDouble(strCellValue.Replace(".", ",").Replace(" ", ""));
                                        }
                                    }
                                    else
                                    {
                                        dataRow[colNum] = Convert.ChangeType(strCellValue, dataColumn.DataType);
                                    }
                                }
                            }

                            colNum++;
                        }

                        dt.Rows.Add(dataRow);
                    }

                    rowNum++;
                }
            }

            return dt;
        }

        /// <summary>
        /// Импорт таблицы в Excell
        /// </summary>
        /// <param name="dt">Таблица</param>
        /// <param name="worksheet">Объект Worksheetт</param>
        /// <param name="sheetData">Объект SheetData</param>
        /// <param name="startRowIndex">Начальный номер строки</param>
        /// <param name="startColumnIndex">Начальный номер колонки</param>
        protected static void ImportDataTable(System.Data.DataTable dt,
            Worksheet worksheet,
            SheetData sheetData,
            UInt32 startRowIndex,
            UInt32 startColumnIndex,
            MergeCells mergeCells,
            AutoFilter autoFilter)
        {
            DataRowCollection dtRows = dt.Rows;
            DataColumnCollection dtColumns = dt.Columns;
            Int32 rCount = dtRows.Count;
            Int32 cCount = dtColumns.Count;
            uint headerRowsCount = 1;
            uint maxSubCaptionIndex = 0;

            Columns columns = new Columns();
            UInt32 cIndex = startColumnIndex;

            for (uint j = 0; j < cCount; j++)
            {
                string columnCaption = "";
                DataColumn dtColumn = dtColumns[(int)j];

                if (IsHiddenDataColumn(dtColumn) == false)
                {
                    uint maxSubCaptionIndexForDataColumn = GetMaxSubCaptionIndexForDataColumn(dtColumn);
                    if (maxSubCaptionIndexForDataColumn > maxSubCaptionIndex)
                    {
                        maxSubCaptionIndex = maxSubCaptionIndexForDataColumn;
                    }

                    if (String.IsNullOrEmpty(dtColumn.Caption) == false)
                    {
                        columnCaption = dtColumn.Caption;
                    }
                    else
                    {
                        columnCaption = dtColumn.ColumnName;
                    }

                    double columnWidth = GetTextWidth("Arial", 8, columnCaption) * 2;
                    if (dtColumn.ExtendedProperties != null
                        && dtColumn.ExtendedProperties.ContainsKey("Width") == true
                        && dtColumn.ExtendedProperties["Width"] != null)
                    {
                        columnWidth = (double)dtColumn.ExtendedProperties["Width"];
                    }
                    Column column = new Column() { Min = (UInt32Value)cIndex, Max = (UInt32Value)cIndex, Width = columnWidth, CustomWidth = true };
                    columns.Append(column);
                    cIndex++;
                }
            }
            worksheet.Append(columns);

            if (maxSubCaptionIndex > 0)
            {
                headerRowsCount += maxSubCaptionIndex;
            }

            UInt32 rIndex = startRowIndex;
            for (uint i = 0; i < headerRowsCount; i++)
            {
                Row headerRow = new Row() { RowIndex = (UInt32Value)rIndex };
                cIndex = startColumnIndex;
                for (uint j = 0; j < cCount; j++)
                {
                    DataColumn dtColumn = dtColumns[(int)j];

                    if (IsHiddenDataColumn(dtColumn) == false)
                    {
                        uint maxSubCaptionIndexForDataColumn = GetMaxSubCaptionIndexForDataColumn(dtColumn);

                        Cell cell = null;

                        string columnCaption = "";

                        if (i < maxSubCaptionIndexForDataColumn)
                        {
                            columnCaption = GetSubCaptionForDataColumn(dtColumn, i + 1);
                        }
                        else if (i == maxSubCaptionIndexForDataColumn)
                        {
                            if (String.IsNullOrEmpty(dtColumns[(int)j].Caption) == false)
                            {
                                columnCaption = dtColumns[(int)j].Caption;
                            }
                            else
                            {
                                columnCaption = dtColumns[(int)j].ColumnName;
                            }
                        }
                        else
                        {
                            columnCaption = "";
                        }

                        cell = CreateHeaderCell(rIndex, cIndex, columnCaption);
                        headerRow.Append(cell);
                        cIndex++;
                    }

                }
                sheetData.Append(headerRow);
                rIndex++;
            }

            if (headerRowsCount != 1
                && mergeCells != null)
            {
                cIndex = startColumnIndex;
                for (uint j = 0; j < cCount; j++)
                {
                    DataColumn dtColumn = dtColumns[(int)j];

                    if (IsHiddenDataColumn(dtColumn) == false)
                    {
                        uint maxSubCaptionIndexForDataColumn = GetMaxSubCaptionIndexForDataColumn(dtColumn);
                        for (uint k = 0; k < maxSubCaptionIndexForDataColumn; k++)
                        {
                            uint subCaptionColumnSpanForDataColumn = GetSubCaptionColumnSpanForDataColumn(dtColumn, k + 1);
                            if (subCaptionColumnSpanForDataColumn > 1)
                            {
                                var mergeHeaderGroupCell = new MergeCell()
                                {
                                    Reference = GetRangeByNumber(cIndex, startRowIndex + k)
                                    + ":"
                                    + GetRangeByNumber(cIndex + subCaptionColumnSpanForDataColumn - 1, startRowIndex + k)
                                };

                                mergeCells.Append(mergeHeaderGroupCell);
                            }
                        }

                        if (headerRowsCount - 1 - maxSubCaptionIndexForDataColumn > 0)
                        {
                            var mergeHeaderCell = new MergeCell()
                            {
                                Reference = GetRangeByNumber(cIndex, startRowIndex)
                                    + ":"
                                    + GetRangeByNumber(cIndex, startRowIndex + headerRowsCount - 1 - maxSubCaptionIndexForDataColumn)
                            };

                            mergeCells.Append(mergeHeaderCell);
                        }

                        cIndex++;
                    }
                }

            }

            uint cIndexMax = 0;
            uint rIndexMax = 0;
            rIndex = startRowIndex + headerRowsCount;
            for (uint i = 0; i < rCount; i++)
            {
                DataRow dtRow = dtRows[(int)i];
                var row = new Row() { RowIndex = (UInt32Value)rIndex };
                bool lightGray = IsLightGrayRow(dtRow);
                cIndex = startColumnIndex;
                for (uint j = 0; j < cCount; j++)
                {
                    DataColumn dtColumn = dtColumns[(int)j];

                    Object dtRowValue = dtRow[dtColumn];
                    if (IsHiddenDataColumn(dtColumn) == false)
                    {
                        if (null != dtRowValue)
                        {
                            Cell cell = null;
                            var cellStyle = new ExcelCellStyle();

                            cellStyle.FillColor = (lightGray == true) ? ExcelCellStyle.CellFillColor.LightGray : ExcelCellStyle.CellFillColor.None;
                            cellStyle.FontBold = lightGray;

                            object cellValue = null;
                            Type columnType = null;

                            if (dtColumn.DataType == typeof(ExcelCell))
                            {
                                if (dtRowValue.Equals(DBNull.Value) == false)
                                {
                                    cellValue = ((ExcelCell)dtRowValue).Value;
                                    cellStyle = ((ExcelCell)dtRowValue).Style;

                                    columnType = ((ExcelCell)dtRowValue).Value?.GetType();

                                    if (((ExcelCell)dtRowValue).RowSpan > 1
                                        || ((ExcelCell)dtRowValue).ColumnSpan > 1)
                                    {
                                        MergeCell mergeCell = new MergeCell()
                                        {
                                            Reference = GetRangeByNumber(cIndex, rIndex)
                                            + ":"
                                            + GetRangeByNumber(cIndex + (uint)((ExcelCell)dtRowValue).ColumnSpan - 1, rIndex + (uint)((ExcelCell)dtRowValue).RowSpan - 1)
                                        };

                                        mergeCells.Append(mergeCell);
                                    }
                                }
                            }
                            else
                            {
                                columnType = dtColumn.DataType;

                                if (dtRowValue.Equals(DBNull.Value) == false)
                                {
                                    cellValue = dtRowValue;
                                }
                            }

                            if (columnType == Type.GetType("System.Int32"))
                            {
                                cellStyle.DataFormat = ExcelCellStyle.CellValueDataFormat.Integer;
                                cell = CreateCellInteger(rIndex, cIndex, (int?)cellValue, cellStyle);
                            }
                            else if (columnType == Type.GetType("System.DateTime"))
                            {
                                if (cellValue == null)
                                {
                                    cellStyle.DataFormat = ExcelCellStyle.CellValueDataFormat.ForcedText;
                                    cell = CreateCellDateTime(rIndex, cIndex, null, cellStyle);
                                }
                                else
                                {
                                    DateTime? dateTimeValue = Convert.ToDateTime(cellValue);

                                    if (dtColumn.ExtendedProperties["SPDateTimeFieldFormatType_DisplayFormat"] != null
                                        /*&& dtColumn.ExtendedProperties["SPDateTimeFieldFormatType_DisplayFormat"].Equals(SPDateTimeFieldFormatType.DateOnly.ToString())*/)
                                    {
                                        cellStyle.DataFormat = ExcelCellStyle.CellValueDataFormat.Date;
                                        cell = CreateCellDateTime(rIndex, cIndex, dateTimeValue.Value.Date, cellStyle);
                                    }
                                    else
                                    {
                                        cellStyle.DataFormat = (dateTimeValue == null || (dateTimeValue != null && dateTimeValue.HasValue == true && dateTimeValue.Value.Date.Equals(dateTimeValue) == true)) ? ExcelCellStyle.CellValueDataFormat.Date : ExcelCellStyle.CellValueDataFormat.DateTime;
                                        cell = CreateCellDateTime(rIndex, cIndex, dateTimeValue, cellStyle);
                                    }
                                }
                            }
                            else if (columnType == Type.GetType("System.Double"))
                            {
                                cellStyle.DataFormat = ExcelCellStyle.CellValueDataFormat.Decimal;
                                cell = CreateCellDouble(rIndex, cIndex, (double?)cellValue, cellStyle);
                            }
                            else if (columnType == typeof(decimal))
                            {
                                cellStyle.DataFormat = ExcelCellStyle.CellValueDataFormat.Decimal;
                                cell = CreateCellDecimal(rIndex, cIndex, (decimal?)cellValue, cellStyle);
                            }
                            else
                            {
                                cellStyle.DataFormat = ExcelCellStyle.CellValueDataFormat.ForcedText;
                                cell = CreateCellText(rIndex, cIndex, (cellValue != null) ? cellValue.ToString() : "", cellStyle);
                            }
                            row.Append(cell);
                            if (cIndexMax < cIndex)
                            {
                                cIndexMax = cIndex;
                            }
                            cIndex++;
                        }
                    }
                }
                sheetData.Append(row);
                if (rIndexMax < rIndex)
                {
                    rIndexMax = rIndex;
                }
                rIndex++;

            }

            if (rCount != 0 && autoFilter != null)
            {
                autoFilter.Reference = GetRangeByNumber(startColumnIndex, startRowIndex + headerRowsCount - 1)
                        + ":"
                        + GetRangeByNumber(cIndexMax, rIndexMax);
            }
        }
        /// <summary>
        /// Применение форматов
        /// </summary>
        /// <param name="workbookPart">Объект WorkbookPart</param>
        /// <param name="id">ИД</param>
        /// <returns></returns>
        protected static List<(UInt32Value styleIndex, ExcelCellStyle cellStyle)> ApplyStylesheet(WorkbookPart workbookPart)
        {
            List<(UInt32Value styleIndex, ExcelCellStyle cellStyle)> stylesList = new List<(UInt32Value styleIndex, ExcelCellStyle cellStyle)>();

            if (workbookPart.WorkbookStylesPart == null)
            {
                WorkbookStylesPart workbookStylesPart = workbookPart.AddNewPart<WorkbookStylesPart>("rId100");

                Stylesheet stylesheet = new Stylesheet() { MCAttributes = new MarkupCompatibilityAttributes() { Ignorable = "x14ac" } };
                stylesheet.AddNamespaceDeclaration("mc", "http://schemas.openxmlformats.org/markup-compatibility/2006");
                stylesheet.AddNamespaceDeclaration("x14ac", "http://schemas.microsoft.com/office/spreadsheetml/2009/9/ac");

                /*Шрифты*/
                Fonts fonts = new Fonts() { Count = (UInt32Value)3U, KnownFonts = true };

                DocumentFormat.OpenXml.Spreadsheet.Font font = new DocumentFormat.OpenXml.Spreadsheet.Font();
                FontSize fontSize = new FontSize() { Val = 8D };
                DocumentFormat.OpenXml.Spreadsheet.Color color = new DocumentFormat.OpenXml.Spreadsheet.Color() { Theme = (UInt32Value)1U };
                FontName fontName = new FontName() { Val = "Arial" };
                FontFamilyNumbering fontFamilyNumbering = new FontFamilyNumbering() { Val = 2 };
                //FontCharSet fontCharSet = new FontCharSet() { Val = 204 };
                //FontScheme fontScheme = new FontScheme() { Val = FontSchemeValues.Minor };

                font.Append(fontSize);
                font.Append(color);
                font.Append(fontName);
                font.Append(fontFamilyNumbering);
                //font.Append(fontCharSet);
                //font.Append(fontScheme);


                DocumentFormat.OpenXml.Spreadsheet.Font boldFont = new DocumentFormat.OpenXml.Spreadsheet.Font();
                Bold boldBoldFont = new Bold();
                FontSize boldFontSize = new FontSize() { Val = 8D };
                DocumentFormat.OpenXml.Spreadsheet.Color boldFontColor = new DocumentFormat.OpenXml.Spreadsheet.Color() { Theme = (UInt32Value)1U };
                FontName boldFontName = new FontName() { Val = "Arial" };
                FontFamilyNumbering boldFontFamilyNumbering = new FontFamilyNumbering() { Val = 2 };
                FontCharSet boldFontCharSet = new FontCharSet() { Val = 204 };
                //FontScheme fontScheme = new FontScheme() { Val = FontSchemeValues.Minor };

                boldFont.Append(boldBoldFont);
                boldFont.Append(boldFontSize);
                boldFont.Append(boldFontColor);
                boldFont.Append(boldFontName);
                boldFont.Append(boldFontFamilyNumbering);
                boldFont.Append(boldFontCharSet);

                DocumentFormat.OpenXml.Spreadsheet.Font titleFont = new DocumentFormat.OpenXml.Spreadsheet.Font();
                Bold boldTitleFont = new Bold();
                FontSize titleFontSize = new FontSize() { Val = 12D };
                DocumentFormat.OpenXml.Spreadsheet.Color titleFontColor = new DocumentFormat.OpenXml.Spreadsheet.Color() { Theme = (UInt32Value)1U };
                FontName titleFontName = new FontName() { Val = "Arial" };
                FontFamilyNumbering titleFontFamilyNumbering = new FontFamilyNumbering() { Val = 2 };
                FontCharSet titleFontCharSet = new FontCharSet() { Val = 204 };

                titleFont.Append(boldTitleFont);
                titleFont.Append(titleFontSize);
                titleFont.Append(titleFontColor);
                titleFont.Append(titleFontName);
                titleFont.Append(titleFontFamilyNumbering);
                titleFont.Append(titleFontCharSet);

                fonts.Append(font); //0U
                fonts.Append(boldFont); //1U

                fonts.Append(titleFont); //2U
                /*************/
                /*Заливка*/
                Fills fills = new Fills() { Count = (UInt32Value)2U };

                Fill fillNone = new Fill();
                PatternFill patternFillNone = new PatternFill() { PatternType = PatternValues.None };
                fillNone.Append(patternFillNone);

                Fill fillGray125 = new Fill();
                PatternFill patternFillGray125 = new PatternFill() { PatternType = PatternValues.Gray125 };
                fillGray125.Append(patternFillGray125);

                Fill fillLightGray = new Fill();
                PatternFill patternFillLightGray = new PatternFill() { PatternType = PatternValues.Solid };
                ForegroundColor patternFillLightGrayForeColor = new ForegroundColor() { Theme = (UInt32Value)4U, Tint = 0.59999389629810485D }; //{ Rgb = "FFE0E0E0" };
                BackgroundColor patternFillLightGrayBkgColor = new BackgroundColor() { };
                patternFillLightGray.Append(patternFillLightGrayForeColor);
                patternFillLightGray.Append(patternFillLightGrayBkgColor);
                fillLightGray.Append(patternFillLightGray);

                Fill fillYellow = new Fill();
                PatternFill patternFillYellow = new PatternFill() { PatternType = PatternValues.Solid };
                ForegroundColor patternFillYellowForeColor = new ForegroundColor() { Rgb = "FFFFFF00" };
                BackgroundColor patternFillYellowBkgColor = new BackgroundColor() { };
                patternFillYellow.Append(patternFillYellowForeColor);
                patternFillYellow.Append(patternFillYellowBkgColor);
                fillYellow.Append(patternFillYellow);

                Fill fillRed = new Fill();
                PatternFill patternFillRed = new PatternFill() { PatternType = PatternValues.Solid };
                ForegroundColor patternFillRedForeColor = new ForegroundColor() { Rgb = "FFFF0000" };
                BackgroundColor patternFillRedBkgColor = new BackgroundColor() { };
                patternFillRed.Append(patternFillRedForeColor);
                patternFillRed.Append(patternFillRedBkgColor);
                fillRed.Append(patternFillRed);

                Fill fillHeader = new Fill();
                PatternFill patternFillHeader = new PatternFill() { PatternType = PatternValues.Solid };
                ForegroundColor patternFillHeaderForeColor = new ForegroundColor() { Theme = (UInt32Value)0U, Tint = -4.9989318521683403E-2D };
                BackgroundColor patternFillHeaderBkgColor = new BackgroundColor() { };
                patternFillHeader.Append(patternFillHeaderForeColor);
                patternFillHeader.Append(patternFillHeaderBkgColor);
                fillHeader.Append(patternFillHeader);

                fills.Append(fillNone); //0U
                fills.Append(fillGray125); //1U
                fills.Append(fillLightGray); //2U
                fills.Append(fillYellow); //3U
                fills.Append(fillRed); //4U
                fills.Append(fillHeader); //5U

                /*************/
                /*Границы*/
                Borders borders = new Borders() { Count = (UInt32Value)2U };

                Border emptyBorder = new Border();
                LeftBorder leftEmptyBorder = new LeftBorder();
                RightBorder rightEmptyBorder = new RightBorder();
                TopBorder topEmptyBorder = new TopBorder();
                BottomBorder bottomEmptyBorder = new BottomBorder();
                DiagonalBorder diagonalEmptyBorder = new DiagonalBorder();

                emptyBorder.Append(leftEmptyBorder);
                emptyBorder.Append(rightEmptyBorder);
                emptyBorder.Append(topEmptyBorder);
                emptyBorder.Append(bottomEmptyBorder);
                emptyBorder.Append(diagonalEmptyBorder);

                Border border = new Border();
                LeftBorder leftBorder = new LeftBorder() { Style = BorderStyleValues.Thin };
                leftBorder.Append(new DocumentFormat.OpenXml.Spreadsheet.Color() { Indexed = (UInt32Value)64U });
                RightBorder rightBorder = new RightBorder() { Style = BorderStyleValues.Thin };
                rightBorder.Append(new DocumentFormat.OpenXml.Spreadsheet.Color() { Indexed = (UInt32Value)64U });
                TopBorder topBorder = new TopBorder() { Style = BorderStyleValues.Thin };
                topBorder.Append(new DocumentFormat.OpenXml.Spreadsheet.Color() { Indexed = (UInt32Value)64U });
                BottomBorder bottomBorder = new BottomBorder() { Style = BorderStyleValues.Thin };
                bottomBorder.Append(new DocumentFormat.OpenXml.Spreadsheet.Color() { Indexed = (UInt32Value)64U });
                DiagonalBorder diagonalBorder = new DiagonalBorder();

                border.Append(leftBorder);
                border.Append(rightBorder);
                border.Append(topBorder);
                border.Append(bottomBorder);
                border.Append(diagonalBorder);

                borders.Append(emptyBorder);
                borders.Append(border);
                /*************/
                /*Форматы*/

                uint iExcelNumberingFormatIndex = 164;

                NumberingFormats numberingFormats = new NumberingFormats();

                NumberingFormat nfInteger = new NumberingFormat();
                nfInteger.NumberFormatId = UInt32Value.FromUInt32(iExcelNumberingFormatIndex++); //164
                nfInteger.FormatCode = StringValue.FromString("0");
                numberingFormats.Append(nfInteger);

                NumberingFormat nfDecimal = new NumberingFormat();
                nfDecimal.NumberFormatId = UInt32Value.FromUInt32(iExcelNumberingFormatIndex++); //165
                nfDecimal.FormatCode = StringValue.FromString("# ### ### ##0.00");
                numberingFormats.Append(nfDecimal);

                NumberingFormat nfForcedText = new NumberingFormat();
                nfForcedText.NumberFormatId = UInt32Value.FromUInt32(iExcelNumberingFormatIndex++); //166
                nfForcedText.FormatCode = StringValue.FromString("@");
                numberingFormats.Append(nfForcedText);

                NumberingFormat nfDateTime = new NumberingFormat();
                nfDateTime.NumberFormatId = UInt32Value.FromUInt32(iExcelNumberingFormatIndex++); //167
                nfDateTime.FormatCode = StringValue.FromString("dd.mm.yyyy hh:mm:ss");
                numberingFormats.Append(nfDateTime);

                NumberingFormat nfDate = new NumberingFormat();
                nfDate.NumberFormatId = UInt32Value.FromUInt32(iExcelNumberingFormatIndex++); //168
                nfDate.FormatCode = StringValue.FromString("dd.mm.yyyy");
                numberingFormats.Append(nfDate);

                numberingFormats.Count = UInt32Value.FromUInt32((uint)numberingFormats.ChildElements.Count);


                CellStyleFormats cellStyleFormats = new CellStyleFormats() { Count = (UInt32Value)2U };
                CellFormat cellFormat = new CellFormat() { NumberFormatId = (UInt32Value)0U, FontId = (UInt32Value)0U, FillId = (UInt32Value)0U, BorderId = (UInt32Value)0U };
                CellFormat cellFormatBoldFont = new CellFormat() { NumberFormatId = (UInt32Value)0U, FontId = (UInt32Value)1U, FillId = (UInt32Value)0U, BorderId = (UInt32Value)1U };

                cellStyleFormats.Append(cellFormat);
                cellStyleFormats.Append(cellFormatBoldFont);

                CellFormats cellFormats = new CellFormats() { Count = (UInt32Value)15U };

                CellFormat headerCellFormat = new CellFormat() { NumberFormatId = (UInt32Value)(uint)ExcelPredefinedFormatNumber.Text/*nfForcedText.NumberFormatId*/, FontId = (UInt32Value)1U, FillId = (UInt32Value)5U, BorderId = (UInt32Value)1U, FormatId = (UInt32Value)1U, ApplyAlignment = true, ApplyBorder = true, ApplyFont = true };
                headerCellFormat.Append(new Alignment() { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Top, WrapText = true });
                CellFormat titleCellFormat = new CellFormat() { NumberFormatId = (UInt32Value)(uint)ExcelPredefinedFormatNumber.Text/*nfForcedText.NumberFormatId*/, FontId = (UInt32Value)2U, FillId = (UInt32Value)0U, BorderId = (UInt32Value)0U, FormatId = (UInt32Value)1U, ApplyAlignment = true, ApplyBorder = false, ApplyFont = true };
                titleCellFormat.Append(new Alignment() { Horizontal = HorizontalAlignmentValues.Left, Vertical = VerticalAlignmentValues.Top, WrapText = true });


                cellFormats.Append(CreateCellFormat(ExcelCellStyles[0].cellStyle)); //0U
                cellFormats.Append(headerCellFormat); //1U
                cellFormats.Append(titleCellFormat); //2U

                for (int i = 3; i < ExcelCellStyles.Count; i++)
                {
                    cellFormats.Append(CreateCellFormat(ExcelCellStyles[i].cellStyle));
                }

                cellFormats.Count = UInt32Value.FromUInt32((uint)cellFormats.ChildElements.Count);



                /*************/
                stylesheet.Append(numberingFormats);
                stylesheet.Append(fonts);
                stylesheet.Append(fills);
                stylesheet.Append(borders);
                stylesheet.Append(cellStyleFormats);
                stylesheet.Append(cellFormats);

                workbookStylesPart.Stylesheet = stylesheet;
            }


            return stylesList;
        }

        protected static CellFormat CreateCellFormat(ExcelCellStyle cellStyle)
        {
            ExcelCellStyle.CellValueDataFormat formatId = cellStyle.DataFormat;
            ExcelCellStyle.CellFillColor cellFillColor = cellStyle.FillColor;

            CellFormat cellFormat = new CellFormat()
            {
                NumberFormatId = UInt32Value.FromUInt32(Convert.ToUInt32(formatId)),
                FontId = (cellStyle.FontBold == true) ? (UInt32Value)1U : (UInt32Value)0U,
                FillId = UInt32Value.FromUInt32(Convert.ToUInt32(cellFillColor)),
                BorderId = (formatId == ExcelCellStyle.CellValueDataFormat.General) ? (UInt32Value)0U : (UInt32Value)1U,
                FormatId = (UInt32Value)0U
            };

            if (formatId != ExcelCellStyle.CellValueDataFormat.General)
            {
                cellFormat.ApplyAlignment = true;
                cellFormat.ApplyBorder = true;
                cellFormat.ApplyFont = true;

                Alignment alignment = new Alignment()
                {
                    Horizontal = (formatId == ExcelCellStyle.CellValueDataFormat.ForcedText) ? HorizontalAlignmentValues.Left : HorizontalAlignmentValues.Right,
                    Vertical = VerticalAlignmentValues.Top,
                    WrapText = true
                };

                cellFormat.Append(alignment);
            }

            return cellFormat;
        }


        public static WorksheetPart CreateWorksheetPartAndImportDataTable(WorkbookPart workbookPart, string id, UInt32 startRowIndex, UInt32 startColumnIndex, string title, IEnumerable<ExcelDataTable> excelDatas)
        {
            ApplyStylesheet(workbookPart);

            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>(id);
            Worksheet worksheet = new Worksheet() { MCAttributes = new MarkupCompatibilityAttributes() { Ignorable = "x14ac" } };
            worksheet.AddNamespaceDeclaration("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
            worksheet.AddNamespaceDeclaration("mc", "http://schemas.openxmlformats.org/markup-compatibility/2006");
            worksheet.AddNamespaceDeclaration("x14ac", "http://schemas.microsoft.com/office/spreadsheetml/2009/9/ac");

            SheetDimension sheetDimension = new SheetDimension() { Reference = "A1" };

            SheetViews sheetViews = new SheetViews();
            SheetView sheetView = new SheetView() { WorkbookViewId = (UInt32Value)0U };
            sheetViews.Append(sheetView);

            SheetData sheetData = new SheetData();
            worksheet.Append(sheetDimension);
            worksheet.Append(sheetViews);

            var maxCols = (uint)excelDatas.Max(d => d.Data.Columns.Count);

            if (String.IsNullOrEmpty(title) == false)
            {
                Row titleRow = new Row()
                {
                    RowIndex = (UInt32Value)startRowIndex,
                    Spans = new ListValue<StringValue>() { InnerText = "1:" + maxCols },
                    Height = 21.95D,
                    CustomHeight = true
                };

                Cell titleCell = CreateTitleCell(startRowIndex, startColumnIndex, title);
                titleRow.Append(titleCell);
                sheetData.Append(titleRow);
            }

            var mergeCells = new MergeCells();

            var autoFilter = excelDatas.Count() == 1 ? new AutoFilter() : null;

            foreach (var excelData in excelDatas)
                ImportDataTable(excelData.Data, worksheet, sheetData, excelData.DataStartRowIndex, excelData.DataStartColumnIndex, mergeCells, autoFilter);

            worksheet.Append(sheetData);

            if (String.IsNullOrEmpty(title) == false)
            {
                var mergeTitleCell = new MergeCell() { Reference = GetRangeByNumber(startColumnIndex, startRowIndex) + ":" + GetRangeByNumber(startColumnIndex + maxCols - 1, startRowIndex) };
                mergeCells.Append(mergeTitleCell);
            }

            if (mergeCells.Elements<MergeCell>() != null && mergeCells.Elements<MergeCell>().Count() != 0)
            {
                worksheet.InsertAfter(mergeCells, worksheet.Elements<SheetData>().First());
            }

            if (autoFilter != null)
                worksheet.InsertAfter(autoFilter, worksheet.Elements<SheetData>().First());

            worksheetPart.Worksheet = worksheet;

            return worksheetPart;
        }


        /// <summary>
        /// Добавления описания пустого объекта Sheet
        /// </summary>
        /// <param name="workbookPart">Объект WorkbookPart</param>
        /// <param name="id">ИД закладки</param>
        /// <returns></returns>
        public static WorksheetPart CreateWorksheetPartAndImportDataTable(WorkbookPart workbookPart, String id,
            UInt32 startRowIndex, UInt32 startColumnIndex,
            uint cols, string title,
            System.Data.DataTable dt,
            UInt32 dataStartRowIndex,
            UInt32 dataStartColumnIndex)
        {
            // параметр cols теперь вычисляемый
            return CreateWorksheetPartAndImportDataTable(workbookPart, id, startRowIndex, startColumnIndex, title, new List<ExcelDataTable>() {
                        new ExcelDataTable(dt, dataStartRowIndex, dataStartColumnIndex),
                    });
        }
        /// <summary>
        /// Создание объекта WorkbookPart
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public static WorkbookPart CreateWorkbookPart(SpreadsheetDocument package, string sheetsName)
        {
            WorkbookPart workbookPart = package.AddWorkbookPart();

            Workbook workbook = new Workbook();

            Sheets sheets = new Sheets();

            if (sheetsName.Contains("|") == false)
            {
                Sheet sheet = new Sheet() { Name = sheetsName, SheetId = (UInt32Value)1U, Id = "rId1" };
                sheets.Append(sheet);
            }
            else
            {
                string[] names = sheetsName.Split('|');

                UInt32Value sheetId = 1U;

                foreach (string name in names)
                {
                    Sheet sheet = new Sheet() { Name = name, SheetId = sheetId, Id = "rId" + sheetId.Value.ToString() };
                    sheets.Append(sheet);

                    sheetId++;
                }
            }

            workbook.Append(sheets);

            workbookPart.Workbook = workbook;

            return workbookPart;
        }
        /// <summary>
        /// Получение названия ячейки
        /// </summary>
        /// <param name="columnIndex">Порядковый номер колонки</param>
        /// <param name="rowIndex">Порядковый номер строки</param>
        /// <returns></returns>        
        public static String GetRangeByNumber(UInt32 columnIndex, UInt32 rowIndex)
        {
            const string str = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            if (columnIndex >= 1 && columnIndex <= 26)
            {
                int cIndex = (int)(columnIndex - 1);
                return String.Format("{0}{1}", str[cIndex], rowIndex);
            }
            else if (columnIndex >= 27 && columnIndex <= 676)
            {
                int cIndex1 = (int)((columnIndex - 27) / 26);
                int cIndex2 = (int)((columnIndex - 1) % 26);
                return String.Format("{0}{1}{2}", str[cIndex1], str[cIndex2], rowIndex);
            }
            else
            {
                throw new Exception("Порядковый номер колонки выходит за предел ожидаемого диапазона");
            }
        }

        /// <summary>
        /// Создание ячейки заголовка
        /// </summary>
        /// <param name="rowIndex">Номер строки</param>
        /// <param name="columnIndex">Номер колонки</param>
        /// <param name="cellValue">Значение</param>
        /// <returns>Ячейка</returns>
        public static Cell CreateTitleCell(UInt32 rowIndex, UInt32 columnIndex, String cellValue)
        {
            Cell cell = new Cell()
            {
                CellReference = GetRangeByNumber(columnIndex, rowIndex),
                DataType = CellValues.InlineString,
                StyleIndex = (UInt32Value)2U,
            };

            InlineString inlineString = new InlineString();
            Text t = new Text();

            t.Text = cellValue.ToString();
            inlineString.AppendChild(t);
            cell.AppendChild(inlineString);

            return cell;
        }

        /// <summary>
        /// Создание ячейки заголовка
        /// </summary>
        /// <param name="rowIndex">Номер строки</param>
        /// <param name="columnIndex">Номер колонки</param>
        /// <param name="cellValue">Значение</param>
        /// <returns>Ячейка</returns>
        public static Cell CreateHeaderCell(UInt32 rowIndex, UInt32 columnIndex, String cellValue)
        {
            Cell cell = new Cell()
            {
                CellReference = GetRangeByNumber(columnIndex, rowIndex),
                DataType = CellValues.InlineString,
                StyleIndex = (UInt32Value)1U,
            };

            InlineString inlineString = new InlineString();
            Text t = new Text();

            t.Text = cellValue.ToString();
            inlineString.AppendChild(t);
            cell.AppendChild(inlineString);

            return cell;
        }

        /// <summary>
        /// Создание текстовой ячейки
        /// </summary>
        /// <param name="rowIndex">Номер строки</param>
        /// <param name="columnIndex">Номер колонки</param>
        /// <param name="cellValue">Значение</param>
        /// <returns>Ячейка</returns>
        public static Cell CreateCellText(UInt32 rowIndex, UInt32 columnIndex, String cellValue, ExcelCellStyle cellStyle)
        {
            Cell cell = new Cell()
            {
                CellReference = GetRangeByNumber(columnIndex, rowIndex),
                DataType = CellValues.InlineString,
                StyleIndex = GetStyleIndexByExcelCellStyle(cellStyle)
            };

            InlineString inlineString = new InlineString();
            Text t = new Text();

            t.Text = cellValue.ToString();
            inlineString.AppendChild(t);
            cell.AppendChild(inlineString);

            return cell;
        }
        /// <summary>
        /// Создание ячейки даты
        /// </summary>
        /// <param name="rowIndex">Номер строки</param>
        /// <param name="columnIndex">Номер колонки</param>
        /// <param name="cellValue">Значение</param>
        /// <returns>Ячейка</returns>
        public static Cell CreateCellDateTime(UInt32 rowIndex, UInt32 columnIndex, DateTime? cellValue, ExcelCellStyle cellStyle)
        {
            Cell cell = new Cell()
            {
                CellReference = GetRangeByNumber(columnIndex, rowIndex),
                StyleIndex = GetStyleIndexByExcelCellStyle(cellStyle)//(cellValue == null || (cellValue != null && cellValue.HasValue == true && cellValue.Value.Date.Equals(cellValue) == false) ? ((lightGray == false) ? (UInt32Value)3U : (UInt32Value)10U) : ((lightGray == false) ? (UInt32Value)3U : (UInt32Value)11U))
            };
            try
            {
                cell.CellValue = new CellValue { Text = (cellValue != null && cellValue.HasValue == true) ? cellValue.Value.ToOADate().ToString(CultureInfo.InvariantCulture) : "" };
            }
            catch (Exception)
            {
                cell.CellValue = null;
            }
            return cell;
        }
        /// <summary>
        /// Создание числовой ячейки - целое число
        /// </summary>
        /// <param name="rowIndex">Номер строки</param>
        /// <param name="columnIndex">Номер колонки</param>
        /// <param name="cellValue">Значение</param>
        /// <returns>Ячейка</returns>
        public static Cell CreateCellInteger(UInt32 rowIndex, UInt32 columnIndex, Int32? cellValue, ExcelCellStyle cellStyle)
        {
            Cell cell = new Cell()
            {
                CellReference = GetRangeByNumber(columnIndex, rowIndex),
                StyleIndex = GetStyleIndexByExcelCellStyle(cellStyle)
            };
            cell.CellValue = new CellValue { Text = (cellValue != null && cellValue.HasValue == true) ? cellValue.ToString() : "" };
            return cell;
        }

        /// <summary>
        /// Создание числовой ячейки - число с плавующей точкой
        /// </summary>
        /// <param name="rowIndex">Номер строки</param>
        /// <param name="columnIndex">Номер колонки</param>
        /// <param name="cellValue">Значение</param>
        /// <returns>Ячейка</returns>
        public static Cell CreateCellDouble(UInt32 rowIndex, UInt32 columnIndex, Double? cellValue, ExcelCellStyle cellStyle)
        {
            Cell cell = new Cell()
            {
                CellReference = GetRangeByNumber(columnIndex, rowIndex),
                StyleIndex = GetStyleIndexByExcelCellStyle(cellStyle)
            };
            cell.CellValue = new CellValue { Text = (cellValue != null && cellValue.HasValue == true) ? cellValue.Value.ToString("f2", CultureInfo.InvariantCulture) : "" };
            return cell;
        }

        /// <summary>
        /// Создание числовой ячейки - число с фиксированной точкой (денежные значения)
        /// </summary>
        /// <param name="rowIndex">Номер строки</param>
        /// <param name="columnIndex">Номер колонки</param>
        /// <param name="cellValue">Значение</param>
        /// <returns>Ячейка</returns>
        public static Cell CreateCellDecimal(UInt32 rowIndex, UInt32 columnIndex, decimal? cellValue, ExcelCellStyle cellStyle)
        {
            Cell cell = new Cell()
            {
                CellReference = GetRangeByNumber(columnIndex, rowIndex),
                StyleIndex = GetStyleIndexByExcelCellStyle(cellStyle)
            };
            cell.CellValue = new CellValue { Text = (cellValue != null && cellValue.HasValue == true) ? cellValue.Value.ToString("f2", CultureInfo.InvariantCulture) : "" };
            return cell;
        }

        private static double GetTextWidth(string font, int fontSize, string text)
        {

            System.Drawing.Font stringFont = new System.Drawing.Font(font, fontSize);
            return GetTextWidthInternal(stringFont, text);
        }

        private static double GetTextWidthInternal(System.Drawing.Font stringFont, string text)
        {
            // This formula is based on this article plus a nudge ( + 0.2M )
            // http://msdn.microsoft.com/en-us/library/documentformat.openxml.spreadsheet.column.width.aspx
            // Truncate(((256 * Solve_For_This + Truncate(128 / 7)) / 256) * 7) = DeterminePixelsOfString

            double width = 0;
            using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                var size = graphics.MeasureString("some text", SystemFonts.DefaultFont);
                width = (double)(((size.Width / (double)7) * 256) - (128 / 7)) / 256;
                width = (double)decimal.Round((decimal)width + 0.2M, 2);
            }
            //Size textSize = TextRenderer.MeasureText(text, stringFont);
            //double width = (double)(((textSize.Width / (double)7) * 256) - (128 / 7)) / 256;

            return width;
        }

        public static DataTable ListToDataTable<T>(List<T> data)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
            DataTable dataTable = new DataTable();

            for (int i = 0; i < properties.Count; i++)
            {
                PropertyDescriptor property = properties[i];
                dataTable.Columns.Add(property.Name, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
            }

            object[] values = new object[properties.Count];
            foreach (T item in data)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = properties[i].GetValue(item);
                }

                dataTable.Rows.Add(values);
            }
            return dataTable;
        }

        public static string ExcelContentType
        {
            get { return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"; }
        }
    }
}
