using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortfolioDemo.Services.Resume
{
    public static class ResumePdfGenerator
    {
        private const string AccentColor = "#1a5276";
        private const string RuleColor = "#aab7b8";

        public static byte[] Generate(ResumeData data)
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.MarginHorizontal(1.1f, Unit.Inch);
                    page.MarginVertical(0.9f, Unit.Inch);
                    page.DefaultTextStyle(x => x.FontFamily(Fonts.Arial).FontSize(10).FontColor("#222222"));

                    page.Content().Column(col =>
                    {
                        // ── Header ──────────────────────────────────────────────
                        col.Item().Text(data.Contact.Name)
                            .FontSize(22).Bold().FontColor(AccentColor);

                        var contactParts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(data.Contact.Email))
                            contactParts.Add(data.Contact.Email);
                        if (!string.IsNullOrWhiteSpace(data.Contact.Phone))
                            contactParts.Add(data.Contact.Phone);
                        if (!string.IsNullOrWhiteSpace(data.Contact.Location))
                            contactParts.Add(data.Contact.Location);

                        if (contactParts.Count > 0)
                        {
                            col.Item()
                                .PaddingBottom(4, Unit.Point)
                                .Text(string.Join("  |  ", contactParts))
                                .FontSize(9).FontColor("#555555");
                        }

                        col.Item()
                            .PaddingVertical(4, Unit.Point)
                            .LineHorizontal(1, Unit.Point)
                            .LineColor(AccentColor);

                        // ── Summary ──────────────────────────────────────────────
                        SectionHeader(col, "Professional Summary");
                        col.Item()
                            .PaddingBottom(10, Unit.Point)
                            .Text(data.Summary)
                            .FontSize(9.5f).LineHeight(1.4f);

                        // ── Experience ───────────────────────────────────────────
                        SectionHeader(col, "Experience");
                        foreach (var job in data.Experience)
                        {
                            col.Item().PaddingBottom(4, Unit.Point).Column(item =>
                            {
                                // Job title + company + period
                                item.Item().Row(row =>
                                {
                                    row.RelativeItem(1).Text(t =>
                                    {
                                        t.Span(job.Title).Bold().FontSize(10);
                                        t.Span($"  —  {job.Company}").FontSize(9.5f).FontColor("#444444");
                                        t.Span($"  ({job.Period})").FontSize(9).FontColor("#666666");
                                    });
                                });

                                foreach (var bullet in job.Bullets)
                                {
                                    item.Item().Row(r =>
                                    {
                                        r.ConstantItem(14, Unit.Point)
                                            .PaddingTop(2, Unit.Point)
                                            .Text("•").FontSize(9);
                                        r.RelativeItem(1).Text(bullet)
                                            .FontSize(9.5f).LineHeight(1.35f);
                                    });
                                }

                                item.Item().PaddingBottom(4, Unit.Point).Text(string.Empty);
                            });
                        }

                        // ── Education ────────────────────────────────────────────
                        SectionHeader(col, "Education");
                        col.Item()
                            .PaddingBottom(10, Unit.Point)
                            .Text(data.Education).FontSize(9.5f);

                        // ── Skills ───────────────────────────────────────────────
                        SectionHeader(col, "Skills");
                        foreach (var group in data.Skills)
                        {
                            col.Item().PaddingBottom(4, Unit.Point).Row(row =>
                            {
                                row.ConstantItem(160, Unit.Point).Text(t =>
                                {
                                    t.Span(group.Key + ":").Bold().FontSize(9.5f);
                                });
                                row.RelativeItem(1).Text(string.Join(", ", group.Value))
                                    .FontSize(9.5f).LineHeight(1.3f);
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.DefaultTextStyle(x => x.FontSize(8).FontColor("#999999"));
                        t.Span("Page ");
                        t.CurrentPageNumber();
                        t.Span(" of ");
                        t.TotalPages();
                    });
                });
            })
            .WithMetadata(new DocumentMetadata
            {
                Title = $"{data.Contact.Name} – Resume",
                Author = data.Contact.Name,
                Creator = "Monica Rivera Portfolio"
            });

            return doc.GeneratePdf();
        }

        private static void SectionHeader(ColumnDescriptor col, string title)
        {
            col.Item().PaddingBottom(2, Unit.Point).Text(title)
                .FontSize(12).Bold().FontColor(AccentColor);
            col.Item()
                .PaddingBottom(6, Unit.Point)
                .LineHorizontal(0.5f, Unit.Point)
                .LineColor(RuleColor);
        }
    }
}

