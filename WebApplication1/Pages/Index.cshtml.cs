using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Pages
{
    public class IndexModel : PageModel
    {

        private readonly ExcelReaderService _excelReaderService;
        private readonly ProductEnrichmentService _productEnrichmentService;
        private readonly ExcelWriterService _excelWriterService;

        public IndexModel(
            ExcelReaderService excelReaderService,
            ProductEnrichmentService productEnrichmentService,
            ExcelWriterService excelWriterService)
        {
            _excelReaderService = excelReaderService;
            _productEnrichmentService = productEnrichmentService;
            _excelWriterService = excelWriterService;
        }

        [BindProperty]
        public IFormFile? UploadedFile { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (UploadedFile == null || UploadedFile.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Wgraj plik Excel.");
                return Page();
            }

            var extension = Path.GetExtension(UploadedFile.FileName);
            if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Dozwolone są tylko pliki .xlsx");
                return Page();
            }

            List<InputRow> rows;

            using (var readStream = UploadedFile.OpenReadStream())
            {
                rows = _excelReaderService.ReadExcel(readStream);
            }

            await _productEnrichmentService.EnrichAsync(rows);

            byte[] fileBytes;

            using (var writeStream = UploadedFile.OpenReadStream())
            {
                fileBytes = _excelWriterService.AddColumnsToExistingExcel(writeStream, rows);
            }

            var outputFileName = $"edited_{UploadedFile.FileName}";

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                outputFileName);

        }
    }
}
