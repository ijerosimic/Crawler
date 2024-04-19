#!/bin/bash

dotnet publish -c Release -o out
docker build -t crawler-image -f ./Crawler/Dockerfile .
docker create --name crawler-main crawler-image
docker run --attach STDOUT crawler-image