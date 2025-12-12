import os
import sys
import re
import json
from pypdf import PdfReader
from collections import Counter
import math

# --- 0. Constants and Helper Dictionaries (Units: Millimeters (mm)) ---

# AWG to Nominal Conductor Diameter (mm) - based on stranded copper, standard AWG tables
AWG_TO_DIAMETER_MM = {
    '10': 2.588, '12': 2.052, '14': 1.628, '16': 1.291,
    '18': 1.024, '20': 0.812, '22': 0.644, '24': 0.511,
    '26': 0.405, '28': 0.321, '30': 0.255, 'KCMIL': 5.0
}

# Generic Insulation Thickness (mm) - simplified for typical low-voltage/data cables
AWG_TO_INSULATION_MM = {
    '10': 0.7, '12': 0.6, '14': 0.5, '16': 0.45,
    '18': 0.4, '20': 0.35, '22': 0.25, '24': 0.2,
    '26': 0.18, '28': 0.15, '30': 0.15, 'KCMIL': 1.0 
}

# Standard Color Codes
# 4-Pair (Ethernet TIA/EIA 568-B) - Paired core colors
COLOR_4PAIR = [
    ("Blue", "White/Blue"), ("Orange", "White/Orange"), 
    ("Green", "White/Green"), ("Brown", "White/Brown")
]
# Multi-Conductor (DIN/VDE 0293 for power/control)
COLOR_MULTICONDUCTOR = [
    "Black", "White", "Red", "Green", "Yellow", "Brown", "Blue", "Gray", 
    "Pink", "Violet", "Orange", "Turquoise", "Black/White", "Red/White", 
    "Blue/White", "Black/Red", "White/Red", "Green/Red"
]

# --- 1. Regex Extraction and Normalization Logic ---

def normalize_cable_properties(row_data, style_name):
    """
    Standardizes the AWG, Pairing Configuration, and Total Conductors across all styles.
    """
    row_data['AWG/Size'] = 'N/A'
    row_data['Pairing Configuration'] = 'N/A'
    row_data['Total Conductors'] = 'N/A'
    
    # --- STYLE 4: ÖLFLEX® Power & Control ---
    if style_name == "ÖLFLEX® Power & Control Style":
        count_field = row_data.get('Conductor Count (incl. ground)', 'N/A').replace('KCMIL', '').strip()
        if count_field.isdigit():
            row_data['Total Conductors'] = int(count_field)
            if row_data['Total Conductors'] > 1:
                row_data['Pairing Configuration'] = f"{row_data['Total Conductors']}C"
        
    # --- STYLES 1, 2, 3: ETHERLINE, PROFIBUS, DeviceNet (Parsing Construction/Description) ---
    else:
        # *** FIX: Use 'row_data' instead of 'row' ***
        parse_field = row_data.get('Construction') or row_data.get('Conductor Description')

        if parse_field:
            # 1. Extract AWG/Size
            match_awg = re.search(r'(\d{1,2}(?:/\d{1,2})?\s*AWG|\d+\s*KCMIL)', parse_field, re.IGNORECASE)
            if match_awg:
                row_data['AWG/Size'] = match_awg.group(1).strip()
            
            match_pair_awg = re.search(r'(\d{1,2})\s*AWG\s*/\s*pr', parse_field, re.IGNORECASE)
            if match_pair_awg:
                 row_data['AWG/Size'] = f"{match_pair_awg.group(1)} AWG"

            # 2. Extract Pairing and Total Conductors
            match_pair = re.search(r'(\d+)\s*(?:pr|pair|x\s*\d+x)', parse_field, re.IGNORECASE)
            if match_pair:
                num_pairs = int(match_pair.group(1).replace('x', ''))
                row_data['Pairing Configuration'] = f"{num_pairs} pr"
                row_data['Total Conductors'] = num_pairs * 2
            
            match_cond = re.search(r'(\d+)[cC](?:\/[gG])?', parse_field)
            if match_cond and not match_pair:
                 row_data['Pairing Configuration'] = f"{match_cond.group(1)}C"
                 row_data['Total Conductors'] = int(match_cond.group(1))
            
            # Special case for DeviceNet 
            if 'DeviceNet' in style_name:
                row_data['Pairing Configuration'] = '2P + 1T'
                row_data['Total Conductors'] = 5 
                
    # Final cleanup on AWG/Size
    if isinstance(row_data['AWG/Size'], str) and 'AWG' in row_data['AWG/Size'].upper():
        row_data['AWG/Size'] = row_data['AWG/Size'].upper().replace('AWG', '').strip()
    
    return row_data


