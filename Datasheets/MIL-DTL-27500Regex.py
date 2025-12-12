import os
import sys
import re
import json
from pypdf import PdfReader
from collections import Counter
import math

# --- 0. Constants and Helper Dictionaries ---

# As seen on Page 2 (with minor cleanup)
M27500_COMPONENT_CODES = {
    'SA': 'M22759/7', 'TA': 'M22759/8', 'RC': 'M22759/11', 'RE': 'M22759/12',
    'TE': 'M22759/16', 'TF': 'M22759/17', 'TG': 'M22759/18', 'TH': 'M22759/19',
    'VA': 'M22759/5', 'WA': 'M22759/6', 'LE': 'M22759/9', 'LH': 'M22759/10',
    'TK': 'M22759/20', 'TL': 'M22759/21', 'TM': 'M22759/22', 'TN': 'M22759/23',
    'JB': 'M22759/28', 'JC': 'M22759/29', 'JD': 'M22759/30', 'JE': 'M22759/31',
    'WB': 'M22759/80', 'WC': 'M22759/81', 'WE': 'M22759/82', 'WG': 'M22759/84',
    'WH': 'M22759/85', 'WJ': 'M22759/86', 'WK': 'M22759/87', 'WL': 'M22759/88',
    'WM': 'M22759/89', 'WN': 'M22759/90', 'WP': 'M22759/91', 'WR': 'M22759/92',
    'JA': 'M25038/1', 'JF': 'M25038/3', 
    'MR': 'M81381/7', 'MS': 'M81381/8', 'MT': 'M81381/9', 'MV': 'M81381/10',
    'MW': 'M81381/11', 'MY': 'M81381/12', 'NA': 'M81381/13', 'NB': 'M81381/14',
    'NE': 'M81381/17', 'NF': 'M81381/18', 'NG': 'M81381/19', 'NH': 'M81381/20',
    'NK': 'M81381/21', 'NL': 'M81381/22'
}

# As seen on Page 2
M27500_SHIELD_CODES = {
    'U': 'None', 'NY': 'Nickel-plated copper (Round)', 'SW': 'Silver-plated copper (Round)',
    'TV': 'Tin-plated copper (Round)', 'CR': 'Heavy Nickel-plated copper (Round)',
    'FZ': 'Stainless steel (Round)', 'PL': 'Ni-plated high-strength Cu alloy (Round)',
    'MK': 'Ag-plated high-strength Cu alloy (Round)', '#': 'Ni-plated copper (Flat)',
    'GA': 'Ag-plated copper (Flat)', 'JD': 'Tin-plated copper (Flat)',
    'EX': 'Ni-plated high-strength Cu alloy (Flat)', 'HB': 'Ag-plated high-strength Cu alloy (Flat)'
}

# As seen on Page 2 (Jacket Material Codes, Single/Double)
M27500_JACKET_CODES = {
    '00': 'None', '50': 'None',
    '15': 'ETFE, extruded, clear', '65': 'ETFE, extruded, clear (Double)',
    '14': 'ETFE, extruded, white', '64': 'ETFE, extruded, white (Double)',
    '05': 'FEP, extruded, clear', '55': 'FEP, extruded, clear (Double)',
    '09': 'FEP, extruded, white', '59': 'FEP, extruded, white (Double)',
    '02': 'Nylon, extruded, clear', '52': 'Nylon, extruded, clear (Double)',
    '21': 'PFA, extruded, clear', '71': 'PFA, extruded, clear (Double)',
    '20': 'PFA, extruded, white', '70': 'PFA, extruded, white (Double)',
    '11': 'Natural polyimide / clear FEP tape', '61': 'Natural polyimide / clear FEP tape (Double)',
    '12': 'Natural polyimide / FEP tape', '62': 'Natural polyimide / FEP tape (Double)',
    '06': 'PTFE, taped, white', '56': 'PTFE, taped, white (Double)',
    '24': 'PTFE/Polyimide tape (Outer PTFE tape)', '74': 'PTFE/Polyimide tape (Double)',
    '07': 'PTFE-coated glass braid', '57': 'PTFE-coated glass braid (Double)',
    '01': 'PVC, extruded, white', '51': 'PVC, extruded, white (Double)',
}

