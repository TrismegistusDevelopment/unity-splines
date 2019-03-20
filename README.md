# unity-navigation-splines
Tool for making navigation bezier splines with points, events and bindings to colliders
## Installation
- Download *.unitypackage from last release (note minimal Unity version in description)
- Double-click on downloaded file or in Unity `Assets - Import Package - Custom Package...` and select downloaded file
- Import all files
## Setting up
1. Create empty GameObject in scene
1. Add `NavigationManager` script on it
1. Create new `NavigationData` as suggested in component or directly in Project window and drag to corresponding field
## Usage
- **Waypoint coloring gradient** - colorize path and waypoints using unity build-in Gradient class
- **Closed spline** - to make spline closed/open
- **Stick to colliders** - to raycast down from every waypoint and change their position.y if colliders found. Expensive
- **Smoothing per unit** - relative value for spline subdivision.
- **Add** - adding new waypoint in custom position. Hold `shift` to quickly add at the end
- Waypoints
  - **Move** - moving waypoint to another position in list
  - **Del** - delete waypoint. Hold `shift` to quickly delete without prompt
  - **Caption** - to display in Scene View
  - **Basic** - if false, caption won't be displayed in Scene View
