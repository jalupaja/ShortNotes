# ShortNotes


# Features:
- saves last used files and their state
- Always On Top option
- Startup option
- Start Menu option
- Drag and Drop for multiple files


# ToDo:

- on page focus: if (saved) see if file changed, ask for reload
- on page focus: if (saved) see if file exists, ask for delete
- auto indent
- option to encrypt page (Hotkey?)
- enable, disable WordWrap (horizontal scrolling)
- DragDrop Tabs (outside -> new window)
- fix DragDrop between Tabs
- change encoding (by reading text to bytes and back to text txtBox.AppendText(Encoding.ASCII.GetString))


# Usage:

Startup:
- start program in tray:

`ShortNotes.exe -s`
- start program clean (delete all temporary, last used files):

`ShortNotes.exe -c`
- start program hidden (loads all last used files but doesn't safe changes to any temporary files):

`ShortNotes.exe -h`
- start program hidden and clean (doesn't load last used files but doesn't delete them. It wont safe changes to any temporary files):

`ShortNotes.exe -h -c`


Hotkeys:

global Hotkeys | Function
-------------- | ---------------
(Strg + .)     | Show/ Hide program from/ to tray


local Hotkeys  | Function
-------------- | --------------
(Strg + T)	   | create new tab
(Strg + N)	   | create new tab
(Strg + S)	   | save file
(Strg+Shift+S) | save file as
(Strg + W)	   | close tab
(Strg + F)	   | search tab
(Strg+Shift+F) | search all tabs
(Strg + R)	   | reload tab


