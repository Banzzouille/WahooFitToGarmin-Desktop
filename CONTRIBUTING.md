# Contributing

## What you need

[.NET 10 SDK](https://dotnet.microsoft.com/download). Nothing else — the
solution restores everything it needs from nuget.org, and `nuget.config` pins it
there so your machine's own package sources do not change the result.

The application builds and runs on Windows and on macOS. There is no
Windows-only project any more.

## The commands CI runs

These are the same three steps `.github/workflows/ci.yml` runs on both
operating systems. If they pass locally they should pass there, and a
disagreement between the two is worth investigating rather than retrying.

```
dotnet restore WahooFitToGarmin-Desktop.slnx
dotnet build   WahooFitToGarmin-Desktop.slnx --configuration Release --no-restore
dotnet test    WahooFitToGarmin-Desktop.slnx --configuration Release --no-build
```

The solution is a `.slnx` file, which needs the .NET 10 SDK. Older SDKs do not
read it.

## Running it

```
dotnet run --project WahooFitToGarmin.UI
```

Settings and logs are written to the usual per-user location for the platform,
not next to the executable:

- Windows: `%LOCALAPPDATA%\WahooFitToGarmin_Desktop`
- macOS: `~/Library/Application Support/WahooFitToGarmin_Desktop`

The settings file is base64 encoded rather than encrypted. That is obfuscation,
not protection, and it is why no password is stored in it.

## Packaging

Release archives are built by `.github/workflows/package.yml`, run by hand from
the Actions tab. It produces three self-contained archives — Windows x64, macOS
Apple Silicon, macOS Intel — which are attached to a release manually.

To produce a macOS application bundle locally:

```
./build/macos/make-app-bundle.sh Release osx-arm64
```

The bundle is ad-hoc signed, which is what lets it run at all on Apple Silicon.
It is not signed for distribution, so macOS still refuses it on first launch
until the quarantine attribute is removed or the application is allowed in
System Settings. The script prints both options when it finishes.

## Planning

Non-trivial work is planned in `openspec/changes/` before it is written: a
proposal, a design recording the decisions and what was rejected, and a task
list. Reading the design for the area you are touching will usually explain why
something is the way it is, including the cases where the obvious approach was
tried and abandoned.

Validate a change document with:

```
npx openspec validate <change-name> --strict
```
