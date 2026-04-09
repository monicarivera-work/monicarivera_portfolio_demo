using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace PortfolioDemo.Services.Resume
{
    public static class ResumeDocxGenerator
    {
        // Colour used for headings (RRGGBB, no '#')
        private const string AccentHex = "1A5276";

        public static byte[] Generate(ResumeData data)
        {
            using var ms = new MemoryStream();
            using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
            {
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                AddStyles(mainPart);
                SetPageMargins(body);

                // ── Header ──────────────────────────────────────────
                AppendNameHeader(body, data.Contact.Name);
                AppendContactLine(body, data.Contact);
                AppendHorizontalRule(body);

                // ── Summary ─────────────────────────────────────────
                AppendSectionHeading(body, "Professional Summary");
                AppendBodyParagraph(body, data.Summary);
                AppendSpacer(body);

                // ── Experience ───────────────────────────────────────
                AppendSectionHeading(body, "Experience");
                foreach (var job in data.Experience)
                {
                    AppendJobHeader(body, job.Title, job.Company, job.Period);
                    foreach (var bullet in job.Bullets)
                        AppendBullet(body, bullet);
                    AppendSpacer(body);
                }

                // ── Education ────────────────────────────────────────
                AppendSectionHeading(body, "Education");
                AppendBodyParagraph(body, data.Education);
                AppendSpacer(body);

                // ── Skills ───────────────────────────────────────────
                AppendSectionHeading(body, "Skills");
                foreach (var group in data.Skills)
                    AppendSkillGroup(body, group.Key, group.Value);

                mainPart.Document.Save();
            }

            return ms.ToArray();
        }

        // ── Helpers ──────────────────────────────────────────────────

        private static void AddStyles(MainDocumentPart mainPart)
        {
            var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
            stylesPart.Styles = new Styles();
        }

        private static void SetPageMargins(Body body)
        {
            var sectPr = new SectionProperties(
                new PageMargin
                {
                    Top = 1008,      // ~0.7 in (1440 twips/in)
                    Bottom = 1008,
                    Left = 1152,     // ~0.8 in
                    Right = 1152,
                    Header = 708,
                    Footer = 708
                });
            body.AppendChild(sectPr);
        }

        private static void AppendNameHeader(Body body, string name)
        {
            var para = body.AppendChild(new Paragraph());
            var run = para.AppendChild(new Run());
            run.AppendChild(new RunProperties(
                new Bold(),
                new FontSize { Val = "48" },          // 24pt
                new Color { Val = AccentHex }
            ));
            run.AppendChild(new Text(name));
            para.ParagraphProperties = new ParagraphProperties(
                new SpacingBetweenLines { After = "60" }
            );
        }

        private static void AppendContactLine(Body body, ResumeContactInfo contact)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(contact.Email)) parts.Add(contact.Email);
            if (!string.IsNullOrWhiteSpace(contact.Phone)) parts.Add(contact.Phone);
            if (!string.IsNullOrWhiteSpace(contact.Location)) parts.Add(contact.Location);

            if (parts.Count == 0) return;

            var para = body.AppendChild(new Paragraph());
            var run = para.AppendChild(new Run());
            run.AppendChild(new RunProperties(
                new FontSize { Val = "18" },          // 9pt
                new Color { Val = "555555" }
            ));
            run.AppendChild(new Text(string.Join("  |  ", parts)));
            para.ParagraphProperties = new ParagraphProperties(
                new SpacingBetweenLines { After = "80" }
            );
        }

        private static void AppendHorizontalRule(Body body)
        {
            var para = body.AppendChild(new Paragraph());
            para.ParagraphProperties = new ParagraphProperties(
                new ParagraphBorders(
                    new BottomBorder
                    {
                        Val = BorderValues.Single,
                        Size = 6,
                        Color = AccentHex
                    }),
                new SpacingBetweenLines { After = "120" }
            );
        }

        private static void AppendSectionHeading(Body body, string title)
        {
            var para = body.AppendChild(new Paragraph());
            var run = para.AppendChild(new Run());
            run.AppendChild(new RunProperties(
                new Bold(),
                new FontSize { Val = "24" },          // 12pt
                new Color { Val = AccentHex }
            ));
            run.AppendChild(new Text(title));
            para.ParagraphProperties = new ParagraphProperties(
                new SpacingBetweenLines { Before = "120", After = "60" },
                new ParagraphBorders(
                    new BottomBorder
                    {
                        Val = BorderValues.Single,
                        Size = 4,
                        Color = "AAAAAA"
                    })
            );
        }

        private static void AppendJobHeader(Body body, string title, string company, string period)
        {
            var para = body.AppendChild(new Paragraph());
            para.ParagraphProperties = new ParagraphProperties(
                new SpacingBetweenLines { Before = "100", After = "40" },
                new Justification { Val = JustificationValues.Left }
            );

            // Title (bold)
            var titleRun = para.AppendChild(new Run());
            titleRun.AppendChild(new RunProperties(new Bold(), new FontSize { Val = "20" }));
            titleRun.AppendChild(new Text(title) { Space = SpaceProcessingModeValues.Preserve });

            // Em dash + company
            var companyRun = para.AppendChild(new Run());
            companyRun.AppendChild(new RunProperties(new FontSize { Val = "19" }, new Color { Val = "444444" }));
            companyRun.AppendChild(new Text($"  —  {company}") { Space = SpaceProcessingModeValues.Preserve });

            // Period (right-aligned via tab)
            var tabRun = para.AppendChild(new Run());
            tabRun.AppendChild(new RunProperties(new FontSize { Val = "18" }, new Color { Val = "666666" }));
            tabRun.AppendChild(new Text($"  ({period})") { Space = SpaceProcessingModeValues.Preserve });
        }

        private static void AppendBullet(Body body, string text)
        {
            var para = body.AppendChild(new Paragraph());
            para.ParagraphProperties = new ParagraphProperties(
                new Indentation { Left = "360", Hanging = "180" },
                new SpacingBetweenLines { After = "40" }
            );
            var run = para.AppendChild(new Run());
            run.AppendChild(new RunProperties(new FontSize { Val = "19" }));
            run.AppendChild(new Text("• " + text) { Space = SpaceProcessingModeValues.Preserve });
        }

        private static void AppendBodyParagraph(Body body, string text)
        {
            var para = body.AppendChild(new Paragraph());
            para.ParagraphProperties = new ParagraphProperties(
                new SpacingBetweenLines { After = "60", Line = "276", LineRule = LineSpacingRuleValues.Auto }
            );
            var run = para.AppendChild(new Run());
            run.AppendChild(new RunProperties(new FontSize { Val = "19" }));
            run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        }

        private static void AppendSkillGroup(Body body, string category, List<string> skills)
        {
            var para = body.AppendChild(new Paragraph());
            para.ParagraphProperties = new ParagraphProperties(
                new SpacingBetweenLines { After = "60" }
            );

            var labelRun = para.AppendChild(new Run());
            labelRun.AppendChild(new RunProperties(new Bold(), new FontSize { Val = "19" }));
            labelRun.AppendChild(new Text(category + ": ") { Space = SpaceProcessingModeValues.Preserve });

            var valueRun = para.AppendChild(new Run());
            valueRun.AppendChild(new RunProperties(new FontSize { Val = "19" }));
            valueRun.AppendChild(new Text(string.Join(", ", skills)) { Space = SpaceProcessingModeValues.Preserve });
        }

        private static void AppendSpacer(Body body)
        {
            var para = body.AppendChild(new Paragraph());
            para.ParagraphProperties = new ParagraphProperties(
                new SpacingBetweenLines { After = "80" }
            );
        }
    }
}
