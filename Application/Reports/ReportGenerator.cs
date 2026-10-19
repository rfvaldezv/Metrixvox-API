using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MetrixVox.Application.Reports
{
    /// <summary>
    /// Genera reportes PDF ejecutivos con los datos del dashboard.
    /// </summary>
    public class ReportGenerator
    {
        public static byte[] GenerateExecutiveReport(ReportData data)
        {
            var doc = Document.Create(container =>
            {
                // ============================================================
                // PÁGINA 1: PORTADA
                // ============================================================
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.PageColor("#4F46E5");

                    page.Content().Column(col =>
                    {
                        col.Item().Height(150);

                        // Logo (3 barras dibujadas)
                        col.Item().AlignCenter().Height(80).Row(row =>
                        {
                            row.ConstantItem(30).Height(40).Background("#FFFFFF");
                            row.ConstantItem(15);
                            row.ConstantItem(30).Height(60).Background("#FFFFFF");
                            row.ConstantItem(15);
                            row.ConstantItem(30).Height(80).Background("#FFFFFF");
                        });

                        col.Item().Height(60);

                        // Título
                        col.Item().AlignCenter().Text("METRIXVOX")
                            .FontSize(48).Bold().FontColor("#FFFFFF");

                        col.Item().Height(20);

                        col.Item().AlignCenter().Text("Reporte de Experiencia de Cliente")
                            .FontSize(24).FontColor("#FFFFFF");

                        col.Item().Height(80);

                        // Tenant + fecha
                        col.Item().AlignCenter().Column(info =>
                        {
                            info.Item().Text(data.TenantName)
                                .FontSize(32).Bold().FontColor("#FFFFFF");

                            info.Item().Height(10);

                            info.Item().Text($"Periodo: {data.Period}")
                                .FontSize(16).FontColor("#E0E7FF");
                        });

                        col.Item().Height(60);

                        // Footer con branding
                        col.Item().AlignBottom().Padding(40).Row(footer =>
                        {
                            footer.RelativeItem().Text("Generado por MetrixVox")
                                .FontSize(12).FontColor("#C7D2FE");

                            footer.RelativeItem().AlignRight()
                                .Text($"Fecha: {DateTime.Now:dd/MM/yyyy}")
                                .FontSize(12).FontColor("#C7D2FE");
                        });
                    });
                });

                // ============================================================
                // PÁGINA 2: RESUMEN EJECUTIVO
                // ============================================================
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Column(h =>
                    {
                        h.Item().Row(r =>
                        {
                            r.RelativeItem().Text("MetrixVox").FontSize(20).Bold().FontColor("#4F46E5");
                            r.RelativeItem().AlignRight().Text(data.TenantName).FontSize(11).FontColor("#64748B");
                        });
                        h.Item().Height(5);
                        h.Item().LineHorizontal(1).LineColor("#E2E8F0");
                        h.Item().Height(15);
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().Text("Resumen Ejecutivo").FontSize(28).Bold().FontColor("#0F172A");
                        col.Item().Height(10);
                        col.Item().Text($"Análisis de {data.TotalResponses} respuestas capturadas")
                            .FontSize(13).FontColor("#64748B");
                        col.Item().Height(30);

                        // KPIs en grid
                        col.Item().Row(row =>
                        {
                            KpiBox(row, "Score Global", data.GlobalScore.ToString("0.00"), "/ 5.0", "#10B981");
                            row.ConstantItem(15);
                            KpiBox(row, "Total Respuestas", data.TotalResponses.ToString(), "", "#4F46E5");
                            row.ConstantItem(15);
                            KpiBox(row, "Positivos", data.PositiveCount.ToString(), "", "#10B981");
                            row.ConstantItem(15);
                            KpiBox(row, "Alertas", data.PendingAlerts.ToString(), "", "#F43F5E");
                        });

                        col.Item().Height(40);

                        // Análisis de sentimiento
                        col.Item().Text("Análisis de Sentimiento").FontSize(18).Bold().FontColor("#0F172A");
                        col.Item().Height(15);

                        col.Item().Text($"El {data.NeutralCount} de {data.TotalResponses} respuestas fueron clasificadas como neutrales por nuestro análisis de IA.")
                            .FontSize(11).FontColor("#475569");

                        col.Item().Height(20);

                        // Categorías
                        col.Item().Text("Rendimiento por Categoría").FontSize(18).Bold().FontColor("#0F172A");
                        col.Item().Height(15);

                        foreach (var cat in data.Categories)
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem(3).Text(cat.Name).FontSize(12);
                                row.RelativeItem(2).AlignRight()
                                    .Text($"{cat.Score:0.00} / 5.0")
                                    .FontSize(12).Bold().FontColor(CategoryColor(cat.Score));
                            });
                            col.Item().Height(5);
                            col.Item().Height(8).Background("#E2E8F0").Row(r =>
                            {
                                r.RelativeItem((float)(cat.Score / 5.0)).Background(CategoryColor(cat.Score));
                                r.RelativeItem((float)(1.0 - cat.Score / 5.0));
                            });
                            col.Item().Height(15);
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });

                // ============================================================
                // PÁGINA 3: ANÁLISIS DETALLADO
                // ============================================================
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);

                    page.Header().Text("Análisis Detallado").FontSize(20).Bold().FontColor("#4F46E5");
                    page.Content().PaddingVertical(15).Column(col =>
                    {
                        col.Item().Text("Verbatims Relevantes").FontSize(16).Bold().FontColor("#0F172A");
                        col.Item().Height(15);

                        if (data.TopVerbatims.Any())
                        {
                            foreach (var v in data.TopVerbatims.Take(10))
                            {
                                col.Item().Background("#F8FAFC").Padding(12).Column(c =>
                                {
                                    c.Item().Text($"\"{v.Text}\"").FontSize(11).Italic().FontColor("#0F172A");
                                    c.Item().Height(4);
                                    c.Item().Text($"Sucursal: {v.Branch} · {v.Sentiment} · {v.Date:dd/MM/yyyy}")
                                        .FontSize(9).FontColor("#64748B");
                                });
                                col.Item().Height(8);
                            }
                        }
                        else
                        {
                            col.Item().Text("No hay comentarios registrados en este período.")
                                .FontSize(11).FontColor("#64748B");
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                    });
                });

                // ============================================================
                // PÁGINA 4: CONCLUSIONES Y RECOMENDACIONES
                // ============================================================
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);

                    page.Header().Text("Conclusiones y Recomendaciones").FontSize(20).Bold().FontColor("#4F46E5");
                    page.Content().PaddingVertical(15).Column(col =>
                    {
                        col.Item().Text("Análisis del Período").FontSize(16).Bold().FontColor("#0F172A");
                        col.Item().Height(10);

                        col.Item().Text(data.ExecutiveSummary).FontSize(11).FontColor("#475569");
                        col.Item().Height(20);

                        col.Item().Text("Recomendaciones Accionables").FontSize(16).Bold().FontColor("#0F172A");
                        col.Item().Height(10);

                        for (int i = 0; i < data.Recommendations.Count; i++)
                        {
                            var rec = data.Recommendations[i];
                            col.Item().Row(row =>
                            {
                                row.ConstantItem(30).Text($"{i + 1}.").FontSize(14).Bold().FontColor("#4F46E5");
                                row.RelativeItem().Text(rec).FontSize(11).FontColor("#0F172A");
                            });
                            col.Item().Height(10);
                        }

                        col.Item().Height(30);

                        // Footer con branding
                        col.Item().Background("#4F46E5").Padding(20).Column(f =>
                        {
                            f.Item().Text("MetrixVox — Inteligencia de Cliente")
                                .FontSize(14).Bold().FontColor("#FFFFFF");
                            f.Item().Height(5);
                            f.Item().Text("Powered by IA · Análisis automático de experiencia")
                                .FontSize(10).FontColor("#E0E7FF");
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                    });
                });
            });

            return doc.GeneratePdf();
        }

        private static void KpiBox(RowDescriptor row, string label, string value, string suffix, string color)
        {
            row.RelativeItem().Background("#F8FAFC").Padding(15).Column(c =>
            {
                c.Item().Text(value).FontSize(24).Bold().FontColor(color);
                c.Item().Height(3);
                c.Item().Text(label).FontSize(10).FontColor("#64748B");
                if (!string.IsNullOrEmpty(suffix))
                {
                    c.Item().Text(suffix).FontSize(9).FontColor("#94A3B8");
                }
            });
        }

        private static string CategoryColor(double score)
        {
            if (score >= 4.5) return "#10B981";
            if (score >= 3.5) return "#0EA5E9";
            if (score >= 2.5) return "#F59E0B";
            return "#F43F5E";
        }
    }

    // ============================================================
    // MODELOS DE DATOS PARA EL REPORTE
    // ============================================================
    public class ReportData
    {
        public string TenantName { get; set; } = "";
        public string Period { get; set; } = "";
        public int TotalResponses { get; set; }
        public double GlobalScore { get; set; }
        public int PositiveCount { get; set; }
        public int NeutralCount { get; set; }
        public int NegativeCount { get; set; }
        public int PendingAlerts { get; set; }
        public List<CategoryData> Categories { get; set; } = new();
        public List<VerbatimData> TopVerbatims { get; set; } = new();
        public string ExecutiveSummary { get; set; } = "";
        public List<string> Recommendations { get; set; } = new();
    }

    public class CategoryData
    {
        public string Name { get; set; } = "";
        public double Score { get; set; }
    }

    public class VerbatimData
    {
        public string Text { get; set; } = "";
        public string Branch { get; set; } = "";
        public string Sentiment { get; set; } = "";
        public DateTime Date { get; set; }
    }
}
