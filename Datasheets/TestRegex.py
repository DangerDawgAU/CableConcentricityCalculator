import os
import sys
import re
import json
from pypdf import PdfReader

# --- 1. Regex Extraction Logic (FIXED SKINTOP ISSUE) ---

def extract_datasheet_info(extracted_text):
    """
    Searches the extracted PDF text for the specific datasheet structure
    and returns a list of dictionaries containing the parsed data.
    """
    
    # --- REGEX FIXES EXPLAINED ---
    # 1. Construction: ([\dAWG/pr\s]+) -> Flexible match for "22 AWG/2pr" etc.
    # 2. Stranding: (solid|\d{1,2}\s*wire) -> Matches "solid" OR "7 wire"
    # 3. Jacket: (PVC|PUR|halogen-free|PVC\*|PUR\*) -> Explicitly handles materials + asterisks.
    # 4. Color: ([\w\—\*/]+) -> Handles colors, em-dashes (—), and asterisks.
    # 5. SKINTOP: (\d{3}\s*\d{5}) -> FIXED! Matches "531 12210" (with space) or "53112210".
    
    pattern = r'(\d{7}[\*A-Z]{0,3})\s+([\dAWG/pr\s]+)\s+(solid|\d{1,2}\s*wire)\s+(PVC|PUR|halogen-free|PVC\*|PUR\*)\s+([\w\—\*/]+)\s+(.+?)\s+(yes|no)\s+(yes|no)\s+([0-9\.]+)\s+([0-9\.]+)\s+([0-9\.]+)\s+(\d{3}\s*\d{5})'

    # re.DOTALL is critical here because the "Approvals" field often contains newlines
    matches = re.findall(pattern, extracted_text, re.DOTALL | re.IGNORECASE)
    
    results = []
    
    headers = [
        "Part Number", "Construction", "Stranding", "Jacket Material", 
        "Jacket Color", "Approvals", "Fast Connect", "PoE", 
        "Nominal OD (in)", "Nominal OD (mm)", "Approx. Weight (lbs/mft)", "SKINTOP MS-SC"
    ]

    for match in matches:
        clean_match = []
        for i, item in enumerate(match):
            cleaned = item.strip()
            # Remove asterisks only from specific fields (Construction, Jacket Mat) to clean data
            if i in [1, 3]:  
                cleaned = cleaned.replace('*', '').strip()
            # Remove embedded newlines in Approvals (Group 5 index 5) to make it one clean string
            if i == 5:
                cleaned = " ".join(cleaned.split())
            
            clean_match.append(cleaned)
            
        row_data = dict(zip(headers, clean_match))
        results.append(row_data)

    return results

# --- 2. PDF Text Extraction Function ---

def extract_pages_for_debug(pdf_path, n_debug_pages=10):
    """
    Opens a PDF and extracts text from all pages for full analysis, 
    AND separately extracts text from the first N pages for debug output.
    """
    if not os.path.exists(pdf_path):
        print(f"Error: File not found at '{pdf_path}'.")
        return None, None

    try:
        reader = PdfReader(pdf_path)
        total_pages = len(reader.pages)
        
        print(f"\n📄 Found PDF: '{os.path.basename(pdf_path)}' with {total_pages} total pages.")
        print(f"⏳ Extracting text from ALL {total_pages} page(s) for analysis...")

        full_text = []
        debug_text = []
        
        for i in range(total_pages):
            page = reader.pages[i]
            try:
                page_text = page.extract_text()
            except:
                page_text = ""
            
            # Append to full text
            full_text.append(f"\n\n----- PAGE {i + 1} -----\n\n")
            full_text.append(page_text)
            
            # Append to debug text
            if i < n_debug_pages:
                debug_text.append(f"\n\n----- PAGE {i + 1} OF {min(n_debug_pages, total_pages)} (DEBUG) -----\n\n")
                debug_text.append(page_text)
                 
        return "".join(full_text), "".join(debug_text)

    except Exception as e:
        print(f"An error occurred during PDF processing: {e}")
        return None, None
    
# --- 3. Main Logic ---

def main():
    current_dir = os.getcwd()
    pdf_files = sorted([f for f in os.listdir(current_dir) if f.lower().endswith('.pdf')])
    
    if not pdf_files:
        print("❌ Error: No PDF files found.")
        sys.exit(1)

    print("\n" + "="*50 + "\n      PDF FILES FOUND\n" + "="*50)
    for i, filename in enumerate(pdf_files):
        print(f"[{i + 1}] {filename}")
    print("="*50)
    
    while True:
        try:
            choice = input(f"Select PDF (1-{len(pdf_files)}): ")
            selection_index = int(choice) - 1
            if 0 <= selection_index < len(pdf_files):
                selected_file = pdf_files[selection_index]
                break
        except ValueError: pass

    # Paths
    pdf_path = os.path.join(current_dir, selected_file) 
    base_name = os.path.splitext(selected_file)[0]
    
    # 1. Extract
    full_text, debug_text = extract_pages_for_debug(pdf_path)
    if not full_text: return

    # 2. Write Debug
    with open(os.path.join(current_dir, f"{base_name}_DEBUG_raw.txt"), 'w', encoding='utf-8') as f:
        f.write(debug_text)
    print(f"\n💡 DEBUG FILE CREATED: '{base_name}_DEBUG_raw.txt'")

    # 3. Apply Regex
    print("🔍 Applying fixed regex pattern...")
    structured_data = extract_datasheet_info(full_text)

    # 4. Output
    if structured_data:
        out_file = f"{base_name}_full_data.json"
        with open(os.path.join(current_dir, out_file), 'w', encoding='utf-8') as f:
            json.dump(structured_data, f, indent=4)
        print("\n" + "="*60)
        print(f"✅ SUCCESS! Found {len(structured_data)} data rows.")
        print(f"Results saved to: '{out_file}'")
        print("="*60)
    else:
        print("\n❌ STILL NO MATCHES. Check the debug text file.")

if __name__ == "__main__":
    main()