# Contributing

Thank you for your interest in Nexus Control Agent.

## Bugs and Suggestions

- Search existing issues before creating a new one.
- For bugs, use the bug report template and provide exact reproduction steps.
- For new features, use the feature request template and describe the specific
  benefit.
- Do not publish device tokens, push tokens, private IP addresses, or personal
  logs.

Security issues must not be reported through public issues. Follow the
reporting process described in `SECURITY.md`.

## Development

1. Fork the repository and create a branch from `main`.
2. Keep changes small and focused on a single topic.
3. Build the project with .NET 10 on Windows.
4. Manually test the affected functionality.
5. Update the documentation and `CHANGELOG.md` when user-visible behavior
   changes.
6. Open a pull request with a clear description.

Example branch names:

```text
fix/pairing-timeout
feature/media-session-volume
docs/installation
```

## Code Style

- preserve the existing folder and namespace structure
- do not work around nullable warnings by disabling them globally
- continue to represent UI changes in the WinForms Designer
- keep English as the neutral resource language and update every supported
  `Localization/Strings.<language>.resx` catalog when adding user-facing text
- never hard-code new user-facing desktop text when a localization resource is
  appropriate
- do not add WPF or XAML dependencies
- keep asynchronous operations cancellable with `CancellationToken`
- do not hard-code credentials or machine-specific values

A pull request should build without introducing new compiler errors and must not
contain generated directories such as `bin`, `obj`, or `artifacts`.
