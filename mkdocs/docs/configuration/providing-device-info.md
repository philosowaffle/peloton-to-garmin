
# Device Info

A given workout must be associated with a Garmin device in order for it to count towards Challenges and Badges on Garmin.  Additionaly, certain devices also unlock additional data fields and measurements on Garmin Connect.  The default devices used by P2G have been chosen specifically to ensure you get the most data possible out of your Peloton workouts.

By default, P2G uses the TACX App device type for Cycling activities. At this time, TACX is the only device that is known to unlock the cycling VO2 Max calculation on Garmin Connect.  For all other workout types, P2G defaults to using a Fenix 6 device.

This means on Garmin Connect your Peloton workouts will show a device image that does not match your personal Garmin device.

## VO2 Max

Garmin _unlocks_ certain workout metrics and fields based on the Garmin device you personally own, one of those metrics is VO2 Max.  This means that if your personal device supports VO2 Max calucations, then your Peloton workouts will also generate VO2 Max when using the default P2G device settings.  If your personal device does not support VO2 Max calculations, then unfortunately your Peloton workouts will also not generate any VO2 Max data.

You can check the [Owners Manual](https://support.garmin.com/en-US/ql/?focus=manuals) for your personal device to see if it already supports the VO2 max field.

## Custom Device Info

If you choose, you can provide P2G with your personal Device Info which will cause the workouts to show the same device you normally use.

**Note:**

* Setting your personal device is completely optional, P2G will work just fine without this extra information
* Setting your personal device *may* cause you to not see certain fields on Garmin (see notes about VO2 max above)
* Setting your personal device will mean it is applied on **all** workout types from Peloton

### Steps

1. Get your Garmin current device info
    1. Log on to Garmin connect and find an activity you recorded with your device
    1. In the upper right hand corner of the activity, click the gear icon and choose `Export to TCX`
    1. A TCX file will be downloaded to your computer
1. Prepare your device info for P2G
    1. Find the TCX file you downloaded in part 1 and open it in any text editor.
    1. Use `ctrl-f` (`cmd-f` on mac) to find the `<Creator` section in the file, it will look similar to the sample device info below.
    1. Delete everything else in this file except for the `Creator` section
    1. If your `Creator` section contains something like `<Creator xsi:type="Device_t">`, delete the `xsi...` so that the final structure matches the example below
    1. Save the file as `deviceInfo.xml`
1. Configure P2G to use the device info file
    1. Move your prepared `deviceInfo.xml` file so that it is in your P2G folder
    1. Modify the [DeviceInfoPath](json.md#format-config) to point to the location of your `deviceInfo.xml`
        1. If you are using Docker, ensure you have mounted the files location into the container

### Example

```xml
<Creator>
  <Name>Garmin Sample Device - please create from exported TCX file</Name>
  <UnitId>00000000000</UnitId>
  <ProductID>0000</ProductID>
  <Version>
    <VersionMajor>0</VersionMajor>
    <VersionMinor>0</VersionMinor>
    <BuildMajor>0</BuildMajor>
    <BuildMinor>0</BuildMinor>
  </Version>
</Creator>
```
