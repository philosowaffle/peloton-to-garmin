#
# Author: Bailey Belvis (https://github.com/philosowaffle)
#
# Tool to generate a valid TCX file from a Peloton activity for Garmin.
#
import argparse
import json
import logging
import urllib3
import time

from lib import configuration
from lib import pelotonApi
from lib import tcx_builder
from lib import garminClient
from tinydb import TinyDB, Query
from datetime import datetime

##############################
# Main
##############################
class PelotonToGarmin:
    
    @staticmethod
    def run(config):
        numActivities = config.numActivities
        logger = config.logger
        pelotonClient = pelotonApi.PelotonApi(config.peloton_email, config.peloton_password)
        database = TinyDB('database.json')
        garminUploadHistoryTable = database.table('garminUploadHistory')

        if numActivities is None:
            numActivities = input("How many past activities do you want to grab?  ")

        logger.info("Get latest " + str(numActivities) + " workouts.")
        workouts = pelotonClient.getXWorkouts(numActivities)

        if config.uploadToGarmin:
            garminUploader = garminClient.GarminClient(config.garmin_email, config.garmin_password)
        
        for w in workouts:
            workoutId = w["id"]
            logger.info("Get workout: {}".format(str(workoutId)))

            if w["status"] != "COMPLETE":
                logger.info("Workout status: {} - skipping".format(w["status"]))
                continue

            workout = pelotonClient.getWorkoutById(workoutId)

            # Print basic summary about the workout here, because if we filter out the activity type
            # we won't otherwise see the activity.
            workoutSummary = tcx_builder.GetWorkoutSummary( workout )
            logger.info( "{workoutId} : {title} ({type}) at {timestamp}".format(workoutId = workoutId, title = workoutSummary['workout_title'], type = workoutSummary['workout_type'], timestamp = workoutSummary['workout_started'] ) )

            # Skip the unwanted activities before downloading the rest of the data.
            if config.pelotonWorkoutTypes and not workoutSummary["workout_type"] in config.pelotonWorkoutTypes :
                logger.info( "Workout type: {type} - skipping".format(type = workoutSummary['workout_type']) )
                continue

            logger.info("Get workout samples")
            workoutSamples = pelotonClient.getWorkoutSamplesById(workoutId)

            logger.info("Get workout summary")
            workoutSummary = pelotonClient.getWorkoutSummaryById(workoutId)

            try:
                title, filename, garmin_activity_type = tcx_builder.workoutSamplesToTCX(workout, workoutSummary, workoutSamples, config.output_directory)
                logger.info("Writing TCX file: " + filename)
            except Exception as e:
                logger.error("Failed to write TCX file for workout {} - Exception: {}".format(workoutId, e))

            if config.uploadToGarmin:
                try:
                    uploadItem = Query()
                    workoutAlreadyUploaded = garminUploadHistoryTable.contains(uploadItem.workoutId == workoutId)
                    if workoutAlreadyUploaded:
                        logger.info("Workout already uploaded to garmin, skipping...")
                        continue

                    logger.info("Queuing activity for upload: {}".format(title))
                    fileToUpload = config.output_directory + "/" + filename
                    garminUploader.addActivity(fileToUpload, garmin_activity_type.lower(), title, workoutId)
                except Exception as e:
                    logger.error("Failed to queue activity for Garmin upload: {}".format(e))

        if config.uploadToGarmin:
            try:
                logger.info("Uploading activities to Garmin...")
                garminUploader.uploadToGarmin(garminUploadHistoryTable)
            except Exception as e:
                database.close()
                logger.info("Failed to upload to Garmin. With error: {}".format(e))
                input("Press the <ENTER> key to quit...")
                raise e

        logger.info("Done!")
        logger.info("Your Garmin TCX files can be found in the Output directory: " + config.output_directory)

        if config.pause_on_finish:
            input("Press the <ENTER> key to continue...")

##############################
# Program Starts Here
##############################
if __name__ == "__main__":
    config = configuration.Configuration(argparse.ArgumentParser())
    logger = config.logger

    if config.polling_enabled:
        logger.info("Begin polling. Fetching {} activities, every {} seconds.".format(config.numActivities, config.polling_interval_seconds))
        while True:
            PelotonToGarmin.run(config)
            logger.info("Sleeping for: {}seconds".format(config.polling_interval_seconds))
            time.sleep(config.polling_interval_seconds)            
    else:
        PelotonToGarmin.run(config)
