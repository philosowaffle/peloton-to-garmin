import json
import os
from lib import tcx_builder

class TestTcxBuilder:
    
    def test_cycling_smoketest(self):
        # Setup
        workout_data = ""
        with open(os.path.join(os.getcwd(),"tests","data","peloton_workout_cycling.json")) as json_file:
            workout_data = json.load(json_file)

        workout_summary = ""
        with open(os.path.join(os.getcwd(),"tests","data","peloton_workoutsummary_cycling.json")) as json_file:
            workout_summary = json.load(json_file)

        workout_samples = ""
        with open(os.path.join(os.getcwd(),"tests","data","peloton_workoutsamples_cycling.json")) as json_file:
            workout_samples = json.load(json_file)

        output_directory = os.path.join(os.getcwd(),"tests","testOutput")

        # Act
        title, filename, garmin_activity_type = tcx_builder.workoutSamplesToTCX(workout_data, workout_summary, workout_samples, output_directory)
        
        # Assert
        assert title == "20 min HIIT Ride with Denis Morton"
        assert filename == "1586208689-20 min HIIT Ride with Denis Morton-6c4d525d20134c74b7395991ed6912ce.tcx"
        assert garmin_activity_type == "Biking"

    def test_strength_smoketest(self):
        # Setup
        workout_data = ""
        with open(os.path.join(os.getcwd(),"tests","data","peloton_workout_strength.json")) as json_file:
            workout_data = json.load(json_file)

        workout_summary = ""
        with open(os.path.join(os.getcwd(),"tests","data","peloton_workoutsummary_strength.json")) as json_file:
            workout_summary = json.load(json_file)

        workout_samples = ""
        with open(os.path.join(os.getcwd(),"tests","data","peloton_workoutsamples_strength.json")) as json_file:
            workout_samples = json.load(json_file)

        output_directory = os.path.join(os.getcwd(),"tests","testOutput")

        # Act
        title, filename, garmin_activity_type = tcx_builder.workoutSamplesToTCX(workout_data, workout_summary, workout_samples, output_directory)
        
        # Assert
        assert title == "10 min Bodyweight Strength with Becs Gentry"
        assert filename == "1589907174-10 min Bodyweight Strength with Becs Gentry-38583cbc0e434e54a2eb91be1a770e01.tcx"
        assert garmin_activity_type == "Other"