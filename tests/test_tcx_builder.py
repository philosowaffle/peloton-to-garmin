import json
import os
from lib import tcx_builder

class TestTcxBuilder:
    
    def loadTestData(self, filename):
        with open(os.path.join(os.getcwd(), "data", filename)) as json_file:
            return json.load(json_file)

    def test_cycling_smoketest(self):
        # Setup
        workout_data = self.loadTestData("peloton_workout_cycling.json")
        workout_summary = self.loadTestData("peloton_workoutsummary_cycling.json")
        workout_samples = self.loadTestData("peloton_workoutsamples_cycling.json")
        output_directory = os.path.join(os.getcwd(),"testOutput")

        # Act
        title, filename, garmin_activity_type = tcx_builder.workoutSamplesToTCX(workout_data, workout_summary, workout_samples, output_directory)
        
        # Assert
        assert title == "20 min HIIT Ride with Denis Morton"
        assert filename == "1586208689-20 min HIIT Ride with Denis Morton-6c4d525d20134c74b7395991ed6912ce.tcx"
        assert garmin_activity_type == "Biking"

    def test_strength_smoketest(self):
        # Setup
        workout_data = self.loadTestData("peloton_workout_strength.json")
        workout_summary = self.loadTestData("peloton_workoutsummary_strength.json")
        workout_samples = self.loadTestData("peloton_workoutsamples_strength.json")
        output_directory = os.path.join(os.getcwd(),"testOutput")

        # Act
        title, filename, garmin_activity_type = tcx_builder.workoutSamplesToTCX(workout_data, workout_summary, workout_samples, output_directory)
        
        # Assert
        assert title == "10 min Bodyweight Strength with Becs Gentry"
        assert filename == "1589907174-10 min Bodyweight Strength with Becs Gentry-38583cbc0e434e54a2eb91be1a770e01.tcx"
        assert garmin_activity_type == "Other"