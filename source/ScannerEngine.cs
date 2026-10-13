using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using ImageMagick;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
namespace CremaTools;

public class SystemFontResolver : IFontResolver
{
    public FontResolverInfo ResolveTypeface(string family, bool bold, bool italic) => new(bold ? "crema-bold" : "crema-regular");
    public byte[] GetFont(string face) => File.ReadAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), face == "crema-bold" ? "segoeuib.ttf" : "segoeui.ttf"));
}
public static class ScannerEngine
{
    public static List<string> Languages() => OcrEngine.AvailableRecognizerLanguages.Select(l => l.LanguageTag).ToList();
    public static async Task<(string Text, OcrResult Result)> Recognize(byte[] bytes, string language) { var engine = OcrEngine.TryCreateFromLanguage(new Language(language)) ?? throw new Exception("La langue OCR n’est pas installée. Ajoutez-la dans Paramètres Windows → Heure et langue → Langue."); using var stream = new InMemoryRandomAccessStream(); using (var writer = new DataWriter(stream)) { writer.WriteBytes(bytes); await writer.StoreAsync(); writer.DetachStream(); } stream.Seek(0); var decoder = await BitmapDecoder.CreateAsync(stream); using var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore); var result = await engine.RecognizeAsync(bitmap); return (string.Join(Environment.NewLine, result.Lines.Select(l => l.Text)), result); }
    public static async Task<JobResult> Scan(List<string> files, string folder, string format, string language, bool enhance, IProgress<string> progress, CancellationToken cancel)
    {
        var outputs = new List<string>(); var errors = new List<string>(); var texts = new List<(string Title, string Text)>(); using var pdf = new PdfDocument(); int index = 0; foreach (var file in files)
        {
            try { int page = 0; foreach (var frame in FileEngine.Frames(file, 180, cancel)) { cancel.ThrowIfCancellationRequested(); progress.Report($"Lecture du texte : {Path.GetFileName(file)}, page {++page} ({++index})…"); using var image = new MagickImage(frame.Data); image.AutoOrient(); image.Resize(new MagickGeometry(OcrEngine.MaxImageDimension - 1, OcrEngine.MaxImageDimension - 1) { Greater = true }); if (enhance) { image.ColorSpace = ColorSpace.Gray; image.Contrast(); image.Deskew(new Percentage(40)); } image.Strip(); image.Format = MagickFormat.Png; var bytes = image.ToByteArray(); var recognized = await Recognize(bytes, language); texts.Add((frame.Name, recognized.Text)); if (format.StartsWith("PDF")) { FileEngine.AddImage(pdf, bytes); var last = pdf.Pages[pdf.PageCount - 1]; using var g = XGraphics.FromPdfPage(last, XGraphicsPdfPageOptions.Append); double factor = Math.Min((last.Width.Point - 36) / image.Width, (last.Height.Point - 36) / image.Height), left = (last.Width.Point - image.Width * factor) / 2, top = (last.Height.Point - image.Height * factor) / 2; foreach (var line in recognized.Result.Lines) { if (line.Words.Count == 0) continue; double x = line.Words.Min(w => w.BoundingRect.X), y = line.Words.Min(w => w.BoundingRect.Y), bottom = line.Words.Max(w => w.BoundingRect.Bottom); var font = new XFont("CremaSans", Math.Max(4, (bottom - y) * factor * .78)); g.DrawString(line.Text, font, new XSolidBrush(XColor.FromArgb(0, 0, 0, 0)), new XPoint(left + x * factor, top + bottom * factor)); } } } }
            catch (OperationCanceledException) { throw; }
            catch (Exception e) { errors.Add(Path.GetFileName(file) + " : " + e.Message); }
        }
        if (format.StartsWith("PDF") && pdf.PageCount > 0) { var file = Storage.Unique(folder, "Documents scannés - recherchables.pdf"); pdf.Save(file); outputs.Add(file); } else if (texts.Count > 0) { var file = Storage.Unique(folder, format.StartsWith("Word") ? "Texte reconnu.docx" : "Texte reconnu.txt"); if (format.StartsWith("Word")) WriteDocx(file, texts); else File.WriteAllText(file, string.Join(Environment.NewLine + Environment.NewLine, texts.Select(t => t.Title + Environment.NewLine + t.Text)), Encoding.UTF8); outputs.Add(file); }
        return new(outputs, errors, files.Sum(f => new FileInfo(f).Length), outputs.Sum(f => new FileInfo(f).Length));
    }
    public static void WriteDocx(string file, List<(string Title, string Text)> texts) { using var zip = ZipFile.Open(file, ZipArchiveMode.Create); void Entry(string name, string data) { using var writer = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false)); writer.Write(data); } Entry("[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/></Types>"); Entry("_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/></Relationships>"); XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"; var body = new XElement(w + "body"); foreach (var text in texts) { body.Add(new XElement(w + "p", new XElement(w + "r", new XElement(w + "rPr", new XElement(w + "b")), new XElement(w + "t", text.Title)))); foreach (var line in text.Text.Replace("\r", "").Split('\n')) body.Add(new XElement(w + "p", new XElement(w + "r", new XElement(w + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), line)))); } body.Add(new XElement(w + "sectPr", new XElement(w + "pgSz", new XAttribute(w + "w", 11906), new XAttribute(w + "h", 16838)), new XElement(w + "pgMar", new XAttribute(w + "top", 1134), new XAttribute(w + "bottom", 1134), new XAttribute(w + "left", 1134), new XAttribute(w + "right", 1134)))); Entry("word/document.xml", new XDocument(new XElement(w + "document", new XAttribute(XNamespace.Xmlns + "w", w), body)).ToString()); }
}
