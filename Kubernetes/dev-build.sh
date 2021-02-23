docker build -t chabloom-accounts-backend:1.0.0 .
docker save chabloom-accounts-backend > chabloom-accounts-backend.tar
microk8s ctr image import chabloom-accounts-backend.tar
rm chabloom-accounts-backend.tar
