This service provides a REST API for sharing samples with partners in the outside world. Use cases are the request
of a list of available samples within a certain time period and the download of listed samples.

### Test setup

The exchange API is in need of a Mongo for storing sample meta data. You can start a database with the following command: 
```docker run -d -it --rm -p 27017:27017 mongo```.
Make sure that the folder ```/mnt/sampleexportstorage/``` exists and execute the Python script located in this repository by typing 
```python3 ./src/FillMongoWithTestData/main.py```. This scripts creates three benign test samples on the share and adds meta data to 
the Mongo.
Now you can start up Exchange API by changing to directory ```./src/SampleExchangeApi.Console/``` and typing ```dotnet run```.

### Usage
 
 By executing ```curl -u "testuser:somenicepassword" -X GET -k -i 'http://localhost:8080/v1/list?start=2020-09-23'``` you will receive
 a list of JWT tokens, which generally have the format ```aaaa.bbbbbbbb.cccc``` of three base64 encoded sections that are separated
 by dots. The first section are header information, declaring the structure as JWT and the used hash algorithm. The second part is the
 actual payload that contains expiration date of the token, SHA256, file size and the platform. The third part is a signature that 
 guarantees that the JWT is valid.
 If after checking that the sample is not already part of your collection and you have interest in the reported platform,
 you can download it with ```curl -X GET -k "http://localhost:8080/v1/download?token=$PUT_TOKEN_HERE"```. 
