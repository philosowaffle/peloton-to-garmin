import json
import os
import importlib
import pytest
from lib import garminClient

class TestGarminClient:
    
    @classmethod
    def setup_class(cls):
        return

    client_throws_when_missing_required_fields_testdata = [
        (None, "pwd"),
        ("", "pwd"),
        ("email", None),
        ("email", ""),
        (None, None),
        ("", "")
    ]
    @pytest.mark.parametrize("user_email, user_password", client_throws_when_missing_required_fields_testdata)
    def test_client_throws_when_missing_required_fields(self, user_email, user_password):
        with pytest.raises(AssertionError) as err:
            garminUploader = garminClient.GarminClient(user_email, user_password)
        
        if user_email is None or user_email == "":
            assert "email" in str(err.value)
        else:
            assert "password" in str(err.value)
    
    def test_addActivity_adds_activity(self):
        # Setup
        garminUploader = garminClient.GarminClient("email", "pwd")

        # Act
        garminUploader.addActivity("some/path", "someType", "someName01", "someId01")
        garminUploader.addActivity("some/path", "someType", "someName02", "someId02")

        # Assert
        assert len(garminUploader.activities) == 2

        assert garminUploader.activities["someId01"].name == "someName01"
        assert garminUploader.activities["someId02"].name == "someName02"

    def test_uploadActivity_when_auth_fails_throws(self, mocker):
        # Setup
        garminUploader = garminClient.GarminClient("email", "pwd")

        # Act
        with pytest.raises(AssertionError) as err:
            garminUploader.uploadToGarmin(None)

        # Assert
        assert "Failed to authenticate garmin user." in str(err.value)