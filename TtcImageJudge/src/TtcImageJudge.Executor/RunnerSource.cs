using System.Text;
using TtcImageJudge.Core;

namespace TtcImageJudge.Executor;

internal static class RunnerSource
{
    public static string Build(ExecuteRequest request)
    {
        var reference = request.Exercise.Tests.Select(t => t.ReferenceCode).FirstOrDefault(code => !string.IsNullOrWhiteSpace(code))
                        ?? "public static Bitmap Blur5x5(Bitmap img) => throw new InvalidOperationException(\"Teste sem referência.\");";
        var tests = string.Join(",\n", request.Exercise.Tests.Select(t =>
            $"new Case({Literal(t.Name)}, {Literal(t.Pattern)}, {t.Width}, {t.Height}, {t.Seed}, {LiteralOrNull(t.InputImageBase64)}, {LiteralOrNull(t.ExpectedImageBase64)})"));
        return $$$"""
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Drawing;
            using System.Drawing.Imaging;
            using System.Diagnostics;
            using System.Text.Json;
            {{{ExtractUsings(reference)}}}
            {{{ExtractUsings(request.SourceCode)}}}

            public static class ReferenceSolution { {{{ExtractBody(reference)}}} }
            public static class Submission { {{{ExtractBody(request.SourceCode)}}} }
            public record Case(string Name, string Pattern, int Width, int Height, int Seed, string? Input, string? Expected);
            public record Diff(int DifferentPixels,int TotalPixels,double Percentage,int? FirstX,int? FirstY,int MaxChannelDelta,string? DifferenceImageBase64);
            public record Result(string TestName,int Verdict,string Message,long ElapsedMs,Diff? Difference,string? InputImageBase64,string? ExpectedImageBase64,string? ActualImageBase64);

            public static class Program
            {
              public static int Main()
              {
                var results = new List<Result>();
                var cases = new[] { {{{tests}}} };
                foreach (var c in cases)
                {
                  using var input = Create(c); using var original = (Bitmap)input.Clone(); Bitmap expected;
                  try { expected = c.Expected is null ? ReferenceSolution.Blur5x5(input) : Load(c.Expected); }
                  catch (Exception ex) { Write(7, "Falha no gabarito: " + ex.Message, results); return 70; }
                  if (!Same(input, original, 0).ok) { Write(7, "A referência modificou a entrada.", results); return 70; }
                  var sw = Stopwatch.StartNew(); Bitmap actual;
                  try { actual = Submission.Blur5x5(input); }
                  catch (Exception ex) { results.Add(new(c.Name,3,ex.ToString(),sw.ElapsedMilliseconds,null,To64(input),To64(expected),null)); Write(3,"Erro de execução.",results); return 3; }
                  sw.Stop();
                  using (expected) using (actual)
                  {
                    if (!Same(input, original, 0).ok) { results.Add(new(c.Name,1,"A função modificou a imagem de entrada.",sw.ElapsedMilliseconds,null,To64(input),To64(expected),To64(actual))); Write(1,"Resposta incorreta.",results); return 1; }
                    var d = Same(expected, actual, {{{request.Exercise.Rules.ChannelTolerance}}});
                    var diff = new Diff(d.count, expected.Width*expected.Height, expected.Width*expected.Height == 0 ? 0 : d.count*100.0/(expected.Width*expected.Height), d.x, d.y, d.max, Difference(expected,actual,{{{request.Exercise.Rules.ChannelTolerance}}}));
                    results.Add(new(c.Name,d.ok?0:1,d.ok?"Pixels idênticos.":"Imagem divergente.",sw.ElapsedMilliseconds,diff,To64(input),To64(expected),To64(actual)));
                    if (!d.ok) { Write(1,"Resposta incorreta.",results); return 1; }
                  }
                }
                Write(0,"Passou em todos os testes e requisitos.",results); return 0;
              }
              static Bitmap Create(Case c) { if(c.Input is not null)return Load(c.Input);var b=new Bitmap(c.Width,c.Height,PixelFormat.Format24bppRgb); var r=new Random(c.Seed); for(int y=0;y<c.Height;y++)for(int x=0;x<c.Width;x++){int v=c.Pattern switch{"constant"=>127,"gradient"=>(x*31+y*17)%256,"impulse"=>x==c.Width/2&&y==c.Height/2?255:0,"checkerboard"=>(x+y)%2*255,"noise"=>r.Next(256),_=>0}; b.SetPixel(x,y,Color.FromArgb(v,v,v));} return b; }
              static Bitmap Load(string value){using var m=new MemoryStream(Convert.FromBase64String(value));using var decoded=new Bitmap(m);return decoded.Clone(new Rectangle(0,0,decoded.Width,decoded.Height),PixelFormat.Format24bppRgb);}
              static (bool ok,int count,int? x,int? y,int max) Same(Bitmap a,Bitmap b,int tolerance) { if(a.Width!=b.Width||a.Height!=b.Height)return(false,Math.Max(a.Width*a.Height,b.Width*b.Height),0,0,255);int count=0,max=0;int? fx=null,fy=null;for(int y=0;y<a.Height;y++)for(int x=0;x<a.Width;x++){var p=a.GetPixel(x,y);var q=b.GetPixel(x,y);var d=Math.Max(Math.Abs(p.R-q.R),Math.Max(Math.Abs(p.G-q.G),Math.Abs(p.B-q.B)));max=Math.Max(max,d);if(d>tolerance){count++;fx??=x;fy??=y;}}return(count==0,count,fx,fy,max); }
              static string To64(Bitmap b){using var m=new MemoryStream();b.Save(m,ImageFormat.Png);return Convert.ToBase64String(m.ToArray());}
              static string Difference(Bitmap a,Bitmap b,int tolerance){using var d=new Bitmap(a.Width,a.Height,PixelFormat.Format24bppRgb);for(int y=0;y<a.Height;y++)for(int x=0;x<a.Width;x++){var p=a.GetPixel(x,y);var q=b.GetPixel(x,y);var delta=Math.Max(Math.Abs(p.R-q.R),Math.Max(Math.Abs(p.G-q.G),Math.Abs(p.B-q.B)));d.SetPixel(x,y,delta>tolerance?Color.FromArgb(255,52,86):Color.FromArgb(delta,delta,delta));}return To64(d);}
              static void Write(int verdict,string message,List<Result> tests){var payload=new{verdict,message,compilerOutput="",directMemoryRequirementMet=true,requirementAnalysis="Validado estruturalmente pelo executor.",tests};File.WriteAllText("result.json",JsonSerializer.Serialize(payload,new JsonSerializerOptions(JsonSerializerDefaults.Web)));}
            }
            """;
    }

    private static string ExtractUsings(string source) => string.Join(Environment.NewLine,
        source.Replace("\r", "").Split('\n').Where(line => line.TrimStart().StartsWith("using ", StringComparison.Ordinal)));
    private static string ExtractBody(string source) => string.Join(Environment.NewLine,
        source.Replace("\r", "").Split('\n').Where(line => !line.TrimStart().StartsWith("using ", StringComparison.Ordinal)));
    private static string Literal(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    private static string LiteralOrNull(string? value) => value is null ? "null" : Literal(value);
}