def clean_match_row(match_tuple, headers, style_name):
    """Helper to clean whitespace and create a dict."""
    clean_items = []
    for item in match_tuple:
        clean_text = item.strip()
        clean_text = clean_text.replace('\n', ' ')
        clean_text = re.sub(r'\s+', ' ', clean_text)
        
        if style_name == "ÖLFLEX® Power & Control Style" and headers[-1] == "SKINTOP®":
             clean_text = clean_text.replace('PG thread', '').strip()

        clean_items.append(clean_text)
    
    row_data = dict(zip(headers, clean_items))
    row_data["_Matched_Style"] = style_name
    return row_data

def extract_datasheet_info(extracted_text):
    """Searches the text for ALL table structures and normalizes them."""
    results = []
    
    # PATTERN 1: ETHERLINE
    pattern_eth = r'(\d{7}[\*A-Z]{0,3})\s+([\dAWG/pr\s]+)\s+(solid|\d{1,2}\s*wire)\s+(PVC|PUR|halogen-free|PVC\*|PUR\*|TPE)\s+([\w\—\*/]+)\s+(.+?)\s+(yes|no)\s+(yes|no)\s+([0-9\.]+)\s+([0-9\.]+)\s+([0-9\.]+)\s+(\d{3}\s*\d{5})'
    headers_eth = ["Part Number", "Construction", "Stranding", "Jacket Material", "Jacket Color", "Approvals", "Fast Connect", "PoE", "Nominal OD (in)", "Nominal OD (mm)", "Approx. Weight (lbs/mft)", "SKINTOP MS-SC"]
    for match in re.findall(pattern_eth, extracted_text, re.DOTALL | re.IGNORECASE):
        row = clean_match_row(match, headers_eth, "Ethernet/Industrial Style")
        results.append(normalize_cable_properties(row, "Ethernet/Industrial Style"))

    # PATTERN 2: PROFIBUS/General
    pattern_pro = r'(\d{7}[\*]*)\s+(PVC|PUR|halogen-free|PVC/PE)\s+(.+?)\s+(.+?)\s+([0-9]\.\d{3})\s+([\d\.]+)\s+([\d\.]+)\s+([\d\.]+)\s+(\d{3}\s*\d{5})'
    headers_pro = ["Part Number", "Jacket Material", "Conductor Description", "Approvals", "Nominal OD (in)", "Nominal OD (mm)", "Copper Weight (lbs/mft)", "Approx. Weight (lbs/mft)", "SKINTOP MS-SC"]
    for match in re.findall(pattern_pro, extracted_text, re.MULTILINE):
        if match[2].strip() not in ['Thick', 'Thin']:
            row = clean_match_row(match, headers_pro, "Profibus/General Style")
            results.append(normalize_cable_properties(row, "Profibus/General Style"))

    # PATTERN 3: DeviceNet
    pattern_dn = r'(\d{4,7})\s+(Thick|Thin)\s+(?:(PVC|PUR|halogen-free(?:.*?FRNC)?)\s+)?(.+?)\s+([0-9]\.\d{3})\s+([\d\.\s]+)\s+([\d\.]+)\s+([\d\.]+)\s+(\d{3}\s*\d{5})'
    headers_dn = ["Part Number", "Type", "Jacket Material", "Conductor Description", "Nominal OD (in)", "Nominal OD (mm)", "Copper Weight (lbs/mft)", "Approx. Weight (lbs/mft)", "SKINTOP MS-SC"]
    for match in re.findall(pattern_dn, extracted_text, re.DOTALL | re.IGNORECASE):
        cleaned_match = list(match)
        if not cleaned_match[2]: cleaned_match[2] = "Not Specified"
        row = clean_match_row(cleaned_match, headers_dn, "DeviceNet Style")
        results.append(normalize_cable_properties(row, "DeviceNet Style"))

    # PATTERN 4: ÖLFLEX® Power & Control
    pattern_olflex = r'(\d{4,7}C?Y?\*?)\s+(\d{1,3}|KCMIL)\s+([0-9]\.\d{3})\s+([\d\.]+)\s+([\d\.]+)\s+([\d\.]+)\s+(S\s*\d{3,5}|[\d\.\s]+\s*PG\s*thread)'
    headers_olflex = ["Part Number", "Conductor Count (incl. ground)", "Nominal OD (in)", "Nominal OD (mm)", "Copper Weight (lbs/mft)", "Approx. Weight (lbs/mft)", "SKINTOP®"]
    for match in re.findall(pattern_olflex, extracted_text, re.DOTALL | re.IGNORECASE):
        if len(match) == len(headers_olflex):
            row = clean_match_row(match, headers_olflex, "ÖLFLEX® Power & Control Style")
            results.append(normalize_cable_properties(row, "ÖLFLEX® Power & Control Style"))

    return results

