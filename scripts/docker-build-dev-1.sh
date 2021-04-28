timestamp=`date +%s`
docker build -t 10.1.1.11:32000/chabloom-accounts-backend:$timestamp -t 10.1.1.11:32000/chabloom-accounts-backend:latest .
docker push 10.1.1.11:32000/chabloom-accounts-backend:$timestamp
docker push 10.1.1.11:32000/chabloom-accounts-backend:latest
