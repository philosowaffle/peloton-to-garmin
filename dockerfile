FROM python:latest

ENV NUM_ACTIVITIES=5
ENV OUTPUT_DIRECTORY="/output"

WORKDIR /usr/local/bin

COPY . /opt/app
WORKDIR /opt/app
RUN pip install -r requirements.txt

CMD python peloton-to-garmin.py

