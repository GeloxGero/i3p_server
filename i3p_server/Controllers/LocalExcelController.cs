
using Microsoft.AspNetCore.Mvc;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using i3p_server.Models;
using i3p_server.Services;


namespace i3p_server.Controllers;


[Route("api/csv")]
[ApiController]
public class LocalExcelController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly openXmlService _openXmlService;
    private readonly closedXmlService _closedXmlService;

    public LocalExcelController(AppDbContext context)
    {
        _context = context;
        _openXmlService = new openXmlService();
        _closedXmlService = new closedXmlService();
    }
    
    private void PrintElement(OpenXmlElement element, int depth)
    {
        string indent = new string(' ', depth * 2);
    
        // Print the Element Tag Name
        Console.WriteLine($"{indent}<{element.LocalName}>");

        // Print Attributes (like Cell References 'A1', 'B1')
        foreach (var attr in element.GetAttributes())
        {
            Console.WriteLine($"{indent}  Attribute: {attr.LocalName} = {attr.Value}");
        }

        // Recurse into children
        foreach (var child in element.ChildElements)
        {
            PrintElement(child, depth + 1);
        }

        Console.WriteLine($"{indent}</{element.LocalName}>");
    }

    [HttpGet("GetCsv")]
    public IActionResult GetCsv()
    {
        using (SpreadsheetDocument doc = SpreadsheetDocument.Open("csvFiles/LHNHS JHS EM 2026.xlsx",false))
        {
            // Navigate to the first Worksheet
            WorkbookPart workbookPart = doc.WorkbookPart;
            Sheet sheet = workbookPart.Workbook.Descendants<Sheet>().First();
            WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
        
            // The root element of the data
            OpenXmlElement root = worksheetPart.Worksheet;

            PrintElement(root, 0);
        }

        return Ok("Got xml files");
    }
    
    
    [HttpGet("get_expenditure_form")]
    public IActionResult GetExpenditureForm()
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "csvFiles", "LHNHS JHS EM 2026.xlsx");
    
        if (!System.IO.File.Exists(filePath)) return NotFound();

        var data = _closedXmlService.GetExpenditureForm(filePath);
        return Ok(data); 
    }
    
    [HttpGet("get_procurement_plan_b")]
    public IActionResult GetProcurementPlanB()
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "csvFiles", "LHNHS_APP_CSE 2026 PMIS.xlsx");
    
        if (!System.IO.File.Exists(filePath)) return NotFound();

        var data = _closedXmlService.GetProcurementPlanB(filePath);
        return Ok(data); 
    }
    
    [HttpGet("get_annual_procurement_plan")]
    public IActionResult GetProcurementPlan()
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "csvFiles", "LHNHS-SHS_APP-CSE 2026 (3).xlsx");
    
        if (!System.IO.File.Exists(filePath)) return NotFound();

        var data = _closedXmlService.GetAnnualProcurementPlan(filePath);
        return Ok(data); 
    }
    
    [HttpGet("get_school_implementation_plan")]
    public IActionResult GetSchoolImplementationPlan()
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "csvFiles", "2026-LHNHS-JHS AND SHS FINAL.xlsx");
    
        if (!System.IO.File.Exists(filePath)) return NotFound();

        var data = _closedXmlService.GetSchoolImplementationPlan(filePath);
        return Ok(data); 
    }
    
    [HttpGet("get_ppmp_plan")]
    public IActionResult GetPPMPPlan()
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "csvFiles", "LHNHS-SHS-PPMP-2026 (1).xlsx");
    
        if (!System.IO.File.Exists(filePath)) return NotFound();

        var data = _closedXmlService.GetPPMPFile(filePath);
        return Ok(data); 
    }
}