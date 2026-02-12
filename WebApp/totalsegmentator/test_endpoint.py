import requests
import base64
import time
import json
import os

RUNPOD_API_KEY = "REDACTED_API_KEY"
ENDPOINT_ID = "REDACTED_ENDPOINT_ID"


def run_segmentation(file_url, organs=["liver", "heart"], callback_url=None):
    """Run segmentation using a file URL.
    
    Args:
        file_url: URL to download the CT scan from
        organs: List of organs to segment
        callback_url: Base URL to upload STL files to (e.g., https://api.ar4ct.com/scans/{scan_id})
                      If provided, STL files are uploaded directly to {callback_url}/stl/{organ}
                      If not provided, STL files are returned base64-encoded (may fail if too large)
    """
    print("=" * 60)
    print("🚀 Starting TotalSegmentator RunPod Job")
    print("=" * 60)
    
    print(f"🔗 File URL: {file_url}")
    print(f"🫀 Organs to segment: {organs}")
    if callback_url:
        print(f"📤 Callback URL: {callback_url}")
    else:
        print(f"⚠️  No callback URL - results will be returned inline (may fail if large)")
    
    # Submit job
    print("\n📤 Submitting job to RunPod...")
    print(f"   Endpoint: {ENDPOINT_ID}")
    
    input_data = {
        "file_url": file_url,
        "organs": organs,
        "fast": False
    }
    if callback_url:
        input_data["callback_url"] = callback_url
    
    response = requests.post(
        f"https://api.runpod.ai/v2/{ENDPOINT_ID}/run",
        headers={
            "Authorization": f"Bearer {RUNPOD_API_KEY}",
            "Content-Type": "application/json"
        },
        json={"input": input_data}
    )
    
    return _handle_job_response(response)


def _handle_job_response(response):
    """Handle the job submission response and poll for results."""
    print(f"   HTTP Status: {response.status_code}")
    
    job = response.json()
    print(f"   Response: {json.dumps(job, indent=2)[:500]}")
    
    if "id" not in job:
        print(f"❌ ERROR: No job ID in response!")
        print(f"   Full response: {json.dumps(job, indent=2)}")
        return {"error": "Failed to submit job", "details": job}
    
    job_id = job["id"]
    print(f"✅ Job submitted: {job_id}")
    
    # Poll for results
    print("\n⏳ Polling for results...")
    poll_count = 0
    start_time = time.time()
    
    while True:
        poll_count += 1
        elapsed = time.time() - start_time
        
        status_response = requests.get(
            f"https://api.runpod.ai/v2/{ENDPOINT_ID}/status/{job_id}",
            headers={"Authorization": f"Bearer {RUNPOD_API_KEY}"}
        )
        status = status_response.json()
        current_status = status.get("status", "UNKNOWN")
        
        print(f"   [{poll_count}] Status: {current_status} (elapsed: {elapsed:.1f}s)")
        
        if current_status == "COMPLETED":
            print(f"\n✅ Job completed in {elapsed:.1f}s!")
            output = status.get("output", {})

            print("\n" + "-" * 40)
            print("📋 Full Status Response:")
            print("-" * 40)
            print(json.dumps(status, indent=2)[:2000])

            # Log response details
            print("\n" + "-" * 40)
            print("📋 Response Details:")
            print("-" * 40)
            if "error" in output:
                print(f"   ❌ Error: {output['error']}")
            else:
                print(f"   Status: {output.get('status', 'N/A')}")
                organs_processed = output.get('organs_processed', [])
                print(f"   Organs processed: {len(organs_processed)} - {organs_processed}")
                stl_files = output.get('stl_files', {})
                print(f"   STL files received: {len(stl_files)}")
                for organ, stl_data in stl_files.items():
                    size_kb = len(stl_data) * 3 / 4 / 1024
                    print(f"      - {organ}: ~{size_kb:.1f} KB")
            print("-" * 40)

            return output
        elif current_status == "FAILED":
            print(f"\n❌ Job failed!")
            print(f"   Error: {status.get('error', 'Unknown error')}")
            return {"error": status.get("error", "Job failed")}
        elif current_status == "IN_QUEUE":
            print(f"       → Waiting in queue...")
        elif current_status == "IN_PROGRESS":
            print(f"       → Processing...")
        
        time.sleep(5)

def save_stl_files(result, output_dir="./output"):
    print("\n💾 Saving STL files...")
    os.makedirs(output_dir, exist_ok=True)
    
    stl_files = result.get("stl_files", {})
    if not stl_files:
        print("   ⚠️ No STL files in result")
        return
    
    print(f"   Found {len(stl_files)} STL files")
    
    for organ, stl_b64 in stl_files.items():
        output_path = os.path.join(output_dir, f"{organ}.stl")
        stl_data = base64.b64decode(stl_b64)
        with open(output_path, "wb") as f:
            f.write(stl_data)
        print(f"   ✅ Saved: {output_path} ({len(stl_data) / 1024:.1f} KB)")

# Run test
if __name__ == "__main__":
    # The scan ID to process
    SCAN_ID = "f0da0ef5-3c13-4978-b5b2-c2517225cfd5"
    
    result = run_segmentation(
        # CT file URL
        file_url=f"https://api.ar4ct.com/scans/{SCAN_ID}/ct",
        organs=["liver", "heart", "lung_upper_lobe_left", "lung_upper_lobe_right"],
        # Callback URL - RunPod will upload STL files directly to your server
        callback_url=f"https://api.ar4ct.com/scans/{SCAN_ID}"
    )
    
    print("\n" + "=" * 60)
    print("📊 Result Summary")
    print("=" * 60)
    
    if "error" in result:
        print(f"❌ Error: {result['error']}")
        if "details" in result:
            print(f"   Details: {json.dumps(result['details'], indent=2)}")
    else:
        print(json.dumps(result, indent=2)[:1000])
        if "stl_files" in result:
            save_stl_files(result)
            print("\n✅ All done!")
