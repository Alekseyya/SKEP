using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using Core.Helpers;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Core.BL.Interfaces;

namespace BL.Implementation
{
    public class ExcelService : IExcelService
    {
        private readonly IReflectionService _reflectionService;

        public ExcelService(IReflectionService reflectionService)
        {
            _reflectionService = reflectionService;
        }

        public byte[] CreateBinaryByDataTable(DataTable dataTable, string sheetName, string title)
        {
            byte[] binData = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, sheetName);

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, (uint)dataTable.Columns.Count,
                        title, dataTable, 3, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                BinaryReader b = new BinaryReader(stream);
                binData = b.ReadBytes((int)stream.Length);
            }
            return binData;
        }

        public DataTable CreateDataTableByEntryList<T>(List<T> list)
        {
            //Todo читать описание метода
            var dataTable = CreateDataTableColumnsByEntryWithType(list.First());
            var rowIndex = 0;
            foreach (var entry in list)
            {
                var listProperties = _reflectionService.GetFieldValuesFromObjectThroughProperties(entry);
                dataTable.Rows.Add();
                for (int i = 0; i < listProperties.Count; i++)
                {
                    dataTable.Rows[rowIndex][listProperties[i].field] = listProperties[i].value;
                }
                rowIndex++;
            }
            return dataTable;
        }

        /// <summary>
        /// Для типов double, decimal, int - столбца имеют тип string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entry"></param>
        /// <returns></returns>
        public DataTable CreateDataTableColumnsByEntryWithType<T>(T entry)
        {
            var dataTable = new DataTable();
            var propertyList = entry.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => typeof(IEnumerable).IsAssignableFrom(typeof(string)))
                .Where(p => p.PropertyType == typeof(DateTime) ||
                            p.PropertyType == typeof(DateTime?) ||
                            p.PropertyType == typeof(string) ||
                            p.PropertyType == typeof(int) ||
                            p.PropertyType == typeof(int?) ||
                            p.PropertyType == typeof(double) ||
                            p.PropertyType == typeof(double?) ||
                            p.PropertyType == typeof(decimal) ||
                            p.PropertyType == typeof(decimal?) ||
                            p.PropertyType.IsEnum ||
                            p.PropertyType == typeof(bool) || p.PropertyType == typeof(bool?))
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => !p.Name.Equals("ItemID") && !p.Name.Equals("IsVersion") && !p.Name.Equals("VersionNumber")
                            && !p.Name.Equals("AuthorSID") && !p.Name.Equals("EditorSID") && !p.Name.Equals("DisplayEditor") && !p.Name.Equals("FullName")
                            && !p.Name.Equals("IsDeleted") && !p.Name.Equals("DeletedDate") && !p.Name.Equals("DeletedBy") && !p.Name.Equals("DeletedBySID"));

            foreach (var property in propertyList)
            {
                if (property.PropertyType == typeof(string))
                {
                    dataTable.Columns.Add(property.Name, typeof(string)).Caption = entry.GetType().GetProperty(property.Name).GetCustomAttributes(typeof(DisplayAttribute)).Cast<DisplayAttribute>().Single().Name;
                    dataTable.Columns[property.Name].ExtendedProperties["Width"] = (double)25;
                }
                else if (property.PropertyType == typeof(int) && (property.Name.Equals("ID") || property.Name.Equals("Id")))
                {
                    dataTable.Columns.Add(property.Name, typeof(string)).Caption = entry.GetType().GetProperty(property.Name).GetCustomAttributes(typeof(DisplayAttribute)).Cast<DisplayAttribute>().Single().Name;
                    dataTable.Columns[property.Name].ExtendedProperties["Width"] = (double)8;
                }
                else if ((property.PropertyType == typeof(int?) || property.PropertyType == typeof(int)) && (property.Name.EndsWith("ID") || property.Name.EndsWith("Id")))
                {
                    dataTable.Columns.Add(property.Name, typeof(string)).Caption = entry.GetType().GetProperty(property.Name).GetCustomAttributes(typeof(DisplayAttribute)).Cast<DisplayAttribute>().Single().Name;
                    dataTable.Columns[property.Name].ExtendedProperties["Width"] = (double)25;
                }
                else if ((property.PropertyType == typeof(int) || property.PropertyType == typeof(int?) && property.Name.EndsWith("ID") == false))
                {
                    dataTable.Columns.Add(property.Name, typeof(int)).Caption = entry.GetType().GetProperty(property.Name).GetCustomAttributes(typeof(DisplayAttribute)).Cast<DisplayAttribute>().Single().Name;
                    dataTable.Columns[property.Name].ExtendedProperties["Width"] = (double)15;
                }
                else if ((property.PropertyType == typeof(double) || property.PropertyType == typeof(double?)) ||
                         (property.PropertyType == typeof(decimal) || property.PropertyType == typeof(decimal?)))
                {
                    dataTable.Columns.Add(property.Name, typeof(double)).Caption = entry.GetType().GetProperty(property.Name).GetCustomAttributes(typeof(DisplayAttribute)).Cast<DisplayAttribute>().Single().Name;
                    dataTable.Columns[property.Name].ExtendedProperties["Width"] = (double)15;
                }
                else if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
                {
                    dataTable.Columns.Add(property.Name, typeof(DateTime)).Caption = entry.GetType().GetProperty(property.Name).GetCustomAttributes(typeof(DisplayAttribute)).Cast<DisplayAttribute>().Single().Name;
                    dataTable.Columns[property.Name].ExtendedProperties["Width"] = (double)15;
                }
                else if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
                {
                    dataTable.Columns.Add(property.Name, typeof(string)).Caption = entry.GetType().GetProperty(property.Name).GetCustomAttributes(typeof(DisplayAttribute)).Cast<DisplayAttribute>().Single().Name;
                    dataTable.Columns[property.Name].ExtendedProperties["Width"] = (double)8;
                }
                else if (property.PropertyType.IsEnum)
                {
                    dataTable.Columns.Add(property.Name, typeof(string)).Caption = entry.GetType().GetProperty(property.Name).GetCustomAttributes(typeof(DisplayAttribute)).Cast<DisplayAttribute>().Single().Name;
                    dataTable.Columns[property.Name].ExtendedProperties["Width"] = (double)25;
                }
            }

            return dataTable;
        }
    }
}
