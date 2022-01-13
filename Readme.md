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

No authentication is needed for the download, as the [JWT](https://jwt.io/) is signed and as such authenticates the request.

The list-endpoints returns a list of Json data structure which contains a JTW for each sample. How a JWT is decoded is shown below.

```bash
# Decode a JWT from the list-endpoint
TOKEN="eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzUxMiJ9.eyJleHAiOjE2MTk1MTc4NzMsInNoYTI1NiI6IjA1YzIyNDU1Zjc3YjVmOTIxY2I1ZWIyM2FkZDBkYjkwNzc3NjljMGNhY2I4NDBjNDYwZjQxZDlhODM1NzkyOWYiLCJmaWxlc2l6ZSI6MTIzNDUsInBsYXRmb3JtIjoiUERGIiwicGFydG5lciI6InRlc3R1c2VyIn0.pACN0JaMnSoA0Dnk1lXk77BU9krCawnRkXAVTDTDKahXT9HKleAfuK8ngZ62SauOj-pGXkO2m3ijH2x3PNRl1A"
# Split the token in its three parts at '.'
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

## Configuration

To configure the MSE itself, the [appsettings.json](./src/SampleExchangeApi.Console/appsettings.json) is used.
```json
{
  "Token": {
    "Secret": "PutSomeNiceSecretHere", // The global secret used to "sign" the JWTs. Only you must know it.
    "Expiration": 86400.0 // The expiration time of a token in seconds. If the time expired, the token is invalid and cannot be used anymore.
  },
  "Config": {
    "YAML": "shareconfig.yml" // The file used to configure users and sample-sets.
  },
  "MongoDb": {
    "ConnectionString": "mongodb://localhost:27017", // Connection string to the MongoDB
    "DatabaseName": "Sample", // Database name in the MongoDB.
    "CollectionName": "Sample" // Collection name in the MongoDB database.
  },
  "Storage": {
    "Path": "/mnt/sampleexportstorage" // Path to the actual samples.
  }
}
```
All settings can be overwritten by **environment variables**. This is useful, if you want to run the Docker image directly of in Kubernetes, where editing the `appsettings.json` is not feasible.
For example `Token__Secret="PutSomeNiceSecretHere`. The delimiter for sub-sections is the **double underscore** `__` in env. vars.

The `Storage` must have a specific folder structure. All files have to be named after their **SHA256**. The folder structure consists of the first hex byte of the SHA256, which contains the second hex byte of the SHA256 as a sub-folder. In the sub-folder the sample itself is stored.

```
# Example of the expected sample structure
/mnt/sampleexportstorage
  - /00
    - /00
      - /00002455f77b5f921cb5eb23add0db9077769c0cacb840c460f41d9a8357929f
      - ..
    - /01
    - ..
    - /FF
  - /01
  - ..
  - /FF
```

For the configuration of users and their corresponding data sets, the [shareconfig.yaml](./src/SampleExchangeApi.Console/sh
) is used. The MongoDB does not know about any users, it only contains samples which belong to a set.

```yaml
# Example sharedconfig.yaml with two exchange partners
Partners:
- Name: partner1 # Name of the exchange partner
  Password: 466fef588adae318d7f50541982785daaf61d51b5c47101c1c751fbd717dd9e8 # Password Hash
  Salt: 79b48cd1d1ed8fa129c58c5c2d0633b3f9d46087feb8b0165a5ed560356db894 # Password Salt
  Enabled: Yes # Is the exchange with the partner enabled?
  Sampleset: Classic # Which set it shared with the partner?
  IncludeFamilyName: Yes # Allows to include a family into the token

- Name: partner2
  Password: c5363549da9f03d8da44db70ec12ca5dce8078d4cb5fda1d7ecadd4372031539
  Salt: 8ec1690da1bf1baad62a20c0db8e4ad26205ec577b741ccc8b1e2e834670a5e4
  Enabled: No
  Sampleset: Extended
  IncludeFamilyName: no
```

The [main.py](./src/FillMongoWithTestData/main.py) is an example script, which show how the MongoDB is filled with samples to share. It does two things. First it moved the sample itself to the sample folder, as described above. Second, it inserts the needed meta-data for the sample into the MongoDB. This is all that is needed to be able to share the sample with a partner.

```python
#!/usr/bin/python3

import hashlib
import pymongo # sudo pip install pymongo
import datetime
import os
import sys, getopt

def put_string_into_db(sha256, platform, file_size, sample_set, mongo_collection, family_name):
    current_iso_datetime = datetime.datetime.utcnow()
    entry = {
                "_id": f"{sha256}:test",                  # Unique ID 
                "Sha256": sha256,                         # SHA256 of the sample
                "Platform": platform,                     # Free to set and not a not a fixed set. E.g. "EXE_PE32", "Mobile", "PDF" ...
                "Imported": current_iso_datetime,         # Date-time, when the sample was added
                "FileSize": file_size,                    # File size in bytes
                "DoNotUseBefore": current_iso_datetime,   # Do not share before this date-time
                "SampleSet": sample_set,                  # Which set the samples belongs to
                "FamilyName": family_name                 # Custom FamilyName
            }
    mongo_collection.insert_one(entry)


def hash_string_and_save_to_file_in_folder(hash_target, folder):
    sha256_of_string = hashlib.sha256(hash_target.encode('utf-8')).hexdigest()
    file_path = f"{folder}/" + f"{sha256_of_string[0:2]}/" + f"{sha256_of_string[2:4]}/" + sha256_of_string
    os.makedirs(os.path.dirname(file_path), exist_ok=True)
    file = open(file_path, 'w+')
    file.write(hash_target)
    file.close()
    return sha256_of_string


def main(argv):
    destination_folder = ''
    mongo_url = ''
    help = 'main.py -s <storage folder> -m <mongodb url>'

    try:
        opts, args = getopt.getopt(argv, "hs:m:", ["storage=", "mongodb="])
    except getopt.GetoptError:
        print (help)
        sys.exit(2)

    for opt, arg in opts:
      if opt == '-h':
         print (help)
         sys.exit()
      elif opt in ("-s", "--storage"):
         destination_folder = arg
      elif opt in ("-m", "--mongodb"):
         mongo_url = arg

    string_1 = '"Your focus determines your reality." – Qui-Gon Jinn'
    string_2 = '"Do. Or do not. There is no try." – Yoda'
    string_3 = '"In my experience there is no such thing as luck." – Obi-Wan Kenobi'

    mongo_client = pymongo.MongoClient(mongo_url)
    mongo_db = mongo_client["Sample"]
    mongo_collection = mongo_db["Sample"]

    sha256_1 = hash_string_and_save_to_file_in_folder(string_1, destination_folder)
    sha256_2 = hash_string_and_save_to_file_in_folder(string_2, destination_folder)
    sha256_3 = hash_string_and_save_to_file_in_folder(string_3, destination_folder)
    put_string_into_db(sha256_1, "PDF", 12345, "test", mongo_collection, "family2")
    put_string_into_db(sha256_2, "PE32", 67890, "test", mongo_collection, "family1")
    put_string_into_db(sha256_3, "AND", 112233, "test", mongo_collection, "family1")

if __name__ == '__main__':
    main(sys.argv[1:])
```

## Build and Release

A GitHub action builds on every push and pull request. A new Docker image will be pushed to the Docker Hub.

 To release a new version, push a tagged version like this:

```bash
git tag -a 1.0.0 -m "Release version 1.0.0"
git push origin 1.0.0
```

Replace with the corresponding version.
