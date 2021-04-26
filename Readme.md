# Malware Sample Exchange (MSE)

The **Malware Sample Exchange (MSE)** provides a modern alternative to [Virex](https://github.com/NextSecurity/virex), to exchange malware samples between AV industry partners. It is easy to set up and supports cloud-native workflows.

See the blog post for a longer introduction: [A modern Sample Exchange System](https://www.gdatasoftware.com/blog/2020/10/36410-a-modern-sample-exchange-system)

Our idea is to provide a standardized exchange system which meets the following criteria:

- The ability to choose only the malware samples you need. 
- The ability for partners to filter before the download.
  - SHA265 
  - Categories which can include, but is not limited to, the target platform or specific detections.
- Easy to consume API and built on current web standards ([OpenAPI](https://www.openapis.org/))
- Easy to set up in a few minutes, so that every exchange partner is able to host it with little added effort.
- Specific sample sets per partner

## HTTP API

The **Malware Sample Exchange** service is [OpenAPI](https://www.openapis.org/) compatible and exposes an API description that can be used to automatically generate a client for a programming language.

HTTP API Web UI: `http://{}/swagger/index.html`

HTTP API Json description: `http://{}/swagger/v1/swagger.json`

Exposed End-Points:

|Route|Parameter|Example|Basic Auth|Description|
|-----|---------|-------|--------------|-----------|
|/swagger/index.html|-|-|No|Shows the OpenAPI web interface|
|/swagger/v1/swagger.json|-|-|No|OpenAPI json description|
|/v1/list|start (required), end (optional)| /v1/list?start=2020-09-23 |Yes (user:password) |Fetch list with available samples
|/v1/download|token (required)| /v1/download?token=eyJ0eX... |No |Download a sample with a token from the list

## Usage

For example, by executing:

```bash
# Get all samples for the user "testuser" in the time range 2020-09-23 until now
curl -u "testuser:somenicepassword" -X GET -k -i 'http://localhost:8080/v1/list?start=2020-09-23'
```

you will receive
a list of [JWT](https://jwt.io/) tokens, which generally have the format ```aaaa.bbbbbbbb.cccc``` of three base64 encoded sections that are separated
by dots. The first section are header information, declaring the structure as JWT and the used hash algorithm. The second part is the
actual payload that contains expiration date of the token, SHA256, file size and the platform. The third part is a signature that guarantees that the JWT is valid.
If after checking that the sample is not already part of your collection and you have interest in the reported platform,
you can download it with: 

```bash
# Download a specific sample
curl -X GET -k "http://localhost:8080/v1/download?token=$PUT_TOKEN_HERE"
```

Not authentication is needed for the download, as the [JWT](https://jwt.io/) is signed and as such authenticates the request.

The list-endpoints returns a list of Json data structure which contains a JTW for each sample. How a JWT is decoded is shown below.

```bash
# Decode a JWT from the list-endpoint
TOKEN="eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzUxMiJ9.eyJleHAiOjE2MTk1MTc4NzMsInNoYTI1NiI6IjA1YzIyNDU1Zjc3YjVmOTIxY2I1ZWIyM2FkZDBkYjkwNzc3NjljMGNhY2I4NDBjNDYwZjQxZDlhODM1NzkyOWYiLCJmaWxlc2l6ZSI6MTIzNDUsInBsYXRmb3JtIjoiUERGIiwicGFydG5lciI6InRlc3R1c2VyIn0.pACN0JaMnSoA0Dnk1lXk77BU9krCawnRkXAVTDTDKahXT9HKleAfuK8ngZ62SauOj-pGXkO2m3ijH2x3PNRl1A"
ARRAY=(`echo $TOKEN | tr '.' ' '`)

# Decode header
echo "HEADER:"
echo ${ARRAY[1]} | base64 -d
# Output:
# {"typ":"JWT","alg":"HS512"}

# Decode the payload
echo "PAYLOAD:"
echo ${ARRAY[2]} | base64 -d
# Output:
# {"exp":1619517873,"sha256":"05c22455f77b5f921cb5eb23add0db9077769c0cacb840c460f41d9a8357929f","filesize":12345,"platform":"PDF","partner":"testuser"}


# Decode signatrue (not printable)
echo "Signature:"
echo ${ARRAY[3]} | base64 -d
# Output:
# ��Ж��*�9��U��T�J�k      ёpL4�)�WO�ʕ���'���I��
```

## Setup

There are several methods to setup MSE in production or for testing.

### Kubernetes Deployment

An example deployment for Kubernetes if given in [k8s-deployment.yaml](./k8s-deployment.yaml).

For ease of use, it uses NodePorts to expose the Mongodb for meta-data and the REST service to the network. If you already have K8S cluster with an ingress/load-balancer, use them instead of the NodePorts.


- All data will be stored here: `/mnt/sampleexportstorage`
  - The folder has to be created before the deployment
- REST API will be reachable under: `http://{your k8s host}:32000`
- Mongodb for meta-data will be reachable under: `mongodb://{your k8s host}:32001`

You can find the latest image on Docker Hub: [Sample-Exchange Docker Image](https://hub.docker.com/r/gdatacyberdefense/sampleexchange/tags?page=1&ordering=last_updated)

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

## Build and Release

A GitHub action builds on every push and pull request. A new Docker image will be pushed to the Docker Hub.

 To release a new version, push a tagged version like this:

```bash
git tag -a 1.0.0 -m "Release version 1.0.0"
git push origin 1.0.0
```

Replace with the corresponding version.
