import logging
import argparse
import sys
import os
from . import config_helper as config

class Configuration:
    """Argument and Config Parser Class"""

    logger = None
    pause_on_finish = True

    peloton_email = None
    peloton_password = None
    output_directory = None
    numActivities = None

    garmin_email = None
    garmin_password = None
    uploadToGarmin = False

    def __init__(self, args):
        self.logger = logging.getLogger('peloton-to-garmin.ArgParser')

        args.add_argument("-email",help="Peloton email address",dest="email",type=str)
        args.add_argument("-password",help="Peloton password",dest="password",type=str)
        args.add_argument("-garmin_email",help="Garmin email address for upload to Garmin",dest="garmin_email",type=str)
        args.add_argument("-garmin_password",help="Garmin password for upload to Garmin",dest="garmin_password",type=str)
        args.add_argument("-path",help="Path to output directory",dest="output_dir",type=str)
        args.add_argument("-num",help="Number of activities to download",dest="num_to_download",type=int)
        args.add_argument("-log",help="Log file name",dest="log_file",type=str)
        args.add_argument("-loglevel",help="[DEBUG, INFO, ERROR]",dest="log_level",type=str)

        argResults = args.parse_args()

        self.loadLoggingConfig(argResults)
        self.loadDebugConfig()
        self.loadPelotonConfig(argResults)
        self.loadGarminConfig(argResults)
        
    def loadDebugConfig(self):
        if config.ConfigSectionMap("DEBUG").get('pauseonfinish', None) == "false":
            self.pause_on_finish = False
        else:
            self.pause_on_finish = True

    def loadLoggingConfig(self, argResults):
        if argResults.log_file is not None:
            file_handler = logging.FileHandler(argResults.log_file)
        elif config.ConfigSectionMap("LOGGER").get('logfile') is not None:
            file_handler = logging.FileHandler(config.ConfigSectionMap("LOGGER")['logfile'])
        else:
            file_handler = logging.FileHandler('peloton-to-garmin.log')

        if argResults.log_level is not None:
            log_level = argResults.log_level
        elif config.ConfigSectionMap("LOGGER").get('loglevel') is not None:
            log_level = config.ConfigSectionMap("LOGGER")['loglevel']
        else:
            log_level = "INFO"
            
        if log_level == "ERROR":
            level = logging.ERROR
        elif log_level == "DEBUG":
            level = logging.DEBUG
        else:
            level = logging.INFO

        self.logger = logging.getLogger('peloton-to-garmin')
        self.logger.setLevel(level)

        # Formatter
        formatter = logging.Formatter('%(asctime)s - %(levelname)s - %(name)s: %(message)s')

        # File Handler
        file_handler.setLevel(logging.DEBUG)
        file_handler.setFormatter(formatter)

        # Console Handler
        console_handler = logging.StreamHandler()
        console_handler.setLevel(logging.INFO)
        console_handler.setFormatter(formatter)

        self.logger.addHandler(file_handler)
        self.logger.addHandler(console_handler)

        self.logger.debug("Peloton to Garmin Magic :)")

    def loadPelotonConfig(self, argResults):
        if argResults.num_to_download is not None:
            self.numActivities = argResults.num_to_download
        else:
            self.numActivities = os.getenv("NUM_ACTIVITIES")

        if argResults.output_dir is not None:
            self.output_directory = argResults.output_dir
        elif os.getenv("OUTPUT_DIRECTORY") is not None:
            self.output_directory = os.getenv("OUTPUT_DIRECTORY")
        elif config.ConfigSectionMap("OUTPUT").get('directory') is not None:
            self.output_directory = config.ConfigSectionMap("OUTPUT")['directory']
        else:
            self.output_directory = "output"

        # Create directory if it does not exist
        if not os.path.exists(self.output_directory):
            os.makedirs(self.output_directory)

        if argResults.email is not None:
            self.peloton_email = argResults.email
        else:
            self.peloton_email = config.ConfigSectionMap("PELOTON").get('email')

        if argResults.password is not None:
            self.peloton_password = argResults.password
        else: 
            self.peloton_password = config.ConfigSectionMap("PELOTON").get('password')

    def loadGarminConfig(self, argResults):
        if argResults.garmin_email is not None:
            self.garmin_email = argResults.garmin_email
        else:
            self.garmin_email = config.ConfigSectionMap("GARMIN").get('email')

        if argResults.garmin_password is not None:
            self.garmin_password = argResults.garmin_password
        else:
            self.garmin_password = config.ConfigSectionMap("GARMIN").get('password')

        if config.ConfigSectionMap("GARMIN").get('uploadenabled') == "true":
            self.uploadToGarmin = True