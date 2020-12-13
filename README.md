# peloton-to-garmin 

#### _#PelotonToGarmin_

Convert workout data from Peloton into a TCX file that can be uploaded to Garmin.

* Fetch latest workouts from Peloton
* Convert Peloton workout to TCX file
* Upload TCX workout to Garmin
* Maintain Upload History to avoid duplicates in Garmin

## Table of Contents

1. [Windows Usage](#windows-setup)
1. [Linux/MacOs Usage](#linuxmacos)
1. [Docker](#docker)
1. [Command Line Arguments](#command-line-arguments)
1. [Database](#database)
1. [Use At Own Risk](#warnings)

<a href="https://www.buymeacoffee.com/philosowaffle" target="_blank"><img src="https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png" alt="Buy Me A Coffee" style="height: 41px !important;width: 174px !important;box-shadow: 0px 3px 2px 0px rgba(190, 190, 190, 0.5) !important;-webkit-box-shadow: 0px 3px 2px 0px rgba(190, 190, 190, 0.5) !important;" ></a>

## Windows Setup

### Quick Start

1. Find the latest release [here](https://github.com/philosowaffle/peloton-to-garmin/releases)
1. Download the file `peloton-to-garmin-windows.zip`
1. Unzip the folder
1. Find the file named `config.ini`, open it with your text editor of choice and modify the Peloton/Garmin settings
1. Save and close the file
1. Find the file named `peloton-to-garmin.exe`, double click to launch the program
1. You will be prompted to enter how many workouts you would like to fetch
1. A TCX file for each workout will be created in the `Output` directory
1. The resulting TCX file can then be uploaded to Garmin manually, or you can configure the `config.ini` settings to upload automagically for you

### Advanced Setup

1. Download the repo [here](https://github.com/philosowaffle/peloton-to-garmin/archive/master.zip)
1. Extract the zip file
1. Install the latest version of [Python 3](https://www.python.org/downloads/)
1. Open `File Explorer` and navigate to the wherever you unzipped the downloaded project
1. Navigate so that you are inside the `peloton-to-garmin` folder
1. Open a command prompt by clicking in the `Location Bar` at the top and typing `cmd` then hit enter
1. From the command prompt run the following command:
    1. `pip install -r requirements.txt`
1. Close the command prompt and return to the `peloton-to-garmin` folder
1. Edit the `config.ini` file and set your Peloton Email and Password, Save and Close
    1. Optionally set your Garmin Email and Password if you wish for activities to be uploaded automatically

### Advanced Usage

* Open a command prompt inside of the `peloton-to-garmin` folder
* Run the following command:
    * `python peloton-to-garmin`
* You will be prompted to enter how many workouts you would like to fetch
* A TCX file for each workout will be created in the `output` directory
* The resulting TCX file can then be uploaded to Garmin

## Linux/MacOs

1. `wget https://github.com/philosowaffle/peloton-to-garmin/archive/master.zip`
1. `unzip master.zip`
1. Install [Python 3](https://www.python.org/downloads/)
1. Navigate so that you are inside the `peloton-to-garmin` folder
1. From the command prompt run the following command:
    1. `pip install -r requirements.txt`
    1. In ubuntu 20.04, if you use the python3 in the repo, the command is `pip3 install -r requirements.txt`
1. `vim config.ini` (or nano or whatever. Just not emacs, please :P)
    1. set your Peloton Email and Password, Save and Close

### Usage

* Open a command prompt inside of the `peloton-to-garmin` folder
* Run the following command:
    * `python3 peloton-to-garmin.py`
* You will be prompted to enter how many workouts you would like to fetch
* A TCX file for each workout will be created in the `output` directory
* The resulting TCX file can then be uploaded to Garmin

## Docker

This repository does not directly maintain support for running the code in a docker container, but @Octopusprime83 has created and published a [container that can be pulled from docker hub](https://hub.docker.com/r/philo138/peloton-to-garmin).  Note that some of the behavior in the container may differ from the latest code published on github.

* `docker pull philo138/peloton-to-garmin`

## Command Line Arguments

Usage:  
peloton-to-garmin.py [-h] [-email EMAIL] [-password PASSWORD] [-path OUTPUT_DIR] [-num NUM_TO_DOWNLOAD] [-log LOG_FILE]

optional arguments:

  * -h, --help            show this help message and exit  
  * -email EMAIL          Peloton email address  
  * -password PASSWORD    Peloton password  
  * -path OUTPUT_DIR      Path to output directory  
  * -num NUM_TO_DOWNLOAD  Number of activities to download  
  * -log LOG_FILE         Log file name## Runnning in docker
  * -loglevel LOGLEVEL    DEBUG, INFO, ERROR  
  * -garmin_email         Garmin email address for upload to Garmin
  * -garmin_password      Garmin password for upload to Garmin
  
  Examples:

  * To get the last 10 activities:  
        * `peloton-to-garmin.py -num 10`  
  * To pass your email and passowrd:  
        * `peloton-to-garmin.py -email you@email.com -password mypassword`  
  
  Note: Command line arguments take precedence over values in the configuration file. 

## Supported Python/OS

The matrix of supported Python versions and OS's can be found [here](https://github.com/philosowaffle/peloton-to-garmin/blob/master/.github/workflows/pr-test.yml#L17).

## Database

Various config and upload history is maintained in a local `database.json` file. Deleting this file will delete any upload history and the servic will attempt to upload all workouts to Garmin Connect again.

## Contributors

Special thanks to all the [contributors](https://github.com/philosowaffle/peloton-to-garmin/graphs/contributors) who have helped improve this project!

Garmin Upload feature is provided by the library: https://github.com/La0/garmin-uploader

## Warnings

⚠️ WARNING!!! Your username and password for Peloton and Garmin Connect are stored in clear text, WHICH IS NOT SECURE. If you have concerns about storing your credentials in an unsecure file, do not use this option.
