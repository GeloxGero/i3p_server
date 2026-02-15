using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;


namespace i3p_server.Services;

public class openXmlService
{
    
    // Helper to extract the actual value regardless of type
    private string GetCellValue(Cell cell, SharedStringTablePart stringTablePart)
    {
        string value = cell.InnerText;

        if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
        {
            return stringTablePart.SharedStringTable.ChildElements[int.Parse(value)].InnerText;
        }
        return value;
    }
    
    public List<Dictionary<string, object>> SerializeFullExcel(string filePath)
    {
        var rows = new List<Dictionary<string, object>>();

        using (SpreadsheetDocument doc = SpreadsheetDocument.Open(filePath, false))
        {
            WorkbookPart workbookPart = doc.WorkbookPart;
            // Get the first sheet
            Sheet sheet = workbookPart.Workbook.Descendants<Sheet>().First();
            WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
            SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();

            // Get Shared String Table (where text is stored)
            SharedStringTablePart stringTablePart = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();

            // Identify Headers (Row 1)
            var headerRow = sheetData.Elements<Row>().FirstOrDefault();
            if (headerRow == null) return rows;

            List<string> headers = headerRow.Elements<Cell>()
                .Select(c => GetCellValue(c, stringTablePart)).ToList();

            // Process Data Rows
            foreach (Row row in sheetData.Elements<Row>().Skip(1)) 
            {
                var rowData = new Dictionary<string, object>();
                var cells = row.Elements<Cell>().ToList();

                for (int i = 0; i < headers.Count; i++)
                {
                    // Access cell by index (handles empty cells safely)
                    var cell = cells.ElementAtOrDefault(i);
                    rowData[headers[i]] = cell != null ? GetCellValue(cell, stringTablePart) : null;
                }
                rows.Add(rowData);
            }
        }
        return rows;
    }
    
    public Sheets GetSheetInfo(string fileName)
    {
        // Open file as read-only
        using (SpreadsheetDocument spread = SpreadsheetDocument.Open(fileName, false))
        {
            Sheets? sheets = spread.WorkbookPart?.Workbook?.Sheets;
            
            Console.WriteLine("Sheets information");
            if (sheets is not null)
            {
                // For each sheet, display the sheet information.
                foreach (OpenXmlElement sheet in sheets)
                {
                    Console.WriteLine(sheet);
                    // foreach (OpenXmlAttribute attr in sheet.GetAttributes())
                    // {
                    //     Console.WriteLine("{0}: {1}", attr.LocalName, attr.Value);
                    // }
                }
            }
            return sheets;
        }
    }
}