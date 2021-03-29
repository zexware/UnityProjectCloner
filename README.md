# UnityProjectCloner
Seamlessly clone unity projects for use in multiple instances.

# About
While working on a multiplayer game project in Unity3D, I was having trouble debugging multiplayer functionality because Unity doesn't support
running multiple instances of game in single project. I started googling as usual and found that there are quite a few workarounds for this.
First one was obviously building the whole project and running an standalone exe build alongside the editor to debug multiplayer functionality. 
But it was quite frustration, so I tried another option: creating symbolic links of the project files. It was working well until the second instance of Unity
started to throw compilation errors and I was no longer able to enter the play mode (even I fixed compilation errors in the original files). 
Then, I came up with an idea of this little program. 

# How it Works?
UnityProjectCloner uses FileSystemWatcher class of System.IO and continuesly monitors the specified project directory. Any changes made to the 
original project directory are reflected to the cloned directory (the performance depends on your system and type of the disk and its r/w speed). 

The program also synchronizes the project directory at startup to make sure all the files are synced in both directories before initializing FileSystemWatcher. 
A notification icon is used to indicate the state of the program. 
Please don't mind the icon of the program, also, I created this program just for the personal use but leaving it here just in case anyone else needs it. 

Please check this video for quick-demonstration. 

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
