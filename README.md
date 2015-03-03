ShowAutoRenamer
===============

Automagically renames all your episodes to nicer, easier to read format. For example by default the name for Walking Dead Season 1 Episode 1 will be:
S01E01 - Days Gone Bye. 

**Alpha builds** are for testing, they will be always at least partly working but they are not tested.

**Beta builds** should be at least somehow tested, most of new things for the release are done and this is bugfixing phase mostly

**Gamma builds** are there just in case of new features and so on

**Release candidate builds** are feature complete builds meant for testing and findings nasty bugs

**Stable builds** should be the most stable builds, but I don't gurantee anything. 

##Features
- Renames your ugly episode names to ones that are easier to read
- Beautiful and user friendly
- Smart-Rename (Automatically and asynchronously finds episode name) (Currently supports English, Czech(coming soon))
- Supports recursive renaming (always assumes that folder = season)
- Supports Drag&Drop
- Written in C#

##Changes in 3.0
- Comletely rewritten, now uses internal classes to keep information about shows, seasons and episodes.
- Updated json plugin
- UI improvements
- Application is now asynchronous!
- Offline renaming is now much better and can detect show names.
