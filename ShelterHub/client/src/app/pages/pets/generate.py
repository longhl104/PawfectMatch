import uuid
import json
from datetime import datetime, timedelta

items = []
base_created_at = datetime.utcnow()

for i in range(24):
    item = {
        "PutRequest": {
            "Item": {
                "PetId": {"S": str(uuid.uuid4())},
                "Age": {"N": "30"},
                "Breed": {"S": "jack russell terrier"},
                "CreatedAt": {"S": (base_created_at + timedelta(seconds=i)).isoformat() + "Z"},
                "Description": {"S": "<p>afwefawef</p>"},
                "Gender": {"S": "Male"},
                "Name": {"S": f"Doggo{i}"},
                "ShelterId": {"S": "522e6b8b-64ed-4bf0-88c2-a44081535338"},
                "Species": {"S": "Dog"},
                "Status": {"S": "Available"}
            }
        }
    }
    items.append(item)

payload = {
    "pawfectmatch-development-shelter-hub-pets": items
}

with open("pets.json", "w") as f:
    json.dump(payload, f, indent=2)
