FROM python:latest

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

CMD python peloton-to-garmin.py