# NEW CONSTANT: Default shield and jacket codes based on M27500 Type Code.
# This replaces the unreliable regex matching for part number examples.
# Shield Code (SS) is assumed to be single-char 'N' (Nickel) or 'S' (Silver) or 'U' (Unshielded).
M27500_TYPE_CODE_DEFAULTS = {
    # SA, TA, RC, RE, VA, WA, LE, LH (Normal/Twisted Pair/Multicore) - Use PTFE Jacket 06, Nickel Shield N
    'SA': ('N', '06'), 'TA': ('N', '06'), 'RC': ('N', '06'), 'RE': ('N', '06'),
    'VA': ('N', '06'), 'WA': ('N', '06'), 'LE': ('N', '06'), 'LH': ('N', '06'),
    
    # TE, TF, TG, TH, TK, TL, TM, TN (Triaxials/High Density) - Often ETFE Jacket 14, Nickel Shield N
    'TE': ('N', '14'), 'TF': ('N', '14'), 'TG': ('N', '14'), 'TH': ('N', '14'),
    'TK': ('N', '14'), 'TL': ('N', '14'), 'TM': ('N', '14'), 'TN': ('N', '14'),
    
    # WB, WC, WE (Bus/Power) - Often PTFE/Polyimide Jacket 24, Nickel Shield N
    'WB': ('N', '24'), 'WC': ('N', '24'), 'WE': ('N', '24'),
}

def get_default_codes_by_type(type_code):
    """
    Looks up the default Shield Material Code (SS) and Jacket Code (JJ) 
    based on the M27500 Type Code (TT).
    """
    return M27500_TYPE_CODE_DEFAULTS.get(
        type_code, 
        ('U', '00') # Fallback to unshielded, no jacket if type is unknown
    )

# --- 1. Regex Extraction and Normalization Logic ---

def parse_unit_pair(data_str):
    """
    Parses a string like 'X.XX (Y.YY)' into a dict of US and Metric units.
    Ensures 'us' and 'metric' keys are present.
    """
    data_str = data_str.strip()
    
    # Pattern to match the standard 'US (Metric)' format
    match = re.match(r'([0-9\.]+)\s*\(([\d\.]+)\)', data_str)
    
    if match:
        try:
            us_val = float(match.group(1))
            metric_val = float(match.group(2))
            return {"us": us_val, "metric": metric_val}
        except ValueError:
            return {"us": 0.0, "metric": 0.0}
    
    # Fallback for single values (treat as US unit)
    try:
        us_val = float(data_str)
        return {"us": us_val, "metric": us_val / 25.4} # Simple fallback conversion for single value
    except ValueError:
        return {"us": 0.0, "metric": 0.0}

def clean_match_row(match_tuple, headers):
    """Helper to clean whitespace and create a dict."""
    clean_items = []
    for item in match_tuple:
        # Check if item is None (e.g., from an optional non-match group in the regex)
        if item is None:
            clean_text = ""
        else:
            clean_text = item.strip()
            clean_text = clean_text.replace('\n', ' ')
            clean_text = re.sub(r'\s+', ' ', clean_text)
        clean_items.append(clean_text)
    
    return dict(zip(headers, clean_items))

