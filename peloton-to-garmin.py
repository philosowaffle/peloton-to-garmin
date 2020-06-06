#
# Author: Bailey Belvis (https://github.com/philosowaffle)
#
# Tool to generate a valid TCX file from a Peloton activity for Garmin.
#
import os
import sys
import json
import logging
import argparse

from lib import pelotonApi
from lib import config_helper as config
from lib import tcx_builder

##############################
# Debugging Setup
##############################
if config.ConfigSectionMap("DEBUG")['pauseonfinish'] is None:
    pause_on_finish = "false"
else:
    pause_on_finish = config.ConfigSectionMap("DEBUG")['pauseonfinish']

##############################
# Command Line Argument Setup 
##############################
argParser = argparse.ArgumentParser()

argParser.add_argument("-email",help="Peloton email address",dest="email",type=str)
argParser.add_argument("-password",help="Peloton password",dest="password",type=str)
argParser.add_argument("-path",help="Path to output directory",dest="output_dir",type=str)
argParser.add_argument("-num",help="Number of activities to download",dest="num_to_download",type=int)
argParser.add_argument("-log",help="Log file name",dest="log_file",type=int)

argResults = argParser.parse_args()

##############################
# Logging Setup
##############################
if argResults.log_file is not None:
    file_handler = logging.FileHandler(argResults.log_file)
elif config.ConfigSectionMap("LOGGER")['logfile'] is not None:
    file_handler = logging.FileHandler(config.ConfigSectionMap("LOGGER")['logfile'])
else:
    logger.error("Please specify a path for the logfile.")
    sys.exit(1)
    
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
# Variables Setup
##############################
user_email = None
user_password = None
output_directory = None
numActivities = None

if argResults.num_to_download is not None:
    numActivities = argResults.num_to_download
elif os.getenv("NUM_ACTIVITIES") is not None:
    numActivities = os.getenv("NUM_ACTIVITIES")

if argResults.output_dir is not None:
    output_directory = argResults.output_dir
elif os.getenv("OUTPUT_DIRECTORY") is not None:
    output_directory = os.getenv("OUTPUT_DIRECTORY")
else:
    output_directory = config.ConfigSectionMap("OUTPUT")['directory']

##############################
# Peloton Setup
##############################
if argResults.email is not None:
    user_email = argResults.email
elif config.ConfigSectionMap("PELOTON")['email'] is not None:
    user_email = config.ConfigSectionMap("PELOTON")['email']
else:
    logger.error("Please specify your Peloton login email in the config.ini file.")
    sys.exit(1)

if argResults.password is not None:
    user_password = argResults.password
elif config.ConfigSectionMap("PELOTON")['password'] is not None:
    user_password = config.ConfigSectionMap("PELOTON")['password']
else:
    logger.error("Please specify your Peloton login password in the config.ini file.")
    sys.exit(1)

api = pelotonApi.PelotonApi(user_email, user_password)

##############################
# Main
##############################
if numActivities is None:
    numActivities = input("How many past activities do you want to grab?  ")

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

if pause_on_finish == "true":
    input("Press the <ENTER> key to continue...")
