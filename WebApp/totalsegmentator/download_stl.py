import requests
import os

# Configuration
SCAN_ID = "f0da0ef5-3c13-4978-b5b2-c2517225cfd5"
BASE_URL = f"https://api.ar4ct.com/scans/{SCAN_ID}"
OUTPUT_DIR = "./downloaded_stl"

ORGANS = ["liver", "heart", "lung_upper_lobe_left", "lung_upper_lobe_right"]

def download_stl_files():
    """Download STL files from the server."""
    print("=" * 60)
    print("📥 Downloading STL files from server")
    print("=" * 60)
    print(f"Scan ID: {SCAN_ID}")
    print(f"Base URL: {BASE_URL}")
    
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    
    for organ in ORGANS:
        url = f"{BASE_URL}/stl/{organ}"
        output_path = os.path.join(OUTPUT_DIR, f"{organ}.stl")
        
        print(f"\n📦 Downloading {organ}...")
        print(f"   URL: {url}")
        
        try:
            response = requests.get(url, stream=True, timeout=120)
            
            if response.status_code == 200:
                with open(output_path, "wb") as f:
                    for chunk in response.iter_content(chunk_size=8192):
                        f.write(chunk)
                
                size_kb = os.path.getsize(output_path) / 1024
                print(f"   ✅ Saved: {output_path} ({size_kb:.1f} KB)")
            else:
                print(f"   ❌ Failed: HTTP {response.status_code}")
                print(f"   Response: {response.text[:200]}")
        except Exception as e:
            print(f"   ❌ Error: {e}")
    
    print("\n" + "=" * 60)
    print("📊 Summary")
    print("=" * 60)
    
    for organ in ORGANS:
        path = os.path.join(OUTPUT_DIR, f"{organ}.stl")
        if os.path.exists(path):
            size_kb = os.path.getsize(path) / 1024
            print(f"   ✅ {organ}.stl: {size_kb:.1f} KB")
        else:
            print(f"   ❌ {organ}.stl: NOT FOUND")

if __name__ == "__main__":
    download_stl_files()
