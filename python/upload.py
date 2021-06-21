import argparse
from lib import garminClient

if __name__ == "__main__":
    args = argparse.ArgumentParser()
    args.add_argument("-ge", "--garmin_email",help="Garmin email address for upload to Garmin",dest="garmin_email",type=str, required=True)
    args.add_argument("-gp", "--garmin_password",help="Garmin password for upload to Garmin",dest="garmin_password",type=str, required=True)
    args.add_argument("-f", "--files", help="Path to file to upload", dest="files", nargs="+", required=True)

    argResults = args.parse_args()

    garmin_email = argResults.garmin_email
    garmin_password = argResults.garmin_password
    files = argResults.files

    garminUploader = garminClient.GarminClient(garmin_email, garmin_password)

    for file in files:
        garminUploader.addActivity(file)

    garminUploader.uploadToGarmin()