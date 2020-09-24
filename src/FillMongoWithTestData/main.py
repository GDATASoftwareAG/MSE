import hashlib
import pymongo
import datetime
import os

destination_folder = '/mnt/sampleexportstorage/'

string_1 = '"Your focus determines your reality." – Qui-Gon Jinn'
string_2 = '"Do. Or do not. There is no try." – Yoda'
string_3 = '"In my experience there is no such thing as luck." – Obi-Wan Kenobi'

mongo_client = pymongo.MongoClient("mongodb://localhost:27017/")
mongo_db = mongo_client["Sample"]
mongo_collection = mongo_db["Sample"]


def put_string_into_db(sha256, platform, file_size, sample_set):
    current_iso_datetime = datetime.datetime.now()
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
    file_path = folder + f"{sha256_of_string[0:2]}/" + f"{sha256_of_string[2:4]}/" + sha256_of_string
    os.makedirs(os.path.dirname(file_path), exist_ok=True)
    file = open(file_path, 'w+')
    file.write(hash_target)
    file.close()
    return sha256_of_string


def main():
    sha256_1 = hash_string_and_save_to_file_in_folder(string_1, destination_folder)
    sha256_2 = hash_string_and_save_to_file_in_folder(string_2, destination_folder)
    sha256_3 = hash_string_and_save_to_file_in_folder(string_3, destination_folder)
    put_string_into_db(sha256_1, "PDF", 12345, "test")
    put_string_into_db(sha256_2, "PE32", 67890, "test")
    put_string_into_db(sha256_3, "AND", 112233, "test")


if __name__ == '__main__':
    main()
