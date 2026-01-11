namespace ProCalcConsole;

interface IClipboard : IDisposable {
    string? GetText();
    void SetText(string value);
}
