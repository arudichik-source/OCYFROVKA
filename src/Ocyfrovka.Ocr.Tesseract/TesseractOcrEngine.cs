using System.Diagnostics;
using System.Globalization;
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

        var arguments = new[]
        {
            Quote(request.FilePath),
            "stdout",
            "--tessdata-dir", Quote(tessdata),
            "-l", Quote(request.Language),
            "--psm", ProfileToPsm(request.Profile),
            "tsv"
        };

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = string.Join(' ', arguments),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(executable)!
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Tesseract завершився з кодом {process.ExitCode}: {stderr.Trim()}");
        }

        return ParseTsv(stdout);
    }

    private static OcrResult ParseTsv(string tsv)
    {
        var words = new List<OcrWord>();
        var text = new List<string>();
        double confidenceSum = 0;
        var confidenceCount = 0;

        foreach (var line in tsv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1))
        {
            var parts = line.TrimEnd('\r').Split('\t');
            if (parts.Length < 12 || string.IsNullOrWhiteSpace(parts[11])) continue;

            _ = int.TryParse(parts[6], out var x);
            _ = int.TryParse(parts[7], out var y);
            _ = int.TryParse(parts[8], out var width);
            _ = int.TryParse(parts[9], out var height);
            _ = double.TryParse(parts[10], NumberStyles.Float, CultureInfo.InvariantCulture, out var confidence);

            var value = parts[11].Trim();
            words.Add(new OcrWord(value, confidence, x, y, width, height));
            text.Add(value);

            if (confidence >= 0)
            {
                confidenceSum += confidence;
                confidenceCount++;
            }
        }

        return new OcrResult(
            string.Join(' ', text),
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

    private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"")}\"";
}
