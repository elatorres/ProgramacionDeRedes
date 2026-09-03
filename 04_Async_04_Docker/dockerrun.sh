#!/bin/bash
docker run -it --rm \
  -v $(pwd)/shared:/app/shared \
  -v mydockerapp_data:/app/data \
  mydockerapp-lnx
