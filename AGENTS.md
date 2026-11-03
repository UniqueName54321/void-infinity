# instructions for AI agents working on void!infinity

hey! you're working on **void!infinity**, an independent, work-in-progress fork of *osu!lazer*.

this file is for coding agents. for human-facing contribution guidelines, see [CONTRIBUTING.md](CONTRIBUTING.md).

## what this project is

void!infinity aims to give players and beatmap creators more freedom than regular osu! allows. that includes experimenting with difficulty limits, hit objects, editor behaviour, and other gameplay rules that may not make sense in upstream osu!.

- upstream base: `2026.112.0-tachyon`, plus fork-specific commits
- language: C#
- SDK: .NET 8 (follow `global.json`)
- primary tested platform: Manjaro Linux
- current status: experimental; expect incomplete features

**this is not an official osu! client.** do not treat upstream's roadmap, contribution policies, or server compatibility as requirements for this project.

## priorities

when making changes, favour the following, in order:

1. **safety and independence:** never compromise user data or interact with official osu! services in ways that could affect real accounts, scores, or multiplayer sessions.
2. **local play:** importing, editing, playing, and saving local beatmaps should work without an osu! account or an internet connection.
3. **creative freedom:** don't reintroduce arbitrary gameplay or editor restrictions just to match upstream.
4. **stability:** keep existing features working when practical, and add tests for changes that need them.
5. **maintainability:** prefer small, understandable changes over giant rewrites.

these goals don't mean every possible limit should be removed blindly. consider performance, memory usage, data integrity, and potential crashes when extending values or formats.

## networking: important!

**void!infinity is intended to become offline-first, but complete network isolation has not yet been verified.**

- do not add or restore automatic connections to official osu! authentication, score submission, multiplayer, spectating, or other online services.
- never submit test scores, modified gameplay results, credentials, or experimental protocol requests to official osu! servers.
- don't use real osu! accounts for testing. use local fixtures, mocks, dummy providers, or isolated test data.
- avoid silent telemetry, automatic update checks, remote asset loading, or other unexpected outbound traffic.
- if a task involves networking, trace the relevant call paths and explain any remaining outbound connections. do not claim the app is fully offline without testing it.
- downloading development dependencies during an explicitly authorised restore/build is different from the *game* contacting osu! servers. don't conflate the two.
- future online features, if any, should be optional and independent of official osu! infrastructure.

some relevant existing code lives in `osu.Game/Online/`, `osu.Game/OsuGameBase.cs`, and `osu.Desktop/`. inspect the current implementation before changing it; local branches may differ from the public repository.

## protecting user data

- don't assume development and release builds use isolated data directories. check storage paths before launching or changing database code.
- don't run potentially destructive migrations against a user's existing osu! installation.
- use temporary or test-specific data directories for risky experiments.
- never log, commit, or expose tokens, credentials, private files, or personally identifying information.
- don't delete or rewrite user data without explicit authorisation.

## working in the codebase

- inspect the relevant implementation and existing tests before editing.
- follow `.editorconfig`, nearby C# conventions, and existing project structure.
- keep the original `osu.*` namespaces and project names unless a task genuinely requires changing them. branding doesn't require renaming every internal symbol.
- prefer targeted edits. don't refactor unrelated code just because you can.
- preserve copyright notices, the `LICENCE` file, and applicable third-party notices.
- don't sync, merge, or rebase onto newer `ppy/osu` commits unless explicitly requested.
- don't assume upstream bug fixes or design decisions are automatically desirable here.
- if you find a security issue or vulnerable dependency, report it clearly. don't hide warnings merely to get a clean build.

## building and testing

from the repository root:

```bash
dotnet --version
dotnet build osu.Desktop/osu.Desktop.csproj -c Debug
```

to run a successful Debug build:

```bash
dotnet run --project osu.Desktop -c Debug --no-build
```

use Debug for normal development. before launching, verify the game's data directory is safe for the current test. run relevant automated tests when practical; some graphical tests may require additional setup.

if a build or test fails, report the error and what you actually tried. **never claim a test passed unless you ran it and saw it pass.**

## AI-assisted development

AI assistance is welcome here. however, you are expected to:

- understand the requested change well enough to avoid unrelated edits.
- check generated code for correctness, security, and compatibility with this older codebase.
- avoid introducing dependencies or copied code with unclear licensing.
- explain significant changes and identify assumptions or unverified behaviour.
- never fabricate test results, file contents, or implemented features.

## before finishing a task

in your final summary, include:

1. what you changed and why.
2. which files were modified.
3. which build/tests you ran, with results (or why you couldn't run them).
4. known risks, especially any possible network access or data-directory impact.

unless explicitly asked, don't create commits, push branches, open pull requests, publish builds, or make destructive changes.

## final reminder

void!infinity isn't supposed to be osu! with a different logo. **preserve the parts that make a rhythm game work, and question the restrictions that stop people from doing interesting things.**
