import json
import os

# Parameters to keep (based on v2 template)
ALLOWED_CABLE_PARAMS = {
    "CableId", "PartNumber", "Manufacturer", "Name", "Type", "Cores",
    "JacketThickness", "JacketColor", "HasShield", "ShieldType",
    "ShieldThickness", "ShieldCoverage", "HasDrainWire", "DrainWireDiameter",
    "IsFiller", "SpecifiedOuterDiameter", "Description"
}

ALLOWED_CORE_PARAMS = {
    "CoreId", "ConductorDiameter", "InsulationThickness", "InsulationColor",
    "Gauge", "ConductorMaterial"
}

def filter_cable_entry(cable):
    """Filter a cable entry to only include allowed parameters"""
    filtered = {}

    # Filter top-level cable parameters
    for key in ALLOWED_CABLE_PARAMS:
        if key in cable:
            if key == "Cores":
                # Filter each core's parameters
                filtered["Cores"] = []
                for core in cable["Cores"]:
                    filtered_core = {k: v for k, v in core.items() if k in ALLOWED_CORE_PARAMS}
                    filtered["Cores"].append(filtered_core)
            else:
                filtered[key] = cable[key]

    return filtered

def merge_json_files():
    """Merge all JSON files from Datasheets folder"""
    datasheets_dir = "Datasheets"

    # Find all JSON files
    json_files = [
        "combined_lapp_cable_data.json",
        "mil_dtl_27500_cable_data.json",
        "mil_w_22759_hookup_wire_data.json"
    ]

    all_cables = []
    cable_ids = set()

    for json_file in json_files:
        filepath = os.path.join(datasheets_dir, json_file)

        if not os.path.exists(filepath):
            print(f"Warning: {filepath} not found, skipping...")
            continue

        print(f"Reading {json_file}...")

        with open(filepath, 'r', encoding='utf-8') as f:
            cables = json.load(f)

        # Filter and add cables, avoiding duplicates
        for cable in cables:
            cable_id = cable.get("CableId")

            if cable_id and cable_id not in cable_ids:
                filtered_cable = filter_cable_entry(cable)
                all_cables.append(filtered_cable)
                cable_ids.add(cable_id)
            elif cable_id in cable_ids:
                print(f"  Skipping duplicate: {cable_id}")

        print(f"  Added {len([c for c in cables if c.get('CableId') not in cable_ids or c.get('CableId') == cable_id])} cables from {json_file}")

    print(f"\nTotal cables merged: {len(all_cables)}")

    # Write to CableLibrary.json with v2 wrapper format
    output_path = "CableConcentricityCalculator/Libraries/CableLibrary.json"

    print(f"\nWriting to {output_path}...")
    output_data = {"Cables": all_cables}
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(output_data, f, indent=4)

    print("Done! CableLibrary.json has been updated.")

    # Print summary
    print(f"\n=== Summary ===")
    print(f"Total unique cables: {len(all_cables)}")
    print(f"Source files processed: {len(json_files)}")

if __name__ == "__main__":
    merge_json_files()
