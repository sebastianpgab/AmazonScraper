using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    public class ExcelController : Controller
    {
        private readonly ExcelReaderService _excelReaderService;
        private readonly ExcelWriterService _excelWriterService;
        private readonly ProductEnrichmentService _productEnrichmentService;

        public ExcelController(
            ExcelReaderService excelReaderService,
            ExcelWriterService excelWriterService,
            ProductEnrichmentService productEnrichmentService)
        {
            _excelReaderService = excelReaderService;
            _excelWriterService = excelWriterService;
            _productEnrichmentService = productEnrichmentService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Nie wybrano pliku.");

            List<WebApplication1.Models.InputRow> rows;
            byte[] resultBytes;

            using (var inputStream = file.OpenReadStream())
            {
                rows = _excelReaderService.ReadExcel(inputStream);
            }

            await _productEnrichmentService.EnrichAsync(rows);

            using (var secondStream = file.OpenReadStream())
            {
                resultBytes = _excelWriterService.AddColumnsToExistingExcel(secondStream, rows);
            }

            var outputFileName = $"enriched_{Path.GetFileName(file.FileName)}";

            return File(
                resultBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                outputFileName
            );
        }
    }
}
