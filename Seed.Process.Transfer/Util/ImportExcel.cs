using OfficeOpenXml;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Seed.Process.Transfer
{
    public class ImportExcel
    {
        

        public ImportExcel()
        {

        }

        public virtual IEnumerable<dynamic> Import(string filePath)
        {
            var rows = this.ImportExcelToDictoray(filePath);
            return rows.DictionaryToObject();

        }
      
        public virtual IEnumerable<Dictionary<string, object>> ImportExcelToDictoray(string filePath)
        {
            var model = new List<Dictionary<string, object>>();
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var sb = new StringBuilder();
                var worksheet = package.Workbook.Worksheets[1];
                var rowCount = worksheet.Dimension.Rows;
                var colCount = worksheet.Dimension.Columns;

                for (var row = 2; row <= rowCount; row++)
                {
                    var item = new Dictionary<string, object>();
                    for (var col = 1; col <= colCount; col++)
                    {
                        if (worksheet.Cells[1, col].Value != null)
                        {
                            var fieldName = worksheet.Cells[1, col].Value.ToString();
                            var fieldValue = worksheet.Cells[row, col].Value != null ? worksheet.Cells[row, col].Value.ToString() : null;
                            item.Add(fieldName, fieldValue);
                        }
                    }
                    model.Add(item);

                }

                return model;
            }
        }

       
    }
}
