README

Welcome to SharpQ3Load – the C# program that loads Quake 3 maps and lets you run around in them!

How to build and run:

1.	Clone the repository
2.	Open sharpq3load.sln in Visual Studio 2019 or a similar version
3.	Build the solution in Debug or Release – both will work
4.	Place the full Quake3 baseq3 directory into the Debug/Release directory. Get this from a Quake3 install directory such as from Steam or a Quake3 cd. On Steam Quake3 is a few bucks. The baseq3 dir has all the built-in artwork and sounds from Quake3. It is not open source and so can’t be included in the repo.
5.	Start the SharpQ3LoadUI project or run the sharpq3load.exe from the Release/Debug directory

Choosing a map and basic controls:

After step 5 above you’ll enter into fullscreen mode and be presented with a map load dialog. Choose one of the built in Quake3 maps from the first three columns. It will load in a progress window. When loading is complete you will be placed in that map at one of the spawnpoints at random. 

Use the WASD keys and mouse to move around and space to jump. Press E to warp forward to get through doors or walls. Press O to open a new map. Press Q to quit the program. Press H for a help screen showing you some more commands.

How to load a custom map from pk3:

In the map map dialog is an open file button on the top left with two options. Open a pk3 and open a pk3 with no collision detection. Usually, you’ll want the first option. Select this and choose the pk3 on disk for the custom map. Note – I tested many maps from https://lvlworld.com/ which has a great selection. Some significant processing occurs when first loading a custom map from pk3. If this takes say more than five minutes, consider using the second option(no collision detection). This will make the map load fast but you won’t experience any collision detection when moving around the map. You’ll be a ghost. Also, once the map loading completes for the first option(with collision detection), it will subsequently load fast as a cached vrml file will be on disk next to the pk3.

Features:

-	Rendering of many elements of Quake 3 maps such as texturing and shader effects
-	WASD style movement
-	Collision detection
-	Jumping and gravity
-	Jumpads
-	Portals
-	Quake 3 music and some sound effects
-	Built in loading of all standard Quake 3 maps
-	Loading custom maps from pk3s on disk
-	Optional on screen text and debugging information
-	Ability to throw two kinds of projectiles – these get destroyed after 10 seconds and don’t hit anything. 

More Details:

-	Most shader effects work the same as in Quake 3 or close.

-	Some shader effects don’t work at all or don’t work fully. 

-	Fog is not supported at all yet. I didn’t attempt it but would like to. 

-	The moving type skies scroll and show the correct layers but they don’t render as a dome like in Quake 3 and so don’t have that very realistic effect. I tried to get this to work by looking at the Quake 3 source but couldn’t figure it out. There are a few built in maps which use a skybox and that is not supported.

-	The moving arrows on launch pads don’t sync right with the color flashing. This was a shader oddity I couldn't easily solve.

-	Custom maps will tend to have more render issues than built-in maps. 

-	This program ultimately loads all maps from vrml files(custom pk3s are converted to vrml by the program) and I organize the faces into groups called shapes. Each shape renders itself based on a Q3 shader. If no Q3 shader exists for a shape then lightmapping or vertex coloring is used. In all cases glsl is used to render everything in the map. The glsl shader can have any number of stages which can be looked at as layering textures on top of eachother to create mesmerizing effects. Some stages rotate the texture. Some scale it. Some shaders appear to animate by changing the texture for a stage on a time interval. See the Q3 shader manual for more info. 

-	As always in graphics programming you have to render overlapping non-opaque faces back to front to have it look right. For example if two see through Q3 lamps line up with each other, you need to render the back one first and the front one on top of it. I take care to do this for all the built in maps. The Quake 3 shaders provide some information to help know when you have to sort things manually but it's not perfect. Custom maps tend to exhibit more sorting problems than the built in maps. I took great pains to make the built-in maps look right in all possible cases with respect to this but there are still a few issues for example with some flourescent lights in Q3. You wouldn't notice unless you looked hard.

-	This program contains code to convert Quake 3 shaders to glsl shader code. These glsl shaders are then used to render the map. If you run in debug you can see these glsl shaders as they are dumped to text files in the temporary directory(for debugging). Look at the end of Shape.InitializeNonGL.

-	While in a map, press P and right click somewhere to shoot an intersecting ray. Printed in the top left will be all the faces you penetrated with accompanying information. This is great for debugging and seeing what’s what in a map. Right click again to shoot another ray.

-	Press middle mouse to zoom in.

-	The map load process is multi-threaded in various ways. It will use all your cores. The playing part is single threaded.

*This program is not intended to reproduce Quake 3 in C# with all its gameplay and features. There are no battle modes or powerups. There is pretty much no HUD. The focus was mostly on the rendering and making that as close to Quake 3 as possible. All of the code except a few shader details taken from the Quake3 source is original.

Future Areas of Work:

Make the projectiles smaller and have collision detection. I would just make them be a point in space for starters. When they hit something play some sound and maybe display an effect. This would be lots of fun to implement.

Enable fog.

Fix the skies.

Support recording demos for fun. This would basically just be recording all the camera turns and position changes to a file and playing it back. I always thought this was cool in other games like halflife and it seems straightforward to implement.

History:

This program started as a final class project for a Graphics class circa 2005. The founders are Scott Nykl and Adam Bruss. The original idea and inspiration to load Quake3 maps from vrml and allow moving around in them came from Scott. Adam would make subs at Quiznos occasionally for Scott and the two started talking. After the class ended Adam continued working on it to bring it to where it is today. The vast majority of functionality and code present today came after the class ended.
