# void!infinity

a wip *osu!lazer* fork that aims to give players and beatmap creators more freedom! based on `2026.112.0-tachyon`.

## installation

proper installation instructions are still a wip, but you can build and run void!infinity from source!

### prerequisites

- Linux (other operating systems are currently untested)
- Git
- .NET 8 SDK

on Arch Linux and Manjaro, you can install the dependencies with:

```bash
sudo pacman -S git dotnet-sdk-8.0
```

### building from source

first, clone the repository:

```bash
git clone https://github.com/UniqueName54321/void-infinity.git
cd void-infinity
```

then, build the game:

```bash
dotnet build osu.Desktop/osu.Desktop.csproj -c Debug
```

finally, launch it:

```bash
dotnet run --project osu.Desktop -c Debug --no-build
```

and that's it! if everything went well, void!infinity should now be running.

**note:** this is a development build, so expect developer-specific behaviour and potentially unfinished features.

## important notes

- void!infinity is a wip. as such, expect bugs, crashes, and other instabilities.
- void!infinity has only been tested on Linux, specifically Manjaro. other operating systems may work, but there are no guarantees.
- void!infinity's rebranding isn't complete, and likely will never be for codebase reasons. you can help! see [CONTRIBUTING.md](CONTRIBUTING.md) for more information
- **void!infinity is not fully offline yet.** although steps have been taken to disable connections to the official osu! servers, some network requests may still occur. until this has been properly addressed and tested, i'd recommend blocking the game's internet access using a firewall or other network restrictions.
- void!infinity is not affiliated with or endorsed by the official osu! project.
- the current development build may share data directories with osu! development builds. keep backups of any important game data.
- some dependencies currently have known security vulnerabilities. while these don't necessarily mean void!infinity itself is exploitable, they should be addressed before a stable release.
