#
# Author: Bailey Belvis (https://github.com/philosowaffle)
#
import configparser
import logging

##############################
# Logging
##############################
logger = logging.getLogger('peloton-to-garmin.config_helper')


Config = configparser.ConfigParser()
Config.read('config.ini')

def ConfigSectionMap(section):
    dict1 = {}
    options = Config.options(section)
    for option in options:
        try:
            dict1[option] = Config.get(section, option, raw=True)
            if dict1[option] == -1:
                logger.debug("skip: %s" % option)
        except Exception as e:
            logger.error("exception on %s!" % option)
            logger.error("Exception: {}".format(e))
            dict1[option] = None
    return dict1