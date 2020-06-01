# mb_MPVPlayer

## Prerequisite

You'll need to install mpv such that it can be ran as a command (i.e. Win+R > "mpv.exe").

[Installation Instructions](https://github.com/mpv-player/mpv)

## Installation

MusicBee > Edit > Edit Preferences > Plugins > Add Plugin
\bin\x86\Release\mb_MPVPlayer.dll

## Configuration

Here are the configurations I'm using:

MPV options

```
--mute=yes --alpha=yes --no-osd-bar --no-resume-playback --ontop=yes --keep-open=yes --no-border --geometry=-450:+1640 --autofit=444x250
```

Play files

```
.mp4 .mov
```

`--mute=yes` is essential if you don't want duplicated audio playback

`--alpha=yes` enable alpha channel

`--no-osd-bar` Hide OSD. Especially useful when mb_MPVPlayer seeks for you.

`--no-resume-playback` In case MPV uses cached playback positioning. Even though mb_MPVPlayer will sync the video at the start of loading, this option takes away unnecessary steps.

`--ontop=yes` Keep video ontop of other windows

`--no-border` No black borders around video in order to match some aspect

`--geometry=-450:+1640 --autofit=444x250` Fit player to a specific position (geometry) with a specific windows size (autofit) 444/250 ~= 16:9

## Notes

+ I'm using a modified version of a [C# mpv wrapper to force singe-instance behavior](https://github.com/SilverEzhik/umpvw). You can still use a regular mpv instance as a media player without confliction 👍

+ Seeking, pausing/playing matches up with mpv (MusicBee master, MPV slave) 👍

+ A transparent 1x1 pixel will display on unapproved files extensions. This is so that MPV stays open without stealing focus 👍

+ Video should sync automatically after loading 👍

+ Configurations are saved in plain text under MusicBee's persistent storage path, typically %APPDATA%\MusicBee