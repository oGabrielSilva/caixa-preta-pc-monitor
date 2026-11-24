# PcMonitor.Tests.Support

Helpers shared by test projects (a class library, not a test project): temporary databases, the read-only ACL
simulation and a seed that writes realistic data through the real stores.

- **May depend on:** production projects. **Used only by** `tests/*`; production code never references it.
- Put a helper here as soon as a second test project needs it, instead of copying it.
- No tests live here; it is exercised by the projects that use it.
