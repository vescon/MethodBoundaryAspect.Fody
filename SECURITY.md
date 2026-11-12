# Security policy

## Supported versions

The latest release of version 3 gets fixes. Version 2 is not maintained anymore, please upgrade (see
[Upgrading to version 3](README.md#upgrading-to-version-3)).

## Reporting a vulnerability

Please do not open a public issue for a vulnerability. Use the private reporting of GitHub: open the
[Security tab](https://github.com/vescon/MethodBoundaryAspect.Fody/security) of the repository and choose "Report a vulnerability".

Describe what you found and how to reproduce it. You get an answer as soon as possible; a fix is released as a new version
of the NuGet package.

Note that the weaver runs during the build of a project and changes the IL of its assemblies, and that the aspects of a
project run with its permissions. The weaver does not execute the code of the aspects at build time.
