using System.Diagnostics;
using System.Globalization;
using System.Text;
using Ocyfrovka.Core.Ocr;

namespace Ocyfrovka.Ocr.Tesseract;

public sealed class TesseractOcrEngine : IOcrEngine
{
    private readonly string _applicationBaseDirectory;

    public TesseractOcrEngine(string applicationBaseDirectory)
    {
        _applicationBaseDirectory = applicationBaseDirectory;
    }

    public string Name => "Tesseract (local)";

    public async Task<OcrResult> RecognizeAsync(
        OcrRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var executable = TesseractRuntimeLocator.GetExecutablePath(_applicationBaseDirectory);
        var tessdata = TesseractRuntimeLocator.GetTessdataPath(_applicationBaseDirectory);

        if (!File.Exists(executable))
        {
            throw new InvalidOperationException(
                $"Локальний OCR runtime не знайдено: {executable}");
        }

        if (!File.Exists(request.FilePath))
        {
            throw new FileNotFoundException("Вхідний файл OCR не знайдено.", request.FilePath);
        }

        var missingLanguages = TesseractLanguageCatalog.GetMissingLanguages(tessdata, request.Language);
        if (missingLanguages.Count > 0)
        {
            throw new InvalidOperationException(
                $"Відсутні OCR-моделі мов: {string.Join(", ", missingLanguages)}. " +
                $"Додайте відповідні *.traineddata у {tessdata}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(executable)!
        };

        startInfo.ArgumentList.Add(request.FilePath);
        startInfo.ArgumentList.Add("stdout");
        startInfo.ArgumentList.Add("--tessdata-dir");
        startInfo.ArgumentList.Add(tessdata);
        startInfo.ArgumentList.Add("-l");
        startInfo.ArgumentList.Add(request.Language);
        startInfo.ArgumentList.Add("--psm");
        startInfo.ArgumentList.Add(ProfileToPsm(request.Profile));
        startInfo.ArgumentList.Add("tsv");

        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
        {
            throw new InvalidOperationException("Не вдалося запустити локальний Tesseract OCR.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Cancellation is the primary outcome.
            }

            throw;
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Tesseract завершився з кодом {process.ExitCode}: {stderr.Trim()}");
        }

        return ParseTsv(stdout);
    }

    internal static OcrResult ParseTsv(string tsv)
    {
        var words = new List<OcrWord>();
        var lines = new List<string>();
        var currentLine = new StringBuilder();
        var currentKey = string.Empty;

        double confidenceSum = 0;
        var confidenceCount = 0;

        foreach (var rawLine in tsv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1))
        {
            var parts = rawLine.TrimEnd('\r').Split('\t');
            if (parts.Length < 12 || string.IsNullOrWhiteSpace(parts[11]))
            {
                continue;
            }

            _ = int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var lineNumber);
            _ = int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var paragraphNumber);
            _ = int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var blockNumber);
            _ = int.TryParse(parts[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x);
            _ = int.TryParse(parts[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y);
            _ = int.TryParse(parts[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out var width);
            _ = int.TryParse(parts[9], NumberStyles.Integer, CultureInfo.InvariantCulture, out var height);
            _ = double.TryParse(parts[10], NumberStyles.Float, CultureInfo.InvariantCulture, out var confidence);

            var value = parts[11].Trim();
            var key = $"{blockNumber}:{paragraphNumber}:{lineNumber}";

            if (currentLine.Length > 0 && !string.Equals(currentKey, key, StringComparison.Ordinal))
            {
                lines.Add(currentLine.ToString());
                currentLine.Clear();
            }

            if (currentLine.Length > 0)
            {
                currentLine.Append(' ');
            }

            currentLine.Append(value);
            currentKey = key;

            words.Add(new OcrWord(value, confidence, x, y, width, height));

            if (confidence >= 0)
            {
                confidenceSum += confidence;
                confidenceCount++;
            }
        }

        if (currentLine.Length > 0)
        {
            lines.Add(currentLine.ToString());
        }

        return new OcrResult(
            string.Join(Environment.NewLine, lines),
            confidenceCount == 0 ? 0 : confidenceSum / confidenceCount,
            words);
    }

    private static string ProfileToPsm(string profile) => profile.ToLowerInvariant() switch
    {
        "line" => "7",
        "text" => "6",
        "table" => "6",
        "numbers" => "6",
        _ => "3"
    };
}
