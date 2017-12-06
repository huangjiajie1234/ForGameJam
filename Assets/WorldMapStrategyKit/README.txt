**************************************
*       WORLD MAP STRATEGY KIT       *
*            README FILE             *
**************************************


How to use this asset
---------------------
Firstly, you should run the Demo Scene provided to get an idea of the overall functionality.
Later, you should read the documentation and experiment with the API/prefabs.


Demo Scenes
-----------
There're several demo scenes, located in "Demos" folder. Just go there from Unity, open them in order to get an idea of the asset possibilities.


Documentation/API reference
---------------------------
The PDF is located in the Doc folder. It contains instructions on how to use the prefab and the API so you can control it from your code.


Support
-------
Please read the documentation PDF and browse/play with the demo scene and sample source code included before contacting us for support :-)

* Support: contact@kronnect.me
* Website-Forum: http://kronnect.me
* Twitter: @KronnectGames


Version history
---------------

Version 1.3 - Current release

New Features:
  - New demo scene 12 "Provinces" to showcase future province-related functionality
  - Significant performance improvement of highlighting system
  
Improvements:
  - Ability to constraint window rect. New inspector section.
  - Can customize path finding route matrix costs. New Demo Scene #11. Manual updated. New APIs: PathFindingCustomRouteMatrixReset & PathFindingCustomRouteMatrixSet
  - Can modify city icons from inspector.
  - API: Added events to GameObjectAnimator: OnMoveStart and OnMoveEnd
  - API: Added GetCountryCoastalPoints / GetProvinceCoastalPoints.
  - API: Added PathFindingGetCountriesInPath, PathFindingGetProvincesInPath, PathFindingGetCitiesInPath & PathFindingGetMountPointsInPath
  - Added geodataResourcesPath property to support different locations for datafiles
  - Added pivotY property to GameObjectAnimator to improve positioning of GameObjects
  
Fixes:
  - Fixed colored countries being cleared when ToggleCountrySurface was called during startup
  - Fixed bug with HideProvinceSurface when the province was highlighted

Version 1.2 - 2-MAR-2016

New Features:
  - Custom Attributes based on JSON. New APIs, inspector section & editor interface. Manual updated.
  - Hexagonal grid. New APIs, inspector section. Manual updated. 2 new demo scenes added.
  
Improvements:
  - Optimized path finding performance.
  - Added new menu GameObject / Create Other / WMSK Viewport for fast creation of viewport and map in one step.
  


Version 1.1 - 12-FEB-2016

New Features:
  - New day/night cycles. New demo scene.
  - Can add circles and rings to the map
  - MiniMap. New component & API (WMSKMiniMap). New demo scene. Manual updated.
  
Improvements:
  - New grouping option for viewport game objects (group property). Ability to toggle group visibility.
  - New preserveOriginalRotation for viewport game objects.
  - Ability to set/clear fog on entire countries or regions (API: FogOfWarSetCountry, FogOfWarSetCountryRegion, FogOfWarSetProvince, FogOfWarSetProvinceRegion)
  - Added Maldives and Male (capital) to geodata files
  - Added RespectOtherUI and showCursorAlways
  
Fixes:
  - Scroll On Screen Edges fail with viewport. Fixed.
  - Colorizing countries was not using alpha component of the color. Fixed.
  - Fixed circles aspect ratio.


Version 1.0 - 1-FEB-2016

New features (with respect to World Political Map 2D Edition):

  - Mount Points. Allows you to define custom landmarks for special purposes. Mount Points attributes includes a name, position, a type and a collection of tags. Manual updated.
  - New Scenic Plus style (animated water, coast foam effect and blurred/softened geography for very close zooms). Still WIP (clouds).
  - New Scenic Plus Alternate 1 (variant from previous one)
  - 3D surface for viewport with adjustable height.
  - New cloud layer for viewport with ajustable speed, elevation, alpha and ground shadows. New APIs (see manual: Earth section).
  - Fog of War layer for viewport. Can set custom transparency on given coordinates. New APIs (see manual: Fog Of War section).
  - New API for units handling over viewport through extensions for GameObject (WMSK prefix). Demo scene included.
  - New Path Finding support based on A-Star algorithm. Added terrainCapability, minAltitude and maxAltitude properties to GameObjectAnimator script.
  - New APIs for detecting water (ContainsWater)
  - New APIs for adding animated grounded and aerial paths. Check demo scene PathAndLines.
  - New line drawing system supporting animated dashed lines.
  
Improvements:
  - New options for country decorator and for country object (now can click-select/hide/rotate/offset/font size any country label)
  - Editor: Mount Point Mass Creation tool
  - Can use alpha component when colorizing countries or provinces (transparent coloring)
  - 2 new free fonts added: Volkron and Actor.
  - Map hovering performance improvement.
  - Tickers: new overlay mode option for displaying ticker texts
  - New prewarm option to compute heavy tasks at initialization time and make it smoother during play
  - Now province highlighting can be enabled irrespective of country highlighting state
  

Credits
-------

- All code, data files and images, otherwise specified, is (C) Copyright 2016 Kronnect
- JSON parser: derived by original code by Matt Schoen, distributed under MIT license
- PathFinding algorithm: derived from original code by Franco, Gustavo, distributed under Public Domain
- Non high-res Earth textures derived from NASA source (Visible Earth)
- Flag images: Licensed under Public Domain via Wikipedia
- Demo models: Unity (tank) and public domain (tower)




