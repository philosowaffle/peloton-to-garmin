FROM python:alpine

ENV P2G_NUM=5
ENV P2G_GARMIN_ENABLE_UPLOAD=false
ENV PTG_ENABLE_POLLING=true
ENV PTG_POLLING_INTERVAL_SECONDS=600
ENV P2G_PATH="/output"
ENV P2G_PAUSE_ON_FINISH=false
ENV P2G_LOG="/logs/p2g.log"
ENV P2G_LOG_LEVEL=INFO

COPY . /opt/app
WORKDIR /opt/app
RUN pip install -r requirements.txt
RUN pip uninstall -y garmin-uploader
RUN pip install https://github.com/La0/garmin-uploader/archive/cloudscraper.zip
RUN chmod +x peloton-to-garmin.py
CMD python peloton-to-garmin.py
