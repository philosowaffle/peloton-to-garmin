import requests, json
import logging
import sys
from . import util

class PelotonApi:
    """Main Peloton Api Class"""
    def __init__(self, user_email, user_password):
        self.logger = logging.getLogger('peloton-to-garmin.PelotonApi')

        assert user_email is not None and user_email != "", "Please specify your Peloton login email."
        assert user_password is not None and user_password != "", "Please specify your Peloton login password."

        self.http_base = "https://api.onepeloton.com/api/"
        self.session = requests.Session()
        
        auth_endpoint = "https://api.onepeloton.com/auth/login"
        payload = {
            'username_or_email': user_email,
            'password': user_password
        }
        
        headers = {
            'peloton-platform': 'web'
        }

        response = self.session.post(auth_endpoint, json=payload, verify=True, headers=headers)
        parsed_response = util.parse_response(response)
        util.handle_error(response)

        self.user_id = parsed_response['user_id']
        self.session_id = parsed_response['session_id']

    def getAuthCookie(self):
        cookies = dict(peloton_session_id=self.session_id)
        return cookies

    def getXWorkouts(self, numWorkouts):
        """
            Gets the latest x workouts from Peloton.
        """
        query = "user/" + self.user_id + "/workouts?joins=ride&limit="+ str(numWorkouts) +"&page=0&sort_by=-created"
        url = util.full_url(self.http_base, query)

        workouts = util.getResponse(self.session, url, {}, self.getAuthCookie())
        data = workouts["data"]

        self.logger.debug("getXWorkouts: {}".format(data))

        return data
    
    def getLatestWorkout(self):
        """
            Gets the latest workout from Peloton.
        """
        query = "user/" + self.user_id + "/workouts?joins=ride&limit=1&page=0&sort_by=-created"
        url = util.full_url(self.http_base, query)

        workouts = util.getResponse(self.session, url, {}, self.getAuthCookie())
        data = workouts["data"][0]

        self.logger.debug("getLatestWorkout: {}".format(data))

        return data
    
    def getWorkoutById(self, workoutId):
        """
            Gets workout from Peloton by id.
        """

        query = "workout/" + workoutId + "?joins=ride,ride.instructor,user"
        url = util.full_url(self.http_base, query)
        data = util.getResponse(self.session, url, {}, self.getAuthCookie())

        self.logger.debug("getWorkoutById: {}".format(data))

        return data

    def getWorkoutSamplesById(self, workoutId):
        """
            Gets workout samples from Peloton by id.
        """

        query = "workout/" + workoutId + "/performance_graph?every_n=1"
        url = util.full_url(self.http_base, query)
        data = util.getResponse(self.session, url, {}, self.getAuthCookie())

        self.logger.debug("getWorkoutSamplesById: {}".format(data))

        return data
    
    def getWorkoutSummaryById(self, workoutId):
        """
            Gets workout summary from Peloton by id.
        """

        query = "workout/" + workoutId + "/summary"
        url = util.full_url(self.http_base, query)
        data = util.getResponse(self.session, url, {}, self.getAuthCookie())

        self.logger.debug("getWorkoutSummaryById: {}".format(data))

        return data


    