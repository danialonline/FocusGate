# FocusGate v0.2 — experimental Windows build

**Not a verified release.** Source-only project. Windows .NET 8 SDK and WiX 5 required. Run `powershell -ExecutionPolicy Bypass -File .\build.ps1` on Windows, then test the resulting `FocusGate.msi` in a VM before installing on your main PC. No Windows compilation or MSI execution has been performed here.

## OAuth setup
Create a Google Cloud project, enable Google Tasks API and Google Calendar API, configure consent screen and add your test account, create OAuth Desktop credentials and download the JSON. Launch the desktop app and enter the file path. The service cannot interactively authenticate as LocalSystem and the desktop token cache is per-user; **Google-dependent rules are not production-functional in the service in this version**. The app's interactive Google sync can be tested, but the service will fail closed for Google rules. Never activate these rules on your main PC.

## Safe test procedure
1. Use a disposable Windows 11 VM with a snapshot and an administrator account.
2. Build and install MSI with administrator privileges. Launch FocusGate from Start Menu.
3. Test only a harmless sample app executable and `example.com`; do not target browsers, security software, or system executables.
4. Confirm the Windows service status using `Get-Service FocusGate`.
5. Remove the test rule and uninstall before leaving the VM. Inspect the hosts file manually.

## Enforcement limitations
Hosts file domain blocking covers exact domain and www only; process termination is polling, not launch prevention. Strict mode is UI-only, not protected by service-side permissions. Service runs as LocalSystem and currently writes ProgramData settings without hardened ACLs. There is no trusted service-to-user OAuth broker, signed executable, browser extension, guaranteed hosts cleanup, or robust lock recovery. No claim of Cold Turkey equivalence. Treat this as an experimental scaffold, not an irreversible blocker.

## Administrator recovery
Stop-Service FocusGate

Edit `%ProgramData%\FocusGate\settings.json` to set `Rules` to `[]`. Remove the managed FocusGate block from `C:\Windows\System32\drivers\etc\hosts` with an elevated editor. Restart service if needed.

## Build MSI in GitHub Actions (no local SDK required)

1. Create a **private** GitHub repository (e.g. `FocusGate`).
2. Upload **the contents of this `FocusGate` directory** to the repository root, including the hidden `.github/workflows/windows-msi.yml` file. Do not upload only the ZIP.
3. In GitHub, open **Actions → Build Windows MSI → Run workflow** on `main`.
4. Wait for the workflow to finish. Download the `FocusGate-Windows-MSI` artifact from that run. It contains `FocusGate.msi`.
5. If the workflow fails, inspect its failed step's log; no MSI artifact will be produced.

**Status:** This is an automated build configuration, **not a verified working installer**. GitHub Actions must actually run before build success can be confirmed. Google OAuth currently runs in a different Windows identity from the service, so Google-driven enforcement is not production-ready. The current hosts-file/process-kill mechanisms are bypassable. Do not enable strict locking on your main machine; test in a disposable Windows VM first. Do not commit Google OAuth secrets or tokens to the repository.
