echo off
docker run -it --rm -v .\shared:/app/shared -v mydockerapp_data:/app/data mydockerapp-win