# --- 2. New Transformation Function for Final Format (FIXED FOR CORRUPTED ROWS) ---

def transform_to_core_format(normalized_data):
    """
    Transforms the normalized row data into the final, detailed JSON structure.
    All dimensions are in Millimeters (mm).
    FIXED: Handles corrupted Nominal OD (mm) strings common in ÖLFLEX rows 
    by splitting and taking the first numeric value.
    """
    final_output = []
    
    for row in normalized_data:
        part_number = row.get('Part Number', 'UNKNOWN').replace('*', '')
        style = row.get('_Matched_Style', 'N/A')
        total_conductors = row.get('Total Conductors')
        
        # Skip if total conductors cannot be determined
        if not isinstance(total_conductors, int) or total_conductors < 1:
             continue
        
        # 1. Determine AWG and Diameters
        base_awg_str = str(row.get('AWG/Size', '22')).replace(' ', '').split('/')[0].strip()
        
        # Handle DeviceNet mixed gauge (assuming the get_dim_mm helper is defined if used)
        if style == "DeviceNet Style":
            def get_dim_mm(awg, dim_map):
                return dim_map.get(awg, dim_map['22'])
            
            core_specs = [
                ('22', COLOR_4PAIR[0][0]), ('22', COLOR_4PAIR[0][1]), 
                ('18', COLOR_MULTICONDUCTOR[2]), ('18', COLOR_MULTICONDUCTOR[0]), 
                ('22', 'Drain')
            ][:total_conductors]

            cores_data = []
            for i, (awg_val, color) in enumerate(core_specs):
                 cores_data.append({
                    "CoreId": str(i + 1),
                    "ConductorDiameter": get_dim_mm(awg_val, AWG_TO_DIAMETER_MM),
                    "InsulationThickness": get_dim_mm(awg_val, AWG_TO_INSULATION_MM),
                    "InsulationColor": color,
                    "Gauge": awg_val,
                    "ConductorMaterial": "Copper",
                 })
        
        # Handle other styles (Single gauge)
        else:
            base_awg = base_awg_str if base_awg_str.isdigit() or base_awg_str == 'KCMIL' else '22'
            cond_diam_mm = AWG_TO_DIAMETER_MM.get(base_awg, AWG_TO_DIAMETER_MM['22'])
            insul_thick_mm = AWG_TO_INSULATION_MM.get(base_awg, AWG_TO_INSULATION_MM['22'])
            
            if row.get('Pairing Configuration', '').endswith('pr') and total_conductors % 2 == 0:
                colors = [c for pair in COLOR_4PAIR for c in pair]
            else:
                colors = COLOR_MULTICONDUCTOR
            
            cores_data = []
            for i in range(total_conductors):
                cores_data.append({
                    "CoreId": str(i + 1),
                    "ConductorDiameter": cond_diam_mm,
                    "InsulationThickness": insul_thick_mm,
                    "InsulationColor": colors[i % len(colors)],
                    "Gauge": base_awg,
                    "ConductorMaterial": "Copper",
                })
                
        # 2. Calculate Jacket Properties (Simplified)
        raw_od_mm = row.get('Nominal OD (mm)', '0')
        
        # *** FIX 2: Check for corrupted OD strings and take only the first number ***
        if style == "ÖLFLEX® Power & Control Style":
            # If the OD field contains multiple space-separated numbers, take the first one.
            od_parts = raw_od_mm.split()
            if len(od_parts) > 1:
                raw_od_mm = od_parts[0]
            
        # FIX 1 (Original fix for internal spaces): Remove all internal whitespace 
        cleaned_od_mm = raw_od_mm.replace(' ', '')
        
        try:
            nominal_od_mm = float(cleaned_od_mm)
        except ValueError:
            print(f"WARNING: Failed to convert Nominal OD string '{raw_od_mm}' (Cleaned: '{cleaned_od_mm}') for Part {part_number}. Skipping row.")
            continue

        # Ensure a valid OD for calculation
        if nominal_od_mm <= 0:
            continue
            
        # --- Remaining calculation logic ---
        
        has_shield = ('C' in part_number.upper() or 'CY' in part_number.upper() or 'braid' in row.get('Approvals', '').lower())
        shield_thickness = 0.05 if has_shield else 0
        
        if cores_data:
            max_core_od = max([c['ConductorDiameter'] + 2 * c['InsulationThickness'] for c in cores_data])
            
            if total_conductors <= 4:
                # A simplified packing density model for small core counts
                inner_bundle_diam = max_core_od * 2 
            else:
                # A common approximation for larger core counts (based on square root packing)
                inner_bundle_diam = max_core_od * (1 + math.sqrt(total_conductors) / 2)
        else:
            inner_bundle_diam = 0

        residual_radial_space_mm = (nominal_od_mm - inner_bundle_diam) / 2
        
        min_jacket_thickness = 0.5 
        # Calculate jacket thickness, ensuring it meets a minimum and accounts for the shield.
        jacket_thickness_mm = max(min_jacket_thickness, residual_radial_space_mm - shield_thickness)
        
        # 3. Final Output Object Construction
        description = row.get('Construction') or row.get('Conductor Description') or f"{total_conductors} core cable"
        if row.get('Jacket Material'): description += f", {row['Jacket Material']} jacket"
        
        final_output.append({
            "CableId": f"{style.lower().split('/')[0].replace('®', '')}-{part_number.lower().replace('*','')}",
            "PartNumber": part_number,
            "Manufacturer": "LAPP",
            "Name": f"{style.split(' ')[0]} {row.get('Pairing Configuration', '')} {row.get('AWG/Size', '')} {row.get('Jacket Material', '')}",
            "Type": 4, 
            "Cores": cores_data,
            "JacketThickness": round(jacket_thickness_mm, 3),
            "JacketColor": row.get('Jacket Color', 'Gray') if 'Jacket Color' in row else 'Gray',
            "HasShield": has_shield,
            "ShieldType": 1 if has_shield else 0,
            "ShieldThickness": shield_thickness,
            "ShieldCoverage": 85 if has_shield else 0,
            "HasDrainWire": "drain" in [c['InsulationColor'].lower() for c in cores_data],
            "DrainWireDiameter": AWG_TO_DIAMETER_MM.get('22', 0) if "drain" in [c['InsulationColor'].lower() for c in cores_data] else 0,
            "IsFiller": False,
            "SpecifiedOuterDiameter": nominal_od_mm,
            "Description": description,
        })
        
    return final_output

