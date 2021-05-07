timestamp=`date +%s`
docker build -t chb-prod-1.chabloom.com:32000/chabloom-accounts-backend:$timestamp -t chb-prod-1.chabloom.com:32000/chabloom-accounts-backend:latest .
docker push chb-prod-1.chabloom.com:32000/chabloom-accounts-backend:$timestamp
docker push chb-prod-1.chabloom.com:32000/chabloom-accounts-backend:latest