def normalize_m27500_properties(row_data, conductor_count, type_code):
    """
    Parses all 'Value (Unit)' strings into structured dictionaries 
    and reconstructs the full part number. (FIXED)
    """
    normalized = {
        "Part Number Base": row_data.get("Part Number Base"),
        "AWG Size": row_data.get("AWG Size"),
        "Stranding": row_data.get("Stranding"),
        "Conductor Count": conductor_count,
        "M27500 Type Code": type_code,
    }
    
    # Unit parsing for numerical fields
    fields_to_parse = [
        "Shield Diameter", "Jacket Diameter", "Weight"
    ]
    
    default_unit_pair = {"us": 0.0, "metric": 0.0}
    
    for field in fields_to_parse:
        if field in row_data:
            normalized[field] = parse_unit_pair(row_data[field])
        else:
            normalized[field] = default_unit_pair

    # --- 2. Full Part Number Reconstruction (FIXED) ---
    
    # Determine the mandatory codes based on the Type Code using the new lookup
    inferred_shield_code, inferred_jacket_code = get_default_codes_by_type(type_code)

    # All M27500 cables in the table are assumed to have 'C' (90%) shield coverage
    shield_coverage_code = "C" 
    
    # AWG code map based on common MIL-spec (06, 08, 10, etc.)
    awg_raw = str(normalized["AWG Size"]).strip()
    
    if awg_raw.isdigit():
        awg_code = f"{int(awg_raw):02}" # Pad single digits (e.g., 8 -> 08)
    elif '/' in awg_raw:
        # Handle 2/0, 1/0. They are typically 20, 10
        awg_code = awg_raw.replace('/', '') 
    else:
        awg_code = 'XX' # Fallback for non-standard AWG

    # M27500-AA TT C SS JJ
    normalized["Full Part Number Base"] = (
        f"M27500-{awg_code}{type_code}{conductor_count}"
        f"{shield_coverage_code}{inferred_shield_code}{inferred_jacket_code}"
    )
    
    # Add inferred codes to normalized data for use in the transformer
    normalized["_Shield_Material_Code"] = inferred_shield_code
    normalized["_Jacket_Material_Code"] = inferred_jacket_code
    
    return normalized

def extract_datasheet_info_27500(extracted_text):
    """
    Searches the text for ALL M27500 table structures. (FIXED REGEX LOGIC)
    
    - Removed the unreliable EX_HEADER pattern.
    - Simplified the context update logic to rely on the CONTEXT and TABLE_HEADER 
      to provide the type code and conductor count.
    """
    results = []
    
    # Header: Component Wire AWG (Stranding) Diameter Diameter Weight
    headers = ["AWG Size", "Stranding", "Shield Diameter", "Jacket Diameter", "Weight"]
    
    # Pattern 1: Context (e.g., SA and TA)
    CONTEXT = r'M27500 Cables—types\s+(?P<TYPE_CODES>[A-Z]{2}(?:\s+and|,)\s*[A-Z]{2}(?:\s+and|,)\s*[A-Z]{2}|[A-Z]{2}(?:\s+and|,)\s*[A-Z]{2}|[A-Z]{2})\s+\([A-Z0-9/\s,]+\)' 
    
    # Pattern 2: Table Header (e.g., Dimensions and Weight—4 Conductor Cables)
    TABLE_HEADER = r'(?P<TABLE_HEADER>Dimensions and Weight—(?P<CONDUCTOR_COUNT>\d)\s+Conductor Cables\*?)'
    
    # Pattern 3: Data Row - FIX: Use a robust pattern for the unit pairs and handle single values.
    UNIT_PAIR_REGEX = r'[0-9\.]+\s*(?:\([\d\.]+\))?' # Matches 'X.XX' or 'X.XX (Y.YY)'
    DATA_ROW = (
        r'(?P<DATA_ROW>'
        r'([0-9/]{1,4})\s+'                  # Group 1: AWG Size
        r'([0-9/]+)\s+'                      # Group 2: Stranding
        r'(' + UNIT_PAIR_REGEX + r')\s+'     # Group 3: Shield Diameter
        r'(' + UNIT_PAIR_REGEX + r')\s+'     # Group 4: Jacket Diameter
        r'(' + UNIT_PAIR_REGEX + r')'        # Group 5: Weight
        r')'
    )

    # Removed EX_HEADER from master_pattern
    master_pattern = re.compile(
        f'{CONTEXT}|{TABLE_HEADER}|{DATA_ROW}',
        re.DOTALL | re.IGNORECASE
    )

    current_context = {
        "conductor_count": 0,
        "type_codes": [],
    }
    
    # Find all matches across the entire text
    matches = master_pattern.finditer(extracted_text)
    
    for m in matches:
        if m.group('TYPE_CODES'):
            # Context match (e.g., SA and TA)
            codes_str = m.group('TYPE_CODES').replace('and', ',')
            current_context['type_codes'] = [c.strip() for c in codes_str.split(',') if c.strip()]
            
        elif m.group('TABLE_HEADER'):
            # Table Header match (e.g., 4 Conductor Cables)
            current_context['conductor_count'] = int(m.group('CONDUCTOR_COUNT'))
            
        elif m.group('DATA_ROW'):
            # Data Row match
            if current_context['conductor_count'] > 0 and current_context['type_codes']:
                
                # The data groups are the 5 groups that capture the data row fields.
                # Since the named groups are at the start, the 5 data fields are the last 5 groups captured.
                data_row_tuple = m.groups()[-5:]
                row_data = clean_match_row(data_row_tuple, headers)
                
                # Create a row for each active type code (e.g., both SA and TA)
                for type_code in current_context['type_codes']:
                    
                    # The normalization function now handles the Shield/Jacket code inference
                    normalized_row = normalize_m27500_properties(
                        row_data, 
                        current_context['conductor_count'],
                        type_code
                    )
                    
                    results.append(normalized_row)

    return results

