# Malware Sample Exchange (MSE)

This service provides a REST API for sharing samples with partners in the outside world. Use cases are the request
of a list of available samples within a certain time period and the download of listed samples.

## Setup

There are several methods to setup MSE in production or for testing.

### Kubernetes Deployment

An example deployment for Kubernetes if given in [k8s-deployment.yaml](./k8s-deployment.yaml).

For ease of use, it uses NodePorts to expose the Mongodb for meta-data and the REST service to the network. If you already have K8S cluster with an ingress/load-balancer, use them instead of the NodePorts.


- All data will be stored here: `/mnt/sampleexportstorage`
  - The folder has to be created before the deployment
- REST API will be reachable under: `http://{your k8s host}:32000`
- Mongodb for meta-data will be reachable under: `mongodb://{your k8s host}:32001`

```bash
# Deploy to k8s
kubectl apply -f k8s-deployment.yaml

# Fill with example data
python3 ./src/ python3 main.py -s "/mnt/sampleexportstorage" -m "mongodb://localhost:32001"

# Fetch list with samples (set date to current)
curl -u "testuser:somenicepassword" -X GET -k -i 'http://localhost:32000/v1/list?start=2020-09-23'

# Remove all k8s resources (does not remove /mnt/sampleexportstorage)
kubectl apply/delete -f k8s-deployment.yaml
```


### Local test setup

The exchange API is in need of a Mongodb for storing sample meta data. You can start a database with the following command:
`docker run -d -it --rm -p 27017:27017 mongo`.

Make sure that the folder `/mnt/sampleexportstorage/` exists and execute the Python script located in this repository by typing 
`python3 ./src/FillMongoWithTestData/main.py -s "/mnt/sampleexportstorage/" -m "mongodb://localhost:27017"`. 

This scripts creates three benign test samples on the share and adds meta data to the Mongodb.
Now you can start up Exchange API by changing to directory `./src/SampleExchangeApi.Console/` and typing `dotnet run`.

#### Usage

 By executing ```curl -u "testuser:somenicepassword" -X GET -k -i 'http://localhost:8080/v1/list?start=2020-09-23'``` you will receive
 a list of JWT tokens, which generally have the format ```aaaa.bbbbbbbb.cccc``` of three base64 encoded sections that are separated
 by dots. The first section are header information, declaring the structure as JWT and the used hash algorithm. The second part is the
 actual payload that contains expiration date of the token, SHA256, file size and the platform. The third part is a signature that guarantees that the JWT is valid.
 If after checking that the sample is not already part of your collection and you have interest in the reported platform,
 you can download it with ```curl -X GET -k "http://localhost:8080/v1/download?token=$PUT_TOKEN_HERE"```.

## HTTP API



## Build and Release

A GitHub action builds on every push and pull request. A new Docker image will be pushed to the Docker Hub.

 To release a new version, push a tagged version like this:

```bash
git tag -a 1.0.0 -m "Release version 1.0.0"
git push origin 1.0.0
```

Replace with the corresponding version.
