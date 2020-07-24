import json
import os
import argparse
import importlib
from lib import configuration
pelotonToGarmin = importlib.import_module("peloton-to-garmin")

class TestPelotonToGarmin:
    
    @classmethod
    def setup_class(cls):
        outputDir = cls.getOutputDir(cls)
        if not os.path.exists(outputDir):
            os.mkdir(outputDir)

    def loadTestData(self, filename):
        with open(os.path.join(os.getcwd(), "data", filename)) as json_file:
            return json.load(json_file)

    def getOutputDir(self):
        return os.path.join(os.getcwd(),"testOutput")

    def test_run_whenWorkoutInProgress_skips(self, mocker):
        # Setup
        workouts_data = self.loadTestData("peloton_workouts_01.json")
        workouts_data[0]["status"] = "IN PROGRESS"
        output_directory = self.getOutputDir()
        config = configuration.Configuration(argparse.ArgumentParser())

        pToG = pelotonToGarmin.PelotonToGarmin()
        pelotonApiMock = mocker.patch('peloton-to-garmin.pelotonApi.PelotonApi').return_value
        pelotonApiMock.getXWorkouts.return_value = workouts_data

        tcx_builderMock = mocker.patch('peloton-to-garmin.tcx_builder')
        garminClientMock = mocker.patch('peloton-to-garmin.garminClient')

        config.numActivities = 1

        # Act
        pToG.run(config)

        # Assert
        pelotonApiMock.getXWorkouts.assert_called_once_with(config.numActivities)
        pelotonApiMock.getWorkoutById.assert_not_called()
        pelotonApiMock.getWorkoutSamplesById.assert_not_called()
        pelotonApiMock.getWorkoutSummaryById.assert_not_called()
        tcx_builderMock.workoutSamplesToTCX.assert_not_called()
        garminClientMock.uploadToGarmin.assert_not_called()



