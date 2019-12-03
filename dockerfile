FROM python:latest

WORKDIR /usr/local/bin

COPY . /opt/app
WORKDIR /opt/app
RUN pip install -r requirements.txt

CMD python peloton-to-garmin.py

