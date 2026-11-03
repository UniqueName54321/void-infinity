# contributing to void!infinity

first off, thanks for showing interest in contributing to void!infinity!

void!infinity is a fork of *osu!lazer* that aims to give players and beatmap creators more freedom. we're especially interested in experimental features, removing unnecessary limitations, and generally allowing things that wouldn't normally be possible in osu!.

contributions are welcome, regardless of your experience level or whether you use AI tools!

## table of contents

1. [reporting bugs](#reporting-bugs)
2. [suggesting features](#suggesting-features)
3. [submitting pull requests](#submitting-pull-requests)
4. [AI](#ai)
5. [development guidelines](#development-guidelines)
6. [important notes](#important-notes)

## reporting bugs

found something broken? feel free to open an issue!

before doing so, please check whether someone else has already reported the same problem.

when reporting bugs, try to include:

- what went wrong
- what you expected to happen
- steps to reproduce the problem
- your operating system and any relevant hardware information
- logs or screenshots, if applicable

please make sure any logs or screenshots don't contain sensitive information before uploading them.

you don't need to know exactly *why* something broke. that's what debugging is for!

## suggesting features

have an idea for something that could make void!infinity better? feel free to suggest it!

experimental and unconventional ideas are especially welcome, even if they would be considered unsuitable for regular osu!.

however, keep in mind that suggesting a feature doesn't guarantee it'll be implemented.

if possible, explain what your idea does, why it might be useful (or just fun!), and how you imagine it working.

## submitting pull requests

pull requests are welcome!

before submitting one, please keep the following in mind:

- try to keep your changes focused on one feature or problem at a time.
- explain what your changes do and why you made them.
- test your changes before submitting them, where practical.
- don't intentionally introduce malware, backdoors, security vulnerabilities, or other harmful functionality.
- for major changes, consider opening an issue first so the idea can be discussed.
- make sure your changes don't introduce unintended connections to official osu! servers.
- don't worry if your code isn't perfect! feedback and revisions are a normal part of development.

pull requests will be reviewed based on their usefulness, correctness, maintainability, and compatibility with the project's goals.

## AI

unlike upstream osu!, **AI is welcome in void!infinity!** in fact, many of void!infinity's early modifications were vibecoded by DifferentDance8/UniqueName54321.

however:

- **you are responsible for your chosen AI tool's output.** if it produces broken, insecure, or otherwise unsuitable code, it's your responsibility to address that before submitting.
- if you're using an AI coding agent, please make sure it reads and follows the instructions in [AGENTS.md](AGENTS.md).
- AI-generated contributions are welcome, but they're not automatically guaranteed to be accepted. the same quality standards apply to everyone, regardless of how their code was written.

## development guidelines

void!infinity currently uses an older version of *osu!lazer* as its foundation, specifically `2026.112.0-tachyon`.

the project is primarily developed and tested on Manjaro Linux, although contributions improving support for other operating systems are welcome.

to build the game, follow the instructions in the [README](README.md).

when developing:

- use the Debug configuration for ordinary development and testing.
- avoid making unnecessary changes to existing osu! components unless they're relevant to your work.
- preserve existing functionality where practical.
- add or update tests for changes that benefit from automated testing.
- don't assume that a feature should follow osu!'s restrictions just because that's how upstream implements it.
- avoid adding unnecessary dependencies, especially ones with known security vulnerabilities.

## important notes

### offline functionality

void!infinity aims to operate independently of official osu! servers.

offline functionality is still being developed, so some network requests may remain.

**do not test modified gameplay, authentication, or score submission against official osu! servers.**

### compatibility with upstream

void!infinity is an independent fork of osu!lazer.

we don't guarantee compatibility with newer upstream versions, and there are no plans to automatically merge every upstream update.

changes should be designed around void!infinity's own goals rather than maintaining compatibility at all costs.

### licences and attribution

void!infinity is based on open-source software developed by the osu! team and other contributors.

please respect the applicable software licences, copyright notices, and third-party asset restrictions when submitting contributions.

## and finally...

void!infinity is still a work in progress, and there's plenty left to figure out.

whether you're fixing a typo, reporting a bug, implementing a new feature, or trying to make something completely ridiculous possible, contributions are appreciated!

have fun!
