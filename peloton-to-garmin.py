#
# Author: Bailey Belvis (https://github.com/philosowaffle)
#
# Tool to generate a valid TCX file from a Peloton activity for Garmin.
#
import argparse
import json
import logging
import urllib3

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

                    logger.info("Queing activity for upload: {}".format(title))
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
    PelotonToGarmin.run(config)
