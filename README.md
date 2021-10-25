# Simulator
A Quake 3 map loader written in C#

This program loads Quake 3 maps in vrml format. The basic things supported are rendering, movement with gravity and jumping, some support for songs and sounds, collision detection, a map loader which supports loading all the built-in maps and loading custom maps in pk3 format(the program converts them to vrml), jumppads, launchpads, portals(in built in maps). 

When a custom pk3 is first loaded, some operations are performed to allow collision
detection to work. This will take some time depending on the map. If this hangs for more than about 5 minutes, then load the map with the NO CD menu item. After successfully 
loading the map with CD, successive loads will be faster as they won't have to do the collision detection precompute step again(the info is cached in a local vrml file).

Quick start guide:

Build the solution in Visual Studio, copy a full baseq3 folder into the output directory where simulator.exe lives, and run the simulator.exe program. You will need the baseq3
folder colocated with simulator.exe to run the program. This is true with all Quake 3 map loaders as the baseq3 artwork is not open sourced. 
The easiest way to get the baseq3 folder is by copying it from a Quake 3 install. You can get Quake 3 for a few bucks on Steam.

Once the program starts choose a map from the map loader form by clicking it. A progress bar comes up while the map is loading. After completion you will be in the level in first person view. The WASD keys move you as in other first person programs. Spacebar jumps. Press the 'H' key to view help on the controls. You can always load a new map by pressing the 'O' key and selecting a different map or loading one from a pk3 on disk. Press 'Q' to quit the program.

Background:

This program started as a class project circa 2005. We were already loading geometry in vrml in prior class programs. My partner had the idea of loading Quake 3 maps for the final project. We found a bsp to vrml converter. The final version of the class project would load q3dm17 with basic texturing and let you fly around. It had LAN ability so classmates could move around with each other.

After this initial work I continued work on it until around 2010. I polished up the texturing and added collision detection. I added the map loader and refined the user interface to feel like a real game(full screen, font support, alt tab support, etc). The vrml file had multitexturing and vertice color info so I tried to use that to make the textures look more like Quake 3 does. At the time I did not comprehend how shaders work so all the cool effects Quake 3 boasts did not work. What you saw was a static textured world.

I picked up the project with renewed interest in getting the shader effects to work in 2020. I switched it from the old Tao framework to an updated OpenTK version. I also realized how shaders work finally and made a converter which converts Quake 3 shaders to glsl so it can be fed to OpenGL. I overhauled the rendering system to use modern OpenGL concepts like VertexBufferObjects, etc. I focused on loading Fatal Instinct initially because it loads very fast. I got things like rgbgen and blending working. After getting Fatal Instinct to work satisfactorily I went through every built in map and made it work as closely as I could to Quake 3. Usually each map would use a new shader effect I'd have to support. 

Not everything works - skies scroll, but they don't render as a dome yet. Also fog is not supported yet. There are some issues with render order of overlapping non opaque faces. This should be only apparent in custom maps because I hardcoded fixes for all the built in maps. I think the render order problem is mostly due to missing information from the bsp. I only have what the bsp to vrml converter gives me.

In summary, the only source code I used from quake 3 were some details about how the shaders work. My code is not a port of the Q3 source to C#. It's all original code. All the artwork for maps comes from the original baseq3 you provide or from custom maps which have their own artwork. The sounds in my program come from baseq3.

What's the point?:

The main benefit of making this codebase public I think is to help people with learning modern OpenGL and shaders and how they work together. Also it will help people who want to get started making a graphics program with a good camera, map loading and advanced rendering concepts. When running the debug build, the glsl shaders(vert and frag) that are directly used for rendering are written to file in the temp directory(search for glsl_dumps in the code) for every shape that does custom shader effects. This is purely for debugging purposes but can be very educational. So for any effect seen in a quake 3 map in this program for example the flashing red lights in dm0, you can open the frag shader and see exactly how it's done. You'll probably have to view the code too a bit to understand what's passed into the glsl shaders i.e. uniforms. Also since the maps loaded by the program are in vrml format, you can see the names of the textures and Quake 3 shaders. You can cross reference these with the artwork of a map to really see what's going on behind the scenes.

-Adam Bruss
