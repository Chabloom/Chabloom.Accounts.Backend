timestamp=`date +%s`
docker build -t mdcasey/chabloom-accounts-backend:$timestamp -t mdcasey/chabloom-accounts-backend:latest .
docker push mdcasey/chabloom-accounts-backend:$timestamp
docker push mdcasey/chabloom-accounts-backend:latest
