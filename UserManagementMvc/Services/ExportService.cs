using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using UserManagementMvc.Models;

namespace UserManagementMvc.Services
{
    public class ExportService : IExportService
    {
        private readonly AppDbContext _context;

        public ExportService(AppDbContext context)
        {
            _context = context;
        }

       
        // COMMON USER QUERY
        

        private async Task<List<User>> GetUsersAsync(
            string? search,
            string? roleFilter,
            string? departmentFilter,
            string? statusFilter)
        {
            IQueryable<User> query = _context.Users
                .AsNoTracking()
                .Where(x => x.IsDeleted == false);

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    (x.Name != null &&
                     x.Name.Contains(search)) ||

                    (x.UserName != null &&
                     x.UserName.Contains(search)) ||

                    (x.Email != null &&
                     x.Email.Contains(search)) ||

                    (x.Mobile != null &&
                     x.Mobile.Contains(search))
                );
            }

            // Role
            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                query = query.Where(x =>
                    x.Role == roleFilter);
            }

            // Department
            if (!string.IsNullOrWhiteSpace(departmentFilter))
            {
                query = query.Where(x =>
                    x.Department == departmentFilter);
            }

            // Status
            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                bool isActive =
                    statusFilter == "Active";

                query = query.Where(x =>
                    x.IsActive == isActive);
            }

            return await query
                .OrderBy(x => x.Id)
                .ToListAsync();
        }


        
        // EXCEL EXPORT
        

        public async Task<byte[]> ExportUsersToExcelAsync(
            string? search,
            string? roleFilter,
            string? departmentFilter,
            string? statusFilter)
        {
            var users = await GetUsersAsync(
                search,
                roleFilter,
                departmentFilter,
                statusFilter);

            using var workbook = new XLWorkbook();

            var worksheet =
                workbook.Worksheets.Add("Users");

            // Title
            worksheet.Cell(1, 1).Value =
                "User Management System - User Export";

            worksheet.Range(1, 1, 1, 10).Merge();

            worksheet.Cell(1, 1)
                .Style.Font.Bold = true;

            worksheet.Cell(1, 1)
                .Style.Font.FontSize = 16;

            worksheet.Cell(1, 1)
                .Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;


            // Export Date
            worksheet.Cell(2, 1).Value =
                "Exported On:";

            worksheet.Cell(2, 2).Value =
                DateTime.Now;

            worksheet.Cell(2, 2)
                .Style.DateFormat.Format =
                    "dd-MM-yyyy HH:mm:ss";


            // Headers
            int headerRow = 4;

            worksheet.Cell(headerRow, 1).Value = "ID";
            worksheet.Cell(headerRow, 2).Value = "Name";
            worksheet.Cell(headerRow, 3).Value = "Username";
            worksheet.Cell(headerRow, 4).Value = "Email";
            worksheet.Cell(headerRow, 5).Value = "Mobile";
            worksheet.Cell(headerRow, 6).Value = "Role";
            worksheet.Cell(headerRow, 7).Value = "Department";
            worksheet.Cell(headerRow, 8).Value = "Status";
            worksheet.Cell(headerRow, 9).Value = "Created At";
            worksheet.Cell(headerRow, 10).Value = "Updated At";


            var headerRange =
                worksheet.Range(
                    headerRow,
                    1,
                    headerRow,
                    10);

            headerRange.Style.Font.Bold = true;

            headerRange.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            headerRange.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;


            // Data
            int row = headerRow + 1;

            foreach (var user in users)
            {
                worksheet.Cell(row, 1).Value =
                    user.Id;

                worksheet.Cell(row, 2).Value =
                    user.Name ?? "";

                worksheet.Cell(row, 3).Value =
                    user.UserName ?? "";

                worksheet.Cell(row, 4).Value =
                    user.Email ?? "";

                worksheet.Cell(row, 5).Value =
                    $"{user.MobileCountryCode} {user.Mobile}";

                worksheet.Cell(row, 6).Value =
                    user.Role ?? "";

                worksheet.Cell(row, 7).Value =
                    user.Department ?? "";

                worksheet.Cell(row, 8).Value =
                    user.IsActive == true
                        ? "Active"
                        : "Inactive";

                if (user.CreatedAt.HasValue)
                {
                    worksheet.Cell(row, 9).Value =
                        user.CreatedAt.Value;

                    worksheet.Cell(row, 9)
                        .Style.DateFormat.Format =
                            "dd-MM-yyyy HH:mm:ss";
                }

                if (user.UpdatedAt.HasValue)
                {
                    worksheet.Cell(row, 10).Value =
                        user.UpdatedAt.Value;

                    worksheet.Cell(row, 10)
                        .Style.DateFormat.Format =
                            "dd-MM-yyyy HH:mm:ss";
                }

                row++;
            }


            // Table
            if (users.Count > 0)
            {
                var tableRange =
                    worksheet.Range(
                        headerRow,
                        1,
                        row - 1,
                        10);

                var table =
                    tableRange.CreateTable();

                table.Theme =
                    XLTableTheme.TableStyleMedium2;
            }


            worksheet.Columns()
                .AdjustToContents();

            worksheet.SheetView
                .FreezeRows(headerRow);

            worksheet.PageSetup.PageOrientation =
                XLPageOrientation.Landscape;


            using var stream =
                new MemoryStream();

            workbook.SaveAs(stream);

            return stream.ToArray();
        }


        
        // PDF EXPORT
        

        public async Task<byte[]> ExportUsersToPdfAsync(
            string? search,
            string? roleFilter,
            string? departmentFilter,
            string? statusFilter)
        {
            var users = await GetUsersAsync(
                search,
                roleFilter,
                departmentFilter,
                statusFilter);

            var document =
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(
                            PageSizes.A4.Landscape());

                        page.Margin(25);

                        page.DefaultTextStyle(
                            x => x.FontSize(8));


        
                        // HEADER
        

                        page.Header()
                            .Column(column =>
                            {
                                column.Item()
                                    .Text(
                                        "User Management System")
                                    .Bold()
                                    .FontSize(18);

                                column.Item()
                                    .Text(
                                        "User Export Report")
                                    .FontSize(12);

                                column.Item()
                                    .Text(
                                        $"Generated On: {DateTime.Now:dd-MM-yyyy HH:mm:ss}")
                                    .FontSize(8);
                            });


        
                        // CONTENT
        

                        page.Content()
                            .PaddingVertical(15)
                            .Table(table =>
                            {
                                // Columns
                                table.ColumnsDefinition(
                                    columns =>
                                    {
                                        columns.ConstantColumn(30);

                                        columns.RelativeColumn(1.2f);

                                        columns.RelativeColumn(1.2f);

                                        columns.RelativeColumn(1.8f);

                                        columns.RelativeColumn(1.3f);

                                        columns.ConstantColumn(65);

                                        columns.RelativeColumn(1.3f);

                                        columns.ConstantColumn(55);
                                    });


                                // Header
                                table.Header(header =>
                                {
                                    AddPdfHeaderCell(
                                        header.Cell(),
                                        "ID");

                                    AddPdfHeaderCell(
                                        header.Cell(),
                                        "Name");

                                    AddPdfHeaderCell(
                                        header.Cell(),
                                        "Username");

                                    AddPdfHeaderCell(
                                        header.Cell(),
                                        "Email");

                                    AddPdfHeaderCell(
                                        header.Cell(),
                                        "Mobile");

                                    AddPdfHeaderCell(
                                        header.Cell(),
                                        "Role");

                                    AddPdfHeaderCell(
                                        header.Cell(),
                                        "Department");

                                    AddPdfHeaderCell(
                                        header.Cell(),
                                        "Status");
                                });


                                // Data
                                foreach (var user in users)
                                {
                                    AddPdfBodyCell(
                                        table.Cell(),
                                        user.Id.ToString());

                                    AddPdfBodyCell(
                                        table.Cell(),
                                        user.Name ?? "");

                                    AddPdfBodyCell(
                                        table.Cell(),
                                        user.UserName ?? "");

                                    AddPdfBodyCell(
                                        table.Cell(),
                                        user.Email ?? "");

                                    AddPdfBodyCell(
                                        table.Cell(),
                                        $"{user.MobileCountryCode} {user.Mobile}");

                                    AddPdfBodyCell(
                                        table.Cell(),
                                        user.Role ?? "");

                                    AddPdfBodyCell(
                                        table.Cell(),
                                        user.Department ?? "");

                                    AddPdfBodyCell(
                                        table.Cell(),
                                        user.IsActive == true
                                            ? "Active"
                                            : "Inactive");
                                }
                            });


        
                        // FOOTER
        

                        page.Footer()
                            .AlignCenter()
                            .Text(text =>
                            {
                                text.Span(
                                    "User Management System | ");

                                text.CurrentPageNumber();

                                text.Span(" / ");

                                text.TotalPages();
                            });
                    });
                });


            // Generate PDF into byte[]
            return document.GeneratePdf();
        }


        
        // PDF HEADER CELL
        

        private static void AddPdfHeaderCell(
            IContainer container,
            string text)
        {
            container
                .Background(Colors.Grey.Darken2)
                .Padding(5)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten1)
                .Text(text)
                .Bold()
                .FontColor(Colors.White)
                .FontSize(8);
        }


        
        // PDF BODY CELL
        

        private static void AddPdfBodyCell(
            IContainer container,
            string text)
        {
            container
                .Padding(4)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Text(text)
                .FontSize(7);
        }
    }
}