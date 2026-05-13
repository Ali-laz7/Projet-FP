**************************************
*             RADIANT GI             *
*        Created by Kronnect         *   
*            README FILE             *
**************************************


Welcome!
-----------------------------------------
This package is designed for Built-in pipeline. It requires Unity 2021.3 or later
Video instructions: 

Check out this video tutorial for the built-in pipeline setup:
https://youtu.be/5uKR4nToQ1Q


Quick help: what's this and how to use this asset?
--------------------------------------------------

Radiant GI brings realtime screen space global illumination to built-in pipeline.
It's a fast way to provide more natural look to the scenes.
Global Illumination means that each pixel acts as a tiny light so it bounces on the pixel surface and illuminate other surfaces.

Follow the instructions above to configure the effect in your project.


Help & Support Forum
--------------------

Check the Documentation folder for detailed instructions.

Have any question or issue?

* Support-Web: https://kronnect.com/support
* Support-Discord: https://discord.gg/EH2GMaM
* Email: contact@kronnect.com
* Twitter: @Kronnect

If you like Radiant GI, please rate it on the Asset Store. It encourages us to keep improving it! Thanks!



Future updates
--------------

All our assets follow an incremental development process by which a few beta releases are published on our support forum (kronnect.com).
We encourage you to signup and engage our forum. The forum is the primary support and feature discussions medium.

Of course, all updates of Radiant GI will be eventually available on the Asset Store.



More Cool Assets!
-----------------
Check out our other assets here:
https://assetstore.unity.com/publishers/15018



Version history
---------------

Version 8.2.1
- [Fix] Fixes an issue that delayed virtual emitters activation
- [Fix] Fixed NFO artifacts on far distance in forward

Version 8.2
- Source Brightness (reduces the brigthness of the original image before adding GI)
- GI Weight (this will reduce the pixel color to allow the added GI to be more prominent in the LDR color range)

Version 8.1
- Temporal Filter chroma threshold max range increased to 2
- Internal raycast improvements
- [Fix] Fixed reflective shadow map lighting changes linked to directional light position
- [Fix] Fixed banding artifacts on WebGL

Version 8.0
- Added "Capture Size" parameter to Radiant Shadow Map script. Increase this value to cover a wider area when using third person view cameras for example.

Version 7.0
- Added "Normals Quality" option in forward rendering path
- Max Brightness option produces now more natural results

Version 6.9
- Added specular contribution option. Decrease to avoid overexposition of GI over shiny materials.

Version 6.8
- Added NFO Tint Color option

- Virtual emitters: culling optimization
- Near Field Obscurance effect improvements
- Other Editor improvements

Version 6.6
- Virtual emitters: added Scene View bounds modifier tool

Version 6.5
- Virtual emitters: added range and material options
- Default maximum virtual emitters increased to 32
- Performance optimizations

Version 6.4.2
- Improved speed response of reflection probe changes
- Fixes

Version 6.4
- Improved near field obscurance option

Version 6.3
- Added support for orthographic camera

Version 6.2
- Added "Near Camera Attenuation" option under Artistic Controls
- Added material index field to virtual emitter
- Filters NaN pixels

Version 6.1
- Added "Volume Mask" option to Radiant Global Illumination component to filter which Radiant Volume affects each camera
- Some shader optimizations

Version 6.0
- Added "Near Field Obscurance" option
- [Fix] Fixed virtual emitters not visible in compare mode

Version 5.2
- Improved accuracy of distance attenuation term

Version 5.1
- Added "Include Forward" in the camera effect component. Can be used in deferred to also use objects that use forward rendering path in opaque queue.
- Added stencil check option under Artistic Controls section
- Improved GI reconstruction speed on new screen pixels

Version 5.0
- First version of Radiant GI for built-in pipeline
- Added "Add Material Emission" option to virtual emitters
- Added "Probes Intensity" option to reflection probe fallback
- Added "Show In Scene View" option
