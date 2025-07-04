[![](https://img.shields.io/static/v1?label=Sponsor&message=%E2%9D%A4&logo=GitHub&color=%23fe8e86)](https://github.com/sponsors/philosowaffle) <span class="badge-buymeacoffee"><a href="https://www.buymeacoffee.com/philosowaffle" title="Donate to this project using Buy Me A Coffee"><img src="https://img.shields.io/badge/buy%20me%20a%20coffee-donate-yellow.svg" alt="Buy Me A Coffee donate button" /></a></span>
---

> [!TIP]
> You can find specific Upgrade Instructions by visitng the [Install Page](https://philosowaffle.github.io/peloton-to-garmin/latest/install/) for your particular flavor of P2G and looking for the section titled `⬆️ Updating`.

## New Features

- [#415] **Enhanced Elevation Gain Calculation for Cycling Workouts** - P2G can now estimate elevation gain from resistance data when Peloton doesn't provide elevation data
  - **Resistance-based calculation**: Uses resistance data to estimate grade and calculate elevation gain by processing resistance data second-by-second
  - Only counts elevation gain when resistance is above the configured "flat road" resistance
  - Provides realistic elevation estimates that account for the actual resistance profile of your workout
  - **Opt-in feature** - disabled by default in cycling settings
  - Only applies to cycling workouts when no existing elevation data is present
  - Configurable via UI under Settings > Conversion > Lap Types > Cycling Elevation Gain
  - Requires resistance and speed data from Peloton workout

## Configuration Changes

- New cycling-specific elevation gain settings added to configuration:
  ```json
  "Cycling": {
    "PreferredLapType": "Default",
    "ElevationGain": {
      "CalculateElevationGain": false,
      "FlatRoadResistance": 30,
      "MaxGradePercentage": 15
    }
  }
  ```

## Technical Notes

- This is an **experimental feature** that provides elevation estimates when Peloton data lacks elevation information
- **Resistance-based calculation**: Provides accurate estimates by processing resistance data second-by-second and only counting elevation gain during climbing segments
- Works only with cycling workouts that have resistance and speed data
- The calculation assumes a linear relationship between resistance and grade, which is a reasonable approximation for most indoor cycling scenarios

## Docker Tags

  - Console
      - `console-stable`
      - `console-latest`
      - `console-v5.1.0-rc`
      - `console-v5`

  - Api
      - `api-stable`
      - `api-latest`
      - `api-v5.1.0-rc`
      - `api-v5`
  - WebUI
      - `webui-stable`
      - `webui-latest`
      - `webui-v5.1.0-rc`
      - `webui-v5`
