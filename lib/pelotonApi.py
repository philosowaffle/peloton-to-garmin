import requests, json
import logging
from . import util

class PelotonApi:
    """Main Peloton Api Class"""
    def __init__(self, user_email, user_password):
        self.logger = logging.getLogger('peloton-to-garmin.PelotonApi')

        self.http_base = "https://api.pelotoncycle.com/api/"
        self.session = requests.Session()
        
        auth_endpoint = "https://api.pelotoncycle.com/auth/login"
        payload = {
            'username_or_email': user_email,
            'password': user_password
        }

        response = self.session.post(auth_endpoint, json=payload, verify=False)
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
        query = "user/" + self.user_id + "/workouts?joins=peloton.ride&limit="+ str(numWorkouts) +"&page=0&sort_by=-created"
        url = util.full_url(self.http_base, query)

        workouts = util.getResponse(self.session, url, {}, self.getAuthCookie())

        return workouts["data"]
    
    def getLatestWorkout(self):
        """
            Gets the latest workout from Peloton.
        """
        query = "user/" + self.user_id + "/workouts?joins=peloton.ride&limit=1&page=0&sort_by=-created"
        url = util.full_url(self.http_base, query)

        workouts = util.getResponse(self.session, url, {}, self.getAuthCookie())

        return workouts["data"][0]
    
    def getWorkoutById(self, workoutId):
        """
            Gets workout from Peloton by id.
        """

        query = "workout/" + workoutId + "?joins=peloton,peloton.ride,peloton.ride.instructor,user"
        url = util.full_url(self.http_base, query)

        return util.getResponse(self.session, url, {}, self.getAuthCookie())

    def getWorkoutSamplesById(self, workoutId):
        """
            Gets workout samples from Peloton by id.
        """

        query = "workout/" + workoutId + "/performance_graph?every_n=1"
        url = util.full_url(self.http_base, query)

        return util.getResponse(self.session, url, {}, self.getAuthCookie())
    
    def getWorkoutSummaryById(self, workoutId):
        """
            Gets workout summary from Peloton by id.
        """

        query = "workout/" + workoutId + "/summary"
        url = util.full_url(self.http_base, query)

        return util.getResponse(self.session, url, {}, self.getAuthCookie())


    