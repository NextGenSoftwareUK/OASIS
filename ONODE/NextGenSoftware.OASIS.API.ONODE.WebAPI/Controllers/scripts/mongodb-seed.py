
import pymongo
from pymongo import MongoClient, TEXT, ASCENDING, DESCENDING
import os
import time
import json
import urllib.request
import urllib.error
import datetime

# Configuration
MONGO_URI = os.getenv("MONGO_URI", "mongodb://oasis:oasis123@localhost:27017/?authSource=admin")
DB_NAME = os.getenv("DB_NAME", "OASISAPI_DEV")

def seed_database():
    try:
        print(f"Connecting to MongoDB at {MONGO_URI}...")
        client = MongoClient(MONGO_URI)
        db = client[DB_NAME]
        print(f"Connected to database: {DB_NAME}")
        print(f"Available Databases: {client.list_database_names()}")

        # Collections to create/ensure
        collections = [
            "Avatars", "AvatarDetails", "Holons", "StoredFiles", "NFTs", 
            "Missions", "Quests", "CelestialBodies", "RefreshTokens"
        ]

        # 1. Avatars Collection (Validation)
        try:
            db.create_collection("Avatars", validator={
                "$jsonSchema": {
                    "bsonType": "object",
                    "required": ["Email", "Username", "CreatedDate"],
                    "properties": {
                        "Email": { "bsonType": "string" },
                        "Username": { "bsonType": "string" },
                        "Password": { "bsonType": "string" },
                        "Title": { "bsonType": "string" },
                        "FirstName": { "bsonType": "string" },
                        "LastName": { "bsonType": "string" },
                        "AvatarType": { "bsonType": "int" },
                        "IsActive": { "bsonType": "bool" },
                        "IsActive": { "bsonType": "bool" },
                        "Verified": { "bsonType": "date" },
                        "CreatedDate": { "bsonType": "date" },
                        "ModifiedDate": { "bsonType": "date" },
                        "Karma": { "bsonType": "int" },
                        "Level": { "bsonType": "int" }
                    }
                }
            })
            print("[+] Created collection: Avatars")
        except pymongo.errors.CollectionInvalid:
            print("[*] Collection Avatars already exists from previous runs")

        # 2. Indexes
        print("Creating indexes...")
        
        # Avatars
        db.Avatars.create_index([("Email", ASCENDING)], unique=True)
        db.Avatars.create_index([("Username", ASCENDING)], unique=True)
        db.Avatars.create_index([("Id", ASCENDING)], unique=True)
        
        # AvatarDetails
        db.AvatarDetails.create_index([("AvatarId", ASCENDING)])
        
        # Holons
        db.Holons.create_index([("Id", ASCENDING)], unique=True)
        db.Holons.create_index([("ParentId", ASCENDING)])
        db.Holons.create_index([("CreatedByAvatarId", ASCENDING)])
        
        # StoredFiles
        db.StoredFiles.create_index([("Id", ASCENDING)], unique=True)
        db.StoredFiles.create_index([("AvatarId", ASCENDING)])
        
        # NFTs
        db.NFTs.create_index([("Id", ASCENDING)], unique=True)
        db.NFTs.create_index([("AvatarId", ASCENDING)])
        
        # Missions
        db.Missions.create_index([("Id", ASCENDING)], unique=True)
        
        # Quests
        db.Quests.create_index([("Id", ASCENDING)], unique=True)
        
        # CelestialBodies
        db.CelestialBodies.create_index([("Id", ASCENDING)], unique=True)
        
        # RefreshTokens
        db.RefreshTokens.create_index([("Token", ASCENDING)])
        db.RefreshTokens.create_index([("AvatarId", ASCENDING)])

        print("OASIS collections and indexes configured successfully!")
        print(f"Database: {DB_NAME}")
        print(f"Collections: {', '.join(collections)}")

    except Exception as e:
        print(f"[-] Error seeding database: {e}")


if __name__ == "__main__":
    seed_database()
