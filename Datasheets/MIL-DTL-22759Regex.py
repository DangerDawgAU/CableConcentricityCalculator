import os
import sys
import re
import json
from pypdf import PdfReader
from collections import Counter
import math

# --- 0. Constants and Helper Dictionaries ---

# Standard Color Codes (Based on MIL-STD-104 from Page 36)
MIL_STD_104_COLORS = {
    '0': 'Black', '1': 'Brown', '2': 'Red', '3': 'Orange', '4': 'Yellow', 
    '5': 'Green', '6': 'Blue', '7': 'Violet', '8': 'Gray', '9': 'White'
}

# --- 1. Regex Extraction and Normalization Logic ---

def parse_unit_pair(data_str):
    """
    Parses a string like 'X.XX (Y.YY)' into a dict of US and Metric units.
    
    Fixes the 'KeyError: 'metric'' by ensuring the 'metric' key is always present.
    """
    data_str = data_str.strip()
    
    # Pattern to match the standard 'US (Metric)' format
    match = re.match(r'([0-9\.]+)\s*\(([\d\.]+)\)', data_str)
    
    if match:
        try:
            # Group 1 is the US unit (e.g., inches, lbs/1000 ft)
            us_val = float(match.group(1))
            # Group 2 is the Metric unit (e.g., mm, kg/1000 m)
            metric_val = float(match.group(2))
            return {
                "us": us_val,
                "metric": metric_val
            }
        except ValueError:
            # Failed to convert numbers, return safe defaults
            return {"us": 0.0, "metric": 0.0}
    
    # Fallback for values that don't follow the 'US (Metric)' pattern. 
    # This assumes it might be a single number (US unit) or a bad parse.
    try:
        us_val = float(data_str)
        # If it's a single value, we assume it's the US unit, and the metric is unknown (0)
        return {"us": us_val, "metric": 0.0}
    except ValueError:
        # Final fallback for completely unparseable strings
        return {"us": 0.0, "metric": 0.0}


def clean_match_row(match_tuple, headers, style_name):
    """Helper to clean whitespace and create a dict."""
    clean_items = []
    for item in match_tuple:
        clean_text = item.strip()
        clean_text = clean_text.replace('\n', ' ')
        clean_text = re.sub(r'\s+', ' ', clean_text)
        clean_items.append(clean_text)
    
    row_data = dict(zip(headers, clean_items))
    row_data["_Matched_Style"] = style_name
    return row_data

def normalize_m22759_properties(row_data):
    """
    Parses all 'Value (Unit)' strings into structured dictionaries, 
    guaranteeing the 'us' and 'metric' keys exist for every property.
    """
    normalized = {
        "Part Number": row_data.get("Part Number"),
        "AWG Size": row_data.get("AWG Size"),
        "Stranding": row_data.get("Stranding"),
        "_Matched_Style": row_data.get("_Matched_Style"),
        "Thermax P/N": row_data.get("Thermax P/N")
    }

    # Standard fields
    fields_to_parse = [
        "Conductor Diameter", "Insulation Diameter Minimum", 
        "Insulation Diameter Maximum", "Weight", 
        "Maximum Resistance"
    ]
    
    default_unit_pair = {"us": 0.0, "metric": 0.0}
    
    for field in fields_to_parse:
        if field in row_data:
            normalized[field] = parse_unit_pair(row_data[field])
        else:
            # Use default to prevent KeyError: 'metric' later
            normalized[field] = default_unit_pair


    # Special field for High-Strength tables
    if "Break Strength" in row_data:
        normalized["Break Strength"] = parse_unit_pair(row_data["Break Strength"])
    else:
        # Use default to prevent KeyError: 'metric' later
        normalized["Break Strength"] = default_unit_pair
        
    # Extract Base MIL Spec
    match = re.search(r'(MIL-W-22759/\d{1,2}|MIL-DTL-22759/\d{1,2})', normalized["Part Number"])
    if match:
        normalized["MIL Spec"] = match.group(1)
    else:
        normalized["MIL Spec"] = "N/A"

    return normalized