# --- 3. PDF Text Extraction Function ---

def extract_pages_for_debug(pdf_path, n_debug_pages=100): 
    """
    Opens a PDF and extracts text from all pages with a progress output.
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
    
# --- 4. Main Logic (Multi-Select) ---

def main():
    current_dir = os.getcwd()
    pdf_files = sorted([f for f in os.listdir(current_dir) if f.lower().endswith('.pdf')])
    
    if not pdf_files:
        print("❌ Error: No PDF files found.")
        sys.exit(1)

    print("\n" + "="*50 + "\n     PDF FILES FOUND\n" + "="*50)
    for i, filename in enumerate(pdf_files):
        print(f"[{i + 1}] {filename}")
    print("="*50)
    
    selected_indices = []
    
    while not selected_indices:
        try:
            choice = input(f"Select PDF(s) (e.g., 1 or 1,3,5 or 1-3): ")
            
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
    print(f"🚀 Processing {len(selected_files)} selected files...")
    print("—"*60)
    
    for selected_file in selected_files:
        pdf_path = os.path.join(current_dir, selected_file) 
        base_name = os.path.splitext(selected_file)[0]
        
        # 1. Extract
        full_text, debug_text = extract_pages_for_debug(pdf_path, n_debug_pages=100)
        if not full_text: continue
        
        # 2. Write Debug
        debug_fname = f"{base_name}_DEBUG_raw.txt"
        with open(os.path.join(current_dir, debug_fname), 'w', encoding='utf-8') as f:
            f.write(debug_text)
        print(f"\n💡 DEBUG FILE CREATED for '{selected_file}': '{debug_fname}' (First 100 pages)")

        # 3. Apply Regex and Normalize
        print(f"🔍 Applying regex strategies to '{selected_file}'...")
        file_structured_data = extract_datasheet_info(full_text)
        
        print(f"   -> Found {len(file_structured_data)} raw data rows in this file.")
        all_structured_data.extend(file_structured_data)

    # 4. Final Transformation and Output
    if all_structured_data:
        print("\n" + "="*60)
        print(f"✨ Transforming all {len(all_structured_data)} collected rows into final JSON format...")
        
        final_data = transform_to_core_format(all_structured_data)
        
        out_file = "combined_lapp_cable_data.json"
        
        with open(os.path.join(current_dir, out_file), 'w', encoding='utf-8') as f:
            json.dump(final_data, f, indent=4)
            
        print("\n" + "="*60)
        print(f"✅ FINAL SUCCESS! Transformed {len(final_data)} total data rows.")
        styles = [x.get("_Matched_Style") for x in all_structured_data if not x.get("_Error")]
        print("    Total Style Breakdown:", dict(Counter(styles)))
        print(f"Results saved to: '{out_file}'")
        print("="*60)
    else:
        print("\n❌ NO MATCHES FOUND ACROSS ALL SELECTED FILES. Check the debug text files.")

if __name__ == "__main__":
    main()