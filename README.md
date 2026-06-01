# RdpManager

RdpManager is a simple RDP connection manager for Windows built with WinForms (.NET).

It provides a lightweight UI to organize and launch Remote Desktop (RDP) connections, including Quick Connect for fast one-off sessions.
## NOTE!!
I`ve been using AI to code this, mostly for fun. 

## Key features

- Tab organization: add, rename, and remove tabs.
- Embedded RDP sessions via the Microsoft RDP ActiveX control (MsTscAx), with automatic window-based session sizing.
- Fallback launch with native mstsc.exe if embedded launch fails.
- Quick Connect: editable dropdown with typed input, history, and autocomplete.
- Quick Connect settings: default username for Quick Connect.
- Import from Active Directory and bulk import into a selected tab.
- Manage Connections dialog for editing, reassigning tabs, and cleanup.
- Persisted connection list, tab settings, sort settings, and Quick Connect history.

## Application flow

1. Startup
	- Program entry point creates and runs MainForm.
	- MainForm loads user settings and saved connections from AppData.
	- Tabs are initialized, the selected tab is restored, and the connection list is rendered.

2. Data loading and UI state
	- Connections are loaded from connections.json.
	- Settings are loaded from settings.json.
	- Sort mode, selected tab, column widths, and Quick Connect history are restored.

3. Launching a connection
	- User launches from list item activate (double-click/Enter), context menu Connect, or Quick Connect.
	- MainForm builds a Connection object for the target host.
	- Launch path is ActiveX-first:
	  - Opens an embedded RDP session window (RdpSessionForm) with RdpActiveXHost.
	  - Configures host, username/domain, optional port, and session behavior.
	  - Session display size follows the window client size and updates on resize.
	- If embedded launch fails, app falls back to mstsc.exe by generating a temporary .rdp file and starting mstsc with that file.

4. Quick Connect behavior
	- Accepts host or host:port input.
	- Applies default Quick Connect username if configured.
	- Saves the entered target to Quick Connect history for autocomplete.

5. Persistence model
	- Stored: connection identity data (name, host, port, domain, username, tab, description), tabs, sort state, column layout, Quick Connect history.
	- Not stored: session resolution fields are runtime-only and not persisted.

## Make commands

The project Makefile wraps common dotnet and release steps.

- `make build`
	- Builds `RdpManager/RdpManager.csproj` in the default configuration.

- `make run`
	- Runs the app from source using `dotnet run`.

- `make clean`
	- Runs `dotnet clean` for the project.

- `make publish`
	- Publishes a Release build for the configured runtime.
	- Defaults from Makefile:
	  - `RUNTIME=win-x64`
	  - `SELF_CONTAINED=true`
	  - `PUBLISH_DIR=publish`
	  - `VERSION=1.0`
	  - `ASSEMBLY_VERSION=$(VERSION)`
	  - `FILE_VERSION=$(VERSION)`
	- Also enables single-file publish and writes output to the publish directory.

- `make release`
	- Runs `publish`, then attempts to:
	  - create tag `v$(VERSION)`
	  - push `main`
	  - push the version tag
	- Note: the git commands are prefixed with `-`, so failures do not stop the target.

## Versioned publish flow

Use this flow when producing a versioned build and Git tag.

1. Make sure `main` is clean and up to date.
2. Pick a version number (for example `1.2.0`).
3. Publish with version metadata:

```sh
make publish VERSION=1.2.0
```

4. Check output in the publish folder (default `publish/`).
5. If everything looks good, run release tagging/push:

```sh
make release VERSION=1.2.0
```

6. Create/update your GitHub release and upload the published executable/artifacts.

Common variations:

```sh
# Publish for a different runtime
make publish VERSION=1.2.0 RUNTIME=win-arm64

# Publish framework-dependent instead of self-contained
make publish VERSION=1.2.0 SELF_CONTAINED=false

# Publish to a custom output folder
make publish VERSION=1.2.0 PUBLISH_DIR=publish/1.2.0
```

## Portable Release

A portable `.exe` build is published on the project's Releases page — download the portable executable and run it directly on any Windows PC without installation. The portable release does not require the .NET SDK to be installed.

If you prefer to build from source, use the .NET SDK and your preferred IDE (Visual Studio, Rider) to build the `RdpManager` project.


## Contributing

Bug reports and pull requests are welcome. Open issues for feature requests or UI suggestions.



## License

See repository for license information.