def extract_datasheet_info(extracted_text):
    """Searches the text for ALL M22759 table structures and normalizes them."""
    results = []
    
    # Common Headers for tables WITHOUT Break Strength (e.g., /5, /9, /11, /16, /18)
    headers_std = [
        "Part Number", "AWG Size", "Stranding", 
        "Conductor Diameter", "Insulation Diameter Minimum", 
        "Insulation Diameter Maximum", "Weight", 
        "Maximum Resistance", "Thermax P/N"
    ]

    # Pattern for standard tables (9 columns of data)
    # Group 1: Part Number (e.g., M22759/5-8-*)
    # Group 2: AWG Size (e.g., 8, 2/0, 1/0)
    # Group 3: Stranding (e.g., 133/29)
    # Groups 4-9: 6 fields of X.X (Y.Y) or X (Y) data (Diameters, Weight, Resistance)
    # Group 10: Thermax P/N
    pattern_std = r'(M22759/\d{1,2}-\d{1,2}[/-]\*?)\s+([\d/]{1,4})\s+([\d/]+)\s+([0-9\.]+\s*\([\d\.]+\))\s+([0-9\.]+\s*\([\d\.]+\))\s+([0-9\.]+\s*\([\d\.]+\))\s+([0-9\.]+\s*\([\d\.]+\))\s+([0-9\.]+\s*\([\d\.]+\))\s+([0-9\.]+\s*\([\d\.]+\))\s+([A-Z0-9\/\-]+)'
    
    for match in re.findall(pattern_std, extracted_text, re.DOTALL | re.IGNORECASE):
        # Filter out high-strength parts which have an extra column (Break Strength)
        part_num = match[0]
        if any(f"/{s}-" in part_num for s in ['17', '19', '20', '21', '22', '23']):
            continue 
            
        row = clean_match_row(match, headers_std, "M22759 Standard")
        results.append(normalize_m22759_properties(row))

    # Common Headers for tables WITH Break Strength (e.g., /17, /19, /20, /21, /22, /23)
    headers_hs = [
        "Part Number", "AWG Size", "Stranding", 
        "Conductor Diameter", "Insulation Diameter Minimum", 
        "Insulation Diameter Maximum", "Weight", 
        "Maximum Resistance", "Break Strength", "Thermax P/N"
    ]
    
    # Pattern for high-strength tables (10 columns of data)
    # Group 1: Part Number (e.g., M22759/20-20-*)
    # Group 2: AWG Size
    # Group 3: Stranding
    # Groups 4-10: 7 fields of X.X (Y.Y) or X (Y) data (Diameters, Weight, Resistance, Break Strength)
    # Group 11: Thermax P/N
    pattern_hs = r'(M22759/\d{1,2}-\d{1,2}-\*)\s+([\d/]+)\s+([\d/]+)\s+([0-9\.]+\s*\([\d\.]+\))\s+([0-9\.]+\s*\([\d\.]+\))\s+([0-9\.]+\s*\([\d\.]+\))\s+([0-9\.]+\s*\([\d\.]+\))\s+([0-9\.]+\s*\([\d\.]+\))\s+([0-9\.]+\s*\([\d\.]+\))\s+([0-9\.]+\s*\([\d\.]+\))\s+([A-Z0-9\/\-]+)'

    for match in re.findall(pattern_hs, extracted_text, re.DOTALL | re.IGNORECASE):
        row = clean_match_row(match, headers_hs, "M22759 High-Strength")
        results.append(normalize_m22759_properties(row))
        
    return results

# --- 2. Transformation Function for Final Format ---

