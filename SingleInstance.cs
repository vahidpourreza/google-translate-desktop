namespace TranslateDesktop;

internal sealed class SingleInstance : IDisposable
{
    private const string MutexName = @"Local\GoogleTranslate.Singleton.v3";
    private const string OpenEventName = @"Local\GoogleTranslate.Open.v3";
    private const string SelectionEventName = @"Local\GoogleTranslate.Selection.v3";
    private const string ClipboardEventName = @"Local\GoogleTranslate.Clipboard.v3";

    private readonly Mutex _mutex;
    private readonly bool _ownsMutex;

    internal SingleInstance()
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        _ownsMutex = createdNew;
        IsFirstInstance = createdNew;

        OpenEvent = new EventWaitHandle(false, EventResetMode.AutoReset, OpenEventName);
        SelectionEvent = new EventWaitHandle(false, EventResetMode.AutoReset, SelectionEventName);
        ClipboardEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ClipboardEventName);
    }

    internal bool IsFirstInstance { get; }

    internal EventWaitHandle OpenEvent { get; }

    internal EventWaitHandle SelectionEvent { get; }

    internal EventWaitHandle ClipboardEvent { get; }

    internal void Signal(AppAction action)
    {
        if (action == AppAction.TranslateSelection)
        {
            SelectionEvent.Set();
            return;
        }

        if (action == AppAction.TranslateClipboard)
        {
            ClipboardEvent.Set();
            return;
        }

        OpenEvent.Set();
    }

    public void Dispose()
    {
        ClipboardEvent.Dispose();
        SelectionEvent.Dispose();
        OpenEvent.Dispose();

        if (_ownsMutex)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
    }
}
