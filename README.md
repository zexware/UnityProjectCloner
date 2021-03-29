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