def transform_to_core_format(normalized_data):
    """
    Transforms the normalized M22759 data into the final, detailed JSON structure 
    for single-conductor hookup wire.
    """
    final_output = []
    
    for row in normalized_data:
        part_number = row.get("Part Number", "UNKNOWN").replace('*', '')
        
        # 1. Core Data (Single Conductor)
        # --- FIX: Change 'mm' to 'metric' ---
        cond_diam = row["Conductor Diameter"]["metric"]
        insul_max_od = row["Insulation Diameter Maximum"]["metric"]
        
        # Calculate Insulation Thickness: (Max_OD - Cond_Dia) / 2
        insul_thick = (insul_max_od - cond_diam) / 2.0
        
        # Simple color parsing (assumes a 9 is the default color code 'white' for *-9)
        color_code_match = re.search(r'-(\d{1,2})$', part_number) 
        if color_code_match:
            # Take the first digit for the base wire color
            base_color_num = color_code_match.group(1) 
            base_color = MIL_STD_104_COLORS.get(base_color_num[0], 'White')
        else:
            base_color = 'White'

        cores_data = [{
            "CoreId": "1",
            # Metric values are now correctly accessed as "metric"
            "ConductorDiameter": round(cond_diam, 3), 
            "InsulationThickness": round(insul_thick, 3) if insul_thick > 0 else 0.0,
            "InsulationColor": base_color,
            "Gauge": str(row["AWG Size"]),
            "ConductorMaterial": f"Plated Copper ({row['_Matched_Style'].replace('M22759 ', '')})"
        }]
        
        # 2. Outer properties (not applicable for single wire)
        
        final_output.append({
            "CableId": f"m22759-{part_number.replace('/','-').replace(' ','-')}".lower(),
            "PartNumber": part_number,
            "Manufacturer": "Thermax (Implied)", 
            "Name": f"{row['MIL Spec']} {row['AWG Size']} AWG Hookup Wire",
            "Type": 1, # Single Core Hookup Wire
            "Cores": cores_data,
            "JacketThickness": 0.0,
            "JacketColor": base_color,
            "HasShield": False,
            "ShieldType": 0,
            "ShieldThickness": 0.0,
            "ShieldCoverage": 0,
            "HasDrainWire": False,
            "DrainWireDiameter": 0.0,
            "IsFiller": False,
            # Metric values are now correctly accessed as "metric"
            "SpecifiedOuterDiameter": round(insul_max_od, 3),
            "Description": f"MIL-W-22759 Hookup Wire, {row['AWG Size']} AWG, Stranding {row['Stranding']}",
            # Metric values are now correctly accessed as "metric"
            "Weight (kg/km)": round(row["Weight"]["metric"], 3),
            "MaxResistance (Ohm/km)": round(row["Maximum Resistance"]["metric"], 3),
            "BreakStrength (kg)": round(row["Break Strength"]["metric"], 3)
        })
        
    return final_output

# --- 3. Main/Utility Functions ---

def extract_pages_for_debug(pdf_path, n_debug_pages=100): 
    """
    Opens a PDF and extracts text from all pages.
    """
    if not os.path.exists(pdf_path):
        print(f"Error: File not found at '{pdf_path}'.")
        return None, None

    try:
        reader = PdfReader(pdf_path)
        total_pages = len(reader.pages)
        
        print(f"\n📄 Found PDF: '{os.path.basename(pdf_path)}' with {total_pages} total pages.")
        print(f"⏳ Extracting text from ALL {total_pages} page(s) for analysis...")
        sys.stdout.write("Progress: 0%")
        sys.stdout.flush()

        full_text = []
        debug_text = []
        
        for i in range(total_pages):
            page_num = i + 1
            page = reader.pages[i]
            
            if page_num == 1 or page_num % 10 == 0 or page_num == total_pages:
                progress_percent = int((page_num / total_pages) * 100)
                sys.stdout.write(f"\rProgress: Page {page_num} of {total_pages} ({progress_percent}%) ")
                sys.stdout.flush()
            
            try:
                page_text = page.extract_text()
            except:
                page_text = "[[Extraction Failed]]"
            
            full_text.append(f"\n\n----- PAGE {page_num} -----\n\n")
            full_text.append(page_text)
            
            if i < n_debug_pages:
                debug_text.append(f"\n\n----- PAGE {page_num} OF {min(n_debug_pages, total_pages)} (DEBUG) -----\n\n")
                debug_text.append(page_text)
        
        print() 
        
        return "".join(full_text), "".join(debug_text)

    except Exception as e:
        print(f"An error occurred during PDF processing: {e}")
        return None, None
    
