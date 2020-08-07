import json
import os
import tests.tcx_test_helper as TCX
import xml.etree.ElementTree as ET
from lib import tcx_builder

class TestTcxBuilder:
    
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

    def loadOutputTCX(self, filepath):
        tree = ET.parse(filepath)
        return tree.getroot()

    def test_cycling_smoketest(self):
        # Setup
        workout_data = self.loadTestData("peloton_workout_cycling.json")
        workout_summary = self.loadTestData("peloton_workoutsummary_cycling.json")
        workout_samples = self.loadTestData("peloton_workoutsamples_cycling.json")
        output_directory = self.getOutputDir()

        # Act
        title, filename, garmin_activity_type = tcx_builder.workoutSamplesToTCX(workout_data, workout_summary, workout_samples, output_directory)
        
        # Assert
        assert title == "20 min HIIT Ride with Denis Morton"
        assert filename == "1586208689-20 min HIIT Ride with Denis Morton-6c4d525d20134c74b7395991ed6912ce.tcx"
        assert garmin_activity_type == "indoor_cycling"
        assert os.path.exists(os.path.join(output_directory, filename))

        tcx = self.loadOutputTCX(os.path.join(output_directory, filename))
        TCX.assertTcxSportMatches("Biking", tcx)
        TCX.assertTcxIdMatches(workout_data, tcx)
        TCX.assertTcxLapStartTimeMatches(workout_data, tcx)
        TCX.assertTcxTotalTimeSecondsMatches(workout_data, tcx)
        TCX.assertTcxMaximumSpeedMatches(workout_summary, tcx)
        TCX.assertTcxCaloriesMatches(workout_summary, tcx)
        TCX.assertTcxAvgHeartRateMatches(workout_summary,tcx)
        TCX.assertTcxAvgSpeedMatches(workout_summary, tcx)
        TCX.assertTcxMaxBikeCadenceMatches(workout_summary, tcx)
        TCX.assertTcxAvgWattsMatches(workout_summary, tcx)
        TCX.assertTcxMaxWattsMatches(workout_summary, tcx)

    def test_cycling_freestyle_smoketest(self):
        # Setup
        workout_data = self.loadTestData("peloton_workout_cycling_freestyle.json")
        workout_summary = self.loadTestData("peloton_workoutsummary_cycling_freestyle.json")
        workout_samples = self.loadTestData("peloton_workoutsamples_cycling_freestyle.json")
        output_directory = self.getOutputDir()

        # Act
        title, filename, garmin_activity_type = tcx_builder.workoutSamplesToTCX(workout_data, workout_summary, workout_samples, output_directory)
        
        # Assert
        assert title == "27 sec Just Ride"
        assert filename == "1595374979-27 sec Just Ride-88963f6daf89445387da4ee2e26015f9.tcx"
        assert garmin_activity_type == "indoor_cycling"
        assert os.path.exists(os.path.join(output_directory, filename))

        tcx = self.loadOutputTCX(os.path.join(output_directory, filename))
        TCX.assertTcxSportMatches("Biking", tcx)
        TCX.assertTcxIdMatches(workout_data, tcx)
        TCX.assertTcxLapStartTimeMatches(workout_data, tcx)
        TCX.assertTcxTotalTimeSecondsMatches(workout_data, tcx)
        TCX.assertTcxMaximumSpeedMatches(workout_summary, tcx)
        TCX.assertTcxCaloriesMatches(workout_summary, tcx)
        TCX.assertTcxAvgHeartRateMatches(workout_summary,tcx)
        TCX.assertTcxAvgSpeedMatches(workout_summary, tcx)
        TCX.assertTcxMaxBikeCadenceMatches(workout_summary, tcx)
        TCX.assertTcxAvgWattsMatches(workout_summary, tcx)
        TCX.assertTcxMaxWattsMatches(workout_summary, tcx)


    def test_strength_smoketest(self):
        # Setup
        workout_data = self.loadTestData("peloton_workout_strength.json")
        workout_summary = self.loadTestData("peloton_workoutsummary_strength.json")
        workout_samples = self.loadTestData("peloton_workoutsamples_strength.json")
        output_directory = self.getOutputDir()

        # Act
        title, filename, garmin_activity_type = tcx_builder.workoutSamplesToTCX(workout_data, workout_summary, workout_samples, output_directory)
        
        # Assert
        assert title == "10 min Bodyweight Strength with Becs Gentry"
        assert filename == "1589907174-10 min Bodyweight Strength with Becs Gentry-38583cbc0e434e54a2eb91be1a770e01.tcx"
        assert garmin_activity_type == "strength_training"
        assert os.path.exists(os.path.join(output_directory, filename))

        tcx = self.loadOutputTCX(os.path.join(output_directory, filename))
        TCX.assertTcxSportMatches("Other", tcx)
        TCX.assertTcxIdMatches(workout_data, tcx)
        TCX.assertTcxLapStartTimeMatches(workout_data, tcx)
        TCX.assertTcxTotalTimeSecondsMatches(workout_data, tcx)
        TCX.assertTcxMaximumSpeedMatches(workout_summary, tcx)
        TCX.assertTcxCaloriesMatches(workout_summary, tcx)
        TCX.assertTcxAvgHeartRateMatches(workout_summary,tcx)
        TCX.assertTcxAvgSpeedMatches(workout_summary, tcx)
        TCX.assertTcxMaxBikeCadenceMatches(workout_summary, tcx)
        TCX.assertTcxAvgWattsMatches(workout_summary, tcx)
        TCX.assertTcxMaxWattsMatches(workout_summary, tcx)


    def test_running_smoketest(self):
        # Setup
        workout_data = self.loadTestData("peloton_workout_running.json")
        workout_summary = self.loadTestData("peloton_workoutsummary_running.json")
        workout_samples = self.loadTestData("peloton_workoutsamples_running.json")
        output_directory = self.getOutputDir()

        # Act
        title, filename, garmin_activity_type = tcx_builder.workoutSamplesToTCX(workout_data, workout_summary, workout_samples, output_directory)
        
        # Assert
        assert title == "20 min Pop Fun Run with Olivia Amato"
        assert filename == "1565299850-20 min Pop Fun Run with Olivia Amato-63eef26a23744af295f14006811b159b.tcx"
        assert garmin_activity_type == "treadmill_running"
        assert os.path.exists(os.path.join(output_directory, filename))

        tcx = self.loadOutputTCX(os.path.join(output_directory, filename))
        TCX.assertTcxSportMatches("Running", tcx)
        TCX.assertTcxIdMatches(workout_data, tcx)
        TCX.assertTcxLapStartTimeMatches(workout_data, tcx)
        TCX.assertTcxTotalTimeSecondsMatches(workout_data, tcx)
        TCX.assertTcxMaximumSpeedMatches(workout_summary, tcx)
        TCX.assertTcxCaloriesMatches(workout_summary, tcx)
        TCX.assertTcxAvgHeartRateMatches(workout_summary,tcx)
        TCX.assertTcxAvgSpeedMatches(workout_summary, tcx)
        TCX.assertTcxMaxBikeCadenceMatches(workout_summary, tcx)
        TCX.assertTcxAvgWattsMatches(workout_summary, tcx)
        TCX.assertTcxMaxWattsMatches(workout_summary, tcx)

    
    def test_running_outdoor_smoketest(self):
        # Setup
        workout_data = self.loadTestData("peloton_workout_running_outdoor.json")
        workout_summary = self.loadTestData("peloton_workoutsummary_running_outdoor.json")
        workout_samples = self.loadTestData("peloton_workoutsamples_running_outdoor.json")
        output_directory = self.getOutputDir()

        # Act
        title, filename, garmin_activity_type = tcx_builder.workoutSamplesToTCX(workout_data, workout_summary, workout_samples, output_directory)
        
        # Assert
        assert title == "20 min Walk + Run with Olivia Amato"
        assert filename == "1574980432-20 min Walk + Run with Olivia Amato-af25d72ea4c0400daeac2707fc30994f.tcx"
        assert garmin_activity_type == "walking"
        assert os.path.exists(os.path.join(output_directory, filename))

        tcx = self.loadOutputTCX(os.path.join(output_directory, filename))
        TCX.assertTcxSportMatches("Running", tcx)
        TCX.assertTcxIdMatches(workout_data, tcx)
        TCX.assertTcxLapStartTimeMatches(workout_data, tcx)
        TCX.assertTcxTotalTimeSecondsMatches(workout_data, tcx)
        TCX.assertTcxMaximumSpeedMatches(workout_summary, tcx)
        TCX.assertTcxCaloriesMatches(workout_summary, tcx)
        TCX.assertTcxAvgHeartRateMatches(workout_summary,tcx)
        TCX.assertTcxAvgSpeedMatches(workout_summary, tcx)
        TCX.assertTcxMaxBikeCadenceMatches(workout_summary, tcx)
        TCX.assertTcxAvgWattsMatches(workout_summary, tcx)
        TCX.assertTcxMaxWattsMatches(workout_summary, tcx)
