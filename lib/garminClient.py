import logging
import time
from datetime import datetime
from garmin_uploader.workflow import Workflow
from garmin_uploader.workflow import User
from garmin_uploader.workflow import Activity

##############################
# Logging Setup
##############################

class GarminClient:

    """Main Garmin Api Class"""
    def __init__(self, user_email, user_password):
        self.logger = logging.getLogger('peloton-to-garmin.garminClient')

        assert user_email is not None and user_email != "", "Please specify your Garmin login email."
        assert user_password is not None and user_password != "", "Please specify your Garmin login password."

        self.user = User(user_email, user_password)
        self.activities = {}
        self.last_request = 0.0

    def addActivity(self, path, activityType=None, activityName=None, activityId=None):
        index = activityId
        if index is None:
            index = len(self.activities) + 1

        self.activities[index] = Activity(path, activityName, activityType)

    def uploadToGarmin(self, uploadHistoryTable=None):
        assert self.user.authenticate(), "Failed to authenticate garmin user."

        for activityId in self.activities:
            try:
                self.rate_limit()
                activityName = self.activities[activityId].name

                self.logger.info("Uploading activity: {}".format(self.activities[activityId].path))
                self.activities[activityId].upload(self.user)                

                if uploadHistoryTable is not None:
                    uploadHistoryTable.insert({'workoutId': activityId, 'title': activityName, 'uploadDt': datetime.now().strftime("%Y-%m-%d %H:%M:%S")})
                
                self.logger.info("Uploaded activity: {}".format(activityName))
            except Exception as e:
                self.logger.error("Failed to upload activity: {} to Garmin Connect with error {}".format(self.activities[activityId].path, e))
    
    def rate_limit(self):
        min_period = 1
        if not self.last_request:
            self.last_request = 0.0

        wait_time = max(0, min_period - (time.time() - self.last_request))
        if wait_time <= 0:
            return
        time.sleep(wait_time)

        self.last_request = time.time()
        self.logger.debug("Rate limiting for %f" % wait_time)