import argparse
from lib import garminClient

args = argparse.ArgumentParser()
args.add_argument("-ge", "--garmin_email",help="Garmin email address for upload to Garmin",dest="garmin_email",type=str, required=True)
args.add_argument("-gp", "--garmin_password",help="Garmin password for upload to Garmin",dest="garmin_password",type=str, required=True)
args.add_argument("-f", "--files", help="Path to file to upload", dest="files", nargs="+", required=True)

garmin_email = args.garmin_email
garmin_password = args.garmin_password
paths = args.paths

garminUploader = garminClient.GarminClient(garmin_email, garmin_password)

for path in paths:
    garminUploader.addActivity(path)

garminUploader.uploadToGarmin()