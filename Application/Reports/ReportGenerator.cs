using System.Text.Json;
using MetrixVox.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MetrixVox.Application.Reports
{
    public class ReportGenerator
    {
        public static byte[] GenerateExecutiveReport(ReportData data, AiReportAnalysis? aiAnalysis)
        {
            var doc = Document.Create(container =>
            {
                // PÁGINA 1: PORTADA
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.PageColor("#4F46E5");

                    page.Content().Column(col =>
                    {
                        col.Item().Height(150);

                        col.Item().AlignCenter().Height(80).Row(row =>
                        {
                            row.ConstantItem(30).Height(40).Background("#FFFFFF");
                            row.ConstantItem(15);
                            row.ConstantItem(30).Height(60).Background("#FFFFFF");
                            row.ConstantItem(15);
                            row.ConstantItem(30).Height(80).Background("#FFFFFF");
                        });

                        col.Item().Height(60);

                        col.Item().AlignCenter().Text("METRIXVOX")
                            .FontSize(48).Bold().FontColor("#FFFFFF");

                        col.Item().Height(20);

                        col.Item().AlignCenter().Text("Reporte de Experiencia de Cliente")
                            .FontSize(24).FontColor("#FFFFFF");

                        col.Item().Height(80);

                        col.Item().AlignCenter().Column(info =>
                        {
                            info.Item().Text(data.TenantName)
                                .FontSize(32).Bold().FontColor("#FFFFFF");

                            info.Item().Height(10);

                            info.Item().Text($"Periodo: {data.Period}")
                                .FontSize(16).FontColor("#E0E7FF");
                        });

                        col.Item().Height(60);

                        col.Item().AlignBottom().Padding(40).Row(footer =>
                        {
                            footer.RelativeItem().Text("Generado por MetrixVox IA")
                                .FontSize(12).FontColor("#C7D2FE");

                            footer.RelativeItem().AlignRight()
                                .Text($"Fecha: {DateTime.Now:dd/MM/yyyy}")
                                .FontSize(12).FontColor("#C7D2FE");
                        });
                    });
                });

                // PÁGINA 2: RESUMEN EJECUTIVO Y KPIs
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
                        
                        // NUEVO: Etiqueta de tamaño de muestra
                        string sampleQuality = data.TotalResponses >= 100 ? "Muestra Robusta (Alta confiabilidad)" 
                                            : data.TotalResponses >= 30 ? "Muestra Válida (Indicativa)" 
                                            : "Muestra Insuficiente (Baja confiabilidad)";
                        string sampleColor = data.TotalResponses >= 100 ? "#10B981" 
                                           : data.TotalResponses >= 30 ? "#F59E0B" 
                                           : "#F43F5E";
                        
                        col.Item().Height(5);
                        col.Item().Text($"Calidad de la muestra: {sampleQuality}")
                            .FontSize(11).Bold().FontColor(sampleColor);

                        col.Item().Height(30);

                        // KPIs
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

                        if (aiAnalysis != null)
                        {
                            col.Item().Text("Conclusiones de la IA").FontSize(18).Bold().FontColor("#0F172A");
                            col.Item().Height(10);
                            col.Item().Background("#F8FAFC").Padding(15).Text(aiAnalysis.ExecutiveSummary)
                                .FontSize(11).FontColor("#334155").LineHeight(1.5f);
                            col.Item().Height(30);
                        }

                        col.Item().Text("Análisis de Sentimiento").FontSize(18).Bold().FontColor("#0F172A");
                        col.Item().Height(15);

                        col.Item().Text($"El {data.NeutralCount} de {data.TotalResponses} respuestas fueron clasificadas como neutrales por nuestro análisis de IA.")
                            .FontSize(11).FontColor("#475569");
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });

                // PÁGINA 3: TOP PROBLEMAS (IA)
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);

                    page.Header().Text("Análisis Detallado de Problemas").FontSize(20).Bold().FontColor("#4F46E5");
                    page.Content().PaddingVertical(15).Column(col =>
                    {
                        if (aiAnalysis != null && !string.IsNullOrEmpty(aiAnalysis.TopProblems))
                        {
                            var problems = JsonSerializer.Deserialize<List<AiProblemDto>>(aiAnalysis.TopProblems, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            
                            if (problems != null)
                            {
                                foreach (var p in problems)
                                {
                                    col.Item().PaddingBottom(15).Column(c =>
                                    {
                                        c.Item().Row(r =>
                                        {
                                            r.RelativeItem().Text(p.Issue).FontSize(14).Bold().FontColor("#0F172A");
                                            r.ConstantItem(60).AlignRight().Text($"{p.Percentage}%").FontSize(14).Bold().FontColor("#F43F5E");
                                        });
                                        c.Item().Height(5);
                                        c.Item().Height(6).Background("#E2E8F0").Row(bar =>
                                        {
                                            bar.RelativeItem((float)(p.Percentage / 100.0)).Background("#F43F5E");
                                            bar.RelativeItem((float)(1.0 - p.Percentage / 100.0));
                                        });
                                        c.Item().Height(10);
                                        c.Item().Background("#F8FAFC").Padding(10).Row(rec =>
                                        {
                                            rec.ConstantItem(20).Text("Tip:").FontSize(11).Bold().FontColor("#4F46E5");
                                            rec.RelativeItem().Text(p.Recommendation).FontSize(10).FontColor("#475569").Italic();
                                        });
                                    });
                                }
                            }
                        }
                        else
                        {
                            col.Item().Text("No se ha generado un análisis de IA para este periodo. Por favor, genere el reporte primero.")
                                .FontSize(12).FontColor("#64748B");
                        }
                    });

                    page.Footer().AlignCenter().Text(x => { x.Span("Página "); x.CurrentPageNumber(); });
                });

                // PÁGINA 4: VERBATIMS
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);

                    page.Header().Text("Comentarios de Clientes").FontSize(20).Bold().FontColor("#4F46E5");
                    page.Content().PaddingVertical(15).Column(col =>
                    {
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

                    page.Footer().AlignCenter().Text(x => { x.Span("Página "); x.CurrentPageNumber(); });
                });

                // PÁGINA 5: CIERRE
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);

                    page.Content().AlignMiddle().Column(col =>
                    {
                        col.Item().AlignCenter().Background("#4F46E5").Padding(40).Column(f =>
                        {
                            f.Item().AlignCenter().Text("MetrixVox — Inteligencia de Cliente")
                                .FontSize(20).Bold().FontColor("#FFFFFF");
                            f.Item().Height(10);
                            f.Item().AlignCenter().Text("Powered by IA · Análisis automático de experiencia")
                                .FontSize(12).FontColor("#E0E7FF");
                        });
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
    }

    // DTOs
    public class AiProblemDto
    {
        public string Issue { get; set; } = string.Empty;
        public int Percentage { get; set; }
        public string Recommendation { get; set; } = string.Empty;
    }

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
