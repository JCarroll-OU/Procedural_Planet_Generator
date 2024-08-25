# Procedural Planet Generator for Unity HDRP with tessellated terrain and water
A procedural planet generator for the Unity game engine which utilizes the burst compiler, jobs system, and a quad-tree LOD system for increased efficiency.
![Image1](/Images/Image1.png)
![Image2](/Images/Image2.png)
![Image3](/Images/Image3.png)
Known Issues:
- Seams between water tiles.
- Some triangles of the terrain mesh appear to be missing.
- System for assigning biomes leaves hard edges at low LODs.
  - The biomes system is not very intuitive and requires precise input tuning for "realistic" results.
- Chunks outside the view frustum are being rendered.
- Water does not render if the 'wave direction' input is zero.
  - Wave direction is calculated, in-part, using the terrain height of the ocean floor. Not assigning the input parameters for this will result in the ocean not rendering.
