import logging
from garmin_uploader.workflow import Workflow

##############################
# Logging Setup
##############################

logger = logging.getLogger('peloton-to-garmin.garminClient')

def uploadToGarmin(paths, garminUsername, garminPassword, activityType, activityName):
    try:
        workflow = Workflow(paths, garminUsername, garminPassword, activityType, activityName)
        workflow.run()
    except Exception as e:
        logger.error("Failed to upload to Garmin Connect. - {}".format(e)) 
        return
