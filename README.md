# CashTracker

Download EXE (GitHub Release):

- Latest release page: https://github.com/01burark-oss/CashTracker/releases/latest
- Direct download (latest): https://github.com/01burark-oss/CashTracker/releases/latest/download/CashTracker-Setup.exe
- SHA256 (latest): https://github.com/01burark-oss/CashTracker/releases/latest/download/CashTracker-Setup.exe.sha256

How to use:

1. Download `CashTracker-Setup.exe` from the direct link above.
2. Run the installer.
3. Complete license activation on first launch.

Create local release artifact:

```powershell
.\scripts\publish-release.ps1
```

Optional:

```powershell
.\scripts\publish-release.ps1 -Version X.Y.Z
```

Publish a new GitHub release (automatic):

1. Update `<Version>` in `CashTracker.App/CashTracker.App.csproj`.
2. Commit and push `main`.
3. Create and push tag:

```powershell
git tag vX.Y.Z
git push origin vX.Y.Z
```

Tag push triggers `.github/workflows/release.yml` and uploads:

- `CashTracker-Setup.exe`
- `CashTracker-Setup.exe.sha256`

Release note:

- Primary install asset is `CashTracker-Setup.exe`.
- In-app updates read the latest GitHub Release, download `CashTracker-Setup.exe`, verify the sibling `.sha256`, and run the installer in per-user upgrade mode.
