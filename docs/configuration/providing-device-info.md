---
layout: default
title: Providing Device Info
parent: Configuration
nav_order: 3
---

# Custom Device Info

By default, P2G using a custom device when converting and upload workouts. This device information is needed in order to count your Peloton workouts towards Challenges and Badges on Garmin. However, you may observe on Garmin Connect that your Peloton workouts will show a device image that does not match your personal device.

If you choose, you can provide P2G with your personal Device Info which will cause the Garmin workout to show the correct to device. Note, this is completely optional and is only for cosmetic preference, your workout will be converted, uploaded, and counted towards challenges regardless of whether this matches your personal device.

## Steps

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
    1. Modify the [DeviceInfoPath]({{ site.baseurl }}{% link configuration/json.md %}#format-config) to point to the location of your `deviceInfo.xml`
        1. If you are using Docker, ensure you have mounted the files location into the container

## Example

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
