# RdpManager

RdpManager is a simple RDP connection manager for Windows built with WinForms (.NET).

It provides a lightweight UI to store and launch Remote Desktop (RDP) connections, including a Quick Connect workflow for fast one-off connections.
## NOTE!!
I`ve been using AI to code this, mostly for fun. 

## Key features

- Quick Connect: editable dropdown with typed input, history, and autocomplete.
- Quick Connect Settings: set a default username and preferred resolution for quick connects.
- Add Connection dialog: name/address, domain/user, resolution presets (including "Fullscreen") and editable custom resolutions; port defaults to 3389.
- Deterministic UI stacking and unified header visuals for a cleaner layout.
- Persisted connection/history/settings via the app's settings store.

## Portable Release

A portable `.exe` build is published on the project's Releases page — download the portable executable and run it directly on any Windows PC without installation. The portable release does not require the .NET SDK to be installed.

If you prefer to build from source, use the .NET SDK and your preferred IDE (Visual Studio, Rider) to build the `RdpManager` project.


## Contributing

Bug reports and pull requests are welcome. Open issues for feature requests or UI suggestions.



## License

See repository for license information.
