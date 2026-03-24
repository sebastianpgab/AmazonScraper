using ClosedXML.Excel;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class ExcelReaderService
    {
        public List<InputRow> ReadExcel(Stream fileStream)
        {
            var results = new List<InputRow>();

            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheet(1); // pierwszy arkusz

            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

            // zakładamy, że dane zaczynają się od 2 wiersza
            // a wiersz 1 to nagłówki
            for (int row = 2; row <= lastRow; row++)
            {
                if (RowIsEmpty(worksheet, row))
                {
                    continue;
                }

                var item = new InputRow
                {
                    ExcelRowNumber = row,
                    InternalCode = worksheet.Cell(row, 1).GetValue<string>().Trim(),
                    ProductCode = worksheet.Cell(row, 2).GetValue<string>().Trim(),
                    ProductName = worksheet.Cell(row, 3).GetValue<string>().Trim(),
                    Ean = worksheet.Cell(row, 4).GetValue<string>().Trim(),
                    Asin = worksheet.Cell(row, 5).GetValue<string>().Trim(),
                    Condition = worksheet.Cell(row, 6).GetValue<string>().Trim(),
                    Marketplace = worksheet.Cell(row, 7).GetValue<string>().Trim(),
                    Price = worksheet.Cell(row, 8).GetValue<string>().Trim()
                };

                results.Add(item);
            }

            return results;
        }

        private bool RowIsEmpty(IXLWorksheet worksheet, int row)
        {
            for (int col = 1; col <= 8; col++)
            {
                if (!string.IsNullOrWhiteSpace(worksheet.Cell(row, col).GetValue<string>()))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
