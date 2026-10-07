namespace Ocyfrovka.Ocr.Tesseract;

public static class TesseractRuntimeLocator
{
    public static string GetExecutablePath(string applicationBaseDirectory)
        => Path.Combine(applicationBaseDirectory, "Engine", "OCR", "tesseract.exe");

    public static string GetTessdataPath(string applicationBaseDirectory)
        => Path.Combine(applicationBaseDirectory, "Engine", "OCR", "tessdata");

    public static bool IsRuntimeAvailable(string applicationBaseDirectory)
        => File.Exists(GetExecutablePath(applicationBaseDirectory))
           && Directory.Exists(GetTessdataPath(applicationBaseDirectory));
}
