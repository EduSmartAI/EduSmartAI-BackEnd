using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StudentService.Application.Applications.LearningPaths.Commands.ExportSubjectMarkUpdateWord;
using StudentService.Application.Interfaces;
using System.Text.RegularExpressions;

namespace StudentService.Infrastructure.Implements;

public class PdfExportService : IPdfExportService
{
    public Task<byte[]> GenerateSubjectMarkUpdatePdfDocumentAsync(
        List<SubjectMarkUpdateDto> subjectMarkUpdates,
        string? title = null,
        string? studentName = null,
        CancellationToken cancellationToken = default)
    {
        if (subjectMarkUpdates == null || subjectMarkUpdates.Count == 0)
            throw new ArgumentException("SubjectMarkUpdates cannot be null or empty", nameof(subjectMarkUpdates));

        QuestPDF.Settings.License = LicenseType.Community;

        var now = DateTime.Now;
        var subjects = subjectMarkUpdates.ToList();

        // Tính lại từ OldMark và NewMark để đảm bảo chính xác (chỉ tính cho các môn có OldMark)
        var subjectsWithOldMark = subjects.Where(s => s.OldMark.HasValue).ToList();
        var improvedCount = subjectsWithOldMark.Count(s => s.NewMark > s.OldMark!.Value);
        var declinedCount = subjectsWithOldMark.Count(s => s.NewMark < s.OldMark!.Value);
        var unchangedCount = subjectsWithOldMark.Count(s => s.NewMark == s.OldMark!.Value);
        var hasComparisonData = subjectsWithOldMark.Count > 0;

        var doc = Document.Create(container =>
        {
            // ======= SUMMARY + TABLE + SNAPSHOT CHARTS (auto paginate) =======
            container.Page(page =>
            {
                ApplyBasePage(page);

                page.Header().Element(x => BuildMainHeader(
                    x,
                    title ?? "Báo cáo phân tích điểm số môn học",
                    studentName,
                    now));

                page.Footer().Element(BuildFooter);

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Spacing(14);

                    // --- Summary cards ---
                    col.Item().Element(card =>
                        SectionCard(card, "Tổng quan", section =>
                        {
                            section.Spacing(10);

                            section.Item().Row(r =>
                            {
                                r.RelativeItem().Text($"Tổng số môn được phân tích: {subjects.Count}")
                                    .FontSize(11).FontColor(Theme.TextSub);

                                r.ConstantItem(180).AlignRight().Text($"Cập nhật lúc: {now:dd/MM/yyyy HH:mm}")
                                    .FontSize(9).FontColor(Theme.TextHint);
                            });

                            if (hasComparisonData)
                            {
                                section.Item().Row(r =>
                                {
                                    r.Spacing(10);

                                    r.RelativeItem().Element(c => StatPill(c,
                                        label: "Số môn có điểm cải thiện",
                                        value: improvedCount.ToString(),
                                        bg: Theme.SuccessSoft,
                                        border: Theme.SuccessBorder,
                                        fg: Theme.SuccessText));

                                    r.RelativeItem().Element(c => StatPill(c,
                                        label: "Số môn có điểm giảm",
                                        value: declinedCount.ToString(),
                                        bg: Theme.DangerSoft,
                                        border: Theme.DangerBorder,
                                        fg: Theme.DangerText));

                                    r.RelativeItem().Element(c => StatPill(c,
                                        label: "Không đổi",
                                        value: unchangedCount.ToString(),
                                        bg: Theme.NeutralSoft,
                                        border: Theme.NeutralBorder,
                                        fg: Theme.TextMain));
                                });
                            }
                            else
                            {
                                section.Item().Text("Không có dữ liệu so sánh điểm số (chưa có điểm cũ).")
                                    .FontSize(10).FontColor(Theme.TextHint).Italic();
                            }
                        })
                    );

                    // --- Table title ---
                    col.Item().AlignCenter().Text("Bảng tổng hợp điểm số")
                        .FontSize(15).Bold().FontColor(Theme.Primary);

                    // --- Table ---
                    col.Item().Element(x => BuildSummaryTable(x, subjects));

                    // --- Snapshot charts title (chỉ hiển thị nếu có dữ liệu so sánh) ---
                    if (hasComparisonData)
                    {
                        col.Item().PaddingTop(6).AlignCenter()
                            .Text("Snapshot so sánh điểm (cũ vs mới)")
                            .FontSize(14).Bold().FontColor(Theme.Primary);

                        // --- Grid of mini cards (2 columns) ---
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });

                            for (int i = 0; i < subjectsWithOldMark.Count; i++)
                            {
                                var s = subjectsWithOldMark[i];

                                table.Cell().Padding(5).Element(cell =>
                                    MiniSubjectCard(cell, s)
                                );

                                // Fill last empty cell if odd
                                if (i == subjectsWithOldMark.Count - 1 && subjectsWithOldMark.Count % 2 == 1)
                                    table.Cell().Padding(5).Element(e => e.Height(0));
                            }
                        });
                    }
                });
            });

            // ======= DETAIL PAGES: 2 pages per subject (cleaner & consistent) =======
            foreach (var subject in subjects)
            {
                // Page A: Overview + ImprovementAnalysis
                container.Page(page =>
                {
                    ApplyBasePage(page);

                    page.Header().Element(x => BuildSubjectHeader(x, subject, studentName));
                    page.Footer().Element(BuildFooter);

                    page.Content().PaddingTop(14).Column(col =>
                    {
                        col.Spacing(14);

                        col.Item().Element(x => SubjectScoreHero(x, subject));

                        col.Item().Element(card =>
                            SectionCard(card, "Phân tích cải thiện điểm số", section =>
                            {
                                section.Spacing(8);
                                section.Item().Element(box =>
                                    box.Background(Colors.White)
                                       .Border(1).BorderColor(Theme.BorderSoft)
                                       .Padding(14)
                                       .Column(md => ParseMarkdownToPdf(md, subject.ImprovementAnalysis))
                                );
                            })
                        );
                    });
                });

                // Page B: ComparisonAnalysis (chỉ tạo nếu có ComparisonAnalysis)
                if (!string.IsNullOrWhiteSpace(subject.ComparisonAnalysis))
                {
                    container.Page(page =>
                    {
                        ApplyBasePage(page);

                        page.Header().Element(x => BuildSubjectHeader(x, subject, studentName));
                        page.Footer().Element(BuildFooter);

                        page.Content().PaddingTop(14).Column(col =>
                        {
                            col.Spacing(14);

                            col.Item().Element(card =>
                                SectionCard(card, "So sánh chi tiết", section =>
                                {
                                    section.Spacing(8);
                                    section.Item().Element(box =>
                                        box.Background(Colors.White)
                                           .Border(1).BorderColor(Theme.BorderSoft)
                                           .Padding(14)
                                           .Column(md => ParseMarkdownToPdf(md, subject.ComparisonAnalysis))
                                    );
                                })
                            );
                        });
                    });
                }
            }
        });

        return Task.FromResult(doc.GeneratePdf());
    }

    // =========================
    // PAGE BASE + THEME
    // =========================
    private static void ApplyBasePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(1.4f, Unit.Centimetre);
        page.PageColor(Theme.Page);
        page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Theme.Font).FontColor(Theme.TextMain));
    }
    private static class Theme
    {
        // Professional modern palette - Premium design
        public static string Font => "Arial"; // safe default; server-friendly

        public static string Page => "#FAFBFC"; // softer, cleaner background

        public static string Primary => "#2563EB";      // vibrant blue 600 - more modern
        public static string PrimaryDark => "#1E40AF";  // deep blue 700 - richer
        public static string Lavender => "#EFF6FF";     // soft blue tint - lighter, fresher
        public static string Beige => "#FEF3C7";        // warm amber tint - more elegant

        public static string TextMain => "#111827";     // gray 900 - deeper black
        public static string TextSub => "#374151";      // gray 700 - better contrast
        public static string TextHint => "#6B7280";     // gray 500 - softer hint

        public static string BorderSoft => "#E5E7EB";   // gray 200 - cleaner
        public static string BorderStrong => "#D1D5DB"; // gray 300 - more defined

        public static string NeutralSoft => "#F9FAFB";  // gray 50 - very light
        public static string NeutralBorder => "#E5E7EB"; // gray 200

        public static string SuccessSoft => "#D1FAE5";  // emerald 100 - brighter, fresher
        public static string SuccessBorder => "#6EE7B7"; // emerald 300 - more vibrant
        public static string SuccessText => "#047857";  // emerald 700 - deeper green

        public static string DangerSoft => "#FEE2E2";   // red 100 - softer pink
        public static string DangerBorder => "#FCA5A5";  // red 300 - warmer red
        public static string DangerText => "#DC2626";   // red 600 - bold red

        public static string Accent => "#F59E0B"; // amber 500 - warm, professional
    }

    // =========================
    // HEADER / FOOTER
    // =========================
    private static IContainer BuildMainHeader(IContainer c, string title, string? studentName, DateTime now)
    {
        c.Background(Theme.Lavender)
         .Border(1).BorderColor(Theme.BorderStrong)
         .Padding(16)
         .Row(row =>
            {
                // Left accent bar
                row.ConstantItem(6).Background(Theme.Primary);

                row.ConstantItem(12);

                // Title block
                row.RelativeItem().Column(col =>
                {
                    col.Spacing(4);

                    col.Item().Text(title)
                        .FontSize(18)
                        .Bold()
                        .FontColor(Theme.PrimaryDark);

                    if (!string.IsNullOrWhiteSpace(studentName))
                    {
                        col.Item().Text($"Sinh viên: {studentName}")
                            .FontSize(10)
                            .FontColor(Theme.TextSub);
                    }

                    col.Item().Text($"Ngày tạo: {now:dd/MM/yyyy HH:mm}")
                        .FontSize(9)
                        .FontColor(Theme.TextHint);
                });

                // Right badge
                row.ConstantItem(110).AlignRight().AlignMiddle()
                    .Background(Theme.Beige)
                    .Border(1).BorderColor(Theme.BorderStrong)
                    .PaddingVertical(8).PaddingHorizontal(10)
                    .AlignCenter()
                    .Text("MARK UPDATE REPORT")
                    .FontSize(8)
                    .Bold()
                    .FontColor(Theme.TextSub);
            });
        return c;
    }

    private static IContainer BuildSubjectHeader(IContainer c, SubjectMarkUpdateDto subject, string? studentName)
    {
        c.Background(Colors.White)
         .Border(1).BorderColor(Theme.BorderSoft)
         .Padding(14)
         .Row(row =>
            {
                row.ConstantItem(6).Background(Theme.Primary);
                row.ConstantItem(12);

                row.RelativeItem().Column(col =>
                {
                    col.Spacing(3);

                    col.Item().Text($"{subject.SubjectCode}")
                        .FontSize(14).Bold().FontColor(Theme.PrimaryDark);

                    col.Item().Text(subject.SubjectName)
                        .FontSize(10).FontColor(Theme.TextSub).LineHeight(1.2f);

                    if (!string.IsNullOrWhiteSpace(studentName))
                        col.Item().Text($"Sinh viên: {studentName}")
                            .FontSize(9).FontColor(Theme.TextHint);
                });

                if (subject.OldMark.HasValue)
                {
                    var (deltaText, bg, fg, border) = DeltaBadge(subject.OldMark.Value, subject.NewMark);
                    row.ConstantItem(140).AlignRight().AlignMiddle()
                        .Background(bg)
                        .Border(1).BorderColor(border)
                        .PaddingVertical(8).PaddingHorizontal(10)
                        .AlignCenter()
                        .Text(deltaText)
                        .FontSize(10).Bold().FontColor(fg);
                }
                else
                {
                    var (bg, fg, border) = GetScoreColor(subject.NewMark);
                    row.ConstantItem(140).AlignRight().AlignMiddle()
                        .Background(bg)
                        .Border(1).BorderColor(border)
                        .PaddingVertical(8).PaddingHorizontal(10)
                        .AlignCenter()
                        .Text($"{subject.NewMark:F1} điểm")
                        .FontSize(10).Bold().FontColor(fg);
                }
            });
        return c;
    }

    private static void BuildFooter(IContainer c)
    {
        c.PaddingTop(6).AlignCenter().Text(t =>
        {
            t.DefaultTextStyle(TextStyle.Default.FontSize(9).FontColor(Theme.TextHint));
            t.Span("Trang ");
            t.CurrentPageNumber();
            t.Span(" / ");
            t.TotalPages();
        });
    }

    // =========================
    // SECTIONS / CARDS
    // =========================
    private static IContainer SectionCard(IContainer c, string title, Action<ColumnDescriptor> content)
    {
        c.Background(Colors.White)
         .Border(1).BorderColor(Theme.BorderSoft)
         .Padding(14)
         .Column(col =>
            {
                col.Spacing(10);

                col.Item().Row(r =>
                {
                    r.ConstantItem(4).Height(16).Background(Theme.Accent);
                    r.ConstantItem(10);
                    r.RelativeItem().Text(title)
                        .FontSize(13).Bold().FontColor(Theme.TextMain);
                });

                col.Item().LineHorizontal(1).LineColor(Theme.BorderSoft);

                col.Item().Column(content);
            });
        return c;
    }

    private static IContainer StatPill(IContainer c, string label, string value, string bg, string border, string fg)
    {
        c.Background(bg)
         .Border(1).BorderColor(border)
         .Padding(10)
         .Column(col =>
            {
                col.Spacing(3);
                col.Item().Text(label).FontSize(9).FontColor(Theme.TextSub);
                col.Item().Text(value).FontSize(18).Bold().FontColor(fg);
            });
        return c;
    }

    // =========================
    // SUMMARY TABLE
    // =========================
    private static IContainer BuildSummaryTable(IContainer c, List<SubjectMarkUpdateDto> subjects)
    {
        c.Background(Colors.White)
         .Border(1).BorderColor(Theme.BorderSoft)
         .Padding(10)
         .Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(1.2f); // code
                    cols.RelativeColumn(2.8f); // name
                    cols.RelativeColumn(1.0f); // old
                    cols.RelativeColumn(1.0f); // new
                    cols.RelativeColumn(1.0f); // delta
                });

                // Header
                table.Header(h =>
                {
                    h.Cell().Element(th => TableHeaderCell(th, alignLeft: true).Text("Mã môn").Bold());
                    h.Cell().Element(th => TableHeaderCell(th, alignLeft: true).Text("Tên môn").Bold());
                    h.Cell().Element(th => TableHeaderCell(th).Text("Điểm cũ").Bold());
                    h.Cell().Element(th => TableHeaderCell(th).Text("Điểm mới").Bold());
                    h.Cell().Element(th => TableHeaderCell(th).Text("Thay đổi").Bold());
                });

                for (int i = 0; i < subjects.Count; i++)
                {
                    var s = subjects[i];
                    var zebra = i % 2 == 0 ? (string)Colors.White : "#F8FAFF";

                    table.Cell().Element(td => TableBodyCell(td, zebra, alignLeft: true).Text(s.SubjectCode).FontColor(Theme.TextMain));
                    table.Cell().Element(td => TableBodyCell(td, zebra, alignLeft: true).Text(s.SubjectName).FontColor(Theme.TextSub));
                    table.Cell().Element(td => TableBodyCell(td, zebra).Text(s.OldMark?.ToString("F1") ?? "—").FontColor(Theme.TextMain));
                    table.Cell().Element(td => TableBodyCell(td, zebra).Text(s.NewMark.ToString("F1")).FontColor(Theme.TextMain));

                    table.Cell().Element(td =>
                    {
                        if (s.OldMark.HasValue)
                        {
                            var (deltaText, bg, fg, border) = DeltaBadge(s.OldMark.Value, s.NewMark);
                            td.Background(zebra)
                              .PaddingVertical(6).PaddingHorizontal(6)
                              .AlignCenter().AlignMiddle()
                              .Element(x =>
                              {
                                  x.Background(bg)
                                   .Border(1).BorderColor(border)
                                   .PaddingVertical(6).PaddingHorizontal(8)
                                   .AlignCenter().AlignMiddle()
                                   .Text(deltaText).FontSize(9).Bold().FontColor(fg);
                              });
                        }
                        else
                        {
                            td.Background(zebra)
                              .PaddingVertical(6).PaddingHorizontal(6)
                              .AlignCenter().AlignMiddle()
                              .Text("—")
                              .FontSize(9)
                              .FontColor(Theme.TextHint);
                        }
                    });
                }
            });
        return c;
    }

    private static IContainer TableHeaderCell(IContainer c, bool alignLeft = false)
    {
        var cell = c.Background(Theme.Lavender)
                    .BorderBottom(1).BorderColor(Theme.BorderStrong)
                    .PaddingVertical(8).PaddingHorizontal(8)
                    .AlignMiddle();

        return alignLeft ? cell.AlignLeft() : cell.AlignCenter();
    }

    private static IContainer TableBodyCell(IContainer c, string zebra, bool alignLeft = false)
    {
        var cell = c.Background(zebra)
                    .BorderBottom(1).BorderColor(Theme.BorderSoft)
                    .PaddingVertical(8).PaddingHorizontal(8)
                    .AlignMiddle();

        return alignLeft ? cell.AlignLeft() : cell.AlignCenter();
    }

    // =========================
    // MINI SUBJECT CARD (summary snapshot)
    // =========================
    private static IContainer MiniSubjectCard(IContainer c, SubjectMarkUpdateDto subject)
    {
        if (!subject.OldMark.HasValue)
        {
            // Layout khi không có điểm cũ - chỉ hiển thị điểm mới
            c.Background(Colors.White)
             .Border(1).BorderColor(Theme.BorderSoft)
             .Padding(12)
             .Column(col =>
             {
                 col.Spacing(8);

                 col.Item().Row(r =>
                 {
                     r.RelativeItem().Column(left =>
                     {
                         left.Spacing(2);
                         left.Item().Text(subject.SubjectCode)
                             .FontSize(11).Bold().FontColor(Theme.PrimaryDark);

                         left.Item().Text(subject.SubjectName)
                             .FontSize(9).FontColor(Theme.TextSub).LineHeight(1.2f);
                     });

                    var (badgeBg, badgeFg, badgeBorder) = GetScoreColor(subject.NewMark);
                    r.ConstantItem(90).AlignRight().AlignTop()
                        .Background(badgeBg)
                        .Border(1).BorderColor(badgeBorder)
                        .PaddingVertical(6).PaddingHorizontal(8)
                        .AlignCenter()
                        .Text($"{subject.NewMark:F1} điểm")
                        .FontSize(9).Bold().FontColor(badgeFg);
                });

                // Chỉ hiển thị điểm mới
                col.Item().PaddingTop(2).Column(bars =>
                {
                    var maxScore = DetectMaxScale(null, subject.NewMark);
                    var newPct = ToPercent(subject.NewMark, maxScore);
                    var (barBg, barFg, _) = GetScoreColor(subject.NewMark);

                    bars.Item().Element(x => ScoreBarRow(x, "Điểm", subject.NewMark, newPct,
                        barBg, barFg));
                });
             });
            return c;
        }

        // Layout khi có điểm cũ - hiển thị so sánh
        var delta = CalculateDelta(subject.OldMark.Value, subject.NewMark);
        var (deltaText, deltaBg, deltaFg, deltaBorder) = DeltaBadge(subject.OldMark.Value, subject.NewMark);

        c.Background(Colors.White)
         .Border(1).BorderColor(Theme.BorderSoft)
         .Padding(12)
         .Column(col =>
         {
             col.Spacing(8);

             col.Item().Row(r =>
             {
                 r.RelativeItem().Column(left =>
                 {
                     left.Spacing(2);
                     left.Item().Text(subject.SubjectCode)
                         .FontSize(11).Bold().FontColor(Theme.PrimaryDark);

                     left.Item().Text(subject.SubjectName)
                         .FontSize(9).FontColor(Theme.TextSub).LineHeight(1.2f);
                 });

                 r.ConstantItem(90).AlignRight().AlignTop()
                     .Background(deltaBg)
                     .Border(1).BorderColor(deltaBorder)
                     .PaddingVertical(6).PaddingHorizontal(8)
                     .AlignCenter()
                     .Text(deltaText)
                     .FontSize(9).Bold().FontColor(deltaFg);
             });

             // Bars
             col.Item().PaddingTop(2).Column(bars =>
             {
                 bars.Spacing(6);

                 var maxScore = DetectMaxScale(subject.OldMark.Value, subject.NewMark);
                 var oldPct = ToPercent(subject.OldMark.Value, maxScore);
                 var newPct = ToPercent(subject.NewMark, maxScore);

                 bars.Item().Element(x => ScoreBarRow(x, "Cũ", subject.OldMark.Value, oldPct, Theme.Lavender, Theme.PrimaryDark));
                 bars.Item().Element(x => ScoreBarRow(x, "Mới", subject.NewMark, newPct,
                     delta >= 0 ? Theme.SuccessSoft : Theme.DangerSoft,
                     delta >= 0 ? Theme.SuccessText : Theme.DangerText));
             });
         });
        return c;
    }

    private static IContainer ScoreBarRow(IContainer c, string label, double score, double percent, string bgSoft, string fg)
    {
        // Avoid 0-width relative items
        var filled = Math.Max(1, Math.Min(100, percent));
        var empty = Math.Max(1, 100 - filled);

        c.Row(r =>
        {
            r.ConstantItem(32).AlignMiddle().Text(label)
                .FontSize(9).FontColor(Theme.TextHint);

            r.RelativeItem().AlignMiddle().Height(16).Element(bar =>
            {
                bar.Background(Theme.NeutralBorder)
                   .Border(1).BorderColor(Theme.BorderSoft)
                   .Padding(1)
                   .Row(br =>
                   {
                       br.RelativeItem((float)filled).Background(bgSoft);
                       br.RelativeItem((float)empty);
                   });
            });

            r.ConstantItem(50).AlignMiddle().AlignRight().Text(score.ToString("F1"))
                .FontSize(9).Bold().FontColor(fg);
        });
        return c;
    }

    // =========================
    // SUBJECT HERO (detail page)
    // =========================
    private static IContainer SubjectScoreHero(IContainer c, SubjectMarkUpdateDto subject)
    {
        if (!subject.OldMark.HasValue)
        {
            // Layout khi không có điểm cũ - chỉ hiển thị điểm mới
            var maxScore = DetectMaxScale(null, subject.NewMark);
            var newPct = ToPercent(subject.NewMark, maxScore);
            var (badgeBg, badgeFg, badgeBorder) = GetScoreColor(subject.NewMark);
            var (barBg, barFg, _) = GetScoreColor(subject.NewMark);

            c.Background(Colors.White)
             .Border(1).BorderColor(Theme.BorderSoft)
             .Padding(16)
             .Column(col =>
             {
                 col.Spacing(12);

                 col.Item().Row(r =>
                 {
                     r.RelativeItem().Column(left =>
                     {
                         left.Spacing(4);

                         left.Item().Text("Tóm tắt điểm số")
                             .FontSize(13).Bold().FontColor(Theme.TextMain);

                         left.Item().Text($"Thang điểm: {maxScore:F0}")
                             .FontSize(9).FontColor(Theme.TextHint);
                     });

                     r.ConstantItem(120).AlignRight().AlignMiddle()
                         .Background(badgeBg)
                         .Border(1).BorderColor(badgeBorder)
                         .PaddingVertical(8).PaddingHorizontal(10)
                         .AlignCenter()
                         .Text($"{subject.NewMark:F1} điểm")
                         .FontSize(11).Bold().FontColor(badgeFg);
                 });

                 col.Item().LineHorizontal(1).LineColor(Theme.BorderSoft);

                 col.Item().Element(x => ScoreBarRow(x, "Điểm", subject.NewMark, newPct,
                     barBg, barFg));

                 // small note
                 col.Item().PaddingTop(2).Text("Ghi chú: Đây là điểm số hiện tại. Hãy tiếp tục nỗ lực để cải thiện kết quả học tập.")
                     .FontSize(9).FontColor(Theme.TextSub).LineHeight(1.25f);
             });
            return c;
        }

        // Layout khi có điểm cũ - hiển thị so sánh
        var maxScoreWithOld = DetectMaxScale(subject.OldMark.Value, subject.NewMark);
        var oldPct = ToPercent(subject.OldMark.Value, maxScoreWithOld);
        var newPctWithOld = ToPercent(subject.NewMark, maxScoreWithOld);

        var delta = CalculateDelta(subject.OldMark.Value, subject.NewMark);
        var (deltaText, deltaBg, deltaFg, deltaBorder) = DeltaBadge(subject.OldMark.Value, subject.NewMark);

        c.Background(Colors.White)
         .Border(1).BorderColor(Theme.BorderSoft)
         .Padding(16)
         .Column(col =>
         {
             col.Spacing(12);

             col.Item().Row(r =>
             {
                 r.RelativeItem().Column(left =>
                 {
                     left.Spacing(4);

                     left.Item().Text("Tóm tắt điểm số")
                         .FontSize(13).Bold().FontColor(Theme.TextMain);

                     left.Item().Text($"Thang điểm: {maxScoreWithOld:F0}")
                         .FontSize(9).FontColor(Theme.TextHint);
                 });

                 r.ConstantItem(120).AlignRight().AlignMiddle()
                     .Background(deltaBg)
                     .Border(1).BorderColor(deltaBorder)
                     .PaddingVertical(8).PaddingHorizontal(10)
                     .AlignCenter()
                     .Text(deltaText)
                     .FontSize(11).Bold().FontColor(deltaFg);
             });

             col.Item().LineHorizontal(1).LineColor(Theme.BorderSoft);

             col.Item().Element(x => ScoreBarRow(x, "Cũ", subject.OldMark.Value, oldPct, Theme.Lavender, Theme.PrimaryDark));
             col.Item().Element(x => ScoreBarRow(x, "Mới", subject.NewMark, newPctWithOld,
                 delta >= 0 ? Theme.SuccessSoft : Theme.DangerSoft,
                 delta >= 0 ? Theme.SuccessText : Theme.DangerText));

             // small note
             col.Item().PaddingTop(2).Text(BuildQuickNote(subject))
                 .FontSize(9).FontColor(Theme.TextSub).LineHeight(1.25f);
         });
        return c;
    }

    private static string BuildQuickNote(SubjectMarkUpdateDto s)
    {
        if (!s.OldMark.HasValue)
            return "Ghi chú: Đây là điểm số hiện tại. Hãy tiếp tục nỗ lực để cải thiện kết quả học tập.";

        var delta = CalculateDelta(s.OldMark.Value, s.NewMark);
        if (delta > 0)
            return "Ghi chú: Điểm mới tăng so với điểm cũ. Ưu tiên giữ vững phần làm tốt và tối ưu các tiêu chí còn thiếu.";
        if (delta < 0)
            return "Ghi chú: Điểm mới giảm so với điểm cũ. Nên rà lại các lỗi thường gặp và tăng mức độ luyện tập theo dạng bài yếu.";
        return "Ghi chú: Điểm số không thay đổi. Có thể tập trung nâng chất lượng ở những tiêu chí nâng cao để tạo đột phá.";
    }

    // =========================
    // DELTA BADGE + SCALE
    // =========================
    private static (string bg, string fg, string border) GetScoreColor(double score)
    {
        // Điểm >= 50: màu xanh (success), điểm < 50: màu đỏ (danger)
        if (score >= 50)
            return (Theme.SuccessSoft, Theme.SuccessText, Theme.SuccessBorder);
        return (Theme.DangerSoft, Theme.DangerText, Theme.DangerBorder);
    }

    private static (string text, string bg, string fg, string border) DeltaBadge(double oldMark, double newMark)
    {
        // Tính lại delta từ NewMark - OldMark để đảm bảo chính xác
        var delta = newMark - oldMark;

        if (delta > 0)
            return ($"+{delta:F1} điểm", Theme.SuccessSoft, Theme.SuccessText, Theme.SuccessBorder);
        if (delta < 0)
            return ($"{delta:F1} điểm", Theme.DangerSoft, Theme.DangerText, Theme.DangerBorder);

        return ("0.0 điểm", Theme.NeutralSoft, Theme.TextMain, Theme.NeutralBorder);
    }

    private static double CalculateDelta(double oldMark, double newMark)
    {
        // Tính delta chính xác: NewMark - OldMark
        return newMark - oldMark;
    }

    private static double DetectMaxScale(double? oldMark, double newMark)
    {
        // Nếu có giá trị > 10, assume thang 100; ngược lại thang 10
        var maxValue = oldMark.HasValue ? Math.Max(oldMark.Value, newMark) : newMark;
        return maxValue > 10 ? 100.0 : 10.0;
    }

    private static double ToPercent(double score, double maxScore)
    {
        if (maxScore <= 0) return 0;
        var pct = (score / maxScore) * 100.0;
        return Math.Min(100, Math.Max(0, pct));
    }

    // =========================
    // MARKDOWN PARSER (simple, clean)
    // =========================
    private static void ParseMarkdownToPdf(ColumnDescriptor column, string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            column.Item().Text("Không có nội dung phân tích.").FontColor(Theme.TextHint);
            return;
        }

        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var inList = false;

        foreach (var raw in lines)
        {
            var line = raw.Trim();

            if (string.IsNullOrWhiteSpace(line))
            {
                if (inList)
                {
                    column.Item().PaddingBottom(2);
                    inList = false;
                }
                else
                {
                    column.Item().PaddingBottom(6);
                }
                continue;
            }

            // Headings
            if (Regex.IsMatch(line, @"^####\s+"))
            {
                inList = false;
                column.Item().PaddingTop(6).PaddingBottom(3)
                    .Text(CleanInline(line[4..].Trim()))
                    .FontSize(10).Bold().FontColor(Theme.TextMain);
                continue;
            }
            if (Regex.IsMatch(line, @"^###\s+"))
            {
                inList = false;
                column.Item().PaddingTop(8).PaddingBottom(4)
                    .Text(CleanInline(line[3..].Trim()))
                    .FontSize(12).Bold().FontColor(Theme.PrimaryDark);
                continue;
            }
            if (Regex.IsMatch(line, @"^##\s+"))
            {
                inList = false;
                column.Item().PaddingTop(10).PaddingBottom(6)
                    .Text(CleanInline(line[2..].Trim()))
                    .FontSize(14).Bold().FontColor(Theme.PrimaryDark);
                continue;
            }

            // Bullet list
            if (line.StartsWith("- ") || line.StartsWith("* "))
            {
                inList = true;
                var bullet = CleanInline(line[2..].Trim());

                column.Item().Row(r =>
                {
                    r.ConstantItem(14).AlignTop().Text("•").FontSize(11).FontColor(Theme.TextSub);
                    r.RelativeItem().Text(bullet).FontSize(10).FontColor(Theme.TextMain).LineHeight(1.25f);
                });
                continue;
            }

            // Numbered list: "1. xxx"
            if (Regex.IsMatch(line, @"^\d+\.\s+"))
            {
                inList = true;
                var m = Regex.Match(line, @"^(?<n>\d+)\.\s+(?<t>.+)$");
                var n = m.Groups["n"].Value;
                var t = CleanInline(m.Groups["t"].Value);

                column.Item().Row(r =>
                {
                    r.ConstantItem(24).AlignTop().Text($"{n}.").FontSize(10).FontColor(Theme.TextSub);
                    r.RelativeItem().Text(t).FontSize(10).FontColor(Theme.TextMain).LineHeight(1.25f);
                });
                continue;
            }

            // Normal paragraph
            inList = false;
            column.Item().Text(CleanInline(line))
                .FontSize(10)
                .FontColor(Theme.TextMain)
                .LineHeight(1.35f);
        }
    }

    private static string CleanInline(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var s = input;

        // Remove basic markdown markers (keep text)
        s = Regex.Replace(s, @"\*\*(.+?)\*\*", "$1"); // bold
        s = Regex.Replace(s, @"\*(.+?)\*", "$1");     // italic
        s = Regex.Replace(s, @"_(.+?)_", "$1");       // italic underscore
        s = Regex.Replace(s, @"`(.+?)`", "$1");       // inline code

        return s.Trim();
    }
}
