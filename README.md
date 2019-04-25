# unity-navigation-splines

[![Unity Version](https://img.shields.io/badge/Unity-2018.3.6f1-blue.svg)](https://unity3d.com/get-unity/download)
[![GitHub](https://img.shields.io/github/license/hermesiss/unity-navigation-splines.svg)](https://github.com/Hermesiss/unity-navigation-splines/blob/develop/LICENSE)
[![CodeFactor](https://www.codefactor.io/repository/github/hermesiss/unity-navigation-splines/badge)](https://www.codefactor.io/repository/github/hermesiss/unity-navigation-splines)

![GitHub last commit (branch)](https://img.shields.io/github/last-commit/hermesiss/unity-navigation-splines/upm.svg?label=upm)
![GitHub last commit (branch)](https://img.shields.io/github/last-commit/hermesiss/unity-navigation-splines/upm-dev.svg?label=upm-dev)

[![image](https://user-images.githubusercontent.com/20972731/54977926-3697b700-4fb8-11e9-8ef2-3e6c35010790.png)]()

Tool for making navigation bezier splines with points, events and bindings to colliders

## Installation

- Open file `manifest.json` in `your_repo/Packages`
- Under `dependencies` add following line:

```json
"trismegistus.splines" : "https://github.com/Hermesiss/unity-navigation-splines.git#upm"
```

- Reopen your project in Unity

You _should_ commit `manifest.json`
## Update

- Open file `manifest.json` in `your_repo/Packages`
- Remove info about this package from `lock` section
- Reopen your project

## Setting up

1. Create empty GameObject in scene
1. Add `NavigationManager` script on it
1. Create new `NavigationData` as suggested in component or directly in Project window and drag to corresponding field

## Usage

### NavigationManager

![image](https://user-images.githubusercontent.com/20972731/54977968-50d19500-4fb8-11e9-9332-850ae47c7861.png)

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

### Follower

![image](https://user-images.githubusercontent.com/20972731/54978050-8b3b3200-4fb8-11e9-98fe-00f91dfa178a.png)

- **Mode** - `None` `Loop` `Once` `Ping Pong`
- **Manager** - select `NavigationManager` with desired path
- **Speed**
- **Follow Rotation** - should rotation of Follower changes according to `WaypointEntity.Velocity`(First derivative)