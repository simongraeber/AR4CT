TriLib – Managed Libraries Notice

TriLib now supports two build modes for its managed libraries:

1) Source Libraries (recommended)
2) Precompiled DLLs (legacy)

This change was made because Unity frequently changes its managed ABI.
Precompiled managed DLLs can break or cause conflicts after Unity updates.
Using the source libraries allows TriLib to be compiled directly by your
Unity version, improving compatibility and long-term stability.

The active build mode is controlled by the following scripting define:

TRILIB_USE_SOURCE_LIBS

- When this define is ENABLED, TriLib uses the managed libraries source code.
- When this define is DISABLED, TriLib uses the legacy precompiled DLLs.

Only one mode is active at a time, so conflicts are avoided.

You can enable or disable Source Libraries mode at any time in:

Edit -> Project Settings -> TriLib

If you are unsure which mode to use, Source Libraries mode is recommended.
