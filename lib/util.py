#
# Author: Bailey Belvis (https://github.com/philosowaffle)
#
import json
from .constants import A_OK_HTTP_CODES, A_ERROR_HTTP_CODES
from . import config_helper as config
import logging
import urllib

##############################
# Logging Setup
##############################

logger = logging.getLogger('peloton-to-garmin.Util')

##############################
# Main
##############################
def parse_response(response):
    """Parse JSON API response, return object."""
    parsed_response = json.loads(response.text)
    logger.debug("parse_response: {}".format(parsed_response))
    return parsed_response

def handle_error(response):
    """Raise appropriate exceptions if necessary."""
    status_code = response.status_code

    if status_code not in A_OK_HTTP_CODES:
        logError(response)
        error_explanation = A_ERROR_HTTP_CODES.get(status_code)
        raise_error = "{}: {}".format(status_code, error_explanation)
        raise Exception(raise_error)
    else:
        return True

def full_url(base, suffix):
        return base + suffix

def getResponse(session, url, payload, cookieDict):
    try:
        response = session.get(url, json=payload, cookies=cookieDict)
        parsed_response = parse_response(response)
        handle_error(response)
        
        return parsed_response
    except Exception as e:
        logger.error("Exception: {}".format(e))
    

def logError(response):
    request = response.request
    url = request.url
    headers = request.headers
    logger.error("handle_error - Url: {}".format(url))
    logger.error("handle_error - Headers: {}".format(headers))