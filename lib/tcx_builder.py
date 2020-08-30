import xml.etree.ElementTree as etree
from datetime import datetime, timezone
import logging
import os

##############################
# Logging Setup
##############################

logger = logging.getLogger('peloton-to-garmin.Tcx_Builder')

METERS_PER_MILE = 1609.34

def getTimeStamp(timeInSeconds):
    timestamp = datetime.fromtimestamp(timeInSeconds, timezone.utc)
    iso = timestamp.isoformat()
    return iso.replace("+00:00", ".000Z")

def getHeartRate(heartRate):
    return "{0:.0f}".format(heartRate)

def getCadence(cadence):
    return "{0:.0f}".format(cadence)

def convertDistanceValueToMeters(distanceValue, distanceUnit):
    meters = 0
    if distanceUnit == "km":
        meters = distanceValue * 1000
    elif distanceUnit == "mi":
        meters = distanceValue * METERS_PER_MILE
    else:
        meters = distanceValue
    return meters

def getSpeedInMetersPerSecond(speedPerHour, distanceUnit):
    metersPerHour = convertDistanceValueToMeters(speedPerHour, distanceUnit)
    metersPerMinute = metersPerHour / 60
    metersPerSecond = metersPerMinute / 60
    return metersPerSecond

def getInstructor(workout):
    if workout["workout_type"] == "class":
        if workout["peloton"]["ride"]["instructor"] is not None:
            return " with " + workout["peloton"]["ride"]["instructor"]["name"]
    return ""
    
def getDistanceMeters(workoutSamples):
    try:
        distanceSlug = next((x for x in workoutSamples["summaries"] if x["slug"] == "distance"), None)
        if distanceSlug is not None:
            distance = distanceSlug["value"]
            originalDistanceUnit = distanceSlug["display_unit"]
            totalMeters = convertDistanceValueToMeters(distance, originalDistanceUnit)
            return "{0:.1f}".format(totalMeters), originalDistanceUnit
    except Exception as e:
            logger.error("Failed to Parse Distance - Exception: {}".format(e))
    
    return "", "unknown"

def getMaxSpeedMetersPerSecond(workoutSamples, distanceUnit):
    try:
        speedSlug = next((x for x in workoutSamples["metrics"] if x["slug"] == "speed"), None)
        if (speedSlug is not None):
            return str(getSpeedInMetersPerSecond(speedSlug["max_value"], distanceUnit))
    except Exception as e:
            logger.error("Failed to Parse MaxSpeed - Exception: {}".format(e))
    
    return "0.0"

def getAverageSpeedMetersPerSecond(workoutSamples, distanceUnit):
    try:
        speedSlug = next((x for x in workoutSamples["metrics"] if x["slug"] == "speed"), None)
        if (speedSlug is not None):
            return str(getSpeedInMetersPerSecond(speedSlug["average_value"], distanceUnit))
    except Exception as e:
            logger.error("Failed to Parse AvgSpeed - Exception: {}".format(e))
    
    return "0.0"

def getGarminActivityType(workout):
    # Valid Garmin TCX Sports: Running/Biking/Other
    # Valid Granular Garmin types: https://github.com/La0/garmin-uploader/blob/master/garmin_uploader/help.txt#L56
    # Peloton Disciplines: cardio, circuit, running, cycling, walking, strength, stretching, meditation, yoga
    fitness_discipline = ""
    try:
        fitness_discipline = workout["fitness_discipline"]
    except Exception as e:
        logger.error("Failed to Parse Activity Type, defaulting to 'Other' - Exception: {}".format(e))

    if fitness_discipline == "cycling":
        sport = dict(Sport="Biking")
        granular_garmin_activity_type = "indoor_cycling"
    elif fitness_discipline == "running":
        sport = dict(Sport="Running")
        granular_garmin_activity_type = "treadmill_running"
    elif fitness_discipline == "walking":
        sport = dict(Sport="Running")
        granular_garmin_activity_type = "walking"
    elif fitness_discipline == "cardio" or fitness_discipline == "circuit":
        sport = dict(Sport="Other")
        granular_garmin_activity_type = "indoor_cardio"
    elif fitness_discipline == "strength":
        sport = dict(Sport="Other")
        granular_garmin_activity_type = "strength_training"
    elif fitness_discipline == "yoga":
        sport = dict(Sport="Other")
        granular_garmin_activity_type = "yoga"
    else:   
        sport = dict(Sport="Other")
        granular_garmin_activity_type = "Other"
    
    return sport, granular_garmin_activity_type


