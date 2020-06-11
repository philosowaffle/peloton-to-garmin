# peloton-to-garmin

Convert workout data from Peloton into a TCX file that can be uploaded to Garmin

## Windows Setup

1. Download the repo [here](https://github.com/philosowaffle/peloton-to-garmin/archive/master.zip)
1. Extract the zip file
1. Install the latest version of [Python 3](https://www.python.org/downloads/), currently 3.6.4
1. Open `File Explorer` and navigate to the wherever you unzipped the downloaded project
1. Navigate so that you are inside the `peloton-to-garmin` folder
1. Open a command prompt by clicking in the `Location Bar` at the top and typing `cmd` then hit enter
1. From the command prompt run the following command:
    1. `pip install -r requirements.txt`
1. Close the command prompt and return to the `peloton-to-garmin` folder
1. Edit the `config.ini` file and set your Peloton Email and Password, Save and Close
    1. Optionally set your Garmin Email and Password if you wish for activities to be uploaded automatically

## Windows Usage

* Open a command prompt inside of the `peloton-to-garmin` folder
* Run the following command:
    * `python peloton-to-garmin`
* You will be prompted to enter how many workouts you would like to fetch
* A TCX file for each workout will be created in the `output` directory
* The resulting TCX file can then be uploaded to Garmin

## Linux Setup

1. `wget https://github.com/philosowaffle/peloton-to-garmin/archive/master.zip`
1. `unzip master.zip`
1. Install [Python 3](https://www.python.org/downloads/) (Windows docs say it works in 3.6.4, Linux tested against 3.8.2)
1. Navigate so that you are inside the `peloton-to-garmin` folder
1. From the command prompt run the following command:
    1. `pip install -r requirements.txt`
    1. In ubuntu 20.04, if you use the python3 in the repo, the command is `pip3 install -r requirements.txt`
1. `vim config.ini` (or nano or whatever. Just not emacs, please :P)
    1. set your Peloton Email and Password, Save and Close

## Linux Usage

* Open a command prompt inside of the `peloton-to-garmin` folder
* Run the following command:
    * `python3 peloton-to-garmin.py`
* You will be prompted to enter how many workouts you would like to fetch
* A TCX file for each workout will be created in the `output` directory
* The resulting TCX file can then be uploaded to Garmin

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
  
  Examples:
  * To get the last 10 activities:  
        * `peloton-to-garmin.py -num 10`  
  * To pass your email and passowrd:  
        * `peloton-to-garmin.py -email you@email.com -password mypassword`  
  
  Note: Command line arguments take precedence over values in the configuration file. 

## Runnning in docker

* Build the image by running
    * `docker build . -t pelotontogarmin`
* Run the container by running:
    * `docker run -v /full_path_here/peloton-to-garmin/output:/output pelotontogarmin`

⚠️ WARNING!!! Your username and password for Peloton and Garmin Connect are stored in clear text, WHICH IS NOT SECURE. If you have concerns about storing your credentials in an unsecure file, do not use this option.

⚠️ WARNING!!! There is no certificate validation. This is open to person-in-the-middle attacks. 
