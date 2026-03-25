using ClosedXML.Excel;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class ExcelWriterService
    {
        public byte[] AddColumnsToExistingExcel(Stream originalFileStream, List<InputRow> rows)
        {
            using var workbook = new XLWorkbook(originalFileStream);
            var worksheet = workbook.Worksheet(1);

            const int headerRow = 1;
            var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 8;

            var marketPriceColumn = lastColumn + 1;
            var imageColumn = lastColumn + 2;
            var statusColumn = lastColumn + 3;

            worksheet.Cell(headerRow, marketPriceColumn).Value = "MarketPrice";
            worksheet.Cell(headerRow, imageColumn).Value = "Image";
            worksheet.Cell(headerRow, statusColumn).Value = "Status";

            worksheet.Column(imageColumn).Width = 18;

            foreach (var item in rows)
            {
                var excelRow = item.ExcelRowNumber;

                worksheet.Cell(excelRow, marketPriceColumn).Value = item.MarketPrice;
                worksheet.Cell(excelRow, statusColumn).Value = item.Status;
                worksheet.Row(excelRow).Height = 85;

                if (item.ImageBytes is { Length: > 0 })
                {
                    using var imageStream = new MemoryStream(item.ImageBytes);

                    var picture = worksheet.AddPicture(imageStream)
                        .MoveTo(worksheet.Cell(excelRow, imageColumn), 5, 5);

                    picture.WithSize(90, 90);
                }
            }

            for (int col = 1; col <= statusColumn; col++)
            {
                if (col != imageColumn)
                    worksheet.Column(col).AdjustToContents();
            }

            worksheet.Column(imageColumn).Width = 18;

            using var outputStream = new MemoryStream();
            workbook.SaveAs(outputStream);

            return outputStream.ToArray();
        }
    }
}