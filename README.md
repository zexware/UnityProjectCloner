# UnityProjectCloner
Seamlessly clone unity projects for use in multiple instances.

# About
The tool is used to create clone of your Unity projects for opening them in multiple instances of Unity editor.
Currently it has two methods: File System Watcher which continuesly synchronizes the projects directories and copies
all the files to the cloned directory, AND, creating symbolic links to the project directories so the Unity
editor always references to the original files instead. 

# How it Works?
## File System Watcher
UnityProjectCloner uses FileSystemWatcher class of System.IO and continuously monitors the specified project directory. Any changes made to the original project directory are reflected in the cloned directory (the performance depends on your system and type of the disk and its r/w speed).
The program also synchronizes the project directory at startup to make sure all the files are synced in both directories before initializing FileSystemWatcher. A notification icon is used to indicate the state of the program. Please don't mind the icon of the program, also, I created this program just for personal use but posting it here just in case anyone else needs it.

## Symbolic Links 
UnityProjectCloner can also create symbolic links to your project instead. 

# License
This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License along
with this program; if not, write to the Free Software Foundation, Inc.,
51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
