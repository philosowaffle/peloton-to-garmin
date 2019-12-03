FROM python:latest

WORKDIR /usr/local/bin

COPY . /opt/app
WORKDIR /opt/app
RUN pip install -r requirements.txt
RUN pip install garmin_uploader

CMD rm /output/*.tcx && python peloton-to-garmin.py && gupload ./garmin-uploader/garmin_uploader/cli.py /output/*.tcx