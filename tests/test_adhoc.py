import json
import os
import argparse
import importlib
from lib import configuration
from lib import garminClient

class TestAdHoc:

    @classmethod
    def setup_class(cls):
        return
    
    def test_adhoc_garmin_upload(self):
        filename = "1606519184-30 min Power Zone Ride with Denis Morton-86e1f383b2164f3392ac4ec6f10d7a56.tcx"
        workout_name = "30 min Power Zone Ride with Denis Morton"
        workout_type = "indoor_cycling"
        garmin_email = ""
        garmin_password = ""
        garminClient.uploadToGarmin([os.path.join(os.getcwd(), "data", filename)], garmin_email, garmin_password, workout_type, workout_name)