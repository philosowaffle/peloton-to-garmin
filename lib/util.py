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

# Formatter
formatter = logging.Formatter('%(asctime)s - %(levelname)s - %(name)s: %(message)s')

# File Handler
file_handler = logging.FileHandler(config.ConfigSectionMap("LOGGER")['logfile'])
file_handler.setLevel(logging.DEBUG)
file_handler.setFormatter(formatter)

logger.addHandler(file_handler)

##############################
# Main
##############################
def parse_response(response):
    """Parse JSON API response, return object."""
    logger.debug("parse_response - input: {}".format(response.text))
    parsed_response = json.loads(response.text)
    logger.debug("parse_response - parsed: {}".format(parsed_response))
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
    response = session.get(url, json=payload, cookies=cookieDict)
    parsed_response = parse_response(response)
    handle_error(response)
    
    return parsed_response

def logError(response):
    request = response.request
    url = request.url
    headers = request.headers
    logger.error("handle_error - Url: {}".format(url))
    logger.error("handle_error - Headers: {}".format(headers))