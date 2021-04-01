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
import shutil
import os
import glob
import re


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
    def exportPelotonWorkoutFile( export_directory, filename, workout, workoutSamples, workoutSummary ) :
        combined_workout = { "workout" : workout, "workoutSamples" : workoutSamples, "workoutSummary" : workoutSummary }
        try:
            filepath = "{dir}/{filename}".format(dir = export_directory, filename = filename)
            with open( filepath, "w" ) as file:
                json.dump( combined_workout, file, indent = 4 )
        except:
            logging.error( "Failed to export Peloton workout file {filepath}".format(filepath = filepath) )
        else:
            logging.debug( "Exported Peloton workout file to {filepath}".format(filepath = filepath) )

    @staticmethod
    def getPelotonWorkoutFiles( import_directory ) :
        filepath = "{dir}/*.json".format(dir = import_directory)
        logging.debug( "Looking for Import files in {dir}".format(dir = filepath) )
        import_files = []
        try:
            import_files = glob.glob( filepath )
        except:
            logging.error( "Failed to list Peloton workout import directory {dir}".format(dir = filepath) )
        else:
            logging.info( "Found {count} Peloton workouts in import directory {dir}".format(count = len(import_files), dir = filepath) )

        return import_files

    @staticmethod
    def importPelotonWorkoutFile( filepath ) :
        logging.debug( "Importing Peloton workout file " + filepath )

        workout = {}
        workoutSamples = {}
        workoutSummary = {}
        try:
            with open( filepath, "r" ) as file:
                combined_workout = json.load( file )
                workout = combined_workout["workout"]
                workoutSamples = combined_workout["workoutSamples"]
                workoutSummary = combined_workout["workoutSummary"]
        except:
            logging.error( "Failed to import Peloton workout " + filepath )
        else:
            logging.info( "Imported Peloton workout " + filepath )

        return workout, workoutSamples, workoutSummary


    @staticmethod
    def run(config):
        numActivities = config.numActivities
        logger = config.logger
        pelotonClient = pelotonApi.PelotonApi(config.peloton_email, config.peloton_password)
        database = TinyDB('database.json')
        garminUploadHistoryTable = database.table('garminUploadHistory')

        if config.uploadToGarmin:
            garminUploader = garminClient.GarminClient(config.garmin_email, config.garmin_password)

        '''
        Split the main work loop into a two-step approach:
        1. Download the requested number of workouts from Peloton API and saving them to the working directory.
        2. Process all the files in the working directory.  This converts the file to the .TCX format and 
            optionally uploads them to Garmin, as before.
        This enables easier dev/test of the steps independently.  Specifically, by skipping the first step,
        previously saved workout files can be processed without re-downloading them from Peloton.
        '''
        if config.skip_download :
            logger.info( "Skipping download of workouts from Peloton!" )
            workouts = []
        else:
            if numActivities is None:
                numActivities = input("How many past activities do you want to grab?  ")
            logger.info("Get latest " + str(numActivities) + " workouts.")
            workouts = pelotonClient.getXWorkouts(numActivities)

        #
        #   Step 1:  Download requested number of workouts to the working_dir directory
        #
        for w in workouts:
            workoutId = w["id"]
            logger.info("Get workout: {}".format(str(workoutId)))

            if w["status"] != "COMPLETE":
                logger.info("Workout status: {} - skipping".format(w["status"]))
                continue

            workout = pelotonClient.getWorkoutById(workoutId)

            # Print basic summary about the workout here, because if we filter out the activity type
            # we won't otherwise see the activity.
            descriptor = tcx_builder.GetWorkoutSummary( workout )
            logger.info( "Downloading {workoutId} : {title} ({type}) at {timestamp}".format(workoutId = workoutId, title = descriptor['workout_title'], type = descriptor['workout_type'], timestamp = descriptor['workout_started'] ) )

            # Skip the unwanted activities before downloading the rest of the data.
            if config.pelotonWorkoutTypes and not descriptor["workout_type"] in config.pelotonWorkoutTypes :
                logger.info( "Workout type: {type} - skipping".format(type = descriptor['workout_type']) )
                continue

            logger.info("Get workout samples")
            workoutSamples = pelotonClient.getWorkoutSamplesById(workoutId)

            logger.info("Get workout summary")
            workoutSummary = pelotonClient.getWorkoutSummaryById(workoutId)

            try:
                filename = tcx_builder.getWorkoutFilename( workout, "json" )
                PelotonToGarmin.exportPelotonWorkoutFile( config.working_dir, filename, workout, workoutSamples, workoutSummary )
            except Exception as e:
                logger.error("Failed to save workout {id} to working directory {dir} - Exception: {ex}".format(id = workoutId, ex = e, dir = config.working_dir))


        #
        #   Step 2:  Process all files found under working_dir directory
        #
        workoutFiles = PelotonToGarmin.getPelotonWorkoutFiles( config.working_dir )
        logger.info("Begin processing {count} workout files.".format(count = len(workoutFiles)))
        for workoutFile in workoutFiles :
            workout, workoutSamples, workoutSummary = PelotonToGarmin.importPelotonWorkoutFile( workoutFile )
            if not workout:
                continue
            workoutId = workout["id"]

            # Print basic summary about the workout here, because if we filter out the activity type
            # we won't otherwise see the activity.
            descriptor = tcx_builder.GetWorkoutSummary( workout )
            workoutType = descriptor["workout_type"]
            logger.info( "Processing {workoutId} : {title} ({type}) at {timestamp}".format(workoutId = workoutId, title = descriptor['workout_title'], type = descriptor['workout_type'], timestamp = descriptor['workout_started'] ) )

            if config.pelotonWorkoutTypes and not workoutType in config.pelotonWorkoutTypes :
                logger.info( "Workout type: {type} - skipping".format(type = workoutType) )
                continue

            try:
                title, filename, garmin_activity_type = tcx_builder.workoutSamplesToTCX(workout, workoutSummary, workoutSamples, config.output_directory)
                logger.info("Fixing File: " + filename)
                
                with open("device.info","r") as file:
                    deviceinfo = file.read()

                with open("author.info","r") as file:
                    authorinfo = file.read()

                with open(config.output_directory+"/"+filename,"r") as file:
                    filedata = file.read()

                filedata = filedata.replace(".0</","</")
                filedata = filedata.replace("</Activity>", deviceinfo + "</Activity>")
                filedata = filedata.replace("</TrainingCenterDatabase>",authorinfo + "</TrainingCenterDatabase>")
                filedata = filedata.replace("activityExtensions","ns3")
                filedata = re.sub(r"[.]\d+</Watts>","</Watts>", filedata)
                with open(config.output_directory + "/" +filename,"w") as file:
                    file.write(filedata)

                logger.info("Wrote TCX file: " + filename)
            except Exception as e:
                logger.error("Failed to write TCX file for workout {} - Exception: {}".format(workoutId, e))

            # After we are done with the file, optionally copy the file to the archive directory, and/or retain
            # a copy of the file in the working directory.  Otherwise, the working file is deleted (default behavior).
            if config.archive_files :
                archive_dir = config.archive_dir
                if config.archive_by_type :
                    archive_dir += "/" + workoutType
                if not os.path.exists(archive_dir) :
                    os.makedirs(archive_dir)
                shutil.copy( workoutFile, archive_dir )
            if not config.retain_files :
                os.remove( workoutFile )

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
