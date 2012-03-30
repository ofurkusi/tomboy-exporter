# Tomboy exporter
A Tomboy plug-in for exporting notebooks as LaTeX documents

This software is functional but far from perfect. It is free and will remain so for the foreseeable future. No responsibility is taken by the creator for any data loss or damage that this software might cause. Use at your own risk!

The addin is known to work with Tomboy version 1.6

## Instructions

1. Download the compressed file containing the .dll
2. Copy the .dll file to the appropriate plugin directory (see [here](https://github.com/mattguo/tomboy-image)). On Linux the directory should be `~/.config/tomboy/addins/`.
3. Start Tomboy and enable the plugin in the Preferences -> add-in menu.
4. In the main window go to Tools -> Export Notebook(s)

## Features

* Allows you to export your notebooks from Tomboy to a LaTeX file for printing or publishing
* The LaTeX file can then be compiled using appropriate software into a .ps or a .pdf file
* Preserves LaTeX formulas (within `\[... \]` braces) and links between different notes
* Automatically creates an index
* Converts greek characters, such as alpha, beta etc. into the appropriate LaTeX code
* Contains bunch of unfinished and buggy code
* Planned: export to .rtf (Rich Text Format) as well, to open in word processors

## Known issues

* Formatted hyperlinked text causes errors when parsing the outputted LaTeX code (e.g. `\hyperlink{\uline{monotonicity}{Monotonicity}}` should be `\uline{\hyperlink{monotonicity}{Monotonicity}}`)
* Tries to work with text inside formulas, treat LaTeX commands as hyperlinks etc.
* May skip some notes within a notebook, probably if an error occurs -> improve error checking?
* The code is far from pretty, it does its job but there is great room for improvements

## Version history

### Version 0.21 - 30. March 2012

* Changed the name from 'Export to LaTeX' to 'Tomboy Exporter'
* LaTeX header, updated a little bit, added a dependency needed for proper underline formatting
* Added: support for more greek letters, should now all be parsed correctly
* Added: support for more formatting styles; strikethrough, underline, monospace, small, large, huge (only one missing now is highlighted)
* Added: options to skip hyperlinking & special icelandic language settings
* Added: detects if the output file already exists and warns the user accordingly
* Added: dialog to select a file for the output
* Fixed: does no longer output the BOM mark at the beginning of files (interrupted LaTeX parsing)
* Fixed: console output now goes through the logger
* Fixed: `\begin{itemize}` should now appear in its own line in the output, not at the end of the previous line
* Fixed: does no longer display the note name both as a latex section name and as part of the main text
* Considerable code cleanup


### Version 0.18 - 27. October 2010

* Removed checkboxes that did not do anything
* Moved to the Tools menu in the main window
* Added the option to select your own filename
* The default LaTeX file header was improved
* Some code cleanup

### Version 0.15 - 14. October 2010

* First version