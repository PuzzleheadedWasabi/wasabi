# wasabi
CHVP player proof of concept

How it works:
* Edits vlcrc config file in %APPDATA% to enable control over the rc interface (creates TWO backups - one once, one every time but overwrites)
  * The changes are very small - adds just three lines
  * The changes are transparent - if not using the app, vlc operates exactly the same way as without the changes
  * Changes persist through vlc updates (tested)
  * Security concerns: I believe nonexistent, as you can only connect from the same machine (as tested and confirmed, including with firewall off)
  * So assuming a virus wants to stalk what you're watching on vlc - it could just add file system hooks, or look at the process in the processes list, or just do what this program does... you don't need admin access to edit the file. So, I believe no security concerns
  * Possible to make it so that on exiting the app it restores vlcrc file to the status it was in before the app was opened
* Waits for VLC to start by hitting a loopback socket
* Once VLC started, uses a (very bad, should be improved) way to ask VLC the name and duration of the file
* If title contains 'wasabi' initiates control (this can be changed to check duration and length from e.g. .CHVP files)
  * For now, as a demonstration, upon initating control, the app will seek the video to 20 seconds in, pause it, resume it, print the current time, then exit VLC
  * Note that user can still continue to control VLC as they want to by exiting the player, seeking, pausing, etc, as they normally would
  * If you want to test, just rename file to include 'wasabi' (capitalisation doesn't matter). Note VLC will use the file's title in its 'properties' if it exists
    * On windows to access this and edit: right click file, select properties, select details tab, 'Title' attribute should be first in the list
  
From users perspective:
* Runs .exe (no admin access or prompt required)
* Opens video normally and control is initiated

If you'd like to try and run this and want to undo the changes made to your vlcrc config file: browse to %appdata%/vlc, delete vlcrc, rename vlcrc-wasabi-1.bak to vlcrc (note no file extension)

Development goals:
* Convince milovana community that this is the best approach to interactive content xD
* Make vlc integration nicer (either by improving rc or by using some other option)
* Add support for other common media players such as Plex Media Player and (insert result from a survey)
* Add support for loading chvp files
* Add keyboard hook to listen to keyboard events and do stuff
* Add silent mode, minimise to taskbar, Windows 10 notifications on control initiated, setting to run on startup, button to restore vlcrc file

Quick summary of why I think this is the best approach for interactive content:
 * Better than every new interactive thing being an .exe, as
   * Many people are not comfortable (for good reason) running random .exes (especially ones they find on pr0n forums)
     * Of course, this is an .exe, but it's one exe (as opposed to a new exe for each release) and it's open source, you can compile it yourself. Not an option for most releases
     * Because it's one .exe, and presumably has users, more likely that if it's doing something bad, community will find that out. Less likely for community to investigate every release with equal dedication than they would a single program
   * If not using Wasabi (e.g. watching online without a wasabi userscript extension) can still play the video normally - so, allows for easier distribution, and if you want bonus interactivity, have the **option** to use Wasabi.
   * A certain stigma surrounding pr0n games?
   * If CHVP format done well, should be easier for creators to create a CHVP file and allow interactivity - 99% of work would continue to be done using mature video editing tools without diving into game-dev stuff
 * Better than a dedicated player app because it doesn't require user to change the way they view content - just open the file as you normally would after running this exe once
   * Especially if we can integrate into plex media player, would allow users to continue to stream content to their e.g. laptop as they do now (I'm assuming people actually use Plex here, AFAIK it's very popular)

Very happy to entertain pull requests, get in touch.
