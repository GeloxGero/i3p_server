using ClosedXML.Excel;

namespace i3p_server.Services;

public class ExcelSheetReport
{
    // Meta info like Office Name, Fiscal Year, etc.
    public string SheetName { get; set; }
    public List<Dictionary<string, string>> Auxilliary { get; set; } = new();
    
    // Flattened headers from rows 9, 10, 11
    public List<object> Headers { get; set; } = new();
    
    // The raw data rows (serialized as strings for the API)
    public List<List<object>> Data { get; set; } = new();
    
    // Totals/Subtotals usually found at the bottom
    public List<List<object>> Summary { get; set; } = new();
    
    // Specific personnel info if extracted from the sheet
    public List<Dictionary<string, string>> Personnel { get; set; } = new();
    
    public string Date { get; set; } = "";
}

public class closedXmlService
{
    private string GetFormattedValue(IXLCell cell)
    {
        if (cell.Value.IsBlank) return null;
        if (cell.Value.IsBoolean) return cell.Value.GetBoolean().ToString();
        if (cell.Value.IsNumber) return cell.Value.GetNumber().ToString();
        if (cell.Value.IsDateTime) return cell.Value.GetDateTime().ToString();

        // Default to string for everything else (text, errors, etc.)
        return cell.Value.ToString();
    }

    public ExcelSheetReport GetExpenditureForm(string filePath)
    {
        ExcelSheetReport sheetReport = new();

        using (var workbook = new XLWorkbook(filePath))
        {
            var worksheet = workbook.Worksheet(1); // Assuming it's the first sheet
            var rangedWorksheet =  worksheet.RangeUsed();
            
            
            sheetReport.SheetName = worksheet.Name;
            
            var lastRow = rangedWorksheet.LastRowUsed().RowNumber();
            var lastColumn = rangedWorksheet.LastColumnUsed().ColumnNumber();

            // 1. Process Headers (Rows 9, 10, 11) 
            // We create a list of header strings to map the data correctly
            List<string> headers = new List<string>();



            //get auxilliaries
            for (int i = 3; i <= 5; i++)
            {
                var row = rangedWorksheet.Row(i);
                string key = row.Cell(1).GetFormattedString().Trim(':').Trim();
                string value = row.Cell(3).GetFormattedString().Trim();
                if (!string.IsNullOrEmpty(key))
                {
                    var auxEntry = new Dictionary<string, string>
                    {
                        { key, value }
                    };
                    sheetReport.Auxilliary.Add(auxEntry);
                }
            }


            //get headers
            for (int colNum = 1; colNum <= lastColumn; colNum++)
            {
                var row = rangedWorksheet.Row(9);
                var cell = row.Cell(colNum).GetFormattedString().Trim(':').Trim();
                sheetReport.Headers.Add(cell);
            }

            var summaryStart = 0;
            //get content until "total" keyword
            for (int rowNum = 12; rowNum <= lastRow; rowNum++)
            {
                var row = rangedWorksheet.Row(rowNum);


                // Skip rows that are empty or purely visual separators
                if (row.IsEmpty()) continue;
                var firstColumn = row.FirstCell();
                if (!firstColumn.IsEmpty() && GetFormattedValue(row.FirstCell()).ToLower().Contains("total"))
                {
                    summaryStart = rowNum;
                    break;
                }

                var currentRowList = new List<object>();

                for (int colNum = 1; colNum <= lastColumn; colNum++)
                {
                    var cell = GetFormattedValue(row.Cell(colNum));
                    currentRowList.Add(cell);
                }



                sheetReport.Data.Add(currentRowList);
            }
        }

        return sheetReport;
    }

