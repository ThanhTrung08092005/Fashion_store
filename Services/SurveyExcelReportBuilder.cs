using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Fashion_store.Models;

namespace Fashion_store.Services;

/// <summary>Creates a dependency-free XLSX report with native Excel column charts.</summary>
public static class SurveyExcelReportBuilder
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace OfficeRel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRel = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace ContentType = "http://schemas.openxmlformats.org/package/2006/content-types";
    private static readonly XNamespace Chart = "http://schemas.openxmlformats.org/drawingml/2006/chart";
    private static readonly XNamespace Drawing = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace SheetDrawing = "http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing";

    public static byte[] Create(SurveyStatisticsViewModel report)
    {
        var sheets = new[] { CreateOverview(report), CreateResults(report) };
        var charts = sheets.SelectMany(sheet => sheet.Charts).ToList();
        for (var index = 0; index < charts.Count; index++) charts[index].ChartIndex = index + 1;
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddXml(archive, "[Content_Types].xml", ContentTypesXml(sheets, charts));
            AddXml(archive, "_rels/.rels", PackageRelationshipsXml());
            AddXml(archive, "xl/workbook.xml", WorkbookXml(sheets));
            AddXml(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationshipsXml(sheets));
            AddXml(archive, "xl/styles.xml", StylesXml());
            for (var index = 0; index < sheets.Length; index++)
            {
                var sheetNumber = index + 1;
                var sheet = sheets[index];
                AddXml(archive, $"xl/worksheets/sheet{sheetNumber}.xml", WorksheetXml(sheet));
                if (sheet.Charts.Count == 0) continue;
                AddXml(archive, $"xl/worksheets/_rels/sheet{sheetNumber}.xml.rels", WorksheetRelationshipsXml(sheetNumber));
                AddXml(archive, $"xl/drawings/drawing{sheetNumber}.xml", DrawingXml(sheet.Charts));
                AddXml(archive, $"xl/drawings/_rels/drawing{sheetNumber}.xml.rels", DrawingRelationshipsXml(sheet.Charts));
                foreach (var chart in sheet.Charts)
                    AddXml(archive, $"xl/charts/chart{chart.ChartIndex}.xml", ChartXml(sheet.Name, chart));
            }
        }
        return output.ToArray();
    }

    private static SheetData CreateOverview(SurveyStatisticsViewModel report)
    {
        var sheet = new SheetData("Tổng quan");
        sheet.Text(1, 1, $"Báo cáo khảo sát: {report.TenKhaoSat}", 1);
        sheet.Merge("A1:D1");
        sheet.Text(3, 1, "Mã khảo sát"); sheet.Number(3, 2, report.MaKS, 7);
        sheet.Text(4, 1, "Bắt đầu"); sheet.Date(4, 2, report.NgayBatDau);
        sheet.Text(5, 1, "Kết thúc"); sheet.Date(5, 2, report.NgayKetThuc);
        sheet.Text(6, 1, "Mô tả"); sheet.Text(6, 2, report.MoTa ?? "", 4); sheet.Merge("B6:D6");
        sheet.Text(8, 1, "Tổng quan phiếu", 2); sheet.Text(8, 2, "Số lượng", 3); sheet.Merge("A8:B8");
        sheet.Text(9, 1, "Đã hoàn thành", 4); sheet.Number(9, 2, report.SoPhieuHoanThanh, 7);
        sheet.Text(10, 1, "Chưa hoàn thành", 4); sheet.Number(10, 2, Math.Max(0, report.TongSoPhieu - report.SoPhieuHoanThanh), 7);
        sheet.Text(12, 1, "Tỷ lệ hoàn thành", 4); sheet.Number(12, 2, report.TyLeHoanThanh / 100, 5);
        sheet.Text(13, 1, "Số câu hỏi", 4); sheet.Number(13, 2, report.CauHoi.Count, 7);
        sheet.Text(15, 1, "Tỷ lệ lựa chọn tính trên số người trả lời câu hỏi; câu nhiều lựa chọn có thể có tổng tỷ lệ trên 100%.", 4);
        sheet.Merge("A15:D15");
        sheet.Charts.Add(new ChartData
        {
            Title = "Tình trạng hoàn thành khảo sát", SeriesName = "Số phiếu",
            HeaderRow = 8, DataStartRow = 9, DataEndRow = 10,
            Labels = new[] { "Đã hoàn thành", "Chưa hoàn thành" },
            Values = new[] { (double)report.SoPhieuHoanThanh, (double)Math.Max(0, report.TongSoPhieu - report.SoPhieuHoanThanh) },
            AnchorRow = 1, AnchorColumn = 4
        });
        return sheet;
    }

    private static SheetData CreateResults(SurveyStatisticsViewModel report)
    {
        var sheet = new SheetData("Kết quả");
        sheet.Text(1, 1, $"Kết quả khảo sát: {report.TenKhaoSat}", 1);
        sheet.Merge("A1:D1");
        sheet.Text(2, 1, "Tổng hợp từ các phiếu đã hoàn thành trong SQL Server.", 4);
        sheet.Merge("A2:D2");
        var row = 4;
        foreach (var question in report.CauHoi.OrderBy(item => item.ThuTu))
        {
            var titleRow = row;
            sheet.Text(titleRow, 1, $"Câu {question.ThuTu}: {question.NoiDung}", 2);
            sheet.Merge($"A{titleRow}:C{titleRow}");
            sheet.Text(titleRow + 1, 1, $"Loại câu hỏi: {question.LoaiCauHoi}", 4);
            sheet.Text(titleRow + 1, 2, "Số người trả lời:", 4);
            sheet.Number(titleRow + 1, 3, question.SoNguoiTraLoi, 7);

            if (question.LoaiCauHoi == "Tự luận")
            {
                var header = titleRow + 3;
                sheet.Text(header, 1, "Mã phiếu", 3);
                sheet.Text(header, 2, "Ngày nộp", 3);
                sheet.Text(header, 3, "Câu trả lời", 3);
                if (question.TraLoiTuLuan.Count == 0)
                {
                    sheet.Text(header + 1, 1, "Chưa có câu trả lời tự luận.", 4);
                    row = header + 3;
                    continue;
                }
                var answerRow = header + 1;
                foreach (var answer in question.TraLoiTuLuan)
                {
                    sheet.Number(answerRow, 1, answer.MaPhieuKS, 7);
                    if (answer.NgayNop.HasValue) sheet.Date(answerRow, 2, answer.NgayNop.Value);
                    sheet.Text(answerRow, 3, answer.NoiDung, 4);
                    answerRow++;
                }
                row = answerRow + 2;
                continue;
            }

            if (question.LoaiCauHoi == "Đánh giá sao")
            {
                sheet.Text(titleRow + 2, 1, "Điểm trung bình", 4);
                if (question.DiemTrungBinh.HasValue) sheet.Number(titleRow + 2, 2, question.DiemTrungBinh.Value, 7);
            }
            var headerRow = titleRow + (question.LoaiCauHoi == "Đánh giá sao" ? 4 : 3);
            sheet.Text(headerRow, 1, question.LoaiCauHoi == "Đánh giá sao" ? "Mức sao" : "Phương án", 3);
            sheet.Text(headerRow, 2, "Số lượt chọn", 3);
            sheet.Text(headerRow, 3, "Tỷ lệ", 3);
            var firstDataRow = headerRow + 1;
            var lastDataRow = firstDataRow + question.LuaChons.Count - 1;
            for (var index = 0; index < question.LuaChons.Count; index++)
            {
                var option = question.LuaChons[index];
                var dataRow = firstDataRow + index;
                sheet.Text(dataRow, 1, option.NoiDung, 4);
                sheet.Number(dataRow, 2, option.SoLuotChon, 7);
                sheet.Number(dataRow, 3, option.TyLe / 100, 5);
            }
            if (question.LuaChons.Count > 0)
            {
                sheet.Charts.Add(new ChartData
                {
                    Title = question.LoaiCauHoi == "Đánh giá sao" ? $"Phân bố sao - Câu {question.ThuTu}" : $"Lượt chọn - Câu {question.ThuTu}",
                    SeriesName = "Số lượt chọn", HeaderRow = headerRow,
                    DataStartRow = firstDataRow, DataEndRow = lastDataRow,
                    Labels = question.LuaChons.Select(option => option.NoiDung).ToArray(),
                    Values = question.LuaChons.Select(option => (double)option.SoLuotChon).ToArray(),
                    AnchorRow = titleRow - 1, AnchorColumn = 4
                });
            }
            row = Math.Max(lastDataRow + 2, titleRow + 17);
        }
        return sheet;
    }

    private static XDocument ContentTypesXml(IReadOnlyList<SheetData> sheets, IReadOnlyList<ChartData> charts)
    {
        var root = new XElement(ContentType + "Types",
            new XElement(ContentType + "Default", new XAttribute("Extension", "rels"), new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
            new XElement(ContentType + "Default", new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")),
            new XElement(ContentType + "Override", new XAttribute("PartName", "/xl/workbook.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
            new XElement(ContentType + "Override", new XAttribute("PartName", "/xl/styles.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml")));
        for (var index = 0; index < sheets.Count; index++)
        {
            root.Add(new XElement(ContentType + "Override", new XAttribute("PartName", $"/xl/worksheets/sheet{index + 1}.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")));
            if (sheets[index].Charts.Count > 0)
                root.Add(new XElement(ContentType + "Override", new XAttribute("PartName", $"/xl/drawings/drawing{index + 1}.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.drawing+xml")));
        }
        foreach (var chart in charts)
            root.Add(new XElement(ContentType + "Override", new XAttribute("PartName", $"/xl/charts/chart{chart.ChartIndex}.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.drawingml.chart+xml")));
        return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), root);
    }

    private static XDocument PackageRelationshipsXml() => new(
        new XDeclaration("1.0", "UTF-8", "yes"),
        new XElement(PackageRel + "Relationships",
            new XElement(PackageRel + "Relationship", new XAttribute("Id", "rId1"),
                new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
                new XAttribute("Target", "xl/workbook.xml"))));

    private static XDocument WorkbookXml(IReadOnlyList<SheetData> sheets) => new(
        new XDeclaration("1.0", "UTF-8", "yes"),
        new XElement(Main + "workbook", new XAttribute(XNamespace.Xmlns + "r", OfficeRel),
            new XElement(Main + "bookViews", new XElement(Main + "workbookView", new XAttribute("activeTab", 0))),
            new XElement(Main + "sheets", sheets.Select((sheet, index) =>
                new XElement(Main + "sheet", new XAttribute("name", sheet.Name),
                    new XAttribute("sheetId", index + 1), new XAttribute(OfficeRel + "id", $"rId{index + 1}")))),
            new XElement(Main + "calcPr", new XAttribute("calcId", 191029))));

    private static XDocument WorkbookRelationshipsXml(IReadOnlyList<SheetData> sheets)
    {
        var root = new XElement(PackageRel + "Relationships");
        for (var index = 0; index < sheets.Count; index++)
            root.Add(new XElement(PackageRel + "Relationship", new XAttribute("Id", $"rId{index + 1}"),
                new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                new XAttribute("Target", $"worksheets/sheet{index + 1}.xml")));
        root.Add(new XElement(PackageRel + "Relationship", new XAttribute("Id", $"rId{sheets.Count + 1}"),
            new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"),
            new XAttribute("Target", "styles.xml")));
        return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), root);
    }

    private static XDocument WorksheetXml(SheetData sheet)
    {
        var maxRow = Math.Max(1, sheet.Cells.Keys.DefaultIfEmpty(1).Max());
        var maxColumn = Math.Max(4, sheet.Cells.Values.SelectMany(row => row.Keys).DefaultIfEmpty(1).Max());
        var sheetData = new XElement(Main + "sheetData");
        foreach (var row in sheet.Cells)
        {
            var rowElement = new XElement(Main + "row", new XAttribute("r", row.Key));
            foreach (var cell in row.Value)
            {
                var cellElement = new XElement(Main + "c", new XAttribute("r", $"{ColumnName(cell.Key)}{row.Key}"));
                if (cell.Value.Style > 0) cellElement.Add(new XAttribute("s", cell.Value.Style));
                if (cell.Value.Number.HasValue)
                    cellElement.Add(new XElement(Main + "v", cell.Value.Number.Value.ToString("G17", CultureInfo.InvariantCulture)));
                else
                {
                    cellElement.Add(new XAttribute("t", "inlineStr"));
                    cellElement.Add(new XElement(Main + "is",
                        new XElement(Main + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), CleanXml(cell.Value.Text ?? ""))));
                }
                rowElement.Add(cellElement);
            }
            sheetData.Add(rowElement);
        }

        var children = new List<object>
        {
            new XElement(Main + "dimension", new XAttribute("ref", $"A1:{ColumnName(maxColumn)}{maxRow}")),
            new XElement(Main + "sheetViews", new XElement(Main + "sheetView", new XAttribute("workbookViewId", 0))),
            new XElement(Main + "sheetFormatPr", new XAttribute("defaultRowHeight", 18)),
            new XElement(Main + "cols",
                new XElement(Main + "col", new XAttribute("min", 1), new XAttribute("max", 1), new XAttribute("width", 42), new XAttribute("customWidth", 1)),
                new XElement(Main + "col", new XAttribute("min", 2), new XAttribute("max", 2), new XAttribute("width", 20), new XAttribute("customWidth", 1)),
                new XElement(Main + "col", new XAttribute("min", 3), new XAttribute("max", 3), new XAttribute("width", 64), new XAttribute("customWidth", 1)),
                new XElement(Main + "col", new XAttribute("min", 4), new XAttribute("max", 4), new XAttribute("width", 18), new XAttribute("customWidth", 1)),
                new XElement(Main + "col", new XAttribute("min", 5), new XAttribute("max", 12), new XAttribute("width", 12), new XAttribute("customWidth", 1))),
            sheetData
        };
        if (sheet.Merged.Count > 0)
            children.Add(new XElement(Main + "mergeCells", new XAttribute("count", sheet.Merged.Count), sheet.Merged.Select(reference => new XElement(Main + "mergeCell", new XAttribute("ref", reference)))));
        if (sheet.Charts.Count > 0) children.Add(new XElement(Main + "drawing", new XAttribute(OfficeRel + "id", "rId1")));
        return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(Main + "worksheet", new XAttribute(XNamespace.Xmlns + "r", OfficeRel), children));
    }

    private static XDocument StylesXml()
    {
        var fonts = new XElement(Main + "fonts", new XAttribute("count", 4),
            Font(11, "FF1F2937", false), Font(18, "FFFFFFFF", true), Font(11, "FFFFFFFF", true), Font(11, "FF173A6A", true));
        var fills = new XElement(Main + "fills", new XAttribute("count", 4),
            new XElement(Main + "fill", new XElement(Main + "patternFill", new XAttribute("patternType", "none"))),
            new XElement(Main + "fill", new XElement(Main + "patternFill", new XAttribute("patternType", "gray125"))),
            SolidFill("FF1F4E78"), SolidFill("FFDCEAF7"));
        var borders = new XElement(Main + "borders", new XAttribute("count", 2),
            new XElement(Main + "border", new XElement(Main + "left"), new XElement(Main + "right"), new XElement(Main + "top"), new XElement(Main + "bottom"), new XElement(Main + "diagonal")),
            new XElement(Main + "border", new XElement(Main + "left"), new XElement(Main + "right"), new XElement(Main + "top"),
                new XElement(Main + "bottom", new XAttribute("style", "thin"), new XElement(Main + "color", new XAttribute("rgb", "FFD9E2F3"))),
                new XElement(Main + "diagonal")));
        var styleFormats = new XElement(Main + "cellStyleXfs", new XAttribute("count", 1),
            new XElement(Main + "xf", new XAttribute("numFmtId", 0), new XAttribute("fontId", 0), new XAttribute("fillId", 0), new XAttribute("borderId", 0)));
        var formats = new XElement(Main + "cellXfs", new XAttribute("count", 8),
            Format(0, 0, 0, 0),
            Format(0, 1, 2, 0, new XAttribute("horizontal", "center"), new XAttribute("vertical", "center")),
            Format(0, 3, 3, 1, new XAttribute("horizontal", "left"), new XAttribute("vertical", "center"), new XAttribute("wrapText", 1)),
            Format(0, 2, 2, 1, new XAttribute("horizontal", "center"), new XAttribute("vertical", "center"), new XAttribute("wrapText", 1)),
            Format(0, 0, 0, 1, new XAttribute("vertical", "top"), new XAttribute("wrapText", 1)),
            Format(164, 0, 0, 1, new XAttribute("horizontal", "right")),
            Format(165, 0, 0, 1, new XAttribute("horizontal", "left")),
            Format(0, 0, 0, 1, new XAttribute("horizontal", "right")));
        return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(Main + "styleSheet",
                new XElement(Main + "numFmts", new XAttribute("count", 2),
                    new XElement(Main + "numFmt", new XAttribute("numFmtId", 164), new XAttribute("formatCode", "0.0%")),
                    new XElement(Main + "numFmt", new XAttribute("numFmtId", 165), new XAttribute("formatCode", "dd/mm/yyyy hh:mm:ss"))),
                fonts, fills, borders, styleFormats, formats,
                new XElement(Main + "cellStyles", new XAttribute("count", 1), new XElement(Main + "cellStyle", new XAttribute("name", "Normal"), new XAttribute("xfId", 0), new XAttribute("builtinId", 0))),
                new XElement(Main + "dxfs", new XAttribute("count", 0)),
                new XElement(Main + "tableStyles", new XAttribute("count", 0), new XAttribute("defaultTableStyle", "TableStyleMedium2"), new XAttribute("defaultPivotStyle", "PivotStyleLight16"))));
    }

    private static XElement Font(int size, string color, bool bold) => new(Main + "font",
        bold ? new XElement(Main + "b") : null,
        new XElement(Main + "sz", new XAttribute("val", size)),
        new XElement(Main + "color", new XAttribute("rgb", color)),
        new XElement(Main + "name", new XAttribute("val", "Calibri")));

    private static XElement SolidFill(string color) => new(Main + "fill",
        new XElement(Main + "patternFill", new XAttribute("patternType", "solid"),
            new XElement(Main + "fgColor", new XAttribute("rgb", color)),
            new XElement(Main + "bgColor", new XAttribute("indexed", 64))));

    private static XElement Format(uint numberFormat, int font, int fill, int border, params XAttribute[] alignment)
    {
        var element = new XElement(Main + "xf",
            new XAttribute("numFmtId", numberFormat), new XAttribute("fontId", font),
            new XAttribute("fillId", fill), new XAttribute("borderId", border),
            new XAttribute("xfId", 0), new XAttribute("applyFont", 1),
            new XAttribute("applyFill", 1), new XAttribute("applyBorder", 1));
        if (numberFormat != 0) element.Add(new XAttribute("applyNumberFormat", 1));
        if (alignment.Length > 0)
        {
            element.Add(new XAttribute("applyAlignment", 1));
            element.Add(new XElement(Main + "alignment", alignment));
        }
        return element;
    }

    private static XDocument WorksheetRelationshipsXml(int number) => new(
        new XDeclaration("1.0", "UTF-8", "yes"),
        new XElement(PackageRel + "Relationships",
            new XElement(PackageRel + "Relationship", new XAttribute("Id", "rId1"),
                new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/drawing"),
                new XAttribute("Target", $"../drawings/drawing{number}.xml"))));

    private static XDocument DrawingRelationshipsXml(IReadOnlyList<ChartData> charts)
    {
        var root = new XElement(PackageRel + "Relationships");
        for (var index = 0; index < charts.Count; index++)
            root.Add(new XElement(PackageRel + "Relationship", new XAttribute("Id", $"rId{index + 1}"),
                new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/chart"),
                new XAttribute("Target", $"../charts/chart{charts[index].ChartIndex}.xml")));
        return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), root);
    }

    private static XDocument DrawingXml(IReadOnlyList<ChartData> charts)
    {
        var anchors = new List<XElement>();
        for (var index = 0; index < charts.Count; index++)
        {
            const long width = 7620000;
            const long height = 4572000;
            var chart = charts[index];
            anchors.Add(new XElement(SheetDrawing + "oneCellAnchor",
                new XElement(SheetDrawing + "from", new XElement(SheetDrawing + "col", chart.AnchorColumn),
                    new XElement(SheetDrawing + "colOff", 0), new XElement(SheetDrawing + "row", chart.AnchorRow),
                    new XElement(SheetDrawing + "rowOff", 0)),
                new XElement(SheetDrawing + "ext", new XAttribute("cx", width), new XAttribute("cy", height)),
                new XElement(SheetDrawing + "graphicFrame", new XAttribute("macro", ""),
                    new XElement(SheetDrawing + "nvGraphicFramePr",
                        new XElement(SheetDrawing + "cNvPr", new XAttribute("id", index + 2), new XAttribute("name", $"Chart {chart.ChartIndex}")),
                        new XElement(SheetDrawing + "cNvGraphicFramePr")),
                    new XElement(SheetDrawing + "xfrm", new XElement(Drawing + "off", new XAttribute("x", 0), new XAttribute("y", 0)),
                        new XElement(Drawing + "ext", new XAttribute("cx", width), new XAttribute("cy", height))),
                    new XElement(Drawing + "graphic",
                        new XElement(Drawing + "graphicData", new XAttribute("uri", "http://schemas.openxmlformats.org/drawingml/2006/chart"),
                            new XElement(Chart + "chart", new XAttribute(OfficeRel + "id", $"rId{index + 1}"))))),
                new XElement(SheetDrawing + "clientData")));
        }
        return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(SheetDrawing + "wsDr", new XAttribute(XNamespace.Xmlns + "a", Drawing),
                new XAttribute(XNamespace.Xmlns + "r", OfficeRel), anchors));
    }

    private static XDocument ChartXml(string sheetName, ChartData chart)
    {
        var axisId = 48650112u + (uint)(chart.ChartIndex * 2);
        var valueAxisId = axisId + 1;
        var sheet = $"'{sheetName.Replace("'", "''", StringComparison.Ordinal)}'";
        var seriesNameFormula = $"{sheet}!$B${chart.HeaderRow}";
        var categoryFormula = $"{sheet}!$A${chart.DataStartRow}:$A${chart.DataEndRow}";
        var valueFormula = $"{sheet}!$B${chart.DataStartRow}:$B${chart.DataEndRow}";
        var series = new XElement(Chart + "ser",
            new XElement(Chart + "idx", new XAttribute("val", 0)),
            new XElement(Chart + "order", new XAttribute("val", 0)),
            new XElement(Chart + "tx", new XElement(Chart + "strRef", new XElement(Chart + "f", seriesNameFormula),
                new XElement(Chart + "strCache", new XElement(Chart + "ptCount", new XAttribute("val", 1)),
                    new XElement(Chart + "pt", new XAttribute("idx", 0), new XElement(Chart + "v", CleanXml(chart.SeriesName)))))),
            new XElement(Chart + "cat", new XElement(Chart + "strRef", new XElement(Chart + "f", categoryFormula), StringCache(chart.Labels))),
            new XElement(Chart + "val", new XElement(Chart + "numRef", new XElement(Chart + "f", valueFormula), NumberCache(chart.Values))));
        var title = new XElement(Chart + "title",
            new XElement(Chart + "tx", new XElement(Chart + "rich",
                new XElement(Drawing + "bodyPr"), new XElement(Drawing + "lstStyle"),
                new XElement(Drawing + "p",
                    new XElement(Drawing + "pPr", new XElement(Drawing + "defRPr", new XAttribute("lang", "vi-VN"))),
                    new XElement(Drawing + "r", new XElement(Drawing + "rPr", new XAttribute("lang", "vi-VN"), new XAttribute("sz", 1300), new XAttribute("b", 1)),
                        new XElement(Drawing + "t", CleanXml(chart.Title))),
                    new XElement(Drawing + "endParaRPr", new XAttribute("lang", "vi-VN"))))),
            new XElement(Chart + "overlay", new XAttribute("val", 0)));
        var bar = new XElement(Chart + "barChart",
            new XElement(Chart + "barDir", new XAttribute("val", "col")),
            new XElement(Chart + "grouping", new XAttribute("val", "clustered")),
            new XElement(Chart + "varyColors", new XAttribute("val", 0)), series,
            new XElement(Chart + "gapWidth", new XAttribute("val", 80)),
            new XElement(Chart + "axId", new XAttribute("val", axisId)),
            new XElement(Chart + "axId", new XAttribute("val", valueAxisId)));
        var categoryAxis = new XElement(Chart + "catAx",
            new XElement(Chart + "axId", new XAttribute("val", axisId)),
            new XElement(Chart + "scaling", new XElement(Chart + "orientation", new XAttribute("val", "minMax"))),
            new XElement(Chart + "delete", new XAttribute("val", 0)), new XElement(Chart + "axPos", new XAttribute("val", "b")),
            new XElement(Chart + "tickLblPos", new XAttribute("val", "nextTo")),
            new XElement(Chart + "crossAx", new XAttribute("val", valueAxisId)), new XElement(Chart + "crosses", new XAttribute("val", "autoZero")),
            new XElement(Chart + "auto", new XAttribute("val", 1)), new XElement(Chart + "lblAlgn", new XAttribute("val", "ctr")),
            new XElement(Chart + "lblOffset", new XAttribute("val", 100)));
        var valueAxis = new XElement(Chart + "valAx",
            new XElement(Chart + "axId", new XAttribute("val", valueAxisId)),
            new XElement(Chart + "scaling", new XElement(Chart + "orientation", new XAttribute("val", "minMax"))),
            new XElement(Chart + "delete", new XAttribute("val", 0)), new XElement(Chart + "axPos", new XAttribute("val", "l")),
            new XElement(Chart + "majorGridlines", new XElement(Chart + "spPr", new XElement(Drawing + "ln", new XAttribute("w", 9525),
                new XElement(Drawing + "solidFill", new XElement(Drawing + "srgbClr", new XAttribute("val", "D9E2F3")))))),
            new XElement(Chart + "numFmt", new XAttribute("formatCode", "0"), new XAttribute("sourceLinked", 1)),
            new XElement(Chart + "tickLblPos", new XAttribute("val", "nextTo")),
            new XElement(Chart + "crossAx", new XAttribute("val", axisId)), new XElement(Chart + "crosses", new XAttribute("val", "autoZero")),
            new XElement(Chart + "crossBetween", new XAttribute("val", "between")));
        return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(Chart + "chartSpace", new XAttribute(XNamespace.Xmlns + "a", Drawing),
                new XElement(Chart + "lang", new XAttribute("val", "vi-VN")), new XElement(Chart + "style", new XAttribute("val", 10)),
                new XElement(Chart + "chart", title,
                    new XElement(Chart + "plotArea", new XElement(Chart + "layout"), bar, categoryAxis, valueAxis),
                    new XElement(Chart + "legend", new XElement(Chart + "legendPos", new XAttribute("val", "b")), new XElement(Chart + "overlay", new XAttribute("val", 0))),
                    new XElement(Chart + "plotVisOnly", new XAttribute("val", 1)),
                    new XElement(Chart + "dispBlanksAs", new XAttribute("val", "gap")))));
    }

    private static XElement StringCache(IReadOnlyList<string> values) => new(Chart + "strCache",
        new XElement(Chart + "ptCount", new XAttribute("val", values.Count)),
        values.Select((value, index) => new XElement(Chart + "pt", new XAttribute("idx", index), new XElement(Chart + "v", CleanXml(value)))));

    private static XElement NumberCache(IReadOnlyList<double> values) => new(Chart + "numCache",
        new XElement(Chart + "formatCode", "General"), new XElement(Chart + "ptCount", new XAttribute("val", values.Count)),
        values.Select((value, index) => new XElement(Chart + "pt", new XAttribute("idx", index),
            new XElement(Chart + "v", value.ToString("G17", CultureInfo.InvariantCulture)))));

    private static void AddXml(ZipArchive archive, string path, XDocument document)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = false });
        document.Save(writer);
    }

    private static string ColumnName(int column)
    {
        var result = "";
        while (column > 0) { column--; result = (char)('A' + column % 26) + result; column /= 26; }
        return result;
    }

    private static string CleanXml(string value) => string.Concat(value.Where(XmlConvert.IsXmlChar));
    private static double ExcelSerial(DateTime date) => (date - new DateTime(1899, 12, 30)).TotalDays;

    private sealed class SheetData(string name)
    {
        public string Name { get; } = name;
        public SortedDictionary<int, SortedDictionary<int, CellData>> Cells { get; } = new();
        public List<string> Merged { get; } = new();
        public List<ChartData> Charts { get; } = new();
        public void Text(int row, int column, string value, int style = 0) => Set(row, column, new CellData(CleanXml(value), null, style));
        public void Number(int row, int column, double value, int style = 0) => Set(row, column, new CellData(null, value, style));
        public void Date(int row, int column, DateTime date) => Number(row, column, ExcelSerial(date), 6);
        public void Merge(string range) => Merged.Add(range);
        private void Set(int row, int column, CellData cell)
        {
            if (!Cells.TryGetValue(row, out var rowCells)) Cells[row] = rowCells = new SortedDictionary<int, CellData>();
            rowCells[column] = cell;
        }
    }

    private sealed record CellData(string? Text, double? Number, int Style);
    private sealed class ChartData
    {
        public int ChartIndex { get; set; }
        public string Title { get; init; } = "";
        public string SeriesName { get; init; } = "";
        public int HeaderRow { get; init; }
        public int DataStartRow { get; init; }
        public int DataEndRow { get; init; }
        public IReadOnlyList<string> Labels { get; init; } = Array.Empty<string>();
        public IReadOnlyList<double> Values { get; init; } = Array.Empty<double>();
        public int AnchorRow { get; init; }
        public int AnchorColumn { get; init; }
    }
}
