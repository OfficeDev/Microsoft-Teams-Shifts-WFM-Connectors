// <copyright file="ExportImportHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using ClosedXML.Excel;

    /// <summary>
    /// Helper class for methods to Export to Excel and Import Excel data.
    /// </summary>
    public static class ExportImportHelper
    {
        /// <summary>
        /// Method to create excel workbook and return the output as stream.
        /// </summary>
        /// <param name="exportDt">List of DataTables Rows.</param>
        /// <returns>stream of excel doc.</returns>
        public static MemoryStream ExportToExcel(List<DataTable> exportDt)
        {
            if (exportDt is null)
            {
                throw new ArgumentNullException(nameof(exportDt));
            }

            using (XLWorkbook wb = new XLWorkbook())
            {
                foreach (var dt in exportDt)
                {
                    wb.Worksheets.Add(dt);
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return stream;
                }
            }
        }

        /// <summary>
        /// Generic Method to convert list to DataTable.
        /// </summary>
        /// <typeparam name="T">Any generic type.</typeparam>
        /// <param name="items">List of type T.</param>
        /// <param name="dtName">Name of data table.</param>
        /// <returns>DataTable.</returns>
        public static DataTable ToDataTable<T>(List<T> items, string dtName)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            DataTable dataTable = new DataTable(dtName);

            // Get all the properties
            PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in props)
            {
                // Defining type of data column gives proper data table
                var type = prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) ? Nullable.GetUnderlyingType(prop.PropertyType) : prop.PropertyType;

                // Setting column names as Property names
                dataTable.Columns.Add(prop.Name, type);
            }

            foreach (T item in items)
            {
                var values = new object[props.Length];
                for (int i = 0; i < props.Length; i++)
                {
                    // inserting property values to datatable rows
                    values[i] = props[i].GetValue(item, null);
                }

                dataTable.Rows.Add(values);
            }

            return dataTable;
        }

        /// <summary>
        ///  Method to convert nested list to DataTable.
        /// </summary>
        /// <typeparam name="TOuter">Outer property.</typeparam>
        /// <typeparam name="TInner">Inner property.</typeparam>
        /// <param name="list">List of outer property.</param>
        /// <param name="innerListPropertyName">Name of the inner list property.</param>
        /// <param name="dtName">Name of data table.</param>
        /// <returns>Returns DataTable.</returns>
        public static DataTable CreateNestedDataTable<TOuter, TInner>(List<TOuter> list, string innerListPropertyName, string dtName)
        {
            if (list is null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            PropertyInfo[] outerProperties = typeof(TOuter).GetProperties().Where(pi => pi.Name != innerListPropertyName).ToArray();
            PropertyInfo[] innerProperties = typeof(TInner).GetProperties();
            MethodInfo innerListGetter = typeof(TOuter).GetProperty(innerListPropertyName).GetGetMethod(true);

            DataTable table = new DataTable(dtName);
            foreach (PropertyInfo pi in outerProperties)
            {
                table.Columns.Add(pi.Name, Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType);
            }

            foreach (PropertyInfo pi in innerProperties)
            {
                table.Columns.Add(pi.Name, Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType);
            }

            // iterate through outer items
            foreach (TOuter outerItem in list)
            {
                var innerList = innerListGetter.Invoke(outerItem, null) as IEnumerable<TInner>;
                if (innerList == null || !innerList.Any())
                {
                    // outer item has no inner items
                    DataRow row = table.NewRow();
                    foreach (PropertyInfo pi in outerProperties)
                    {
                        row[pi.Name] = pi.GetValue(outerItem, null) ?? DBNull.Value;
                    }

                    table.Rows.Add(row);
                }
                else
                {
                    // iterate through inner items
                    foreach (object innerItem in innerList)
                    {
                        DataRow row = table.NewRow();
                        foreach (PropertyInfo pi in outerProperties)
                        {
                            row[pi.Name] = pi.GetValue(outerItem, null) ?? DBNull.Value;
                        }

                        foreach (PropertyInfo pi in innerProperties)
                        {
                            row[pi.Name] = pi.GetValue(innerItem, null) ?? DBNull.Value;
                        }

                        table.Rows.Add(row);
                    }
                }
            }

            return table;
        }
    }
}
