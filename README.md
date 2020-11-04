# UnityB3D
Imports B3D models from Blitz3D into Unity
To use, just place a B3DLoader onto a game object and have an external script call the LoadB3D function on it.
This was originally created for Sonic Journey to be able to load Blitz Sonic levels, but I release it free to use! Just give credit.
Includes 3 scripts and some shaders, all written by myself. Even imports rigging and animations!

KNOWN ISSUES:

TGA textures do not load. I've not written a TGA importer.

Using with Gamma color space will result in messed up colors. This is because the shader simulates a gamma color space in linear mode; you will have to remove some instances of "pow" near the end of the fragment code.