# --- 3. Transformation Function for Final Format ---

def transform_to_core_format_27500(normalized_data):
    """
    Transforms the normalized M27500 data into the final, detailed JSON structure 
    for multi-core cables.
    """
    final_output = []
    
    for row in normalized_data:
        full_pn = row["Full Part Number Base"]
        part_type = row["M27500 Type Code"]
        awg_size = row["AWG Size"]
        cond_count = row["Conductor Count"]
        
        # 1. Component Wire (Cores) Data
        comp_mil_spec = M27500_COMPONENT_CODES.get(part_type, "UNKNOWN")
        
        # The inferred codes were saved in the normalized row
        inferred_shield_code = row["_Shield_Material_Code"]
        inferred_jacket_code = row["_Jacket_Material_Code"]
        
        cores_data = [{
            "CoreId": str(i + 1),
            "Gauge": str(awg_size),
            "ConductorMaterial": f"Plated Copper (from {comp_mil_spec})",
            "InsulationThickness": 0.1, # Placeholder (mm)
            "ConductorDiameter": 0.5, # Placeholder (mm)
            "InsulationColor": f"Code {i+1} (M27500)",
        } for i in range(cond_count)]
        
        # 2. Outer properties
        jacket_desc = M27500_JACKET_CODES.get(inferred_jacket_code, f"Code {inferred_jacket_code}")
        shield_desc = M27500_SHIELD_CODES.get(inferred_shield_code, f"Code {inferred_shield_code}")
        
        is_shielded = inferred_shield_code not in ["U", "u"]
        
        # The table OD is the Jacket Diameter (in mm)
        specified_od = row["Jacket Diameter"]["metric"]
        
        # Calculate Jacket Thickness (Estimation): (Jacket OD - Shield OD) 
        shield_od = row["Shield Diameter"]["metric"]
        jacket_thickness_est = (specified_od - shield_od) / 2.0
        
        final_output.append({
            "CableId": f"m27500-{full_pn.replace('/','-').lower()}",
            "PartNumber": full_pn,
            "Manufacturer": "Thermax (Implied)", 
            "Name": f"MIL-DTL-27500 {cond_count}C {awg_size}AWG Cable Type {part_type}",
            "Type": 2, # Multi-Core Cable
            "Cores": cores_data,
            "JacketThickness": round(jacket_thickness_est, 3) if jacket_thickness_est > 0 else 0.0,
            "JacketColor": "White/Clear (Code dependent)",
            "JacketMaterial": jacket_desc,
            "HasShield": is_shielded,
            "ShieldType": 1 if is_shielded else 0, # Braid
            "ShieldMaterial": shield_desc,
            "ShieldThickness": 0.0, # Not provided in table
            "ShieldCoverage": 90, # Assumed 90% (Code 'C')
            "HasDrainWire": False,
            "DrainWireDiameter": 0.0,
            "IsFiller": False,
            "SpecifiedOuterDiameter": round(specified_od, 3),
            "Description": f"MIL-DTL-27500 Cable: {cond_count} conductors of {comp_mil_spec} wire, Stranding {row['Stranding']}.",
            "Weight (kg/km)": round(row["Weight"]["metric"], 3)
        })
        
    return final_output

