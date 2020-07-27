ShowAutoRenamer
===============

Automagically renames all your episodes to nicer, easier to read format. For example by default the name for Walking Dead Season 1 Episode 1 will be: S01E01 - Days Gone Bye. Remember though, I can't gurantee it won't ruin your file names, so use with caution. Mostly it should work, but there might be some cases, where issues appear, if you encounter them, don't hesitate to report them.

**Alpha builds** are for testing, they will be always at least partly working but they are not thoroughly tested.

**Beta builds** should be at least somehow tested, most of new things for the release are done and this is bugfixing phase mostly

**Release candidate builds** these builds have no known issues and are in for additional testing

**Stable builds** should be the most stable builds, but there still might be bugs and issues.

## Features
- Renames your ugly episode names to ones that are easier to read
- Beautiful and user friendly
- Smart-Rename - automatically and asynchronously finds episode name in trakt database
- Selective renaming - just pick the files you want to rename (Warning, they should be from the same show)
- Supports Drag&Drop
- Support advanced naming (you can create your own type of name like this {showname} S{episode}E{season} {title})
- Written in C#

## Changes in 4.0, 4.1
- Trakt api updated
- Redesigned
- Multi-file support
- Removed recursive and folder options, since they are no longer required and only created issues
- Many functions improved
- Added advanced naming support (4.1)
- bugfixes

## Changes in 3.0
- Completely rewritten, now uses internal classes to keep information about shows, seasons and episodes.
- Updated json plugin
- UI improvements
- Application is now asynchronous!
- Offline renaming is now much better and can detect show names.
