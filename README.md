# Simulator
A Quake 3 map loader written in C#

This program loads Quake 3 maps in vrml format. The basic things supported are rendering, movement with gravity and jumping, some support for songs and sounds, collision detection, 
a map loader which supports loading all the built-in maps and loading custom maps in pk3 format. When a custom pk3 is loaded, at first some operations are performed to allow collision
detection to work. This will take some time depending on the map. If this hangs for more than about 5 minutes, then load the map with the NO CD menu item. After successfully 
loading the map with CD, successive loads will be faster as they won't have to do the collision detection precompute step again(the info is cached in a local vrml file).

Quick start guide:

Build the program in Visual Studio, copy a full baseq3 folder into the output directory where simulator.exe lives, and run the simulator.exe program. You will need the baseq3
folder colocated with simulator.exe to run the program. This is true with all Quake 3 map loaders as the baseq3 artwork is not open sourced. 
The easiest way to get this is by buying Quake 3 from steam and looking in the Quake 3 install folder. You can copy the folder from there.

Choose a map from the map loader form by clicking it. A progress bar comes up while the map is loading. After completion you will be in the level in first person view.
The WASD keys move you as in other first person programs. Spacebar jumps. Press the 'H' key to view help on the controls. You can always load a new map by pressing the 'O' key. Press 'Q' to quit the program.

Background:

Technical Details:

