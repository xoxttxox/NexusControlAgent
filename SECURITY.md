# Security Policy

## Supported Versions

Security fixes are generally provided for the latest published version of
Nexus Control Agent. Before submitting a report, please verify that the issue
still occurs with the latest release.

## Reporting a Security Vulnerability

Please do not disclose security vulnerabilities through a public GitHub issue.
Instead, use **Report a vulnerability** under the repository's **Security**
section. Private Vulnerability Reporting must be enabled in the repository
settings for this option to be available.

A useful report should include:

- the affected Agent version and Windows version
- a description of the vulnerability and its impact
- reproducible steps or a minimal proof of concept
- existing mitigations or prerequisites required for exploitation
- a possible remediation approach, if known

Do not include real device tokens, push tokens, passwords, private keys, or
personal data. Test data must be sanitized before it is uploaded.

## Secure Operation

- Never expose port `5188` directly to the public internet.
- Use a private Tailscale network for remote access.
- Pair only with devices that belong to the user and are trusted.
- In local device management, grant each smartphone only the permissions it
  actually requires, and immediately pause or remove lost devices.
- Diagnostic reports do not contain tokens, but they should still be reviewed
  for the PC name and local IP addresses before being published publicly.
- The local activity log does not contain passwords, tokens, command parameters,
  text input, filenames, or file contents and can be cleared directly from the
  activity log window.
- Keep Windows, Tailscale, and Nexus Control Agent up to date.
- Digitally sign public builds before distribution.