# --- 4. Main/Utility Functions (Copied from previous file for self-contained execution) ---
    
def extract_pages_for_debug(pdf_path, n_debug_pages=100):
    # (Same as before)
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

    print("\n" + "="*50 + "\n     PDF FILES FOUND\n" + "="*50)
    for i, filename in enumerate(pdf_files):
        print(f"[{i + 1}] {filename}")
    print("="*50)
    
    selected_indices = []
    
    while not selected_indices:
        try:
            choice = input(f"Select PDF(s) containing MIL-DTL-27500 data (e.g., 1 or 1,3,5 or 1-3): ")
            
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
    print(f"🚀 Processing {len(selected_files)} selected files for MIL-DTL-27500 data...")
    print("—"*60)
    
    for selected_file in selected_files:
        pdf_path = os.path.join(current_dir, selected_file) 
        base_name = os.path.splitext(selected_file)[0]
        
        # 1. Extract
        full_text, debug_text = extract_pages_for_debug(pdf_path, n_debug_pages=100)
        if not full_text: continue
        
        # 2. Write Debug
        debug_fname = f"{base_name}_M27500_DEBUG_raw.txt"
        with open(os.path.join(current_dir, debug_fname), 'w', encoding='utf-8') as f:
            f.write(debug_text)
        print(f"\n💡 DEBUG FILE CREATED for '{selected_file}': '{debug_fname}' (First 100 pages)")

        # 3. Apply Regex and Normalize
        print(f"🔍 Applying MIL-DTL-27500 specific regex strategies to '{selected_file}'...")
        file_structured_data = extract_datasheet_info_27500(full_text)
        
        print(f"   -> Found {len(file_structured_data)} raw data rows in this file.")
        all_structured_data.extend(file_structured_data)

    # 4. Final Transformation and Output
    if all_structured_data:
        print("\n" + "="*60)
        print(f"✨ Transforming all {len(all_structured_data)} collected rows into final JSON format...")
        
        final_data = transform_to_core_format_27500(all_structured_data)
        
        out_file = "mil_dtl_27500_cable_data.json"
        
        with open(os.path.join(current_dir, out_file), 'w', encoding='utf-8') as f:
            json.dump(final_data, f, indent=4)
            
        print("\n" + "="*60)
        print(f"✅ FINAL SUCCESS! Transformed {len(final_data)} total data rows.")
        styles = [f"{x.get('M27500 Type Code')} ({x.get('Conductor Count')}C)" for x in all_structured_data]
        print("    Total Style Breakdown:", dict(Counter(styles)))
        print(f"Results saved to: '{out_file}'")
        print("="*60)
    else:
        print("\n❌ NO MIL-DTL-27500 DATA ROWS FOUND ACROSS ALL SELECTED FILES. Check the debug text files.")

if __name__ == "__main__":
    main()