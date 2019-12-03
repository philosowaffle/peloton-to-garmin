#
# Author: Bailey Belvis (https://github.com/philosowaffle)
#
# Tool to generate a valid TCX file from a Peloton activity for Garmin.
#
import os
import sys
import json
import logging

from lib import pelotonApi
from lib import config_helper as config
from lib import tcx_builder

##############################
# Logging Setup
##############################
if len(sys.argv) > 3:
    file_handler = logging.FileHandler(sys.argv[3])
else:
    if config.ConfigSectionMap("LOGGER")['logfile'] is None:
        logger.error("Please specify a path for the logfile.")
        sys.exit(1)
    file_handler = logging.FileHandler(config.ConfigSectionMap("LOGGER")['logfile'])

logger = logging.getLogger('peloton-to-garmin')
# logger.setLevel(logging.DEBUG)
logger.setLevel(logging.INFO)

# Formatter
formatter = logging.Formatter('%(asctime)s - %(levelname)s - %(name)s: %(message)s')

# File Handler
file_handler.setLevel(logging.DEBUG)
file_handler.setFormatter(formatter)

# Console Handler
console_handler = logging.StreamHandler()
console_handler.setLevel(logging.INFO)
console_handler.setFormatter(formatter)

logger.addHandler(file_handler)
logger.addHandler(console_handler)

logger.debug("Peloton to Garmin Magic :)")

##############################
# Peloton Setup
##############################

if len(sys.argv) > 2:
    user_email = sys.argv[1]
    user_password = sys.argv[2]
else :
    if config.ConfigSectionMap("PELOTON")['email'] is None:
        logger.error("Please specify your Peloton login email in the config.ini file.")
        sys.exit(1)

    if config.ConfigSectionMap("PELOTON")['password'] is None:
        logger.error("Please specify your Peloton login password in the config.ini file.")
        sys.exit(1)

    user_email = config.ConfigSectionMap("PELOTON")['email']
    user_password = config.ConfigSectionMap("PELOTON")['password']
    output_directory = config.ConfigSectionMap("OUTPUT")['directory']

api = pelotonApi.PelotonApi(user_email, user_password)

##############################
# Main
##############################

numActivities = 5

logger.info("Get latest " + str(numActivities) + " workouts.")
workouts = api.getXWorkouts(numActivities)

for w in workouts:
    workoutId = w["id"]
    logger.info("Get workout: " + str(workoutId))

    workout = api.getWorkoutById(workoutId)

    logger.info("Get workout samples")
    workoutSamples = api.getWorkoutSamplesById(workoutId)

    logger.info("Get workout summary")
    workoutSummary = api.getWorkoutSummaryById(workoutId)

    logger.info("Writing TCX file")
    try:
        tcx_builder.workoutSamplesToTCX(workout, workoutSummary, workoutSamples, output_directory)
    except Exception as e:
        logger.error("Failed to write TCX file for workout {} - Exception: {}".format(workoutId, e))
    

logger.info("Done!")
logger.info("Your Garmin TCX files can be found in the Output directory: " + output_directory)

