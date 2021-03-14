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

    polling_enabled = False
    polling_interval_seconds = 600

    def __init__(self, args):
        self.logger = logging.getLogger('peloton-to-garmin.ArgParser')

        args.add_argument("-email",help="Peloton email address",dest="email",type=str, default=os.environ.get('P2G_PELOTON_EMAIL'))
        args.add_argument("-password",help="Peloton password",dest="password",type=str, default=os.environ.get('P2G_PELOTON_PASS'))
        args.add_argument("-garmin_email",help="Garmin email address for upload to Garmin",dest="garmin_email",type=str, default=os.environ.get('P2G_GARMIN_EMAIL'))
        args.add_argument("-garmin_password",help="Garmin password for upload to Garmin",dest="garmin_password",type=str, default=os.environ.get('P2G_GARMIN_PASS'))
        args.add_argument("-garmin_enable_upload",help="True will try to upload activities to Garmin", dest="garmin_enable_upload", type=str, default=os.environ.get('P2G_GARMIN_ENABLE_UPLOAD'))
        args.add_argument("-path",help="Path to output directory",dest="output_dir",type=str, default=os.environ.get('P2G_PATH'))
        args.add_argument("-num",help="Number of activities to download",dest="num_to_download",type=int, default=os.environ.get('P2G_NUM'))
        args.add_argument("-workout_types",help="Process specified activity types only", dest="workout_types", type=str, default=os.environ.get('P2G_WORKOUT_TYPES'))
        args.add_argument("-log",help="Log file name",dest="log_file",type=str, default=os.environ.get('P2G_LOG'))
        args.add_argument("-loglevel",help="[DEBUG, INFO, ERROR]",dest="log_level",type=str, default=os.environ.get('P2G_LOG_LEVEL'))
        args.add_argument("-pause_on_finish",help="Do not automatically close the application on completion.", dest="pause_on_finish",type=str, default=os.environ.get('P2G_PAUSE_ON_FINISH'))
        args.add_argument("-enable_polling", help="True will automatically and periodically check for new activities.",dest="polling_enabled",type=str,default=os.environ.get('PTG_ENABLE_POLLING'))
        args.add_argument("-polling_interval_seconds",help="How frequently to poll for new activities if polling is enabled.",dest="polling_interval_seconds",type=int,default=os.environ.get('PTG_POLLING_INTERVAL_SECONDS'))

        args.add_argument("-working", help="Directory for to-be-processed Peloton files", dest="working_dir", type=str, default=os.environ.get('P2G_WORKING_DIR'))
        args.add_argument("-archive", help="Directory for archive post-processed Peloton files", dest="archive_dir", type=str, default=os.environ.get('P2G_ARCHIVE_DIR'))
        args.add_argument("-skip_download", help="True = Use existing files in working dir rather than downloading new ones from Peloton", dest="skip_download", type=str, default=os.environ.get('P2G_SKIP_DOWNLOAD'))
        args.add_argument("-retain_files", help="True = Retain Peloton files in working dir after processing", type=str, dest="retain_files", default=os.environ.get('P2G_RETAIN_FILES') )
        args.add_argument("-archive_files", help="True = Move Peloton files to archive dir after processing", type=str, dest="archive_files", default=os.environ.get('P2G_ARCHIVE_FILES') )
        args.add_argument("-archive_by_type", help="True = Archive to subdirs based on Peloton workout type", type=str, dest="archive_by_type", default=os.environ.get('P2G_ARCHIVE_BY_TYPE') )

        argResults = args.parse_args()

        self.loadLoggingConfig(argResults)
        self.loadDebugConfig(argResults)
        self.loadPelotonConfig(argResults)
        self.loadGarminConfig(argResults)
        self.loadPtoGConfig(argResults)
        
    def loadDebugConfig(self, argResults):
        if argResults.pause_on_finish is not None:
            self.pause_on_finish = argResults.pause_on_finish == "true"
        elif config.ConfigSectionMap("DEBUG").get('pauseonfinish', None) == "false":
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
        #self.logger.addHandler(console_handler)

        self.logger.debug("Peloton to Garmin Magic :)")

    def loadPelotonConfig(self, argResults):
        if argResults.num_to_download is not None:
            self.numActivities = argResults.num_to_download
        elif os.getenv("NUM_ACTIVITIES") is not None:
            self.numActivities = os.getenv("NUM_ACTIVITIES")
        elif config.ConfigSectionMap("PELOTON").get('numactivities') is not None:
            self.numActivities = int(config.ConfigSectionMap("PELOTON")['numactivities'])
        else:
            self.numActivities = None

        if argResults.output_dir is not None:
            self.output_directory = argResults.output_dir
        elif os.getenv("OUTPUT_DIRECTORY") is not None:
            self.output_directory = os.getenv("OUTPUT_DIRECTORY")
        elif config.ConfigSectionMap("OUTPUT").get('directory') is not None:
            self.output_directory = config.ConfigSectionMap("OUTPUT")['directory']
        else:
            self.output_directory = "output"

        if argResults.skip_download is not None:
            self.skip_download = argResults.skip_download == "true"
        elif config.ConfigSectionMap("OUTPUT").get('skipdownload') is not None:
            self.skip_download = config.ConfigSectionMap("OUTPUT")['skipdownload'] == "true"
        else:
            self.skip_download = False

        if argResults.working_dir is not None:
            self.working_dir = argResults.working_dir
        elif config.ConfigSectionMap("OUTPUT").get('workingdirectory') is not None:
            self.working_dir = config.ConfigSectionMap("OUTPUT")['workingdirectory']
        else:
            self.working_dir = "working"

        if argResults.retain_files is not None:
            self.retain_files = argResults.retain_files == "true"
        elif config.ConfigSectionMap("OUTPUT").get('retainfiles') is not None:
            self.retain_files = config.ConfigSectionMap("OUTPUT")['retainfiles'] == "true"
        else:
            self.retain_files = False

        # archive_dir is optional
        if argResults.archive_dir is not None:
            self.archive_dir = argResults.archive_dir
        elif config.ConfigSectionMap("OUTPUT").get('archivedirectory') is not None:
            self.archive_dir = config.ConfigSectionMap("OUTPUT")['archivedirectory']
        else:
            self.archive_dir = None

        if argResults.archive_files is not None:
            self.archive_files = argResults.archive_files == "true"
        elif config.ConfigSectionMap("OUTPUT").get('archivefiles') is not None:
            self.archive_files = config.ConfigSectionMap("OUTPUT")['archivefiles'] == "true"
        else:
            self.archive_files = False

        if argResults.archive_by_type is not None:
            self.archive_by_type = argResults.archive_by_type == "true"
        elif config.ConfigSectionMap("OUTPUT").get('archivebytype') is not None:
            self.archive_by_type = config.ConfigSectionMap("OUTPUT")['archivebytype'] == "true"
        else:
            self.archive_by_type = False


        # Create directory if it does not exist
        if not os.path.exists(self.output_directory):
            os.makedirs(self.output_directory)
        if not os.path.exists(self.working_dir):
            os.makedirs(self.working_dir)
        if self.archive_dir and self.archive_files and not os.path.exists(self.archive_dir):
            os.makedirs(self.archive_dir)

        if argResults.email is not None:
            self.peloton_email = argResults.email
        else:
            self.peloton_email = config.ConfigSectionMap("PELOTON").get('email')

        if argResults.password is not None:
            self.peloton_password = argResults.password
        else: 
            self.peloton_password = config.ConfigSectionMap("PELOTON").get('password')

        # If set, the opt-in list of activities to include.  Peloton activity types not included
        # in this list will be ignored (not saved, not uploaded).  Omit or leave blank to allow all.
        if argResults.workout_types is not None:
            pelotonWorkoutTypes = argResults.workout_types
        else:
            pelotonWorkoutTypes = config.ConfigSectionMap("PELOTON").get("workouttypes")
        if pelotonWorkoutTypes is not None :
            self.pelotonWorkoutTypes = [ item.strip().lower() for item in pelotonWorkoutTypes.split(",") ]
        else:
            self.pelotonWorkoutTypes = []

    def loadGarminConfig(self, argResults):
        if argResults.garmin_email is not None:
            self.garmin_email = argResults.garmin_email
        else:
            self.garmin_email = config.ConfigSectionMap("GARMIN").get('email')

        if argResults.garmin_password is not None:
            self.garmin_password = argResults.garmin_password
        else:
            self.garmin_password = config.ConfigSectionMap("GARMIN").get('password')

        if argResults.garmin_enable_upload is not None:
            self.uploadToGarmin = argResults.garmin_enable_upload == "true"
        elif config.ConfigSectionMap("GARMIN").get('uploadenabled') == "true":
            self.uploadToGarmin = True

    def loadPtoGConfig(self, argResults):
        if argResults.polling_enabled is not None:
            self.polling_enabled = argResults.polling_enabled == "true"
        elif config.ConfigSectionMap("PTOG").get('enablepolling') is not None:
            self.polling_enabled = config.ConfigSectionMap("PTOG").get('enablepolling') == "true"

        if argResults.polling_interval_seconds is not None:
            self.polling_interval_seconds = argResults.polling_interval_seconds
        elif config.ConfigSectionMap("PTOG").get('pollingintervalseconds') is not None:
            self.polling_interval_seconds = int(config.ConfigSectionMap("PTOG").get('pollingintervalseconds'))