    public ExcelSheetReport GetProcurementPlanB(string filePath)
    {
        ExcelSheetReport sheetReport = new();

        using (var workbook = new XLWorkbook(filePath))
        {
            var worksheet = workbook.Worksheet(1); // Assuming it's the first sheet
            var rangedWorksheet =  worksheet.RangeUsed();
            
            
            sheetReport.SheetName = worksheet.Name;
            
            var lastRow = rangedWorksheet.LastRowUsed().RowNumber();
            var lastColumn = rangedWorksheet.LastColumnUsed().ColumnNumber();

            // 1. Process Headers (Rows 9, 10, 11) 
            // We create a list of header strings to map the data correctly
            List<string> headers = new List<string>();



            //get auxilliaries
            for (int i = 3; i <= 5; i++)
            {
                var row = rangedWorksheet.Row(i);
                string key = row.Cell(1).GetFormattedString().Trim(':').Trim();
                string value = row.Cell(3).GetFormattedString().Trim();
                if (!string.IsNullOrEmpty(key))
                {
                    var auxEntry = new Dictionary<string, string>
                    {
                        { key, value }
                    };
                    sheetReport.Auxilliary.Add(auxEntry);
                }
            }


            //get headers
            for (int colNum = 1; colNum <= lastColumn; colNum++)
            {
                var row = rangedWorksheet.Row(8);
                var cell = row.Cell(colNum).GetFormattedString().Trim(':').Trim();
                sheetReport.Headers.Add(cell);
            }

            var summaryStart = 0;
            //get content until "total" keyword
            for (int rowNum = 18; rowNum <= lastRow; rowNum++)
            {
                var row = rangedWorksheet.Row(rowNum);


                // Skip rows that are empty or purely visual separators
                if (row.IsEmpty()) continue;
                var firstColumn = row.FirstCell();
                if (!firstColumn.IsEmpty() && GetFormattedValue(row.FirstCell()).ToLower().Contains("total"))
                {
                    summaryStart = rowNum;
                    break;
                }

                var currentRowList = new List<object>();

                for (int colNum = 1; colNum <= lastColumn; colNum++)
                {
                    var cell = GetFormattedValue(row.Cell(colNum));
                    currentRowList.Add(cell);
                }



                sheetReport.Data.Add(currentRowList);
            }
        }

        return sheetReport;
    }
    
    public ExcelSheetReport GetAnnualProcurementPlan(string filePath)
    {
        ExcelSheetReport sheetReport = new();

        using (var workbook = new XLWorkbook(filePath))
        {
            var worksheet = workbook.Worksheet(1); // Assuming it's the first sheet
            var rangedWorksheet =  worksheet.RangeUsed();
            
            
            sheetReport.SheetName = worksheet.Name;
            
            var lastRow = rangedWorksheet.LastRowUsed().RowNumber();
            var lastColumn = rangedWorksheet.LastColumnUsed().ColumnNumber();

            // 1. Process Headers (Rows 9, 10, 11) 
            // We create a list of header strings to map the data correctly
            List<string> headers = new List<string>();



            //get auxilliaries
            for (int i = 3; i <= 5; i++)
            {
                var row = rangedWorksheet.Row(i);
                string key = row.Cell(1).GetFormattedString().Trim(':').Trim();
                string value = row.Cell(3).GetFormattedString().Trim();
                if (!string.IsNullOrEmpty(key))
                {
                    var auxEntry = new Dictionary<string, string>
                    {
                        { key, value }
                    };
                    sheetReport.Auxilliary.Add(auxEntry);
                }
            }


            //get headers
            for (int colNum = 1; colNum <= lastColumn; colNum++)
            {
                var row = rangedWorksheet.Row(15);
                var cell = row.Cell(colNum).GetFormattedString().Trim(':').Trim();
                sheetReport.Headers.Add(cell);
            }

            var summaryStart = 0;
            //get content until "total" keyword
            for (int rowNum = 18; rowNum <= lastRow; rowNum++)
            {
                var row = rangedWorksheet.Row(rowNum);


                // Skip rows that are empty or purely visual separators
                if (row.IsEmpty()) continue;
                var firstColumn = row.FirstCell();
                if (!firstColumn.IsEmpty() && GetFormattedValue(row.FirstCell()).ToLower().Contains("total"))
                {
                    summaryStart = rowNum;
                    break;
                }

                var currentRowList = new List<object>();

                for (int colNum = 1; colNum <= lastColumn; colNum++)
                {
                    var cell = GetFormattedValue(row.Cell(colNum));
                    currentRowList.Add(cell);
                }



                sheetReport.Data.Add(currentRowList);
            }
        }

        return sheetReport;
    }
    
    
    
    
    
