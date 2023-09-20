using System.Data;
using System.IO.Compression;
using System.Text;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        public async Task ExportDataToXLSXAsync(string destinationFilePath, DataTable dataTable)
        {
            try
            {
                using (var archive = new ZipArchive(new FileStream(destinationFilePath, FileMode.Create), ZipArchiveMode.Create))
                {
                    await CreateWorkbookRelationshipAsync(archive);

                    await CreateWorkbookAsync(archive);

                    await CreateWorksheetAsync(archive, dataTable);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error exporting data to XLSX: " + ex.Message);
                throw;
            }
        }

        private async Task CreateWorkbookRelationshipAsync(ZipArchive archive)
        {
            var workbookRelationship = archive.CreateEntry("_rels/.rels").Open();
            using (var writer = new StreamWriter(workbookRelationship, Encoding.UTF8))
            {
                await writer.WriteAsync("<?xml version=\"1.0\" encoding=\"utf-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\" Id=\"rId1\"/></Relationships>");
            }
        }

        private async Task CreateWorkbookAsync(ZipArchive archive)
        {
            var workbook = archive.CreateEntry("xl/workbook.xml").Open();
            using (var writer = new StreamWriter(workbook, Encoding.UTF8))
            {
                await writer.WriteAsync("<?xml version=\"1.0\" encoding=\"utf-8\" ?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            }
        }

        private async Task CreateWorksheetAsync(ZipArchive archive, DataTable dataTable)
        {
            var worksheet = archive.CreateEntry("xl/worksheets/sheet1.xml").Open();
            using (var writer = new StreamWriter(worksheet, Encoding.UTF8))
            {
                await writer.WriteAsync("<?xml version=\"1.0\" encoding=\"utf-8\" ?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");

                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    await writer.WriteAsync("<row>");
                    for (int j = 0; j < dataTable.Columns.Count; j++)
                    {
                        await writer.WriteAsync($"<c t=\"inlineStr\"><is><t>{dataTable.Rows[i][j]}</t></is></c>");
                    }
                    await writer.WriteAsync("</row>");
                }

                await writer.WriteAsync("</sheetData></worksheet>");
            }
        }
    }
}
