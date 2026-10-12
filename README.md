# Google Translate desktop companion

This is a small, personal Windows host for the real [Google Translate](https://translate.google.com/) website. The website runs inside a native WinForms window backed by the installed Microsoft Edge WebView2 runtime, so the taskbar identifies this app instead of Microsoft Edge.

The companion is unofficial and is not affiliated with or endorsed by Google. Google Translate and its logo are trademarks of Google LLC.

## Fast workflow

- Select text in any application and press **Ctrl+Alt+T**. The companion waits for the hotkey keys to be released, sends one native Copy command to the active application, reads only the newly copied text, and opens its translation.
- Press **Ctrl+Alt+V** to translate the text already in the clipboard without sending another Copy command.
- Press **Ctrl+Alt+G** to open or focus Google Translate without copying anything.
- Press **Ctrl+Alt+W** to hide the translation window while leaving the tray companion and its shortcuts running.
- Right-click the tray icon and choose **Settings** to change any of the four global shortcuts.
- Settings also provide width and height controls plus an option to apply that exact size whenever a global shortcut opens the translation window.
- Settings provide searchable **From** and **To** language lists. The default target is **Smart: Persian ↔ English**, while fixed source and target languages can be saved for every quick translation.
- The tray menu retains a separate **Translate clipboard** action for text already copied manually.

Persian/Arabic-script selections target English. Other selections target Persian. The real website remains available for changing either language, using speech, uploading images or documents, and other supported browser features.

The companion starts quietly with Windows so its shortcuts remain ready while other work continues. It does not open a translation window at sign-in. Closing the translation window hides it; use **Exit** from the tray to stop the resident companion.

## Data and browser profile

The app stores only shortcut and window-size settings under `%LOCALAPPDATA%\TranslateDesktop`. WebView2 keeps Google website data in a dedicated persistent profile under the same folder. Translation text and account data are not logged by the companion.

## Official artwork

The app, window, tray, taskbar, Desktop shortcut, Start-menu shortcut, loading state, and settings use Google's published 512×512 Google Translate artwork. The original download and checksum are documented in [assets/SOURCE.md](assets/SOURCE.md). The build resamples those pixels into ICO sizes without reconstructing the logo.

## Build, verify, and install

Requirements: Windows 10 or later, .NET 10 SDK for building, and the Microsoft Edge WebView2 runtime for execution.

For normal installation, download `GoogleTranslate-Setup-1.1.0-win-x64.exe` from the repository's [Releases](https://github.com/vahidpourreza/google-translate-desktop/releases) page. The per-user setup requires no administrator rights, installs under `%LOCALAPPDATA%\Programs\GoogleTranslate`, creates Start-menu and optional Desktop shortcuts, and can start the tray companion with Windows. The setup is not code-signed, so Windows may identify the publisher as unknown.

To build the verified setup package locally:

```powershell
.\scripts\release.ps1
```

This requires the .NET 10 SDK and Inno Setup 6 on the build machine. The release script builds the app, runs code and live offscreen WebView tests, performs isolated install/uninstall validation, then emits the setup executable and SHA-256 file under `dist\installer`.

For a developer build installed directly from the project:

```powershell
.\scripts\publish.ps1
.\scripts\install.ps1
```

Publishing creates a self-contained x64 build at `dist\GoogleTranslate\GoogleTranslate.exe`. It runs both code-level self-tests and an offscreen, non-activating WebView2 smoke test against the live Google Translate website. Installation creates **Google Translate** shortcuts on the Desktop and Start menu, then registers a hidden current-user startup launch.

To remove shortcuts and startup registration while keeping the project and executable:

```powershell
.\scripts\uninstall.ps1
```