    public List<ExcelSheetReport> GetPPMPFile(string filePath)
    {
        List<ExcelSheetReport> multipleSheetsReport = new List<ExcelSheetReport>();

        using (var workbook = new XLWorkbook(filePath))
        {
            foreach (var worksheet in workbook.Worksheets)
            {
                ExcelSheetReport sheetReport = new();
                sheetReport.SheetName = worksheet.Name;
                var rangedWorksheet = worksheet.RangeUsed();
                var lastRow = rangedWorksheet.LastRowUsed().RowNumber();
                var lastColumn = rangedWorksheet.LastColumnUsed().ColumnNumber();

                
                
                
                
                
                //get auxilliaries
                for (int i = 3; i <= 5; i++)
                {
                    var row = rangedWorksheet.Row(i);
                    string key = row.Cell(1).GetFormattedString().Trim(':').Trim();
                    string value = row.Cell(3).GetFormattedString().Trim();
                    if (!string.IsNullOrEmpty(key))
                    {
                        var auxEntry = new Dictionary<string, string>
                        {
                            { key, value }
                        };
                        sheetReport.Auxilliary.Add(auxEntry);
                    }
                }
                
                //get headers
                for (int colNum = 1; colNum <= lastColumn; colNum++)
                {
                    var row = rangedWorksheet.Row(15);
                    var cell = row.Cell(colNum).GetFormattedString().Trim(':').Trim();
                    sheetReport.Headers.Add(cell);
                }
                
                //get content until "total" keyword
                for (int rowNum = 17; rowNum <= lastRow; rowNum++)
                {
                    var row = rangedWorksheet.Row(rowNum);


                    // Skip rows that are empty or purely visual separators
                    if (row.IsEmpty()) continue;
                    var firstColumn = row.FirstCell();
                    if (!firstColumn.IsEmpty() && GetFormattedValue(row.FirstCell()).ToLower().Contains("total"))
                    {
                        break;
                    }

                    var currentRowList = new List<object>();

                    for (int colNum = 1; colNum <= lastColumn; colNum++)
                    {
                        var cell = GetFormattedValue(row.Cell(colNum));
                        currentRowList.Add(cell);
                    }



                    sheetReport.Data.Add(currentRowList);
                }
                multipleSheetsReport.Add(sheetReport);
            }
        }

        return multipleSheetsReport;
    }
    
    
    
    
    
    public List<ExcelSheetReport> GetSchoolImplementationPlan(string filePath)
    {
        List<ExcelSheetReport> multipleSheetsReport = new List<ExcelSheetReport>();

        using (var workbook = new XLWorkbook(filePath))
        {
            foreach (var worksheet in workbook.Worksheets)
            {
                ExcelSheetReport sheetReport = new();
                sheetReport.SheetName = worksheet.Name;
                var rangedWorksheet = worksheet.RangeUsed();
                var lastRow = rangedWorksheet.LastRowUsed().RowNumber();
                var lastColumn = rangedWorksheet.LastColumnUsed().ColumnNumber();

                //get auxilliaries
                for (int i = 3; i <= 5; i++)
                {
                    var row = rangedWorksheet.Row(i);
                    string key = row.Cell(1).GetFormattedString().Trim(':').Trim();
                    string value = row.Cell(3).GetFormattedString().Trim();
                    if (!string.IsNullOrEmpty(key))
                    {
                        var auxEntry = new Dictionary<string, string>
                        {
                            { key, value }
                        };
                        sheetReport.Auxilliary.Add(auxEntry);
                    }
                }
                
                //get headers
                for (int colNum = 1; colNum <= lastColumn; colNum++)
                {
                    var row = rangedWorksheet.Row(6);
                    var cell = row.Cell(colNum).GetFormattedString().Trim(':').Trim();
                    sheetReport.Headers.Add(cell);
                }
                
                //get content until "total" keyword
                for (int rowNum = 8; rowNum <= lastRow; rowNum++)
                {
                    var row = rangedWorksheet.Row(rowNum);


                    // Skip rows that are empty or purely visual separators
                    if (row.IsEmpty()) continue;
                    var firstColumn = row.FirstCell();
                    if (!firstColumn.IsEmpty() && GetFormattedValue(row.FirstCell()).ToLower().Contains("total budget"))
                    {
                        break;
                    }

                    var currentRowList = new List<object>();

                    for (int colNum = 1; colNum <= lastColumn; colNum++)
                    {
                        var cell = GetFormattedValue(row.Cell(colNum));
                        currentRowList.Add(cell);
                    }
                    
                    sheetReport.Data.Add(currentRowList);
                }
                multipleSheetsReport.Add(sheetReport);
            }
            
        }

        return multipleSheetsReport;
    }
    
    
    
    
}