def main():
    current_dir = os.getcwd()
    pdf_files = sorted([f for f in os.listdir(current_dir) if f.lower().endswith('.pdf')])
    
    if not pdf_files:
        print("❌ Error: No PDF files found in the current directory.")
        sys.exit(1)

    print("\n" + "="*50 + "\n     PDF FILES FOUND\n" + "="*50)
    for i, filename in enumerate(pdf_files):
        print(f"[{i + 1}] {filename}")
    print("="*50)
    
    selected_indices = []
    
    while not selected_indices:
        try:
            choice = input(f"Select PDF(s) containing MIL-W-22759 data (e.g., 1 or 1,3,5 or 1-3): ")
            
            for part in choice.split(','):
                part = part.strip()
                if '-' in part:
                    start, end = map(int, part.split('-'))
                    selected_indices.extend(range(start, end + 1))
                elif part.isdigit():
                    selected_indices.append(int(part))
            
            selected_indices = sorted(list(set(selected_indices)))
            
            valid_selections = [i for i in selected_indices if 1 <= i <= len(pdf_files)]
            
            if not valid_selections:
                print("Invalid selection. Please use numbers within the range 1 to", len(pdf_files))
                selected_indices = []
            else:
                selected_files = [pdf_files[i - 1] for i in valid_selections]
                break
        except ValueError:
            print("Invalid input format.")
            selected_indices = []

    all_structured_data = []
    
    print("\n" + "—"*60)
    print(f"🚀 Processing {len(selected_files)} selected files for MIL-W-22759 data...")
    print("—"*60)
    
    for selected_file in selected_files:
        pdf_path = os.path.join(current_dir, selected_file) 
        base_name = os.path.splitext(selected_file)[0]
        
        # 1. Extract
        full_text, debug_text = extract_pages_for_debug(pdf_path, n_debug_pages=100)
        if not full_text: continue
        
        # 2. Write Debug
        debug_fname = f"{base_name}_M22759_DEBUG_raw.txt"
        with open(os.path.join(current_dir, debug_fname), 'w', encoding='utf-8') as f:
            f.write(debug_text)
        print(f"\n💡 DEBUG FILE CREATED for '{selected_file}': '{debug_fname}' (First 100 pages)")

        # 3. Apply Regex and Normalize
        print(f"🔍 Applying MIL-W-22759 specific regex strategies to '{selected_file}'...")
        file_structured_data = extract_datasheet_info(full_text)
        
        print(f"   -> Found {len(file_structured_data)} raw data rows in this file.")
        all_structured_data.extend(file_structured_data)

    # 4. Final Transformation and Output
    if all_structured_data:
        print("\n" + "="*60)
        print(f"✨ Transforming all {len(all_structured_data)} collected rows into final JSON format...")
        
        final_data = transform_to_core_format(all_structured_data)
        
        out_file = "mil_w_22759_hookup_wire_data.json"
        
        with open(os.path.join(current_dir, out_file), 'w', encoding='utf-8') as f:
            json.dump(final_data, f, indent=4)
            
        print("\n" + "="*60)
        print(f"✅ FINAL SUCCESS! Transformed {len(final_data)} total data rows.")
        styles = [x.get("_Matched_Style") for x in all_structured_data]
        print("    Total Style Breakdown:", dict(Counter(styles)))
        print(f"Results saved to: '{out_file}'")
        print("="*60)
    else:
        print("\n❌ NO MIL-W-22759 DATA ROWS FOUND ACROSS ALL SELECTED FILES. Check the debug text files.")

if __name__ == "__main__":
    main()