def workoutSamplesToTCX(workout, workoutSummary, workoutSamples, outputDir):

    if(workoutSamples is None):
        logger.error("No workout sample data.") 
        return

    startTimeInSeconds = workout['start_time']

    etree.register_namespace("","http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2")
    etree.register_namespace("activityExtensions", "http://www.garmin.com/xmlschemas/ActivityExtension/v2")    

    root = etree.fromstring("""<TrainingCenterDatabase
  xsi:schemaLocation="http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2 http://www.garmin.com/xmlschemas/TrainingCenterDatabasev2.xsd"
  xmlns:ns3="http://www.garmin.com/xmlschemas/ActivityExtension/v2"
  xmlns="http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2"
  xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:ns4="http://www.garmin.com/xmlschemas/ProfileExtension/v1">
  </TrainingCenterDatabase>""")

    activities = etree.Element("Activities")

    activity = etree.Element("Activity")
    sport, granular_garmin_activity_type = getGarminActivityType(workout)
    activity.attrib = sport

    activityId = etree.Element("Id")
    activityId.text = getTimeStamp(startTimeInSeconds)

    lap = etree.Element("Lap")
    lap.attrib = dict(StartTime=getTimeStamp(startTimeInSeconds))

    totalTimeSeconds = etree.Element("TotalTimeSeconds")
    totalTimeSeconds.text = str(workout["ride"]["duration"])

    instructor = getInstructor(workout)
    title = "{0}{1}".format(workout["ride"]["title"].replace("/","-").replace(":","-"), instructor)

    try:
        notes = etree.Element("Notes")
        notes.text = "{} - {}".format(title, workout["ride"]["description"])
    except Exception as e:
        logger.error("Failed to Parse Description - Exception: {}".format(e))

    distanceMeters = etree.Element("DistanceMeters")
    distanceMeters.text, originalDistanceUnit = getDistanceMeters(workoutSamples)  

    maximumSpeed = etree.Element("MaximumSpeed")
    maximumSpeed.text = getMaxSpeedMetersPerSecond(workoutSamples, originalDistanceUnit)

    try:
        calories = etree.Element("Calories")
        calories.text = str(int(round((workoutSummary["calories"]))))

        averageHeartRateBpm = etree.Element("AverageHeartRateBpm")
        ahrbValue = etree.Element("Value")
        ahrbValue.text = getHeartRate(workoutSummary["avg_heart_rate"])
        averageHeartRateBpm.append(ahrbValue)

        maximumHeartRateBpm = etree.Element("MaximumHeartRateBpm")
        mhrbValue = etree.Element("Value")
        mhrbValue.text = getHeartRate(workoutSummary["max_heart_rate"])
        maximumHeartRateBpm.append(mhrbValue)

        extensions = etree.Element("Extensions")
        lx = etree.Element("{http://www.garmin.com/xmlschemas/ActivityExtension/v2}LX")
        avgSpeed = etree.Element("{http://www.garmin.com/xmlschemas/ActivityExtension/v2}AvgSpeed")
        avgSpeed.text = str(getAverageSpeedMetersPerSecond(workoutSamples, originalDistanceUnit))
        maxBikeCadence = etree.Element("{http://www.garmin.com/xmlschemas/ActivityExtension/v2}MaxBikeCadence")
        maxBikeCadence.text = getCadence(workoutSummary["max_cadence"])
        avgWatts = etree.Element("{http://www.garmin.com/xmlschemas/ActivityExtension/v2}AvgWatts")
        avgWatts.text = "{0:.0f}".format(workoutSummary["avg_power"])
        maxWatts = etree.Element("{http://www.garmin.com/xmlschemas/ActivityExtension/v2}MaxWatts")
        maxWatts.text = "{0:.0f}".format(workoutSummary["max_power"])
        lx.append(avgSpeed)
        lx.append(maxBikeCadence)
        lx.append(avgWatts)
        lx.append(maxWatts)
        extensions.append(lx)
    except Exception as e:
        logger.error("Failed to Parse Speed/Cal/HR - Exception: {}".format(e))
        return

    track = etree.Element("Track")

    metrics = workoutSamples["metrics"]
    heartRateMetrics = []
    outputMetrics = []
    cadenceMetrics = []
    speedMetrics = []

    if(metrics is None):
        logger.error("No workout metrics data.") 
        return

    for item in metrics:
        if item["slug"] == "heart_rate":
            heartRateMetrics = item
        if item["slug"] == "output":
            outputMetrics = item
        if item["slug"] == "cadence":
            cadenceMetrics = item
        if item["slug"] == "speed":
            speedMetrics = item
        
    seconds_since_start = workoutSamples["seconds_since_pedaling_start"]

    for index, second in enumerate(seconds_since_start):
        trackPoint = etree.Element("Trackpoint")

        trackTime = etree.Element("Time")
        secondsSinceStart = second
        timeInSeconds = startTimeInSeconds + secondsSinceStart
        trackTime.text = getTimeStamp(timeInSeconds)

        try:
            if heartRateMetrics:
                trackHeartRate = etree.Element("HeartRateBpm")
                thrValue = etree.Element("Value")
                thrValue.text = getHeartRate(heartRateMetrics["values"][index])
                trackHeartRate.append(thrValue)
                trackPoint.append(trackHeartRate)
                
        except Exception as e:
            logger.error("Exception: {}".format(e))

        try:
            if cadenceMetrics:
                trackCadence = etree.Element("Cadence")
                trackCadence.text = getCadence(cadenceMetrics["values"][index])
                trackPoint.append(trackCadence)
        except Exception as e:
            logger.error("Exception: {}".format(e))

        trackExtensions = etree.Element("Extensions") 
        tpx = etree.Element("{http://www.garmin.com/xmlschemas/ActivityExtension/v2}TPX")
        tpxSpeed = etree.Element("{http://www.garmin.com/xmlschemas/ActivityExtension/v2}Speed")

        try:
            if speedMetrics:
                tpxSpeed.text = str(getSpeedInMetersPerSecond(speedMetrics["values"][index], originalDistanceUnit))
                tpx.append(tpxSpeed)
        except Exception as e:
            logger.error("Exception: {}".format(e))

        try:
            if outputMetrics:
                tpxWatts = etree.Element("{http://www.garmin.com/xmlschemas/ActivityExtension/v2}Watts")
                tpxWatts.text = "{0:.0f}".format(outputMetrics["values"][index])
                tpx.append(tpxWatts)
        except Exception as e:
            logger.error("Exception: {}".format(e))
        
        trackExtensions.append(tpx)
        
        trackPoint.append(trackTime)
        trackPoint.append(trackExtensions)
        
        track.append(trackPoint)

    lap.append(totalTimeSeconds)
    lap.append(distanceMeters)
    lap.append(maximumSpeed)
    lap.append(calories)
    lap.append(averageHeartRateBpm)
    lap.append(maximumHeartRateBpm)
    lap.append(track)
    lap.append(extensions)

    activity.append(activityId)
    activity.append(lap)
    activity.append(notes)

    activities.append(activity)
    root.append(activities)
    tree = etree.ElementTree(root)

    
    filename = "{0}-{1}-{2}.tcx".format(startTimeInSeconds, title, workout['id'])

    outputDir = outputDir.replace("\"", "")
    tree.write(os.path.join(outputDir,filename), xml_declaration=True, encoding="UTF-8", method="xml")
    return title, filename, granular_garmin_activity_type
