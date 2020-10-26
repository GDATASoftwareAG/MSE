#!/usr/bin/python3

import hashlib
import pymongo # sudo pip install pymongo
import datetime
import os
import sys, getopt

def put_string_into_db(sha256, platform, file_size, sample_set, mongo_collection):
    current_iso_datetime = datetime.datetime.utcnow()
    entry = {
                "_id": f"{sha256}:test",
                "Sha256": sha256,
                "Platform": platform,
                "Imported": current_iso_datetime,
                "FileSize": file_size,
                "DoNotUseBefore": current_iso_datetime,
                "SampleSet": sample_set
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
    put_string_into_db(sha256_1, "PDF", 12345, "test", mongo_collection)
    put_string_into_db(sha256_2, "PE32", 67890, "test", mongo_collection)
    put_string_into_db(sha256_3, "AND", 112233, "test", mongo_collection)

if __name__ == '__main__':
    main(sys.argv[1:])
