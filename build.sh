#!/bin/bash

dotnet publish -c Release
docker build -t crawler-image -f ./Crawler/Dockerfile .
docker create --name crawler-main crawler-image